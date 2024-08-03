using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Permissions;

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

    public List<string> GetInactiveUsers(int days)
    {
        DateTime cutoffDate = DateTime.UtcNow.AddDays(-days);
        List<string> inactiveUsers = new List<string>();

        using (var connection = new LdapConnection(_ldapPath))
        {
            connection.Credential = new NetworkCredential(_username, _password);
            connection.AuthType = AuthType.Basic;

            string searchFilter = "(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp<=*))";
            var request = new SearchRequest("DC=yourdomain,DC=com", searchFilter, SearchScope.Subtree, "sAMAccountName", "lastLogonTimestamp");

            var response = (SearchResponse)connection.SendRequest(request);

            foreach (SearchResultEntry entry in response.Entries)
            {
                DateTime? lastLogon = GetLastLogonTimestamp(entry);

                if (lastLogon == null || lastLogon < cutoffDate)
                {
                    inactiveUsers.Add(entry.Attributes["sAMAccountName"][0].ToString());
                }
            }
        }

        return inactiveUsers;
    }

    private DateTime? GetLastLogonTimestamp(SearchResultEntry user)
    {
        try
        {
            if (user.Attributes["lastLogonTimestamp"] != null)
            {
                long lastLogonTimestamp = (long)user.Attributes["lastLogonTimestamp"][0];
                return DateTime.FromFileTimeUtc(lastLogonTimestamp);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading lastLogonTimestamp for user: {user.DistinguishedName}. Exception: {ex.Message}");
        }

        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "yourdomain.com"; // Adjust this to your AD domain
        string username = "yourUsername"; // Your AD username
        string password = "yourPassword"; // Your AD password

        var adService = new ActiveDirectoryService(ldapPath, username, password);
        int days = 30; // Example: users inactive for the last 30 days
        List<string> inactiveUsers = adService.GetInactiveUsers(days);

        foreach (var user in inactiveUsers)
        {
            Console.WriteLine($"User {user} is inactive.");
        }

        Console.WriteLine($"Total number of inactive users: {inactiveUsers.Count}");
    }
}



using System;
using System.Collections.Generic;
using System.DirectoryServices;

public class ActiveDirectoryService
{
    private readonly string _ldapPath;

    public ActiveDirectoryService(string ldapPath)
    {
        _ldapPath = ldapPath;
    }

    public List<DirectoryEntry> GetInactiveUsers(int days)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-days);
        List<DirectoryEntry> inactiveUsers = new List<DirectoryEntry>();

        using (var rootEntry = new DirectoryEntry(_ldapPath))
        {
            SearchInactiveUsers(rootEntry, cutoffDate, inactiveUsers);
        }

        return inactiveUsers;
    }

    private void SearchInactiveUsers(DirectoryEntry entry, DateTime cutoffDate, List<DirectoryEntry> inactiveUsers)
    {
        foreach (DirectoryEntry child in entry.Children)
        {
            if (child.SchemaClassName == "user")
            {
                DateTime? lastLogon = GetLastLogonTimestamp(child);

                if (lastLogon == null || lastLogon < cutoffDate)
                {
                    inactiveUsers.Add(child);
                }
            }
            else if (child.SchemaClassName == "organizationalUnit" || child.SchemaClassName == "container")
            {
                // Recursive call for OUs and containers
                SearchInactiveUsers(child, cutoffDate, inactiveUsers);
            }
        }
    }

    private DateTime? GetLastLogonTimestamp(DirectoryEntry user)
    {
        if (user.Properties["lastLogonTimestamp"].Value != null)
        {
            var lastLogonTimestamp = (long)user.Properties["lastLogonTimestamp"].Value;
            return DateTime.FromFileTime(lastLogonTimestamp);
        }

        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "LDAP://DC=yourdomain,DC=com"; // Adjust this to your AD path

        var adService = new ActiveDirectoryService(ldapPath);
        int days = 30; // Example: users inactive for the last 30 days
        List<DirectoryEntry> inactiveUsers = adService.GetInactiveUsers(days);

        foreach (var user in inactiveUsers)
        {
            Console.WriteLine($"User {user.Properties["sAMAccountName"].Value} is inactive.");
        }
    }
}












using System;
using System.Collections.Generic;
using System.DirectoryServices;

public class ActiveDirectoryService
{
    private readonly string _ldapPath;

    public ActiveDirectoryService(string ldapPath)
    {
        _ldapPath = ldapPath;
    }

    public List<DirectoryEntry> GetInactiveUsers(int days)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-days);
        List<DirectoryEntry> inactiveUsers = new List<DirectoryEntry>();

        using (var rootEntry = new DirectoryEntry(_ldapPath))
        {
            foreach (DirectoryEntry user in rootEntry.Children)
            {
                if (user.SchemaClassName == "user")
                {
                    DateTime? lastLogon = GetLastLogonTimestamp(user);

                    if (lastLogon == null || lastLogon < cutoffDate)
                    {
                        inactiveUsers.Add(user);
                    }
                }
            }
        }

        return inactiveUsers;
    }

    private DateTime? GetLastLogonTimestamp(DirectoryEntry user)
    {
        if (user.Properties["lastLogonTimestamp"].Value != null)
        {
            var lastLogonTimestamp = (long)user.Properties["lastLogonTimestamp"].Value;
            return DateTime.FromFileTime(lastLogonTimestamp);
        }

        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string ldapPath = "LDAP://CN=Users,DC=yourdomain,DC=com"; // Adjust this to your AD path

        var adService = new ActiveDirectoryService(ldapPath);
        int days = 30; // Example: users inactive for the last 30 days
        List<DirectoryEntry> inactiveUsers = adService.GetInactiveUsers(days);

        foreach (var user in inactiveUsers)
        {
            Console.WriteLine($"User {user.Properties["sAMAccountName"].Value} is inactive.");
        }
    }
}



using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

public class ActiveDirectoryService
{
    private readonly string _domainName;
    private readonly string _container;

    public ActiveDirectoryService(string domainName, string container)
    {
        _domainName = domainName;
        _container = container;
    }

    public List<UserPrincipal> GetInactiveUsers(int days)
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-days);
        List<UserPrincipal> inactiveUsers = new List<UserPrincipal>();

        using (var context = new PrincipalContext(ContextType.Domain, _domainName, _container))
        {
            using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
            {
                foreach (var result in searcher.FindAll())
                {
                    var user = result as UserPrincipal;
                    if (user != null)
                    {
                        DateTime? lastLogon = GetLastLogon(user);

                        if (lastLogon == null || lastLogon < cutoffDate)
                        {
                            inactiveUsers.Add(user);
                        }
                    }
                }
            }
        }

        return inactiveUsers;
    }

    private DateTime? GetLastLogon(UserPrincipal user)
    {
        var directoryEntry = (DirectoryEntry)user.GetUnderlyingObject();
        if (directoryEntry.Properties["lastLogon"].Value != null)
        {
            var lastLogon = (long)directoryEntry.Properties["lastLogon"].Value;
            return DateTime.FromFileTime(lastLogon);
        }

        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {
        string domainName = "yourdomain.com";
        string container = "OU=Users,DC=yourdomain,DC=com"; // Adjust this to your AD container

        var adService = new ActiveDirectoryService(domainName, container);
        int days = 30; // Example: users inactive for the last 30 days
        List<UserPrincipal> inactiveUsers = adService.GetInactiveUsers(days);

        foreach (var user in inactiveUsers)
        {
            Console.WriteLine($"User {user.SamAccountName} is inactive.");
        }
    }
}





using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;

namespace ActiveDirectoryExample
{
    class Program
    {
        static void Main(string[] args)
        {
            int daysInactive = 30; // Example: users inactive for the last 30 days
            var inactiveUsers = GetInactiveUsers(daysInactive);
            foreach (var user in inactiveUsers)
            {
                Console.WriteLine($"Username: {user.Username}, Last Logon: {user.LastLogon}");
            }
        }

        public static List<User> GetInactiveUsers(int daysInactive)
        {
            List<User> inactiveUsers = new List<User>();
            string ldapPath = "LDAP://YourDomain"; // Replace with your domain LDAP path

            using (DirectoryEntry entry = new DirectoryEntry(ldapPath))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = "(&(objectCategory=person)(objectClass=user))";
                    searcher.PropertiesToLoad.Add("samaccountname");
                    searcher.PropertiesToLoad.Add("lastLogonTimestamp");

                    DateTime inactiveSince = DateTime.UtcNow.AddDays(-daysInactive);
                    long fileTime = inactiveSince.ToFileTime();

                    foreach (SearchResult result in searcher.FindAll())
                    {
                        if (result.Properties.Contains("lastLogonTimestamp"))
                        {
                            long lastLogonTimestamp = (long)result.Properties["lastLogonTimestamp"][0];
                            DateTime lastLogon = DateTime.FromFileTime(lastLogonTimestamp);

                            if (lastLogon < inactiveSince)
                            {
                                User user = new User
                                {
                                    Username = result.Properties["samaccountname"][0].ToString(),
                                    LastLogon = lastLogon
                                };
                                inactiveUsers.Add(user);
                            }
                        }
                    }
                }
            }

            return inactiveUsers;
        }

        public class User
        {
            public string Username { get; set; }
            public DateTime LastLogon { get; set; }
        }
    }
}





using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;

namespace ActiveDirectoryExample
{
    class Program
    {
        static void Main(string[] args)
        {
            int daysInactive = 30; // Example: users inactive for the last 30 days
            var inactiveUsers = GetInactiveUsers(daysInactive);
            foreach (var user in inactiveUsers)
            {
                Console.WriteLine($"Username: {user.Username}, Last Logon: {user.LastLogon}");
            }
        }

        public static List<User> GetInactiveUsers(int daysInactive)
        {
            List<User> inactiveUsers = new List<User>();
            string ldapServer = "yourdomain.com"; // Replace with your domain
            string ldapBaseDn = "DC=yourdomain,DC=com"; // Replace with your base DN
            string ldapUser = "yourusername"; // Replace with a username with access to query AD
            string ldapPassword = "yourpassword"; // Replace with the user's password

            LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier(ldapServer);
            NetworkCredential credential = new NetworkCredential(ldapUser, ldapPassword);

            using (LdapConnection connection = new LdapConnection(identifier, credential))
            {
                connection.Bind();

                DateTime inactiveSince = DateTime.UtcNow.AddDays(-daysInactive);
                long fileTime = inactiveSince.ToFileTime();
                string filter = $"(&(objectCategory=person)(objectClass=user)(lastLogonTimestamp<={fileTime}))";

                SearchRequest searchRequest = new SearchRequest(ldapBaseDn, filter, SearchScope.Subtree, "samAccountName", "lastLogonTimestamp");

                SearchResponse searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    User user = new User
                    {
                        Username = entry.Attributes["samAccountName"][0].ToString(),
                        LastLogon = DateTime.FromFileTime((long)entry.Attributes["lastLogonTimestamp"][0])
                    };
                    inactiveUsers.Add(user);
                }
            }

            return inactiveUsers;
        }

        public class User
        {
            public string Username { get; set; }
            public DateTime LastLogon { get; set; }
        }
    }
}
