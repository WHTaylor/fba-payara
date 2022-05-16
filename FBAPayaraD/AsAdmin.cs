﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FBAPayaraD
{
    public class AsAdmin
    {
        private readonly Process _asAdminProc;

        private const string AsadminExe =
            "C:/payara/installations/payara-4.1.2.181/payara41/bin/asadmin.bat";

        public AsAdmin()
        {
            _asAdminProc = new Process
            {
                StartInfo = new ProcessStartInfo(AsadminExe)
                {
                    Arguments = string.Join(" ", "--interactive=true"),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
        }

        public void Start()
        {
            _asAdminProc.Start();
            // Skip startup help line
            _asAdminProc.StandardOutput.ReadLine();
            // Skip prompt
            var buf = new char[_prompt.Count];
            _asAdminProc.StandardOutput.Read(buf, 0, _prompt.Count);
        }

        public async Task<CommandOutput> ListApplications()
        {
            await _asAdminProc.StandardInput.WriteLineAsync("list-applications");
            var output = await GetCommandOutput();
            return output.Map(line => line.Split().First());
        }

        private readonly List<char> _prompt = new()
            { 'a', 's', 'a', 'd', 'm', 'i', 'n', '>', ' ' };

        private async Task<List<string>> ReadToPrompt()
        {
            var stdout = _asAdminProc.StandardOutput;
            var lines = new List<string>();
            var atPrompt = false;
            while (!atPrompt)
            {
                if (stdout.Peek() != -1 && stdout.Peek() != 'a')
                {
                    lines.Add(await stdout.ReadLineAsync());
                }
                // If the line starts with 'a', it might be the prompt.
                // Because the prompt doesn't end with a newline, we need to
                // read the line one character at a time.
                else
                {
                    var chars = new List<char>();
                    int c;
                    while ((c = stdout.Read()) != -1)
                    {
                        // If we hit a newline, this isn't the prompt.
                        // New lines are added later, so don't include it in
                        // the output
                        if (c == '\n')
                        {
                            break;
                        }

                        chars.Add((char)c);

                        // If this is the prompt, don't include it in the
                        // output and stop reading.
                        if (chars.Count == _prompt.Count
                            && chars.SequenceEqual(_prompt))
                        {
                            atPrompt = true;
                            break;
                        }
                    }

                    if (!atPrompt)
                    {
                        lines.Add(string.Join("", chars));
                    }
                }
            }

            return lines;
        }

        public async Task<CommandOutput> Deploy(string war)
        {
            Console.WriteLine($"Deploying {war}");
            await _asAdminProc.StandardInput.WriteLineAsync($"deploy {war}");
            return await GetCommandOutput();
        }

        public async Task<CommandOutput> Undeploy(string war)
        {
            Console.WriteLine($"Undeploying {war}");
            await _asAdminProc.StandardInput.WriteLineAsync($"undeploy {war}");
            return await GetCommandOutput();
        }

        private async Task<CommandOutput> GetCommandOutput()
        {
            var output = await ReadToPrompt();
            if (output.Count == 0) return CommandOutput.Failure("No output");

            var success = output[^1].Trim().StartsWith("Command")
                          && output[^1].Trim().EndsWith("successfully.");
            var returnOutput = output
                .Take(output.Count - 1)
                .Select(s => s.Trim())
                .ToList();

            return success
                ? CommandOutput.Successful(returnOutput)
                : CommandOutput.Failure(returnOutput);
        }
    }
}
