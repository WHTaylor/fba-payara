using System;
using System.Collections.Generic;

namespace FBAPayaraD
{
    public class Deployment
    {
        public readonly Service service;
        private readonly string version;
        private readonly DateTime? deployDate;
        public string repoBranch;

        public Deployment(Service service, string version, DateTime? deployDate, string repoBranch=null)
        {
            this.service = service;
            this.version = version;
            this.deployDate = deployDate;
            this.repoBranch = repoBranch;
        }

        public string Serialize() => string.Join(",", Values());

        public List<string> Values() => new()
        {
            service.ToString(),
            version,
            deployDate?.ToString("u") ?? "",
            repoBranch ?? "",
        };

        public static Deployment Deserialize(string serialized)
        {
            var parts = serialized.Split(",");
            return new Deployment(
                (Service)Enum.Parse(typeof(Service), parts[0], true),
                parts[1],
                DateTime.Parse(parts[2]),
                parts[3]);
        }

        public static Deployment FromWar(string war, DateTime? deployedTime=null)
        {
            var parts = war.Split("-war-");
            return new Deployment(
                Services.WarToService(war),
                parts[1],
                deployedTime);
        }
    }
}
