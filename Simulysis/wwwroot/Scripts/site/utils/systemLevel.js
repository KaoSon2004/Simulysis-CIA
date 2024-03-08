var SystemLevelUtil = {
	dict: [
		{
			level: 0,
			name: 'ECU'
		},
		{
			level: 1,
			name: 'MFMdl'
		},
		{
			level: 2,
			name: 'Function'
		},
		{
			level: 3,
			name: 'Logic'
		},
		{
			level: 4,
			name: 'Block'
		},
		{
			level: 5,
			name: 'Block Level 5'
		},
		{
			level: 6,
			name: 'Block Level 6'
		},
		{
			level: 7,
			name: 'Block Level 7'
		},
		{
			level: 8,
			name: 'Block Level 8'
		},
		{
			level: 9,
			name: 'Block Level 9'
		}
	],
	mapLevelToName(level) {
		if (typeof level == 'number' && level >= 0 && level < 10) {
			return this.dict.find(pair => pair.level == level).name
		} else {
			console.error('Invalid level provided!')
			console.log(level)
		}
	},
	mapNameToLevel(name) {
		if (typeof name == 'string') {
			let pair = this.dict.find(p => p.name.toLowerCase() == name.toLowerCase())

			if (pair) {
				return pair.level
			} else {
				console.error('Invalid name provided!')
				console.log(name)
			}
		} else {
			console.error('Invalid name provided!')
			console.log(name)
		}
	},
	getSystemLevel(textId) {
		return $(`#${textId}`).text()
	},
	updateSystemLevelText(textId, level) {
		$(`#${textId}`).text(this.mapLevelToName(level))
	}
}

export default SystemLevelUtil
