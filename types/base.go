package types


type CVBase interface{
	Filter(tags []string) (data CVBase, passed bool)
}

func UnmarshalCVBase (value any) (CVBase, bool) {
	var val CVBase
	val, ok := MakeCVForgeString(value)
	if ok {
		return val, true
	}
	val, ok = MakeCVForgeSlice(value)
	if ok {
		return val, true
	}
	val, ok = MakeCVForgeMap(value)
	if ok {
		return val, true
	}
	return nil, false
}