var FileUtil = {
	name(files, fileId) {
		var file = files.find(file => file.id == fileId)
		return file?.name
	},
	nameWithoutExt(name) {
		return name.endsWith('.mdl') || name.endsWith('.slx') ? name.slice(0, -4) : name
	},
	toFolder(path) {
		return `\\${path.split('\\').slice(2, -1).join('\\')}`
	},
	isInFolder(file, folderName) {
		const pathToCheck = file.containingFilePath ?? file.path
		return pathToCheck.includes(folderName)
	},
	parentFolder(file) {
		const pathToGetFolder = file.containingFilePath ?? file.path
		return pathToGetFolder.replace(file.name, '').split('\\').at(-2)
	},
	file(files, fileId) {
		return files.find(file => file.id == fileId)
	},
	findIdFromName(files, name) {
		if (typeof name != 'string') return null

		//console.log(files);
		//console.log(name);

		var foundFile = files.find(file => file.name == name)

		if (!foundFile) {
			console.log("Not found");
			return null
		}

		return foundFile.id
	},
	isFake(file) {
		return !file.path.startsWith('\\')
	},
	getRealFileNameFromFake(file) {
		var parts = file.path.split(':')[0].split('/');
		return parts[parts.length - 1];
	},
	getContainingFilePath(files, file) {
		return this.file(files, this.findIdFromName(files, this.getRealFileNameFromFake(file))).path
	},
	isProjectFileCommonLibrary(files, fileId) {
		var file = files.find(file => file.id == fileId)
		if (!file) {
			return false
		}

		if (this.nameWithoutExt(file.name) == 'lib2011b') {
			console.log('Find lib2011b file! Tagging as common library')
			return true
		}

		if (file.path.includes('SUB_SYSTEM_LIB')) {
			console.log(
				'File that resides in SUB_SYSTEM_LIB found (' +
					file.name +
					"). Checking if it's MFMdl and library variant to hide"
			)

			if (file.systemLevel == 'MFMdl' && file.levelVariant == 'Library') {
				console.log('SUB_SYSTEM_LIB model library found name=' + file.name)
				return true
			}
		}

		return false
	}
}

export default FileUtil
