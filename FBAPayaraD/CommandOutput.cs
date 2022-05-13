using System;
using System.Collections.Generic;
using System.Linq;

namespace FBAPayaraD
{
    public class CommandOutput
    {
        private readonly List<string> _outLines;
        private readonly List<string> _errLines;
        public bool WasSuccess => _errLines.Count == 0;

        public string Output => string.Join("\n", WasSuccess
            ? _outLines
            : _errLines);

        public List<string> Values => WasSuccess
            ? _outLines
            : _errLines;

        private CommandOutput(List<string> outLines, List<string> errLines)
        {
            _outLines = outLines;
            _errLines = errLines;
        }

        public static CommandOutput Success(List<string> values)
        {
            return new CommandOutput(values, new List<string>());
        }

        public static CommandOutput Success(string value)
        {
            return new CommandOutput(new List<string> { value }, new List<string>());
        }

        public static CommandOutput Failure(List<string> values)
        {
            return new CommandOutput(new List<string>(), values);
        }

        public static CommandOutput Failure(string value)
        {
            return new CommandOutput(new List<string>(), new List<string> { value });
        }

        public CommandOutput Map(Func<string, string> f)
        {
            return WasSuccess
                ? new CommandOutput(_outLines.Select(f).ToList(), _errLines)
                : this;
        }
    }
}
