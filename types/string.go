package types

import "fmt"

type CVForgeString struct {
	CVTagInfo
	Value string
}

func MakeCVForgeString(value any, inheritedCVTagInfo CVTagInfo) (CVForgeString, bool) {
	info := DefaultCVTagInfo()
	info.inherit(inheritedCVTagInfo)
	if value == nil {
		return CVForgeString{CVTagInfo: info}, false
	}
	switch value.(type) {
	case string, int, int64, float64, bool:
		return CVForgeString{
			CVTagInfo: info,
			Value: fmt.Sprintf("%v", value),
		}, true
	case map[string]any:
		m := value.(map[string]any)
		info = CVTagInfoFromMap(m)
		info.inherit(inheritedCVTagInfo)
		if m["value"] == nil {
			return CVForgeString{CVTagInfo: info}, false
		}
		if str, ok := m["value"].(string); ok {
			if len(str) == 0 {
				return CVForgeString{CVTagInfo: info}, false
			}
			return CVForgeString{
				CVTagInfo: info,
				Value:     str,
			}, true
		}
	}
	return CVForgeString{CVTagInfo: info}, false
}

func (s CVForgeString) Filter(tags []string) (data CVBase, passed bool) {
	if s.FilterPass(tags) {
		return s.Copy(), true
	}
	return s.Copy(), false
}
func (s CVForgeString) GetEveryTag() []string {
	return s.Tags[:]
}
func (s CVForgeString) Copy() CVBase {
	return CVForgeString{
		CVTagInfo: s.CVTagInfo,
		Value:     s.Value,
	}
}
