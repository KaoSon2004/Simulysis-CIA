import FileUtil from './file.js'

var FileRelationshipsUtil = {
	// get relationships with distinct fileId1 and fileId2
	distinct(relationships) {
		return relationships.filter(
			(relationship, i, selfArr) =>
				selfArr.indexOf(
					selfArr.find(
						otherRelationship =>
							(otherRelationship.fK_ProjectFileId1 == relationship.fK_ProjectFileId1 &&
								otherRelationship.fK_ProjectFileId2 == relationship.fK_ProjectFileId2) ||
							(otherRelationship.fK_ProjectFileId1 == relationship.fK_ProjectFileId2 &&
								otherRelationship.fK_ProjectFileId2 == relationship.fK_ProjectFileId1)
					)
				) == i
		)
	},
	toCountList(relaList) {
		return relaList.map(relationship => relationship.uniCount)
	},
	// list of relationships of all displayed files to other displayed files in view
	getRelationshipsBetweenMainDisplayFiles(mainRelationships, subRelaObj, mainFileId) {
		return mainRelationships.map(relationship =>
			this.inView(
				this.sub(subRelaObj, this.getSubFileId(relationship, mainFileId)),
				mainRelationships,
				mainFileId
			)
		)
	},
	getRecursiveChildrenRelationships(
		mainRelationships,
		subRelaObj,
		relaOpts,
		showChildrenLibraries,
		mainFileId,
		files
	) {
		if (!relaOpts[0] ?? true) {
			return []
		}

		var finalRelaArr = []

		var getRecursiveImpl = (relaArr, id, subRelaObj, relaOpts, currentLvl) => {
			if (!relaOpts[currentLvl] ?? true) {
				return []
			}

			var arr = []

			relaArr.forEach(rela => {
				const childId = this.getSubFileId(rela, id)

				// next level children
				var childrenOfChild = this.sub(subRelaObj, childId).filter(relaSub => this.isChild(relaSub, childId))

				if (showChildrenLibraries) {
					childrenOfChild = childrenOfChild.filter(relaSub =>
						FileUtil.isProjectFileCommonLibrary(files, this.getSubFileId(relaSub, childId))
					)
				}

				arr = arr
					.concat(childrenOfChild)
					.concat(getRecursiveImpl(childrenOfChild, childId, subRelaObj, relaOpts, currentLvl + 1))
			})

			return arr
		}

		mainRelationships.forEach(relationship => {
			// Check direct children and equal
			if (this.isChild(relationship, mainFileId) || relationship.type == 0) {
				const subFileId = this.getSubFileId(relationship, mainFileId)

				// get direct children of sub file
				var allChildren = this.sub(subRelaObj, subFileId).filter(relaSub => this.isChild(relaSub, subFileId))

				finalRelaArr = finalRelaArr
					.concat(allChildren)
					.concat(getRecursiveImpl(allChildren, subFileId, subRelaObj, relaOpts, 1))
			}
		})

		return finalRelaArr
	},
	// get list of relationships that match current display modes
	getDisplayRelationships(
		mainRelationships,
		displayChildren,
		displayEquals,
		displayParents,
		displayChildLibraries,
		//displaySubsystemAsChild,
		mainFileId,
		files
	) {
		return mainRelationships.filter(relationship => {
			if (this.isChild(relationship, mainFileId)) {
				if (displayChildren) {
					//if (relationship.System1) {
					//	return displaySubsystemAsChild
					//}
					if (FileUtil.isProjectFileCommonLibrary(files, this.getSubFileId(relationship, mainFileId))) {
						return displayChildLibraries
					}
					return true
				}
				return false
			}
			if (this.isParent(relationship, mainFileId)) return displayParents
			return displayEquals
		})
	},
	// get file id of the relationship that is not the id of the main file (file in middle)
	getSubFileId(relationship, mainFileId) {
		return relationship.fK_ProjectFileId1 == mainFileId
			? relationship.fK_ProjectFileId2
			: relationship.fK_ProjectFileId1
	},
	// a relationship is in-view only if the sub-relationships object contains both file ids as keys
	inView(subRelationships, mainRelationships, mainFileId) {
		var inViewIds = mainRelationships.map(relationship => this.getSubFileId(relationship, mainFileId))
		return subRelationships.filter(
			relationship =>
				inViewIds.includes(relationship.fK_ProjectFileId1) &&
				inViewIds.includes(relationship.fK_ProjectFileId2)
		)
	},
	// return relationships of a sub file to other sub files
	sub(subRelaObj, subFileId) {
		return subRelaObj[subFileId].filter(obj => obj.fK_ProjectFileId1 != obj.fK_ProjectFileId2)
	},
	// return all relationships drawn on site
	all(mainRelationships, inViewSubRelationships) {
		return mainRelationships.concat(inViewSubRelationships.flat())
	},
	// new relationship list with parent relationship at index 0
	parentToTop(mainRelationships, mainFileId) {
		const index = mainRelationships.findIndex(relationship => this.isParent(relationship, mainFileId))

		if (index == -1) return mainRelationships

		var newRelationships = [...mainRelationships]
		;[newRelationships[0], newRelationships[index]] = [newRelationships[index], newRelationships[0]]
		return newRelationships
	},
	getFromToName(first, second, relationship, firstId) {
		return this.firstNameIsFrom(relationship, firstId) ? [first, second] : [second, first]
	},
	isParentChild(relationship) {
		return relationship.type == 1
	},
	isParent(relationship, fileId) {
		return relationship.type == 1 && relationship.fK_ProjectFileId1 == fileId
	},
	isChild(relationship, fileId) {
		return relationship.type == 1 && relationship.fK_ProjectFileId2 == fileId
	},
	firstNameIsFrom(relationship, firstId) {
		return firstId == relationship.fK_ProjectFileId1
	},
	isNotInRelationship(relationship, fileId) {
		return fileId != relationship.fK_ProjectFileId1 && fileId != relationship.fK_ProjectFileId2
	},
	subInFolder(relationships, files, folderName, mainFile) {
		const pathToCheck = mainFile.containingFilePath ?? mainFile.path

		return relationships.filter(relationship => {
			const subFileId = this.getSubFileId(relationship, mainFile.id)
			var subFile = FileUtil.file(files, subFileId)

			// this is for 2 cases
			// main file is a subsystem (fake file) => other subsystem in the containing file and the containing file will not be filtered out
			// main file is real => subsystem in that file will not be filtered out
			return (
				subFile.containingFilePath == pathToCheck ||
				subFile.path == pathToCheck ||
				FileUtil.isInFolder(subFile, folderName)
			)
		})
	},
	bothInFolder(relationships, files, folderName, mainFile) {
		const pathToCheck = mainFile.containingFilePath ?? mainFile.path

		return relationships.filter(relationship => {
			var file1 = FileUtil.file(files, relationship.fK_ProjectFileId1)
			var file2 = FileUtil.file(files, relationship.fK_ProjectFileId2)

			// same checking as the above function
			const file1InFolder =
				file1.containingFilePath == pathToCheck ||
				file1.path == pathToCheck ||
				FileUtil.isInFolder(file1, folderName)
			const file2InFolder =
				file2.containingFilePath == pathToCheck ||
				file2.path == pathToCheck ||
				FileUtil.isInFolder(file2, folderName)

			return file1InFolder && file2InFolder
		})
	}
}

export default FileRelationshipsUtil
