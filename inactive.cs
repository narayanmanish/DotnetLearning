using System;
using System.DirectoryServices;

public class ActiveDirectoryService
{
    private readonly string _ldapPath;
    private readonly string _username;
    private readonly string _password;

    public ActiveDirectoryService(string ldapPath, string username, string password)
    {
        _ldapPath = ldapPath;
        _username = username;
        _password = password;
    }

    public int GetInactiveUsersCount(int days)
    {
        DateTime cutoffDate = DateTime.UtcNow.AddDays(-days);
        int inactiveUsersCount = 0;

        using (DirectoryEntry rootEntry = new DirectoryEntry(_ldapPath, _username, _password))
        {
            SearchInactiveUsers(rootEntry, cutoffDate, ref inactiveUsersCount);
        }

        return inactiveUsersCount;
    }

    private void SearchInactiveUsers(DirectoryEntry entry, DateTime cutoffDate, ref int inactiveUsersCount)
    {
        foreach (DirectoryEntry child in entry.Children)
        {
            try
            {
                if (child.SchemaClassName == "user")
                {
                    if (!IsSystemAccount(child))
                    {
                        DateTime? lastLogonTimestamp = GetLastLogonTimestamp(child);
                        DateTime? lastLogon = GetLastLogon(child);
                        DateTime? pwdLastSet = GetPwdLastSet(child);
                        DateTime? whenChanged = GetWhenChanged(child);

                        bool isInactive = true;

                        // Check lastLogonTimestamp
                        if (lastLogonTimestamp.HasValue && lastLogonTimestamp.Value >= cutoffDate)
                        {
                            isInactive = false;
                        }

                        // Check lastLogon
                        if (lastLogon.HasValue && lastLogon.Value >= cutoffDate)
                        {
                            isInactive = false;
                        }

                        // Check pwdLastSet
                        if (pwdLastSet.HasValue && pwdLastSet.Value >= cutoffDate)
                        {
                            isInactive = false;
                        }

                        // Check whenChanged
                        if (whenChanged.HasValue && whenChanged.Value >= cutoffDate)
                        {
                            isInactive = false;
                        }

                        if (isInactive)
                        {
                            inactiveUsersCount++;
                            Console.WriteLine($"Inactive user found: {child.Properties["sAMAccountName"].Value}");
                        }
                    }
                }
                else if (child.SchemaClassName == "organizationalUnit" || child.SchemaClassName == "container")
                {
                    // Recursive call for OUs and containers
                    SearchInactiveUsers(child, cutoffDate, ref inactiveUsersCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing entry: {child.Path}. Exception: {ex.Message}");
            }
        }
    }

    private bool IsSystemAccount(DirectoryEntry user)
    {
        if (user.Properties["sAMAccountName"].Value != null)
        {
            string samAccountName = user.Properties["sAMAccountName"].Value.ToString();
            // Adjust filter to exclude system or service accounts as needed
            return samAccountName.StartsWith("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                   samAccountName.StartsWith("Admin", StringComparison.OrdinalIgnoreCase); // Example: adjust based on naming conventions
        }
        return false;
    }

    private DateTime? GetLastLogonTimestamp(DirectoryEntry user)
    {
        if (user.Properties["lastLogonTimestamp"].Value != null)
        {
            return DateTime.FromFileTime((long)user.Properties["lastLogonTimestamp"].Value);
        }
        return null;
    }

    private DateTime? GetLastLogon(DirectoryEntry user)
    {
        if (user.Properties["lastLogon"].Value != null)
        {
            return DateTime.FromFileTime((long)user.Properties["lastLogon"].Value);
        }
        return null;
    }

    private DateTime? GetPwdLastSet(DirectoryEntry user)
    {
        if (user.Properties["pwdLastSet"].Value != null)
        {
            long pwdLastSetFileTime = (long)user.Properties["pwdLastSet"].Value;
            return DateTime.FromFileTime(pwdLastSetFileTime);
        }
        return null;
    }

    private DateTime? GetWhenChanged(DirectoryEntry user)
    {
        if (user.Properties["whenChanged"].Value != null)
        {
            return DateTime.Parse(user.Properties["whenChanged"].Value.ToString());
        }
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "LDAP://DC=yourdomain,DC=com"; // Adjust this to your AD path
        string username = "yourusername";
        string password = "yourpassword";

        var adService = new ActiveDirectoryService(ldapPath, username, password);
        int days = 30; // Example: users inactive for the last 30 days
        int inactiveUsersCount = adService.GetInactiveUsersCount(days);

        Console.WriteLine($"Total inactive users found: {inactiveUsersCount}");
    }
}




using System;
using System.DirectoryServices;

public class ActiveDirectoryService
{
    private readonly string _ldapPath;
    private readonly string _username;
    private readonly string _password;

    public ActiveDirectoryService(string ldapPath, string username, string password)
    {
        _ldapPath = ldapPath;
        _username = username;
        _password = password;
    }

    public int GetInactiveUsersCount(int days)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-days);
        int inactiveUsersCount = 0;

        using (DirectoryEntry rootEntry = new DirectoryEntry(_ldapPath, _username, _password))
        {
            SearchInactiveUsers(rootEntry, cutoffDate, ref inactiveUsersCount);
        }

        return inactiveUsersCount;
    }

    private void SearchInactiveUsers(DirectoryEntry entry, DateTime cutoffDate, ref int inactiveUsersCount)
    {
        foreach (DirectoryEntry child in entry.Children)
        {
            try
            {
                if (child.SchemaClassName == "user")
                {
                    bool isInactive = false;

                    DateTime? lastLogon = GetLastLogon(child);
                    DateTime? whenChanged = GetWhenChanged(child);

                    if ((lastLogon.HasValue && lastLogon.Value < cutoffDate) ||
                        (whenChanged.HasValue && whenChanged.Value < cutoffDate))
                    {
                        isInactive = true;
                    }

                    if (isInactive)
                    {
                        inactiveUsersCount++;
                    }
                }
                else if (child.SchemaClassName == "organizationalUnit" || child.SchemaClassName == "container")
                {
                    // Recursive call for OUs and containers
                    SearchInactiveUsers(child, cutoffDate, ref inactiveUsersCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing entry: {child.Path}. Exception: {ex.Message}");
            }
        }
    }

    private DateTime? GetLastLogon(DirectoryEntry user)
    {
        if (user.Properties["lastLogonTimestamp"].Value != null)
        {
            return DateTime.FromFileTime((long)user.Properties["lastLogonTimestamp"].Value);
        }
        if (user.Properties["lastLogon"].Value != null)
        {
            return DateTime.FromFileTime((long)user.Properties["lastLogon"].Value);
        }
        return null;
    }

    private DateTime? GetWhenChanged(DirectoryEntry user)
    {
        if (user.Properties["whenChanged"].Value != null)
        {
            return DateTime.Parse(user.Properties["whenChanged"].Value.ToString());
        }
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "LDAP://DC=yourdomain,DC=com"; // Adjust this to your AD path
        string username = "yourusername";
        string password = "yourpassword";

        var adService = new ActiveDirectoryService(ldapPath, username, password);
        int days = 30; // Example: users inactive for the last 30 days
        int inactiveUsersCount = adService.GetInactiveUsersCount(days);

        Console.WriteLine($"Total inactive users found: {inactiveUsersCount}");
    }
}



using System;
using System.Collections.Generic;
using System.DirectoryServices;

public class ActiveDirectoryService
{
    private readonly string _ldapPath;
    private readonly string _username;
    private readonly string _password;

    public ActiveDirectoryService(string ldapPath, string username, string password)
    {
        _ldapPath = ldapPath;
        _username = username;
        _password = password;
    }

    public int GetInactiveUsersCount(int days)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-days);
        int inactiveUsersCount = 0;

        using (DirectoryEntry rootEntry = new DirectoryEntry(_ldapPath, _username, _password))
        {
            SearchInactiveUsers(rootEntry, cutoffDate, ref inactiveUsersCount);
        }

        return inactiveUsersCount;
    }

    private void SearchInactiveUsers(DirectoryEntry entry, DateTime cutoffDate, ref int inactiveUsersCount)
    {
        foreach (DirectoryEntry child in entry.Children)
        {
            try
            {
                if (child.SchemaClassName == "user")
                {
                    bool isInactive = false;

                    if (child.Properties["lastLogonTimestamp"].Value != null)
                    {
                        DateTime lastLogon = DateTime.FromFileTime((long)child.Properties["lastLogonTimestamp"].Value);
                        if (lastLogon < cutoffDate)
                        {
                            isInactive = true;
                        }
                    }

                    if (child.Properties["whenChanged"].Value != null)
                    {
                        DateTime whenChanged = DateTime.Parse(child.Properties["whenChanged"].Value.ToString());
                        if (whenChanged < cutoffDate)
                        {
                            isInactive = true;
                        }
                    }

                    if (isInactive)
                    {
                        inactiveUsersCount++;
                    }
                }
                else if (child.SchemaClassName == "organizationalUnit" || child.SchemaClassName == "container")
                {
                    // Recursive call for OUs and containers
                    SearchInactiveUsers(child, cutoffDate, ref inactiveUsersCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing entry: {child.Path}. Exception: {ex.Message}");
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "LDAP://DC=yourdomain,DC=com"; // Adjust this to your AD path
        string username = "yourusername";
        string password = "yourpassword";

        var adService = new ActiveDirectoryService(ldapPath, username, password);
        int days = 30; // Example: users inactive for the last 30 days
        int inactiveUsersCount = adService.GetInactiveUsersCount(days);

        Console.WriteLine($"Total inactive users found: {inactiveUsersCount}");
    }
}
