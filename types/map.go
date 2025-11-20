package types

import (
	"slices"
	"strings"
)


type CVForgeMap struct {
	CVTagInfo
	Value map[string]CVBase
}

func MakeCVForgeMap(value any, inheritedCVTagInfo CVTagInfo) (CVForgeMap, bool) {
	info := DefaultCVTagInfo();
	info.inherit(inheritedCVTagInfo)
	if value == nil {
		return CVForgeMap{CVTagInfo: info}, false
	}
	if m, ok := value.(map[string]any); ok {
		info = CVTagInfoFromMap(m)
		info.inherit(inheritedCVTagInfo)
		cvm := make(map[string]CVBase)
		for k, v := range m {
			if len(strings.TrimSpace(k)) == 0 {
				continue
			}
			if cvb, ok := UnmarshalCVBase(v,info); ok {
				cvm[k] = cvb
			}
		}
		return CVForgeMap{
			CVTagInfo: info,
			Value:     cvm,
		}, true
	}
	return CVForgeMap{CVTagInfo: info}, false
}

func (cm CVForgeMap) Filter(tags []string) (data CVBase, passed bool) {
	m := cm.Copy().(CVForgeMap)
    keys := make([]string, 0)
	for k := range m.Value {
		keys = append(keys, k)
	}
	for i:=0; i < len(keys); i++ {
		key := keys[i]
		value, passed := m.Value[key].Filter(tags)
		if passed {
			m.Value[key] = value
		} else {
			delete(m.Value, key)
		}
	}
	return m, m.FilterPass(tags)
}
func (m CVForgeMap) GetEveryTag() []string {
	tags := m.Tags[:]
	for _, v := range m.Value {
		vTags := v.GetEveryTag()
		for _, tag := range vTags {
			if !slices.Contains(tags, tag) {
				tags = append(tags, tag)
			}
		}
	}
	return tags
}
func (m CVForgeMap) Copy() CVBase {
	cvm := make(map[string]CVBase)
	for k, v := range m.Value {
		cvm[k] = v.Copy()
	}
	return CVForgeMap{
		CVTagInfo: m.CVTagInfo,
		Value:     cvm,
	}
}
