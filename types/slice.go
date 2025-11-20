package types

import "slices"

type CVForgeSlice struct {
	CVTagInfo
	Value []CVBase
}

func MakeCVForgeSlice(value any, inheritedCVTagInfo CVTagInfo) (CVForgeSlice, bool) {
	info := DefaultCVTagInfo()
	info.inherit(inheritedCVTagInfo)
	if value == nil {
		return CVForgeSlice{CVTagInfo:info}, false
	}

	var slm []CVBase
	if sl, ok := value.([]any); ok {
		for i := 0; i < len(sl); i++ {
			if cvb, ok := UnmarshalCVBase(sl[i],info); ok {
				slm = append(slm, cvb)
			}
		}
	}
	if m, ok := value.(map[string]any); ok && m["value"] != nil {
		info = CVTagInfoFromMap(m)
		info.inherit(inheritedCVTagInfo)
		if sl, ok := m["value"].([]any); ok {
			for i := 0; i < len(sl); i++ {
				if cvb, ok := UnmarshalCVBase(sl[i],info); ok {
					slm = append(slm, cvb)
				}
			}
		}
		return CVForgeSlice{
			CVTagInfo: info,
			Value:     slm,
		}, true
	}
	return CVForgeSlice{
		CVTagInfo: info,
		Value:     slm,
	}, len(slm) > 0
}
func (cs CVForgeSlice) Filter(tags []string) (data CVBase, passed bool) {
	s := cs.Copy().(CVForgeSlice)
	filtered := s.Value[:0] // aynı array'i kullanır, GC yok
	for i := range s.Value {
		f, passed := s.Value[i].Filter(tags)
		if passed {
			filtered = append(filtered, f)
		}
	}
	s.Value = filtered
	return s, s.FilterPass(tags)
}

func (s CVForgeSlice) GetEveryTag() []string {
	tags := s.Tags[:]
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
