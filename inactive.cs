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
