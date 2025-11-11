// main.go
package main

import (
	"cvforge/engine"
	"cvforge/types"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/spf13/cobra"
	"gopkg.in/yaml.v3"
)

const version = "1.0.0"

var (
	templatePath string
	dataPath     string
	outputPath   string
	format       string
	verbose      bool
)

func main() {
	rootCmd := &cobra.Command{
		Use:   "cvforge",
		Short: "CVForge - Modern CV/Resume Template Engine",
		Long: `CVForge is a powerful template engine that converts HTML templates 
and structured data into beautiful PDFs or HTML documents.`,
		Version: version,
		RunE:    run,
	}

	// Flags
	rootCmd.Flags().StringVarP(&templatePath, "template", "t", "", "Path to HTML template file (required)")
	rootCmd.Flags().StringVarP(&dataPath, "data", "d", "", "Path to JSON (or YAML) data file (required)")
	rootCmd.Flags().StringVarP(&outputPath, "output", "o", "output.pdf", "Output file path")
	rootCmd.Flags().StringVarP(&format, "format", "f", "pdf", "Output format: pdf or html")
	rootCmd.Flags().BoolVarP(&verbose, "verbose", "v", false, "Enable verbose logging")

	rootCmd.MarkFlagRequired("template")
	rootCmd.MarkFlagRequired("data")

	if err := rootCmd.Execute(); err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}
}

func run(cmd *cobra.Command, args []string) error {
	// Validate inputs
	if err := validateInputs(); err != nil {
		return err
	}

	if verbose {
		fmt.Printf("🚀 CVForge v%s\n", version)
		fmt.Printf("📄 Template: %s\n", templatePath)
		fmt.Printf("📊 Data: %s\n", dataPath)
		fmt.Printf("💾 Output: %s\n", outputPath)
		fmt.Printf("🎨 Format: %s\n", strings.ToUpper(format))
		fmt.Println(strings.Repeat("─", 50))
	}

	// Load data
	data, err := loadData(dataPath)
	if err != nil {
		return fmt.Errorf("failed to load data: %w", err)
	}

	if verbose {
		fmt.Println("✅ Data loaded successfully")
	}

	// Determine output format
	var outputFormat engine.OutputFormat
	switch strings.ToLower(format) {
	case "pdf":
		outputFormat = engine.OutputPDF
	case "html":
		outputFormat = engine.OutputHTML
	default:
		return fmt.Errorf("invalid format: %s (use 'pdf' or 'html')", format)
	}

	// Render template
	if verbose {
		fmt.Println("🔄 Rendering template...")
	}

	result, err := engine.Render(templatePath, data, outputFormat)
	if err != nil {
		return fmt.Errorf("failed to render template: %w", err)
	}

	if verbose {
		fmt.Printf("✅ Template rendered (%d bytes)\n", len(result))
	}

	// Write output
	if err := os.WriteFile(outputPath, result, 0644); err != nil {
		return fmt.Errorf("failed to write output: %w", err)
	}

	fmt.Printf("✨ Success! Output written to: %s\n", outputPath)
	return nil
}

func validateInputs() error {
	// Check template exists
	if _, err := os.Stat(templatePath); os.IsNotExist(err) {
		return fmt.Errorf("template file not found: %s", templatePath)
	}

	// Check data exists
	if _, err := os.Stat(dataPath); os.IsNotExist(err) {
		return fmt.Errorf("data file not found: %s", dataPath)
	}

	// Create output directory if needed
	outputDir := filepath.Dir(outputPath)
	if outputDir != "." && outputDir != "" {
		if err := os.MkdirAll(outputDir, 0755); err != nil {
			return fmt.Errorf("failed to create output directory: %w", err)
		}
	}

	return nil
}

func loadData(path string) (types.CVBase, error) {
	fileData, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}

	var rawData map[string]interface{}
	if err := json.Unmarshal(fileData, &rawData); err != nil {
		if err = yaml.Unmarshal(fileData, &rawData); err != nil {
			return nil, err
		}
	}

	return convertToCVBase(rawData), nil
}

// convertToCVBase converts generic JSON data to CVBase types
func convertToCVBase(data interface{}) types.CVBase {
	switch v := data.(type) {
	case map[string]interface{}:
		cvMap := &types.CVForgeMap{
			Value: make(map[string]types.CVBase),
		}

		// Check for special fields
		if url, ok := v["_url"].(string); ok {
			cvMap.URL = url
			delete(v, "_url")
		}
		if tags, ok := v["_tags"].([]interface{}); ok {
			for _, tag := range tags {
				if tagStr, ok := tag.(string); ok {
					cvMap.Tags = append(cvMap.Tags, tagStr)
				}
			}
			delete(v, "_tags")
		}

		for key, val := range v {
			cvMap.Value[key] = convertToCVBase(val)
		}
		return cvMap

	case []interface{}:
		cvSlice := &types.CVForgeSlice{
			Value: make([]types.CVBase, 0, len(v)),
		}
		for _, item := range v {
			cvSlice.Value = append(cvSlice.Value, convertToCVBase(item))
		}
		return cvSlice

	case string:
		return &types.CVForgeString{Value: v}

	case float64, int, int64:
		return &types.CVForgeString{Value: fmt.Sprintf("%v", v)}

	case bool:
		return &types.CVForgeString{Value: fmt.Sprintf("%t", v)}

	default:
		return &types.CVForgeString{Value: ""}
	}
}
