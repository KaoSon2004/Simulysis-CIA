import LineUtil from "../../utils/line.js";
import FileContentsAPI from "../../api/fileContents.js";
import { noop } from "../../noop.js";
import SystemUtil from "../../utils/system.js";
var TracerState = {
	/*
	* Properties
	*/
	stack: [],
	foundBlocks: [],
	foundLines: [],
	foundBranches: [],
	modelDraw: {},
	traceSignalLevelContents: {},
	currentSignalLevel: -1,
	depthLevel: -1,
	drawCachesByName: {},
	drawCachesByFileId: {},
	fileContentCache: {},

	init(modelDraw) {
		this.stack = [];
		this.foundBlocks = [];
		this.foundLines = [];
		this.foundBranches = [];
		this.traceSignalLevelContents = {};
		this.depthLevel = -1;
		this.currentSignalLevel = -1;

		this.modelDraw = modelDraw;
	},
	addStack(element) {
		if (element) {
			return this.stack.push(element);
		}

	},

	removeStack() {
		if (this.stack.length > 0) {
			return this.stack.pop();
		}
	},

	isEmptyStack() {
		return this.stack.length == 0;
	},

	clearStack() {
		this.stack = [];
	},
	checkIfFoundBlocksDetails(entity, arrr) {
		let ans = false

		arrr.forEach(block => {
			if (
				block != undefined &&
				block.sid == entity.sid &&
				block.id == entity.id &&
				block.name == entity.name &&
				block.props.Position == entity.props.Position &&
				block.blockType == entity.blockType
			) {
				ans = true
			}
		})
		return ans
	},
	checkIfFoundBlocks(entity) {
		return this.checkIfFoundBlocksDetails(entity, this.foundBlocks)
	},
	checkIfFoundLines(entity) {
		let ans = false
		if (typeof entity == 'undefined') return ans;
		this.foundLines.forEach(line => {
			if (
				line.id == entity.id &&
				JSON.stringify(line.points[0]) == JSON.stringify(entity.points[0]) &&
				JSON.stringify(line.points[0]) == JSON.stringify(entity.points[0])
			) {
				ans = true
			}
		})

		return ans
	},
	checkIfFoundBranches(entity) {
		let ans = false
		this.foundBranches.forEach(branch => {
			if (
				branch.id == entity.id &&
				branch.props.Dst == entity.props.Dst &&
				branch.props.Points == entity.props.Points
			) {
				ans = true
			}
		})
		return ans;
	},

	spreadInfor(entity, systemDraws, lineDraws, branchDraws, prevEntity ) {
		if (!entity) {
			return;
		}
/*		s = this.state.spreadInfor(
			s, systems, lines, branches
			, system.getParentSystem()
			, system.getResponse()
			, system.getCurrentDeepLevel()
			, system.getTraceDeepLevel()
			, system.getFileContent()
			, system.getRootSysId()
		);*/

		entity.initListObjDraws(systemDraws, lineDraws, branchDraws);
		entity.setParentSystem(prevEntity.getParentSystem());
		entity.setCurrentDeepLevel(prevEntity.getCurrentDeepLevel());
		entity.setTraceDeepLevel(prevEntity.getTraceDeepLevel());
		entity.setFileContent(prevEntity.getFileContent());
		entity.setRootSysId(prevEntity.getRootSysId());
		return entity;
	},
	findLineByStringId(lines, type, system) {
		return lines.filter(line => {
			return line[type]?.stringId == system.stringId
		})
	},
	findLineByPort(lines, type, portType, port, system,) {
		return lines.find(line => {
			return line[portType] == port && system.stringId == line[type].stringId
		})
	},
	findBranchByStringId(branches, type, system) {
		return branches.filter(branch => {
			return branch[type]?.stringId == system.stringId
		})
	},
	findBranchByPort(branches, type, portType, port, system,) {
		return branches.find(branch => {
			return branch[portType] == port && system.stringId == branch[type].stringId
		})
	},
	findSystemByPort(systems, obj, reversedDirection) {
		var type = reversedDirection ? 'srcPort' : 'dstPort'
		var blockType = reversedDirection ? 'Outport' : 'Inport'
		var blockTypeShadow = reversedDirection ? 'Outport' : 'InportShadow'
		return systems.find(
			system =>
				Number((system.props.Port ?? 1) == obj[type] ?? 1) &&
				(system.blockType == blockType || system.blockType == blockTypeShadow)
		)
	},

	findSystem(lineResult, systems, lines, branches, reversedDirection = false) {
		let type = reversedDirection ? 'src' : 'dst';

		let sys = systems.find(system => {
			return system?.stringId == lineResult[type]?.stringId
		});

		if (!sys) {
			return null
		}

		sys = this.spreadInfor(
			sys, systems, lines, branches, lineResult
		);
		var newSys = Object.create(sys);
		newSys.setCurrentTraceLevel(lineResult.getCurrentTraceLevel());
		return newSys;
	},
	findLineResult(system, systems, lines, branches, reversedDirection = false) {

		var type = reversedDirection ? 'dst' : 'src'

		var lineResults = [];
		var newLineResults = [];
		var num = 0;
		const { cx: rootCx, cy: rootCy } = system.treeCoordinate;
		lineResults = this.findLineByStringId(lines, type, system);
		if (lineResults.length > 1) {
			num = lineResults.length;
		}
		lineResults.forEach(lineResult => {
			lineResult = this.spreadInfor(
				lineResult, systems, lines, branches, system
			)
			var newLineResult = Object.create(lineResult);

			newLineResult?.setCurrentTraceLevel(system.getCurrentTraceLevel() + num);
			newLineResult.rootObj = system;

			newLineResult.rootCoor = { cx: rootCx, cy: rootCy }

			num--
			newLineResults.push(newLineResult);
		})
		var result = [...newLineResults]

		return [...new Set(result)]
	},
	findBranchResult(system, systems, lines, branches, reversedDirection = false, plusTraceLevel) {
		var type = reversedDirection ? 'dst' : 'src';
		var branchResults = [];
		var newBranchResults = [];
		const { cx: rootCx, cy: rootCy } = system.treeCoordinate;

		branchResults = this.findBranchByStringId(branches, type, system);
		branchResults.forEach(branchResult => {

			branchResult = this.spreadInfor(
				branchResult, systems, lines, branches, system

			);
			var newBranchResult = Object.create(branchResult);
			newBranchResult.setCurrentTraceLevel(system.getCurrentTraceLevel() + plusTraceLevel);
			plusTraceLevel++;
			newBranchResult.rootObj = system;
			newBranchResult.rootCoor = { cx: rootCx, cy: rootCy };
			newBranchResults.push(newBranchResult);
		})
		return newBranchResults
	},
	filterAndInitDraws(draws, systemDraws, lineDraws, branchDraws
		, block, reversedDirection, depthLevel, currentTraceLevel) {
		const type = reversedDirection == true ? 'dst' : 'src';
		var filterResults = [];
		filterResults = draws.filter(draw => {
			return draw[type]?.stringId == block.stringId
		})

		filterResults.forEach(filterResult => {
			filterResult?.initListObjDraws(systemDraws, lineDraws, branchDraws);
			filterResult?.setCurrentDeepLevel(depthLevel);
			filterResult?.setCurrentTraceLevel(currentTraceLevel);

			if (filterResult) {
				filterResult.originSystem = block;
			}

			this.addStack(filterResult);

		})
		return filterResults;
	},
	async fetchingFileContentById(projectFileId, sid = 0) {
		let drawCacheKey = `${projectFileId}_${sid}`
		if (this.drawCachesByFileId[drawCacheKey]) {
			return this.drawCachesByFileId[drawCacheKey]
		}
		else {
			if (!this.fileContentCache[projectFileId]) {
				this.fileContentCache[projectFileId] = await FileContentsAPI.getFileContentById(projectFileId)
			}

			let { response } = this.fileContentCache[projectFileId]

			var { systemDraws: newSystems, lineDraws: newLines } = this.modelDraw.initDrawEntities(false, response, (sid == 0) ? null : sid)
			let cacheEntry = { newSystems, newLines, response }

			this.drawCachesByFileId[`${response.fileId}_${sid}`] = cacheEntry
			return cacheEntry
		}
	},
	async fetchingFileContent(s, sid = 0) {
		let drawCacheKey = `${s.sourceFile}_${sid}`
		if (this.drawCachesByName[drawCacheKey]) {
			return this.drawCachesByName[drawCacheKey]
		}
		else {
			var { response } = await FileContentsAPI.getFileContentByName({
				projId: $('#projectId').val(),
				fileName: s.sourceFile
			})
			this.traceSignalLevelContents[`level${this.currentSignalLevel++}`] = response;

			var { systemDraws: newSystems, lineDraws: newLines } = this.modelDraw.initDrawEntities(false, response, (sid == 0) ? null : sid)
			let cacheEntry = { newSystems, newLines, response }

			this.drawCachesByName[drawCacheKey] = cacheEntry
			this.drawCachesByFileId[`${response.fileId}_${sid}`] = cacheEntry

			return cacheEntry
		}
	},
	fetchingDrawEntities(fileContent, sid = 0) {
		let drawCacheKey = `${fileContent.fileId}_${sid}`
		if (this.drawCachesByFileId[drawCacheKey]) {
			return this.drawCachesByFileId[drawCacheKey]
		}
		else {
			var { systemDraws: newSystems, lineDraws: newLines } = this.modelDraw.initDrawEntities(false, fileContent, (sid == 0) ? null : sid)
			let cacheEntry = { newSystems, newLines, response: fileContent }
			
			this.drawCachesByFileId[`${fileContent.fileId}_${sid}`] = cacheEntry
			return cacheEntry
		}
	},
	resetState() {
		this.stack = [];
		this.foundBlocks = [];
		this.foundLines = [];
		this.foundBranches = [];
		this.traceSignalLevelContents = {};
		this.depthLevel = -1;
		this.currentSignalLevel = -1;
	},
	findBranchFromBranch(branchObj, systems, lines, branches) {
		var branchresult;
		var newBranchResult;
		const { cx: rootCx, cy: rootCy } = branchObj.treeCoordinate;
		branches.forEach(branch => {
			if (branch.branchDraws.length != 0) {
				branch.branchDraws.forEach(childBranch => {
					if (childBranch.stringId == branchObj.stringId) {
						
						branchresult = branch
						branchresult = this.spreadInfor(
							branchresult, systems, lines, branches, branchObj
						)
						newBranchResult = Object.create(branchresult);
						newBranchResult.setCurrentTraceLevel(branchObj.getCurrentTraceLevel());
						newBranchResult.rootObj = branchObj;
						newBranchResult.rootCoor = { cx: rootCx, cy: rootCy }
						
						
					}
				})
			}
		})
		return newBranchResult

	},
	findLineFromBranch(branchObj, systems, lines, branches) {
		var lineResult;
		const { cx: rootCx, cy: rootCy } = branchObj.treeCoordinate;
		var newLineResult;
		lines.forEach(line => {
			if (line.branchDraws.length != 0) {
				line.branchDraws.forEach(childBranch => {
					if (childBranch.stringId == branchObj.stringId) {
						lineResult = line

						lineResult = this.spreadInfor(
							lineResult, systems, lines, branches, branchObj
						);
						newLineResult = Object.create(lineResult);
						newLineResult.setCurrentTraceLevel(branchObj.getCurrentTraceLevel());
						newLineResult.rootObj = branchObj;
						newLineResult.rootCoor = { cx: rootCx, cy: rootCy }
					}
				})
			}
		})
		return newLineResult;
	},
}

export default TracerState;