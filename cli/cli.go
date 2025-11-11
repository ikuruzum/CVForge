package cli

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
)

type OutputFormat string

const (
	FormatHTML OutputFormat = "html"
	FormatPDF  OutputFormat = "pdf"
)

type CommandLine struct {
	DataFile     string
	TemplateFile string
	OutputFile   string
	Format       OutputFormat
	ShowHelp     bool
	ShowVersion  bool
}

func ParseArgs() *CommandLine {
	cwd, _ := os.Getwd()
	defaultDataFile := filepath.Join(cwd, "examples", "example.yaml")
	defaultTemplate := filepath.Join(cwd, "examples", "template.html")

	cli := &CommandLine{
		DataFile:     defaultDataFile,
		TemplateFile: defaultTemplate,
		OutputFile:   "output.html",
		Format:       FormatHTML,
	}

	// Flags with both short and long forms
	flag.StringVar(&cli.DataFile, "data", cli.DataFile, "Path to YAML data file")
	flag.StringVar(&cli.DataFile, "d", cli.DataFile, "Path to YAML data file (shorthand)")

	flag.StringVar(&cli.TemplateFile, "template", cli.TemplateFile, "Path to HTML template file")
	flag.StringVar(&cli.TemplateFile, "t", cli.TemplateFile, "Path to HTML template file (shorthand)")

	flag.StringVar(&cli.OutputFile, "output", cli.OutputFile, "Output file path")
	flag.StringVar(&cli.OutputFile, "o", cli.OutputFile, "Output file path (shorthand)")

	// Format flag
	format := flag.String("format", string(FormatHTML), "Output format (html or pdf)")
	flag.StringVar(format, "f", string(FormatHTML), "Output format (html or pdf) (shorthand)")

	// Info flags
	flag.BoolVar(&cli.ShowHelp, "help", false, "Show help message")
	flag.BoolVar(&cli.ShowHelp, "h", false, "Show help message (shorthand)")
	flag.BoolVar(&cli.ShowVersion, "version", false, "Show version information")
	flag.BoolVar(&cli.ShowVersion, "v", false, "Show version information (shorthand)")

	flag.Parse()

	// Handle format
	if *format == string(FormatPDF) {
		cli.Format = FormatPDF
		if filepath.Ext(cli.OutputFile) != ".pdf" {
			cli.OutputFile = changeExt(cli.OutputFile, ".pdf")
		}
	}

	// Handle positional arguments (overrides flags)
	args := flag.Args()
	switch len(args) {
	case 3:
		cli.OutputFile = args[2]
		fallthrough
	case 2:
		cli.TemplateFile = args[1]
		fallthrough
	case 1:
		cli.DataFile = args[0]
	}

	// Ensure absolute paths
	if !filepath.IsAbs(cli.DataFile) {
		cli.DataFile = filepath.Join(cwd, cli.DataFile)
	}
	if !filepath.IsAbs(cli.TemplateFile) {
		cli.TemplateFile = filepath.Join(cwd, cli.TemplateFile)
	}
	if !filepath.IsAbs(cli.OutputFile) {
		cli.OutputFile = filepath.Join(cwd, cli.OutputFile)
	}

	return cli
}

func changeExt(path, newExt string) string {
	ext := filepath.Ext(path)
	if ext == "" {
		return path + newExt
	}
	return path[:len(path)-len(ext)] + newExt
}

func PrintHelp() {
	helpText := `CVForge - Generate CVs from YAML and HTML templates

Usage:
  cvforge [flags] [data.yaml] [template.html] [output]

Arguments:
  data.yaml         Path to YAML data file
  template.html     Path to HTML template file
  output            Output file path (default: output.html or output.pdf)

Flags:
  -d, --data string        Path to YAML data file (default "examples/example.yaml")
  -t, --template string    Path to HTML template file (default "examples/template.html")
  -o, --output string      Output file path (default "output.html")
  -f, --format string      Output format: html or pdf (default "html")
  -v, --version            Show version information
  -h, --help               Show this help message

Examples:
  # Generate PDF from default files
  cvforge -f pdf -o my-cv.pdf

  # Specify all files explicitly
  cvforge data.yaml template.html output.pdf -f pdf

  # Use short flags
  cvforge -d data.yaml -t template.html -o output.html -f html
`
	fmt.Println(helpText)
}

func PrintVersion() {
	fmt.Println("CVForge version 1.0.0")
}
