using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FBAPayaraD;

public class CommandRunner
{
    private StreamWriter _output;
    private AsAdmin _asAdmin;
    private Dictionary<Service, Deployment> _deploymentInfo;

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

    private CommandOutput Deploy(string serviceName)
    {
        if (!Services.IsValidName(serviceName))
        {
            throw new ArgumentException(
                $"{serviceName} is not a known service");
        }

        var warPath = Services.NameToWar(serviceName);
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

        // Undeploy doesn't output anything, unlike other commands, so give
        // some indication of success.
        result.Add($"Successfully undeployed {serviceName}");

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
        undeploy.StreamOutput(_output);

        return Deploy(serviceName);
    }
}
