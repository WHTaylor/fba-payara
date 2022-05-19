using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using FBAPayara.Shared;

namespace FBAPayara
{
    class Program
    {
        static void Main(string[] args)
        {
            var cmd = string.Join(" ", args);
            // Validate command
            try
            {
                Command.Parse(cmd);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            using var client = new NamedPipeClientStream(
                ".", "fba-payarad", PipeDirection.InOut);
            client.Connect();

            var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            writer.WriteLine(cmd);

            var reader = new StreamReader(client);
            while (reader.ReadLine() is { } output)
            {
                Console.WriteLine(output);
            }
        }
    }
}
