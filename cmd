using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        string username = "yourusername";
        string password = "yourpassword";
        string domain = "yourdomain";
        int daysInactive = 30; // Example: users inactive for the last 30 days

        string command = $@"
            $SecurePassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
            $Credential = New-Object PSCredential('{username}', $SecurePassword)
            $CutoffDate = (Get-Date).AddDays(-{daysInactive})
            Get-ADUser -Filter * -Properties LastLogonTimestamp, LastLogon, pwdLastSet, whenChanged -Credential $Credential | Where-Object {
                ($_.LastLogonTimestamp -eq $null -or [DateTime]::FromFileTime($_.LastLogonTimestamp) -lt $CutoffDate) -and
                ($_.LastLogon -eq $null -or [DateTime]::FromFileTime($_.LastLogon) -lt $CutoffDate) -and
                ($_.pwdLastSet -eq $null -or [DateTime]::FromFileTime($_.pwdLastSet) -lt $CutoffDate) -and
                ($_.whenChanged -eq $null -or [DateTime]::Parse($_.whenChanged) -lt $CutoffDate)
            } | Select-Object Name | Measure-Object | Select-Object -ExpandProperty Count
        ";

        string powershellCommand = $"powershell.exe -Command \"{command}\"";

        ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {powershellCommand}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processInfo))
        using (System.IO.StreamReader reader = process.StandardOutput)
        {
            string result = reader.ReadToEnd();
            Console.WriteLine($"Total inactive users found: {result}");
        }
    }
}





# Specify the LDAP path
$ldapPath = "LDAP://DC=yourdomain,DC=com" # Adjust this to your AD path

# Specify the credentials
$username = "yourusername"
$password = "yourpassword"

# Specify the number of inactive days
$days = 30

# Calculate the cutoff date
$cutoffDate = (Get-Date).AddDays(-$days)

# Function to convert FileTime to DateTime
function Convert-FileTimeToDateTime {
    param (
        [Parameter(Mandatory = $true)]
        [long]$FileTime
    )

    return [datetime]::FromFileTime($FileTime)
}

# Function to search for inactive users
function Search-InactiveUsers {
    param (
        [Parameter(Mandatory = $true)]
        [ADSI]$Entry,
        [Parameter(Mandatory = $true)]
        [datetime]$CutoffDate,
        [ref]$InactiveUsersCount
    )

    foreach ($child in $Entry.psbase.Children) {
        try {
            if ($child.SchemaClassName -eq "user") {
                $lastLogonTimestamp = $null
                if ($child.Properties["lastLogonTimestamp"].Count -gt 0) {
                    $lastLogonTimestamp = Convert-FileTimeToDateTime -FileTime $child.Properties["lastLogonTimestamp"][0]
                }

                $whenChanged = $null
                if ($child.Properties["whenChanged"].Count -gt 0) {
                    $whenChanged = [datetime]::Parse($child.Properties["whenChanged"][0])
                }

                if (($lastLogonTimestamp -ne $null -and $lastLogonTimestamp -lt $CutoffDate) -or
                    ($whenChanged -ne $null -and $whenChanged -lt $CutoffDate)) {
                    $InactiveUsersCount.Value++
                }
            } elseif ($child.SchemaClassName -eq "organizationalUnit" -or $child.SchemaClassName -eq "container") {
                # Recursive call for OUs and containers
                Search-InactiveUsers -Entry ([ADSI]$child.psbase.Path) -CutoffDate $CutoffDate -InactiveUsersCount ([ref]$InactiveUsersCount.Value)
            }
        } catch {
            Write-Host "Error accessing entry: $($child.psbase.Path). Exception: $($_.Exception.Message)"
        }
    }
}

# Create a DirectoryEntry object with credentials
$rootEntry = New-Object DirectoryServices.DirectoryEntry($ldapPath, $username, $password)

# Initialize inactive users count
$inactiveUsersCount = 0

# Search for inactive users
Search-InactiveUsers -Entry $rootEntry -CutoffDate $cutoffDate -InactiveUsersCount ([ref]$inactiveUsersCount)

# Output the count of inactive users
Write-Output "Total inactive users found: $inactiveUsersCount"
