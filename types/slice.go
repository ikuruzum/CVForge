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
func (cs CVForgeSlice) Filter(tags []string) (data CVBase, passed bool) {
	s := cs.Copy().(CVForgeSlice)
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

func (s CVForgeSlice) GetEveryTag() []string {
	tags := make([]string, 0)
	for _, v := range s.Value {
		vTags := v.GetEveryTag()
		for _, tag := range vTags {
			if !slices.Contains(tags, tag) {
				tags = append(tags, tag)
			}
		}
	}
	return tags
}
func (s CVForgeSlice) Copy() CVBase {
	cvs := make([]CVBase, len(s.Value))
	for i := 0; i < len(s.Value); i++ {
		cvs[i] = s.Value[i].Copy()
	}
	return CVForgeSlice{
		CVTagInfo: s.CVTagInfo,
		Value:     cvs,
	}
}
