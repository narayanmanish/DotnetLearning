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
