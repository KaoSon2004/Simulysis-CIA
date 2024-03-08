var SystemUtil = {
	getReferencedSystemId(refPath, systems) {
		var modelNames = refPath.split('/')
		var parentId = 0
		var referencedSys = null

		modelNames.forEach(modelName => {
			referencedSys = systems.find(
				system => system.fK_ParentSystemId == parentId + 1 && system.name == modelName
			)

			parentId = referencedSys.id
		})

		return parentId
	},
	isCalibration(sourceBlock) {
		return sourceBlock && sourceBlock.includes('MC_')
	},
	notLibrary(sourceFile) {
		return !sourceFile.includes('simulink')
	},
	isSubSystem(blockType) {
		return blockType === 'SubSystem'
	},
	isReference(blockType) {
		return blockType === 'Reference'
	},
	isModelReference(blockType) {
		return blockType === 'ModelReference'
	},
	isRefToFile(blockType, sourceFile) {
		return this.isModelReference(blockType) || (this.isReference(blockType) && this.notLibrary(sourceFile))
	},
	getAllSystemsPorts(systems) {
		return systems.reduce(
			(portsObj, system) => ({
				...portsObj,
				[system.fK_ParentSystemId - 1]: system.blockType?.includes?.('port')
					? [...(portsObj[system.fK_ParentSystemId - 1] ?? []), system]
					: portsObj[system.fK_ParentSystemId - 1] ?? []
			}),
			{}
		)
	},
	systemDrawsToSidNameObj(systemDraws) {
		return Object.assign(
			{},
			...systemDraws.map(systemDraw => ({ [systemDraw.sid]: systemDraw, [systemDraw.name]: systemDraw }))
		)
	},
	branchesToObj(branches) {
		var branchObj =  branches.reduce(
			(branchObj, branch) => {

				var key = branch.fK_LineId ? 'line' + branch.fK_LineId : 'branch' + branch.fK_BranchId
				return ({
					...branchObj,
					[key]: [
						...(branchObj[key] ?? []),
						branch
					]
				})
			},
			{}
		)
		
		return branchObj
	},
	getSystemInstanceDatas(instanceDatas, id) {
		if (instanceDatas != null) return instanceDatas.filter(data => data.fK_SystemId == id)
		else return 0
	},
	getSystemLists(lists, id) {
		return lists
			.filter(list => list.fK_SystemId == id)
			.map(list => ({ ...list, props: JSON.parse(list.properties) }))
	},
	getHigherSystemId(systems, currentSystemId) {
		if (typeof currentSystemId == 'undefined') return [-1, -1]

		if (currentSystemId == 0) return [0, 0]

		var currentSystem = systems.find(system => system.id == currentSystemId)

		if (!currentSystem) {
			console.error('Cannot find current system')
			console.log(systems, currentSystemId)
			return [-1, -1]
		}

		var parentSys = systems.find(system => system.id == currentSystem.fK_ParentSystemId - 1)

		return [parentSys?.id ?? 0, parentSys?.fK_FakeProjectFileId ?? -1]
	},
	getLevelContent({ systems, lines }, highLevelParentId) {
		var levelSystems = systems.filter(system => system.fK_ParentSystemId == highLevelParentId + 1)
		var levelLines = lines.filter(line => line.fK_SystemId == highLevelParentId + 1)

		if (levelSystems.length == 0) console.warn('Empty systems')
		if (levelLines.length == 0) console.warn('Empty lines')
		//if (levelLines.length == 0 && levelSystems.length == 0) console.log(systems, lines, highLevelParentId)

		return { systems: levelSystems, lines: levelLines }
	},
	getLevelSubSystems(systems, parentId) {
		return systems.filter(
			({ blockType, fK_ParentSystemId }) => this.isSubSystem(blockType) && fK_ParentSystemId == parentId + 1
		)
	},
	getSystemName(systems, systemId) {
		return (systems.find(system => system.id == systemId) ?? {}).name
	}
}

export default SystemUtil
