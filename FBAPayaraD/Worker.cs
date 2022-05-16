using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FBAPayaraD.ServiceExtensions;

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
                    CommandType.List => await ListApplications(),
                    CommandType.Deploy => await Deploy(cmd.Arg),
                    CommandType.Undeploy => await Undeploy(cmd.Arg),
                    _ => CommandOutput.Successful("Coming Soon"),
                };
            }
            catch (ArgumentException ex)
            {
                cmdOut = CommandOutput.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                cmdOut = CommandOutput.Failure($"Internal error '{ex.Message}");
            }

            await StreamOutput(server, cmdOut);
        }

        private async Task<CommandOutput> ListApplications()
        {
            var appList = await _asAdmin.ListApplications();
            if (!appList.Success)
            {
                return CommandOutput.Failure("Couldn't get applications");
            }

            if (appList.Value.Count == 0)
            {
                return CommandOutput.Successful("No applications deployed");
            }

            var deployedApps = appList.Value
                .ToDictionary(a => a, FromWarName);
            var appValues = new List<List<string>>();
            foreach (var (war, service) in deployedApps)
            {
                var app = _deploymentInfo.ContainsKey(service)
                    ? _deploymentInfo[service]
                    : DeployedApp.FromWar(war);
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
            var padded = appValues.Select(r =>
                    string.Join(
                        " ",
                        r.Select((v, i) => v.PadRight(longestValueLengths[i]))))
                .ToList();

            return CommandOutput.Successful(padded);
        }

        private static async Task StreamOutput(
            Stream server, CommandOutput cmdOut)
        {
            var writer = new StreamWriter(server);
            writer.AutoFlush = true;

            var prefix = cmdOut.Success ? "" : "Error: ";
            await writer.WriteAsync(prefix + string.Join("\n", cmdOut.Value));
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
            var repo = ServicesMap.ServiceRepo(serviceName);
            if (result.Success)
            {
                var war = new FileInfo(warPath).Name;
                var deployment = DeployedApp.FromWar(war, DateTime.Now);
                deployment.repoBranch = repo.Branch();
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
            if (!appsList.Success)
            {
                return CommandOutput.Failure("Couldn't get applications");
            }

            var warPath = appsList.Value
                .FirstOrDefault(a => a.Contains(serviceName.ToLower()));
            if (warPath == null)
            {
                return CommandOutput.Failure($"{serviceName} is not deployed");
            }

            var result = await _asAdmin.Undeploy(warPath);
            if (result.Success)
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
