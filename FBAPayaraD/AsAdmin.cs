using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
                    Arguments = string.Join(" ", "--terse", "--interactive=true"),
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

        public (List<string>, List<string>) ListApplications()
        {
            _asAdminProc.StandardInput.WriteLine("list-applications");
            var output = ReadToPrompt()
                .Select(line => line.Split().First())
                .ToList();
            var errs = output.Count > 0
                ? new()
                : ReadErrors();
            return (output, errs);
        }

        private readonly List<char> _prompt = new()
            { 'a', 's', 'a', 'd', 'm', 'i', 'n', '>', ' ' };

        private List<string> ReadToPrompt()
        {
            var stdout = _asAdminProc.StandardOutput;
            var lines = new List<string>();
            var atPrompt = false;
            while (!atPrompt)
            {
                if (stdout.Peek() != -1 && stdout.Peek() != 'a')
                {
                    lines.Add(stdout.ReadLine());
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

        public (List<string>, List<string>) Deploy(string war)
        {
            _asAdminProc.StandardInput.WriteLine($"deploy {war}");
            var output = ReadToPrompt();
            var errs = ReadErrors();
            return (output, errs);
        }

        public (List<string>, List<string>) Undeploy(string war)
        {
            _asAdminProc.StandardInput.WriteLine($"undeploy {war}");
            return (ReadToPrompt(), ReadErrors());
        }
 
        private List<string> ReadErrors()
        {
            List<string> errs = new();
            while (_asAdminProc.StandardError.Peek() != -1)
            {
                errs.Add(_asAdminProc.StandardError.ReadLine());
            }

            return errs;
        }
    }
}
