# Define the remote domain controller and credentials
$remoteDC = "remote.domain.controller"  # Replace with the remote domain controller's hostname
$remoteDomain = "remote.domain"         # Replace with the remote domain
$remoteUsername = "username"            # Replace with the username
$remotePassword = "password"            # Replace with the password

# Create a PSCredential object
$secPassword = ConvertTo-SecureString $remotePassword -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential ($remoteDomain + "\" + $remoteUsername, $secPassword)

# Create a new PowerShell session on the remote domain controller
$session = New-PSSession -ComputerName $remoteDC -Credential $credential

# Define the script block to run on the remote domain controller
$scriptBlock = {
    # Import the Active Directory module
    Import-Module ActiveDirectory

    # Define the number of inactive days
    $daysInactive = 30

    # Calculate the date N days ago from today
    $inactiveDate = (Get-Date).AddDays(-$daysInactive)

    # Get all users who have not logged on since the inactive date and count them
    $inactiveUserCount = Get-ADUser -Filter {lastLogonTimestamp -lt $inactiveDate} -Properties lastLogonTimestamp | 
        Measure-Object | 
        Select-Object -ExpandProperty Count

    # Return the count of inactive users
    return $inactiveUserCount
}

# Run the script block on the remote session and retrieve the result
$inactiveUserCount = Invoke-Command -Session $session -ScriptBlock $scriptBlock

# Output the count of inactive users
Write-Output "Number of inactive users: $inactiveUserCount"

# Close the remote session
Remove-PSSession -Session $session
