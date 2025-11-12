package types

type CVForgeString struct {
	CVTagInfo
	Value string
}

func MakeCVForgeString(value any) (CVForgeString, bool) {
	if value == nil {
		return CVForgeString{}, false
	}
	if str, ok := value.(string); ok {
		return CVForgeString{
			Value: str,
		}, true
	}
	if m, ok := value.(map[string]any); ok {
		if m["value"] == nil {
			return CVForgeString{}, false
		}
		if str, ok := m["value"].(string); ok {
			return CVForgeString{
				CVTagInfo: CVTagInfoFromMap(m),
				Value:     str,
			}, true
		}
	}
	return CVForgeString{}, false
}
func (s CVForgeString) Filter(tags []string) (data CVBase, passed bool) {
	if s.FilterPass(tags) {
		return s.Copy(), true
	}
	return s.Copy(), false
}
func (s CVForgeString) GetEveryTag() []string {
	tags := []string{}
	if s.Tags != nil {
		tags = append(tags, s.Tags...)
	}
	return tags
}
func (s CVForgeString) Copy() CVBase {
	return CVForgeString{
		CVTagInfo: s.CVTagInfo,
		Value:     s.Value,
	}
}
