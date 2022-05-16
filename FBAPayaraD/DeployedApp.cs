using System;
using System.Collections.Generic;
using static FBAPayaraD.ServiceExtensions;

namespace FBAPayaraD
{
    public class DeployedApp
    {
        public readonly Service service;
        private readonly string version;
        private readonly DateTime? deployDate;

        public DeployedApp(Service service, string version, DateTime? deployDate)
        {
            this.service = service;
            this.version = version;
            this.deployDate = deployDate;
        }

        public string Serialize()
        {
            return $"{service},{version},{deployDate:u}";
        }

        public List<string> Values() => new()
        {
            service.ToString(), version, deployDate?.ToString("u") ?? ""
        };

        public static DeployedApp Deserialize(string serialized)
        {
            var parts = serialized.Split(",");
            return new DeployedApp(
                (Service)Enum.Parse(typeof(Service), parts[0], true),
                parts[1],
                DateTime.Parse(parts[2]));
        }

        public static DeployedApp FromWar(string war, DateTime? deployedTime=null)
        {
            var parts = war.Split("-war-");
            return new DeployedApp(
                FromWarName(war),
                parts[1],
                deployedTime);
        }
    }
}
