using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FBAPayara.Shared;

namespace FBAPayaraD;

public class CommandRunner
{
    private readonly StreamWriter _output;
    private readonly AsAdmin _asAdmin;
    private readonly Dictionary<Service, Deployment> _deploymentInfo;

    public CommandRunner(
        StreamWriter output,
        AsAdmin asAdmin,
        ref Dictionary<Service, Deployment> deploymentInfo)
    {
        _output = output;
        _output.AutoFlush = true;
        _asAdmin = asAdmin;
        _deploymentInfo = deploymentInfo;
    }

    public CommandOutput Run(Command cmd) =>
        Try<CommandOutput>.TryTo(() =>
                cmd.Type switch
                {
                    CommandType.List => ListApplications(),
                    CommandType.Deploy => Deploy(cmd.Arg),
                    CommandType.Undeploy => Undeploy(cmd.Arg),
                    CommandType.Redeploy => Redeploy(cmd.Arg),
                    _ => CommandOutput.Successful("Coming Soon"),
                })
            .Or(CommandOutput.Failure("Internal error"));

    private CommandOutput ListApplications()
    {
        var deployedWars = _asAdmin.ListApplications();
        if (!deployedWars.Success)
        {
            return CommandOutput.Failure("Couldn't get applications");
        }

        return deployedWars.Value.Count == 0
            ? CommandOutput.Successful("No applications deployed")
            : CommandOutput.Successful(
               Utils.FormatDeploymentOutput(deployedWars.Value, _deploymentInfo));
    }

    private CommandOutput Deploy(string serviceName)
    {
        if (!Services.TryNameToService(serviceName, out var service))
        {
            throw new ArgumentException(
                $"{serviceName} is not a known service");
        }

        var warPath = Services.ServiceToWar(service);
        _output.WriteLine($"Deploying {warPath}. May take up to a minute.");
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
        if (!Services.TryNameToService(serviceName, out var service))
        {
            throw new ArgumentException(
                $"{serviceName} is not a known service");
        }

        return Undeploy(service);
    }

    private CommandOutput Undeploy(Service service)
    {
        var deployedWars = _asAdmin.ListApplications();
        if (!deployedWars.Success)
        {
            return CommandOutput.Failure("Couldn't get applications");
        }

        var warPath = deployedWars.Value
            .FirstOrDefault(a => a.Contains(Services.ServiceToWarPrefix(service)));
        if (warPath == null)
        {
            return CommandOutput.Failure($"{service.DisplayName()} is not deployed");
        }

        var result = _asAdmin.Undeploy(warPath);
        if (result.Success)
        {
            var war = new FileInfo(warPath).Name;
            _deploymentInfo.Remove(Services.WarToService(war));
            Utils.SaveDeploymentInfo(_deploymentInfo.Values.ToList());
        }

        // Undeploy doesn't output anything, unlike other commands, so give
        // some indication of success.
        result.Add($"Successfully undeployed {service.DisplayName()}");

        return result;
    }

    private CommandOutput Redeploy(string serviceName)
    {
        var undeploy = Undeploy(serviceName);
        if (!undeploy.Success
            // This is like stringly typed exception handling...
            && undeploy.Value.FirstOrDefault() !=
            $"{serviceName} is not deployed")
        {
            return undeploy;
        }
        undeploy.StreamOutput(_output);

        return Deploy(serviceName);
    }
}
