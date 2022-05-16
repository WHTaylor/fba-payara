using System;
using System.Collections.Generic;
using GitWrapper;

namespace FBAPayaraD
{
    public class Deployment
    {
        public readonly Service Service;
        private readonly string _version;
        private readonly DateTime? _deployTime;
        private readonly string _repoBranch;

        public Deployment(
            Service service,
            string version,
            DateTime? deployTime,
            string repoBranch = null)
        {
            Service = service;
            _version = version;
            _deployTime = deployTime;
            _repoBranch = repoBranch;
        }

        public string Serialize() => string.Join(",", Values());

        public List<string> Values() => new()
        {
            Service.ToString(),
            _version,
            _deployTime?.ToString("u") ?? "",
            _repoBranch ?? "",
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
    }

    public class DeploymentBuilder
    {
        private Service _service;
        private string _version;
        private DateTime? _deployTime;
        private string _repoBranch;

        public DeploymentBuilder FromWar(string war)
        {
            _service = Services.WarToService(war);
            _version = war.Split("-war-")[1];
            return this;
        }

        public DeploymentBuilder AtTime(DateTime dt)
        {
            _deployTime = dt;
            return this;
        }

        public DeploymentBuilder ForRepo(Git repo)
        {
            _repoBranch = repo.Branch();
            return this;
        }

        public Deployment Build()
        {
            return new Deployment(
                _service, _version, _deployTime, _repoBranch
            );
        }
    }
}
