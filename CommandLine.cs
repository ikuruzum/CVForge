using System;
using System.IO;
using System.Threading.Tasks;

public class CommandLine
{
    private static readonly string DefaultDataFile = Path.Combine("examples", "example.yaml");
    private static readonly string DefaultTemplateFile = Path.Combine("examples", "template.html");
    private static readonly string DefaultOutputFile = "output.html";

    public string DataFile { get; private set; }
    public string TemplateFile { get; private set; }
    public string OutputFile { get; private set; }
    public OutputFormat Format { get; private set; }
    public bool ShouldShowHelp { get; private set; }
    public bool ShouldShowVersion { get; private set; }

    public CommandLine(string[] args)
    {
        DataFile = DefaultDataFile;
        TemplateFile = DefaultTemplateFile;
        OutputFile = DefaultOutputFile;
        Format = OutputFormat.HTML;
ShouldShowHelp = false;
        ShouldShowVersion = false;
        
        ParseArguments(args);
    }

    private void ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            string nextArg = i + 1 < args.Length ? args[i + 1] : null;

            switch (arg)
            {
                case "-h":
                case "--help":
                    ShouldShowHelp = true;
                    return;

                case "-v":
                case "--version":
                    ShouldShowVersion = true;
                    return;

                case "-d":
                case "--data":
                    if (nextArg != null && !nextArg.StartsWith("-"))
                    {
                        DataFile = nextArg;
                        i++;
                    }
                    break;

                case "-t":
                case "--template":
                    if (nextArg != null && !nextArg.StartsWith("-"))
                    {
                        TemplateFile = nextArg;
                        i++;
                    }
                    break;

                case "-o":
                case "--output":
                    if (nextArg != null && !nextArg.StartsWith("-"))
                    {
                        OutputFile = nextArg;
                        i++;
                    }
                    break;

                case "-f":
                case "--format":
                    if (nextArg != null && !nextArg.StartsWith("-"))
                    {
                        if (nextArg.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            Format = OutputFormat.PDF;
                            if (!OutputFile.EndsWith(".pdf"))
                            {
                                OutputFile = Path.ChangeExtension(OutputFile, ".pdf");
                            }
                        }
                        else if (!nextArg.Equals("html", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException("Invalid format. Use 'html' or 'pdf'.");
                        }
                        i++;
                    }
                    break;

                default:
                    // Handle positional arguments
                    if (i == 0 && !arg.StartsWith("-"))
                    {
                        DataFile = arg;
                    }
                    else if (i == 1 && !arg.StartsWith("-"))
                    {
                        TemplateFile = arg;
                    }
                    else if (i == 2 && !arg.StartsWith("-"))
                    {
                        OutputFile = arg;
                    }
                    break;
            }
        }
    }

    public static void ShowHelp()
    {
        Console.WriteLine("CVForge - Generate CVs from YAML and HTML templates");
        Console.WriteLine();
        Console.WriteLine("Usage: cvforge [options] [data.yaml] [template.html] [output]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  [data.yaml]         Path to YAML data file (default: examples/example.yaml)");
        Console.WriteLine("  [template.html]     Path to HTML template file (default: examples/template.html)");
        Console.WriteLine("  [output]            Output file path (default: output.html or output.pdf)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -d, --data FILE     Path to YAML data file");
        Console.WriteLine("  -t, --template FILE Path to HTML template file");
        Console.WriteLine("  -o, --output FILE   Output file path");
        Console.WriteLine("  -f, --format FORMAT Output format: html or pdf (default: html)");
        Console.WriteLine("  -v, --version       Show version information");
        Console.WriteLine("  -h, --help          Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  cvforge -f pdf -o my-cv.pdf");
        Console.WriteLine("  cvforge data.yaml template.html output.pdf -f pdf");
    }

    public static void ShowVersion()
    {
        var version = typeof(CommandLine).Assembly.GetName().Version;
        Console.WriteLine($"CVForge version {version?.ToString(3) ?? "1.0.0"}");
    }

    public async Task<bool> ExecuteAsync()
    {
        try
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Check if input files exist
            if (!File.Exists(DataFile))
            {
                Console.Error.WriteLine($"Error: Data file not found: {Path.GetFullPath(DataFile)}");
                return false;
            }

            if (!File.Exists(TemplateFile))
            {
                Console.Error.WriteLine($"Error: Template file not found: {Path.GetFullPath(TemplateFile)}");
                return false;
            }

            // Read the YAML content
            var yamlContent = await File.ReadAllTextAsync(DataFile);

            // Process the template
            var data = CVForgeValue.FromYaml(yamlContent);
            var engine = new TemplateEngine();
            var result = await engine.Render(TemplateFile, data, Format);

            // Save the result
            if (Format == OutputFormat.PDF)
            {
                await File.WriteAllBytesAsync(OutputFile, result);
            }
            else
            {
                await File.WriteAllTextAsync(OutputFile, System.Text.Encoding.UTF8.GetString(result));
            }

            Console.WriteLine($"Successfully generated {Format} at {Path.GetFullPath(OutputFile)}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
            }
            return false;
        }
    }
}
