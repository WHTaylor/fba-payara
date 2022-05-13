using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;

namespace FBAPayaraD
{
    public enum Service
    {
        Schedule,
        Users,
        Visits,
        ProposalLookup,
    }

    public class ServicesMap
    {
        private static readonly string AppsRootDir =
            Path.Combine("C:", "Users", "rop61488", "Documents", "Apps");
            // TODO: Environment
            //.GetEnvironmentVariable("APPS_HOME_DIR");

        private static readonly Dictionary<Service, string> TargetDirectories =
            new()
            {
                { Service.Schedule, Path.Combine(
                    "Schedule", "SchedulePackage", "war") },
                { Service.Users, Path.Combine(
                    "Users", "users", "users-services-war") },
                { Service.Visits, Path.Combine(
                    "Visits", "VisitsPackage", "visits-war") },
                { Service.ProposalLookup, Path.Combine(
                    "Schedule", "proposal-lookup", "proposal-lookup-war") },
            };

        public static string ServiceWar(string serviceName)
        {
            var service = (Service)Enum.Parse(typeof(Service), serviceName, true);
            var targetDir = Path.Join(
                AppsRootDir,
                TargetDirectories[service],
                "target");
            var wars = Directory.GetFiles(
                targetDir, "*.war", SearchOption.TopDirectoryOnly);
            // asadmin doesn't handle single backslashes as path separators
            return wars[0].Replace("\\", "/");
        }

        public static bool IsServiceName(string name)
        {
            return Enum.TryParse(typeof(Service), name, true, out _);
        }
    }
}
