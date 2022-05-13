using System;
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

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _asAdmin.Start();
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

            return await _asAdmin.Deploy(ServicesMap.ServiceWar(serviceName));
        }

        private async Task<CommandOutput> Undeploy(string serviceName)
        {
            if (!ServicesMap.IsServiceName(serviceName))
            {
                throw new ArgumentException(
                    $"{serviceName} is not a known service");
            }

            var listApps = await _asAdmin.ListApplications();
            if (!listApps.WasSuccess)
            {
                return listApps;
            }

            var war = listApps.Values
                .First(a => a.Contains(serviceName.ToLower()));
            return await _asAdmin.Undeploy(war);
        }
    }
}
