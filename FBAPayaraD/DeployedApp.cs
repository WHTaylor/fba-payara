using System;

namespace FBAPayaraD
{
    public class DeployedApp
    {
        private readonly Service service;
        private readonly string version;
        private readonly DateTime deployDate;

        public DeployedApp(Service service, string version, DateTime deployDate)
        {
            this.service = service;
            this.version = version;
            this.deployDate = deployDate;
        }
    }
}
