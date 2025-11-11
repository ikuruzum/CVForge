package types

type CVForgeMap struct {
	CVTagInfo
	Value map[string]CVBase
}

func MakeCVForgeMap(value any) (CVForgeMap, bool) {
	if value == nil {
		return CVForgeMap{}, false
	}
	if m, ok := value.(map[string]any); ok {
		if m["value"] == nil {
			return CVForgeMap{}, false
		}
		cvm := make(map[string]CVBase)
		for k, v := range m {
			if cvb, ok := UnmarshalCVBase(v); ok {
				cvm[k] = cvb
			}
		}
		return CVForgeMap{
			CVTagInfo: CVTagInfoFromMap(m),
			Value:     cvm,
		}, true
	}
	return CVForgeMap{}, false
}

func (m CVForgeMap) Filter(tags []string) (data CVBase, passed bool) {
	if m.FilterPass(tags) {
		return m, true
	}
	keys := make([]string, 0)
	for k := range m.Value {
		keys = append(keys, k)
	}
	for i := 0; i < len(keys); i++ {
		key := keys[i]
		_, passed = m.Value[key].Filter(tags)
		if !passed {
			delete(m.Value, key)
		}
	}
	return m,  len(m.Value) > 0
}
