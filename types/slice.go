package types

import "slices"

type CVForgeSlice struct {
	CVTagInfo
	Value []CVBase
}

func MakeCVForgeSlice(value any) (CVForgeSlice, bool) {
	if value == nil {
		return CVForgeSlice{}, false
	}

	var slm []CVBase
	if sl, ok := value.([]any); ok {
		for i := 0; i < len(sl); i++ {
			if cvb, ok := UnmarshalCVBase(sl[i]); ok {
				slm = append(slm, cvb)
			}
		}
	}
	if m, ok := value.(map[string]any); ok && m["value"] != nil {
		if sl, ok := m["value"].([]any); ok {
			for i := 0; i < len(sl); i++ {
				if cvb, ok := UnmarshalCVBase(sl[i]); ok {
					slm = append(slm, cvb)
				}
			}
		}
		return CVForgeSlice{
			CVTagInfo: CVTagInfoFromMap(m),
			Value:     slm,
		}, true
	}
	return CVForgeSlice{
		Value: slm,
	}, true
}
func (s CVForgeSlice) Filter(tags []string) (data CVBase, passed bool) {
	if s.FilterPass(tags) {
		return s, true
	}
	for i := 0; i < len(s.Value); i++ {
		_, passed = s.Value[i].Filter(tags)
		if !passed {
			s.Value = slices.Delete(s.Value, i, 1)
			i--
		}
	}
	return s, len(s.Value) > 0
}
