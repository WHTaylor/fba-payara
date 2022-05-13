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

            List<string> output = new();
            List<string> errs;
            try
            {
                var cmd = Command.Parse(input);
                (output, errs) = cmd.Type switch
                {
                    CommandType.List => _asAdmin.ListApplications(),
                    CommandType.Deploy => Deploy(cmd.Arg),
                    CommandType.Undeploy => Undeploy(cmd.Arg),
                    _ => (new List<string> { "Coming Soon" }, new List<string>())
                };
            }
            catch (ArgumentException ex)
            {
                errs = new List<string> { ex.Message };
            }
            catch (Exception)
            {
                errs = new List<string> { "Internal error" };
            }

            await StreamOutput(server, output, errs);
        }

        private static async Task StreamOutput(
            Stream server, List<string> output, List<string> errs)
        {
            var writer = new StreamWriter(server);
            writer.AutoFlush = true;

            var message = string.Join(
                Environment.NewLine,
                errs.Count > 0 ? errs : output);
            await writer.WriteAsync(message);
        }
 
        private (List<string>, List<string>) Deploy(string serviceName)
        {
            if (!ServicesMap.IsServiceName(serviceName))
            {
                throw new ArgumentException($"{serviceName} is not a known service");
            }

            return _asAdmin.Deploy(ServicesMap.ServiceWar(serviceName));
        }
 
        private (List<string>, List<string>) Undeploy(string serviceName)
        {
            if (!ServicesMap.IsServiceName(serviceName))
            {
                throw new ArgumentException($"{serviceName} is not a known service");
            }

            var war = _asAdmin.ListApplications().Item1
                .First((s => s.Contains(serviceName.ToLower())));

            return _asAdmin.Undeploy(war);
        }
    }
}
