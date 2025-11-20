package engine

import (
	"bytes"

	"cvforge/types"
	"fmt"

	"os"
	"strconv"
	"strings"

	"github.com/PuerkitoBio/goquery"

	"golang.org/x/net/html"
)

type OutputFormat string

const (
	OutputHTML OutputFormat = "html"
	OutputPDF  OutputFormat = "pdf"
)

// Render renders HTML template with data and outputs in specified format
func Render(templatePath string, data types.CVBase, format OutputFormat) ([]byte, error) {
	file, err := os.Open(templatePath)
	if err != nil {
		return nil, err
	}
	defer file.Close()

	doc, err := goquery.NewDocumentFromReader(file)
	if err != nil {
		return nil, err
	}

	// Process the document starting from root
	processNode(doc.Selection, data)

	htmlContent, err := doc.Html()
	if err != nil {
		return nil, err
	}

	switch format {
	case OutputPDF:
		return GeneratePDF(htmlContent)
	default:
		return []byte(htmlContent), nil
	}
}

// processNode recursively processes HTML nodes
func processNode(node *goquery.Selection, context types.CVBase) {
	node.Each(func(i int, s *goquery.Selection) {
		if ifExist, exists := s.Attr("if-exists"); exists {
			if !checkIfExists(s, context, ifExist) {
				s.Remove()
				return // Node removed, no further processing needed
			}
			s.RemoveAttr("if-exists") // Remove attribute, continue processing
		}
		// Process repeat-for first (it replaces the node)
		if repeatFor, exists := s.Attr("repeat-for"); exists {
			processRepeatFor(s, context, repeatFor)
			return
		}

		// Process value-of
		if valueOf, exists := s.Attr("value-of"); exists {
			cvValue := getCVBaseFromPath(context, valueOf)
			if cvValue != nil {
				content := getStringValue(cvValue)
				url := getURL(cvValue)

				if url != "" {
					s.SetHtml(fmt.Sprintf(`<a href="%s">%s</a>`, url, content))
				} else {
					s.SetHtml(content)
				}
			}
			s.RemoveAttr("value-of")
		}

		// Process children recursively
		processNode(s.Children(), context)
	})
}
func checkIfExists(node *goquery.Selection, context types.CVBase, path string) bool {
	value := getCVBaseFromPath(context, path)

	if value == nil {
		return false
	}

	// Check different value types
	switch v := value.(type) {
	case types.CVForgeString:
		// Empty string is considered as non-existent
		return v.Value != ""

	case types.CVForgeSlice:
		// Empty slice is considered as non-existent
		return len(v.Value) > 0

	case types.CVForgeMap:
		// Empty map is considered as non-existent
		return len(v.Value) > 0

	default:
		return false
	}
}

// processRepeatFor handles repeat-for attribute for collections
func processRepeatFor(node *goquery.Selection, context types.CVBase, repeatPath string) {
	parent := node.Parent()

	// Get the collection value
	value := getValueFromPath(context, repeatPath)

	// Try last part if not found (for nested contexts)
	if value == nil && strings.Contains(repeatPath, ".") {
		lastPart := repeatPath[strings.LastIndex(repeatPath, ".")+1:]
		value = getValueFromPath(context, lastPart)
	}

	var collection []types.CVBase

	// Handle different value types
	switch v := value.(type) {
	case types.CVForgeSlice:
		collection = v.Value
	case types.CVForgeString:
		// Split comma-separated string
		if v.Value != "" {
			parts := strings.Split(v.Value, ",")
			for _, part := range parts {
				collection = append(collection, &types.CVForgeString{Value: strings.TrimSpace(part)})
			}
		}
	case types.CVBase:
		collection = []types.CVBase{v}
	}

	if len(collection) == 0 {
		node.Remove()
		return
	}

	// Get outer HTML of template node
	templateHTML := getOuterHTML(node)

	// Remove original node
	node.Remove()
	ignore := false
	// Create a clone for each item
	for _, item := range collection {
		// Parse template HTML
		itemDoc, err := goquery.NewDocumentFromReader(strings.NewReader(templateHTML))
		if err != nil {
			continue
		}

		clone := itemDoc.Find("body").Children().First()
		if clone.Length() == 0 {
			continue
		}
		clone.RemoveAttr("repeat-for")

		// Process all value-of attributes in the clone
		clone.Find("[value-of]").Each(func(i int, valueNode *goquery.Selection) {
			valueOf, _ := valueNode.Attr("value-of")

			// Extract last parts for comparison
			valueOfLast := valueOf
			if idx := strings.LastIndex(valueOf, "."); idx != -1 {
				valueOfLast = valueOf[idx+1:]
			}

			repeatLast := repeatPath
			if idx := strings.LastIndex(repeatPath, "."); idx != -1 {
				repeatLast = repeatPath[idx+1:]
			}

			// If last parts match, use current item's value
			if valueOfLast == repeatLast {
				strval := getStringValue(item)
				if strings.TrimSpace(strval) == "" {
					ignore = true
				}
				url := getURL(item)
				if url != "" {
					valueNode.SetHtml(fmt.Sprintf(`<a href="%s">%s</a>`, url, strval))
				} else {
					valueNode.SetHtml(strval)
				}
				valueNode.RemoveAttr("value-of")
			} else if strings.HasPrefix(valueOf, repeatPath+".") {
				// Make path relative to current item
				propertyPath := valueOf[len(repeatPath)+1:]
				valueNode.SetAttr("value-of", propertyPath)
			}
		})

		// Process the clone with current item as context
		processNode(clone, item)

		// Insert clone into parent
		cloneHTML := getOuterHTML(clone)
		if ignore {
			ignore = false
			continue
		}
		parent.AppendHtml(cloneHTML)
	}
}

// getCVBaseFromPath resolves dot-notation path to get CVBase value
func getCVBaseFromPath(data types.CVBase, path string) types.CVBase {
	if data == nil || path == "" {
		return nil
	}

	parts := strings.Split(path, ".")
	current := data

	for _, part := range parts {
		if current == nil {
			return nil
		}

		// Handle array index
		if index, err := strconv.Atoi(part); err == nil {
			if slice, ok := current.(types.CVForgeSlice); ok {
				if index >= 0 && index < len(slice.Value) {
					current = slice.Value[index]
					continue
				}
			}
			return nil
		}

		// Handle map access
		if cvMap, ok := current.(types.CVForgeMap); ok {
			if val, exists := cvMap.Value[part]; exists {
				current = val
			} else {
				return nil
			}
		} else {
			return nil
		}
	}

	return current
}

// getValueFromPath is a wrapper for getCVBaseFromPath
func getValueFromPath(data types.CVBase, path string) types.CVBase {
	return getCVBaseFromPath(data, path)
}

// getStringValue extracts string representation from CVBase
func getStringValue(cv types.CVBase) string {
	if cv == nil {
		return ""
	}

	switch v := cv.(type) {
	case types.CVForgeString:
		return v.Value
	case types.CVForgeSlice:
		var parts []string
		for _, item := range v.Value {
			parts = append(parts, getStringValue(item))
		}
		return strings.Join(parts, ", ")
	default:
		return ""
	}
}

// getURL extracts URL from CVTagInfo
func getURL(cv types.CVBase) string {
	if cv == nil {
		return ""
	}

	switch v := cv.(type) {
	case types.CVForgeString:
		return v.URL
	case types.CVForgeMap:
		return v.URL
	case types.CVForgeSlice:
		return v.URL
	}

	return ""
}

// getOuterHTML returns outer HTML of a selection
func getOuterHTML(sel *goquery.Selection) string {
	if sel.Length() == 0 {
		return ""
	}

	var buf bytes.Buffer
	node := sel.Get(0)
	if err := html.Render(&buf, node); err != nil {
		return ""
	}
	return buf.String()
}
