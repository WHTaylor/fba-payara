using System;
using System.IO;
using System.IO.Pipes;

namespace FBAPayara
{
    class Program
    {
        static void Main(string[] args)
        {
            using var client = new NamedPipeClientStream(
                ".", "fba-payarad", PipeDirection.InOut);
            client.Connect();

            var writer = new StreamWriter(client);
            writer.AutoFlush = true;
            writer.WriteLine(string.Join(" ", args));

            var reader = new StreamReader(client);
            while (reader.ReadLine() is { } output)
            {
                Console.WriteLine(output);
            }
        }
    }
}
