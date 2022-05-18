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
    }
}
