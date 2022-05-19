using System.Diagnostics;

namespace FBAPayara.Shared
{
    public class Command
    {
        public readonly CommandType Type;
        public readonly string? Arg;

        private Command(CommandType type, string? arg)
        {
            Type = type;
            Arg = arg;
        }

        public static Command Parse(string cmdString)
        {
            var words = cmdString.Split();
            if (words.Length == 0)
            {
                throw new ArgumentException("Need a command");
            }

            if (!Enum.TryParse(
                    typeof(CommandType),
                    words[0],
                    true,
                    out var type))
                throw new ArgumentException($"{words[0]} is not a valid command");

            Debug.Assert(type != null, nameof(type) + " != null");
            var ctype = (CommandType)type;
            return ctype switch
            {
                CommandType.List => new Command(ctype, null),
                _ => RequireArg(ctype, words),
            };
        }

        private static Command RequireArg(CommandType type, string[] words)
        {
            if (words.Length < 2)
            {
                throw new ArgumentException(
                    $"{type} command requires an argument");
            }

            return new Command(type, words[1]);
        }
    }

    public enum CommandType
    {
        Deploy,
        Undeploy,
        Redeploy,
        List
    }
}
