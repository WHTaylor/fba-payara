using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FBAPayaraD
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AsAdmin _asAdmin = new();
        private Dictionary<Service, Deployment> _deploymentInfo;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _asAdmin.Start();
            _deploymentInfo = Utils.LoadDeploymentInfo();
            _logger.LogInformation(_deploymentInfo.Count.ToString());
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var server = new NamedPipeServerStream(
                    "fba-payarad", PipeDirection.InOut);
                await server.WaitForConnectionAsync(stoppingToken);
                HandleConnection(server);
            }
        }

        private void HandleConnection(Stream server)
        {
            var input = new StreamReader(server).ReadLine();
            _logger.LogInformation(input);

            var runner = new CommandRunner(
                new StreamWriter(server),
                _asAdmin,
                ref _deploymentInfo);
            runner.StreamOutput(
                Try<Command>.TryTo(() => Command.Parse(input))
                .Map(cmd => runner.Run(cmd))
                .RecoverFrom(
                    typeof(ArgumentException),
                    () => CommandOutput.Failure("asdf"))
                .Get());
        }
    }
}
