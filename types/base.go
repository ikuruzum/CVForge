package types


type CVBase interface{
	Filter(tags []string) (data CVBase, passed bool)
	GetEveryTag() []string
	Copy() CVBase
}

func UnmarshalCVBase (value any, inheritedCVTagInfo CVTagInfo) (CVBase, bool) {
	var val CVBase
	val, ok := MakeCVForgeString(value, inheritedCVTagInfo)
	if ok {
		return val, true
	}
	val, ok = MakeCVForgeSlice(value, inheritedCVTagInfo)
	if ok {
		return val, true
	}
	val, ok = MakeCVForgeMap(value, inheritedCVTagInfo)
	if ok {
		return val, true
	}
	return nil, false
}
