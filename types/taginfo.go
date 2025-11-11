package types

import (
	"slices"
	"strings"
)

type CVTagInfo struct {
	Tags     []string `yaml:"tags,omitempty"`
	URL      string   `yaml:"url,omitempty"`
	Explicit bool     `yaml:"explicit,omitempty"`
}

func CVTagInfoFromMap(m map[string]any) CVTagInfo {
	tags := []string{}
	url := ""
	explicit := false
	if m == nil {
		return CVTagInfo{}
	}
	if m["tags"] != nil {
		if _, ok := m["tags"].([]string); ok {
			tags = m["tags"].([]string)
		}
		if _, ok := m["tags"].(string); ok {
			tags = strings.Split(m["tags"].(string), ",")
		}
	}
	if m["url"] != nil {
		if _, ok := m["url"].(string); ok {
			url = m["url"].(string)
		}
	}
	if m["explicit"] != nil {
		if _, ok := m["explicit"].(bool); ok {
			explicit = m["explicit"].(bool)
		}
	}
	return CVTagInfo{
		Tags:     tags,
		URL:      url,
		Explicit: explicit,
	}
}

func (t *CVTagInfo) FilterPass(tags []string) (pass bool) {

	for _, tag := range t.Tags {
		pass = slices.Contains(tags, tag)
		if pass {
			break
		}
	}
	if !pass && t.Explicit {
		return false
	}

	return pass || t.Tags == nil || len(t.Tags) == 0
}
