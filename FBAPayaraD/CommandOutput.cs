using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FBAPayaraD
{
    public class CommandOutput
    {
        public readonly List<string> Value;
        public readonly bool Success;

        private CommandOutput(List<string> value, bool success)
        {
            Value = value;
            Success = success;
        }

        public static CommandOutput Successful(List<string> values)
        {
            return new CommandOutput(values, true);
        }

        public static CommandOutput Successful(string value)
        {
            return new CommandOutput(new List<string> { value }, true);
        }

        public static CommandOutput Failure(List<string> values)
        {
            return new CommandOutput(values, false);
        }

        public static CommandOutput Failure(string value)
        {
            return new CommandOutput(new List<string> { value }, false);
        }

        public CommandOutput Map(Func<string, string> f)
        {
            return Success
                ? new CommandOutput(Value.Select(f).ToList(), Success)
                : this;
        }

        public void Add(string s) => Value.Add(s);

        public void StreamOutput(StreamWriter stream)
        {
            if (!Success) stream.Write("Error: ");
            foreach (var line in Value)
            {
                stream.WriteLine(line);
            }
        }
    }
}
