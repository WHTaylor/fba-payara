using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitWrapper;

namespace FBAPayaraD
{
    public enum Service
    {
        Schedule,
        Users,
        Visits,
        ProposalLookup,
    }

    public static class ServiceExtensions
    {
        public static string DisplayName(this Service s) => s.ToString();
    }

    public static class Services
    {
        private static readonly string AppsRootDir =
            Path.Combine("C:", "Users", "rop61488", "Documents", "Apps");
        // TODO: Environment
        //.GetEnvironmentVariable("APPS_HOME_DIR");

        private static readonly Dictionary<Service, string> RepoDirectories =
            new()
            {
                { Service.Schedule, "Schedule" },
                { Service.Users, "Users" },
                { Service.Visits, "Visits" },
                { Service.ProposalLookup, "Schedule" },
            };

        private static readonly Dictionary<Service, string> TargetDirectories =
            new()
            {
                { Service.Schedule, Path.Combine("SchedulePackage", "war") },
                { Service.Users, Path.Combine("users", "users-services-war") },
                { Service.Visits, Path.Combine("VisitsPackage", "visits-war") },
                {
                    Service.ProposalLookup, Path.Combine(
                        "proposal-lookup", "proposal-lookup-war")
                },
            };

        // Services whose war files are named differently to the enum
        private static readonly Dictionary<string, Service> WarPrefixServices =
            new()
            {
                { "proposal-lookup", Service.ProposalLookup },
                { "users-services", Service.Users },
            };

        private static readonly Dictionary<Service, string> ServiceWarPrefixes =
            WarPrefixServices.ToDictionary(kv => kv.Value, kv => kv.Key);

        public static bool TryNameToService(
            string serviceName,
            out Service service)
        {
            return Enum.TryParse(serviceName.Replace("-", ""), true, out service);
        }

        private static Service NameToService(string s)
            => (Service)Enum.Parse(typeof(Service), s.Replace("-", ""), true);

        public static string ServiceToWarPrefix(Service s) =>
            ServiceWarPrefixes.ContainsKey(s)
                ? ServiceWarPrefixes[s]
                : s.ToString().ToLower();

        public static string ServiceToWar(Service service)
        {
            var targetDir = Path.Join(
                AppsRootDir,
                RepoDirectories[service],
                TargetDirectories[service],
                "target");
            var wars = Directory.GetFiles(
                targetDir, "*.war", SearchOption.TopDirectoryOnly);
            // asadmin doesn't handle single backslashes as path separators
            return wars[0].Replace("\\", "/");
        }

        public static Service WarToService(string war)
        {
            var parts = war.Split("-war-");
            return WarPrefixServices.ContainsKey(parts[0])
                ? WarPrefixServices[parts[0]]
                : NameToService(parts[0]);
        }

        public static Git ServiceRepo(Service service) =>
            new(Path.Join(AppsRootDir, RepoDirectories[service]));

        public static Git NameToRepo(string serviceName) =>
            ServiceRepo(NameToService(serviceName));
    }
}
