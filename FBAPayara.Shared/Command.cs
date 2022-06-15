using PrefixMatch;

namespace FBAPayara.Shared
{
    public class Command
    {
        public readonly CommandType Type;
        public readonly string? Arg;

        private static readonly PrefixMatcher CommandTypeMatcher = new(
            Enum.GetNames<CommandType>().Select(t => t.ToLower()).ToList());

        private Command(CommandType type, string? arg)
        {
            Type = type;
            Arg = arg;
        }

        public static Command Parse(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString))
            {
                throw new ArgumentException("Need a command");
            }

            var words = cmdString.Trim().Split();
            var commandMatch = CommandTypeMatcher.Search(words[0].ToLower());

            if (commandMatch.Type == ResultType.NoMatch)
            {
                throw new ArgumentException($"{words[0]} is not a valid command");
            }
            else if (commandMatch.Type == ResultType.NonUnique)
            {
                var suggestions = string.Join("\n", commandMatch.Suggestions!);
                throw new ArgumentException($"{words[0]} is ambiguous. " +
                                            $"Could be one of:\n{suggestions}");
            }

            var type = Enum.Parse<CommandType>(commandMatch.Word!, true);
            return type switch
            {
                CommandType.List => new Command(type, null),
                _ => RequireArg(type, words),
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
