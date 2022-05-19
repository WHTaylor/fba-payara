using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace FBAPayaraD
{
    public static class Utils
    {
        private static readonly string AppDataDir =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        private static readonly string SaveFile =
            Path.Join(AppDataDir, "fba-payara", "data");

        public static void SaveDeploymentInfo(List<Deployment> apps)
        {
            if (!File.Exists(SaveFile))
            {
                var dir = Directory.GetParent(SaveFile);
                Directory.CreateDirectory(dir.FullName);
            }

            File.WriteAllLines(
                SaveFile,
                apps.Select(app => app.Serialize()).ToArray());
        }

        public static Dictionary<Service, Deployment> LoadDeploymentInfo()
        {
            if (!File.Exists(SaveFile)) return new();

            return File.ReadLines(SaveFile)
                .Select(Deployment.Deserialize)
                .ToDictionary(a => a.Service, a => a);
        }

        /// <summary>
        /// Format a list of wars as a table
        ///
        /// The name and version of the service is inferred from the war name,
        /// and any extra information stored in deploymentInfo is added in
        /// separate columns.
        ///
        /// Each column is as wide as the widest value in the column.
        /// </summary>
        /// <param name="deployedWars">The wars to list in the output</param>
        /// <param name="deploymentInfo">Additional information about the
        /// deployments (ie. when wars were deployed)</param>
        /// <returns></returns>
        public static List<string> FormatDeploymentOutput(
            IEnumerable<string> deployedWars,
            Dictionary<Service, Deployment> deploymentInfo)
        {
            var deployedApps = deployedWars
                .ToDictionary(a => a, Services.WarToService);
            var appValues = new List<List<string>>();
            foreach (var (war, service) in deployedApps)
            {
                var app = deploymentInfo.ContainsKey(service)
                    ? deploymentInfo[service]
                    : new DeploymentBuilder().FromWar(war).Build();
                appValues.Add(app.Values());
            }

            // Get the longest length for each of the value fields
            var numValues = appValues[0].Count;
            var longestValueLengths = Enumerable.Range(0, numValues)
                .Select(i =>
                    appValues.Select(r => r[i])
                        .Max(v => v.Length)
                ).ToList();
            // Pad all value fields to be as long as the longest value across
            // all the apps
            return appValues.Select(r =>
                    string.Join(
                        " ",
                        r.Select((v, i) => v.PadRight(longestValueLengths[i]))))
                .ToList();
        }
    }
}
