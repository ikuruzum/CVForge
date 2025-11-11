using System;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var cli = new CommandLine(args);

            if (cli.ShouldShowHelp)
            {
                CommandLine.ShowHelp();
                return 0;
            }

            if (cli.ShouldShowVersion)
            {
                CommandLine.ShowVersion();
                return 0;
            }

            return await cli.ExecuteAsync() ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            CommandLine.ShowHelp();
            return 1;
        }
    }
}