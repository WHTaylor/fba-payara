using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
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
        private Dictionary<Service, DeployedApp> _deploymentInfo;

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
                await HandleConnection(server);
            }
        }

        private async Task HandleConnection(Stream server)
        {
            var reader = new StreamReader(server);
            var input = await reader.ReadLineAsync();
            _logger.LogInformation(input);

            CommandOutput cmdOut;
            try
            {
                var cmd = Command.Parse(input);
                cmdOut = cmd.Type switch
                {
                    CommandType.List => await _asAdmin.ListApplications(),
                    CommandType.Deploy => await Deploy(cmd.Arg),
                    CommandType.Undeploy => await Undeploy(cmd.Arg),
                    _ => CommandOutput.Success("Coming Soon"),
                };
            }
            catch (ArgumentException ex)
            {
                cmdOut = CommandOutput.Failure(ex.Message);
            }
            catch (Exception)
            {
                cmdOut = CommandOutput.Failure("Internal error");
            }

            await StreamOutput(server, cmdOut);
        }

        private static async Task StreamOutput(
            Stream server, CommandOutput cmdOut)
        {
            var writer = new StreamWriter(server);
            writer.AutoFlush = true;

            await writer.WriteAsync(cmdOut.Output);
        }

        private async Task<CommandOutput> Deploy(string serviceName)
        {
            if (!ServicesMap.IsServiceName(serviceName))
            {
                throw new ArgumentException(
                    $"{serviceName} is not a known service");
            }

            var warPath = ServicesMap.ServiceWar(serviceName);
            var result = await _asAdmin.Deploy(warPath);
            if (result.WasSuccess)
            {
                var war = new FileInfo(warPath).Name;
                var deployment = DeployedApp.FromWar(war, DateTime.Now);
                _deploymentInfo[deployment.service] = deployment;
                Utils.SaveDeploymentInfo(_deploymentInfo.Values.ToList());
            }

            return result;
        }

        private async Task<CommandOutput> Undeploy(string serviceName)
        {
            if (!ServicesMap.IsServiceName(serviceName))
            {
                throw new ArgumentException(
                    $"{serviceName} is not a known service");
            }

            var appsList = await _asAdmin.ListApplications();
            if (!appsList.WasSuccess)
            {
                return appsList;
            }

            var warPath = appsList.Values
                .First(a => a.Contains(serviceName.ToLower()));
            var result = await _asAdmin.Undeploy(warPath);
            if (result.WasSuccess)
            {
                var war = new FileInfo(warPath).Name;
                var deployment = DeployedApp.FromWar(war, DateTime.Now);
                _deploymentInfo.Remove(deployment.service);
                Utils.SaveDeploymentInfo(_deploymentInfo.Values.ToList());
            }

            return result;
        }
    }
}
