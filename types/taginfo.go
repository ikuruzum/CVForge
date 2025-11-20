package types

import (
	"slices"
	"strings"
)

type CVTagInfo struct {
	Tags      []string `yaml:"tags,omitempty"`
	URL       string   `yaml:"url,omitempty"`
	Exclusive bool     `yaml:"exclusive,omitempty"`
}

func DefaultCVTagInfo() CVTagInfo {
	return CVTagInfo{Tags: make([]string, 0), URL: "", Exclusive: false}
}
func (t *CVTagInfo) inherit(inheritedCVTagInfo CVTagInfo) {
	if t.URL == "" {
		t.URL = inheritedCVTagInfo.URL
	}

}

func CVTagInfoFromMap(m map[string]any) CVTagInfo {
	info := DefaultCVTagInfo()
	if m == nil {
		return info
	}
	if m["tags"] != nil {
		if _, ok := m["tags"].([]any); ok {
			for _, tag := range m["tags"].([]any) {
				if t, ok := tag.(string); ok {
					info.Tags = append(info.Tags, strings.ToLower(t))
				}
			}
		}
		if _, ok := m["tags"].(string); ok {
			for _, tag := range strings.Split(m["tags"].(string), ",") {
				info.Tags = append(info.Tags, strings.ToLower(tag))
			}
		}
	}
	if m["url"] != nil {
		if _, ok := m["url"].(string); ok {
			info.URL = m["url"].(string)
		}
	}
	if m["exclusive"] != nil {
		if _, ok := m["exclusive"].(bool); ok {
			info.Exclusive = m["exclusive"].(bool)
		}
		if _, ok := m["exclusive"].(string); ok {
			info.Exclusive = m["exclusive"].(string) == "true" || m["exclusive"].(string) == "1"
		}
		if _, ok := m["exclusive"].(int); ok {
			info.Exclusive = m["exclusive"].(int) == 1
		}

	}
	return info

}

func (t *CVTagInfo) FilterPass(tags []string) (pass bool) {

	for _, tag := range t.Tags {
		pass = slices.Contains(tags, tag)
		if pass {
			break
		}
	}
	if !pass && t.Exclusive {
		return false
	}

	return pass || t.Tags == nil || len(t.Tags) == 0
}
