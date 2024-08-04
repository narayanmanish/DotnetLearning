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
