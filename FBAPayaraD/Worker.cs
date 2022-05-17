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
            var reader = new StreamReader(server);
            var input = reader.ReadLine();
            _logger.LogInformation(input);

            CommandOutput cmdOut;
            try
            {
                var cmd = Command.Parse(input);
                cmdOut = cmd.Type switch
                {
                    CommandType.List => ListApplications(),
                    CommandType.Deploy => Deploy(cmd.Arg),
                    CommandType.Undeploy => Undeploy(cmd.Arg),
                    CommandType.Redeploy => Redeploy(cmd.Arg),
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

            StreamOutput(server, cmdOut);
        }

        private CommandOutput ListApplications()
        {
            var appList = _asAdmin.ListApplications();
            if (!appList.Success)
            {
                return CommandOutput.Failure("Couldn't get applications");
            }

            if (appList.Value.Count == 0)
            {
                return CommandOutput.Successful("No applications deployed");
            }

            var deployedApps = appList.Value
                .ToDictionary(a => a, Services.WarToService);
            var appValues = new List<List<string>>();
            foreach (var (war, service) in deployedApps)
            {
                var app = _deploymentInfo.ContainsKey(service)
                    ? _deploymentInfo[service]
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
            var padded = appValues.Select(r =>
                    string.Join(
                        " ",
                        r.Select((v, i) => v.PadRight(longestValueLengths[i]))))
                .ToList();

            return CommandOutput.Successful(padded);
        }

        private static void StreamOutput(
            Stream server, CommandOutput cmdOut)
        {
            var writer = new StreamWriter(server);
            writer.AutoFlush = true;

            var prefix = cmdOut.Success ? "" : "Error: ";
            writer.Write(prefix + string.Join("\n", cmdOut.Value));
        }

        private CommandOutput Deploy(string serviceName)
        {
            if (!Services.IsValidName(serviceName))
            {
                throw new ArgumentException(
                    $"{serviceName} is not a known service");
            }

            var warPath = Services.NameToWar(serviceName);
            var result = _asAdmin.Deploy(warPath);
            if (result.Success)
            {
                var war = new FileInfo(warPath).Name;
                var deployment = new DeploymentBuilder()
                    .FromWar(war)
                    .AtTime(DateTime.Now)
                    .ForRepo(Services.NameToRepo(serviceName))
                    .Build();
                _deploymentInfo[deployment.Service] = deployment;
                Utils.SaveDeploymentInfo(_deploymentInfo.Values.ToList());
            }

            return result;
        }

        private CommandOutput Undeploy(string serviceName)
        {
            if (!Services.IsValidName(serviceName))
            {
                throw new ArgumentException(
                    $"{serviceName} is not a known service");
            }

            var appsList = _asAdmin.ListApplications();
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

            var result = _asAdmin.Undeploy(warPath);
            if (result.Success)
            {
                var war = new FileInfo(warPath).Name;
                _deploymentInfo.Remove(Services.WarToService(war));
                Utils.SaveDeploymentInfo(_deploymentInfo.Values.ToList());
            }

            return result;
        }

        private CommandOutput Redeploy(string serviceName)
        {
            var undeploy = Undeploy(serviceName);
            if (!undeploy.Success
                && undeploy.Value.FirstOrDefault() !=
                $"{serviceName} is not deployed")
            {
                return undeploy;
            }

            return Deploy(serviceName);
        }
    }
}
