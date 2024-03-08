import SystemUtil from "../../utils/system.js";
import LineUtil from "../../utils/line.js";
import Tracer from "../../draw/common/tracer.js";

var Algorithm = {
	__proto__: Tracer,

	/*
	* Properties
	*/
	traceSignalTree: {},
	map: {},
	/*
	* Methods
	*/
	init(utils, modelDraw, traceSignalTree, levelClickContents) {
		this.__proto__.init(utils, modelDraw, levelClickContents)
		this.traceSignalTree = traceSignalTree;

		this.modelDraw = modelDraw;
		this.levelClickContents = levelClickContents;
		this.map = {};

	},
	async TrackLineLoop(reversedDirection) {
		if (this.state.isEmptyStack()) {
				if ($('#trackInput').val().trim() == "") {
					$("#loaderTraceSignal").removeClass("loaderTraceSignal");
					$("#loaderTraceSignal").html("");
					$("#traceSignalResult").text("");
					return;
				}
				$("#traceSignalResult").text("Success");
				$("#loaderTraceSignal").removeClass(
					"loaderTraceSignal"
				)
				$("#loaderTraceSignal").html(
					"<i class=\"fa fa-check\" ></i>"
				)


				this.traceSignalTree.appendNode(null, true);

				return;
		}
		var obj = this.state.removeStack();

		if (obj.type == 'system') {
			if (!this.state.checkIfFoundBlocks(obj)) {

				var s = obj;
				var blockTypeOutGF = reversedDirection ? 'From' : 'Goto'
				this.state.drawOnSignalTree(s, false);
				if (s.blockType == blockTypeOutGF) {
					this.highlightGoto(s, s.props.GotoTag, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection)
				} 
				/*
				* process outport block
				* cache outport result
				*/
				var blockType = reversedDirection ? 'Inport' : 'Outport'
				if (s.blockType == blockType) {
					const outportNum = Number(s.props.Port ?? 1)
					this.traceBackOutports(s, Array(1).fill(outportNum));

					var parentSystem = s.getParentSystem();
					if (parentSystem != null) {

						if (!parentSystem.tracedOutport?.includes(s.props.Port ?? 1)) {
							var lineResult = this.state.findLineByPort(s, parentSystem.lineDraws, reversedDirection, parentSystem);
							this.processPortResult(lineResult,parentSystem);

							if (reversedDirection) {
								var branchResult = this.state.findBranchByPort(s, parentSystem.branchDrawsLevel, reversedDirection, parentSystem);
								this.processPortResult(branchResult, parentSystem)
							}

							if (!parentSystem.tracedOutport) {
								parentSystem.tracedOutport = [];
							}
							parentSystem.tracedOutport.push(s.props.Port ?? 1);
						}

						 
					}
					/*this.handleInOutPort(s, reversedDirection);*/
				}

				else {
					if (s.blockType != blockTypeOutGF) {
						var lineResults = this.state.findLineResult(s, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection)

						// If searching in reserverd direction, node nexts to
						// a system mayble a branch

						if (reversedDirection) {

							var branchResults = this.state.findBranchResult(s, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection);

							branchResults?.forEach(branchResult => {
								if (!this.state.checkIfFoundBranches(branchResult)) {
									this.state.addStack(branchResult);
								}
							})
						}


						lineResults.forEach(lineResult => {
							this.state.addStack(lineResult);
						})
					}
				}
				this.state.foundBlocks.push(s);
			}
			else {

				this.processLoopObj(obj, reversedDirection);
			}



		}

		if (obj.type == 'line') {

			if (!this.state.checkIfFoundLines(obj)) {
				var lineResult = obj;
				this.state.drawOnSignalTree(lineResult)
				///if searching in direct direction find prev system and connect 2 nodes on grapj
				if (!reversedDirection) {

					if (lineResult.branchDraws.length != 0) {
						this.highlightBranches(lineResult, lineResult.branchDraws, lineResult.systemDraws, lineResult.lineDraws)
					} else {


						var s = this.state.findSystem(lineResult, lineResult.systemDraws, lineResult.lineDraws, lineResult.branchDrawsLevel, reversedDirection)
						var newS = Object.create(s);
						if (SystemUtil.isRefToFile(newS.blockType, newS.sourceFile) && newS.sourceFile != '') {
							await this.handleModelReference(newS, obj, reversedDirection,)
						}
					
						else if (newS.blockType == 'SubSystem') {
							await this.handleSubsystem(newS, obj, reversedDirection, obj.getResponse(),)
						}
						else {
							this.state.addStack(newS);
						}
					}

				}





				else {
					var s = this.state.findSystem(lineResult, lineResult.systemDraws, lineResult.lineDraws, lineResult.branchDrawsLevel, reversedDirection)
					var newS = Object.create(s);
					if (SystemUtil.isRefToFile(newS.blockType, newS.sourceFile) && newS.sourceFile != '') {
						await this.handleModelReference(newS, obj, reversedDirection,)
					}
					else if (newS.blockType == 'SubSystem') {
						await this.handleSubsystem(newS, obj, reversedDirection, obj.getResponse(),)
					}
					else {
						this.state.addStack(newS);
					}


				}
				this.state.foundLines.push(lineResult)
			}
			else {
				this.processLoopObj(obj, reversedDirection);
			}
		}
		if (obj.type == 'branch') {
			if (!this.state.checkIfFoundBranches(obj)) {
				var branch = obj;

				this.state.drawOnSignalTree(branch)
				if (!reversedDirection) {


					if (branch.branchDraws.length != 0) {
						this.highlightBranches(branch, branch.branchDraws, branch.systemDraws, branch.lineDraws)
					} else {
						var s = this.state.findSystem(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel, reversedDirection)

						var newS = Object.create(s);
						if (SystemUtil.isRefToFile(newS.blockType, newS.sourceFile) && newS.sourceFile != '') {
							await this.handleModelReference(newS, obj, reversedDirection,)
						}
						else if (newS.blockType == 'SubSystem') {
							await this.handleSubsystem(newS, obj, reversedDirection, obj.getResponse(),)
						}
						else {
							this.state.addStack(newS);
						}
					}

				}
				else {
					var branchResult = this.state.findBranchFromBranch(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel);
					if (branchResult) {
						this.state.addStack(branchResult);
					}
					var lineResult = this.state.findLineFromBranch(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel);
					this.state.addStack(lineResult);
				}


				this.state.foundBranches.push(branch);
			} else {
				this.processLoopObj(obj, reversedDirection);
			}

		}
		this.state.highlightCurrentObj(obj);

		setTimeout(async () => {
			await this.TrackLineLoop(reversedDirection);
		}, 0)


	},

	highlightGoto(system, input, systems, lines, branches, reversedDirection) {

		var blockTypeGF = reversedDirection ? 'Goto' : 'From'

		var ses = systems.filter(system => {
			return system.props.GotoTag == input && system.blockType == blockTypeGF
		})

		for (let s of ses) {
				s = this.state.spreadInfor(
					s, systems, lines, branches, system
				);
			const newSys = this.createNewObj(s, system)
			this.state.addStack(newSys);
		}

	},
	highlightBranches(lineResult, branches, systems, lines) {
		for (let branch of branches) {
			branch = this.state.spreadInfor(
				branch, systems, lines, undefined, lineResult
			)


			const newBranch = this.createNewObj(branch, lineResult);
			this.state.addStack(newBranch);
		}


	},


	processPortResult(result,parentSystem) {

		if (result) {
			result = this.state.spreadInfor(
				result, parentSystem.systemDraws, parentSystem.lineDraws
				, parentSystem.branchDrawsLevel, parentSystem

			)
			var newResult = Object.create(result);
			//root obj of lineresult connected to port is subsystem/modelRef
			newResult.rootObj = parentSystem;
			newResult.fromLine = parentSystem.lineInSubsys;
			newResult.addToRootObjArr(parentSystem);

			this.state.addStack(newResult);
		}
	},

	handleInOutPort(obj, reversedDirection) {
		var depth = obj.getCurrentDeepLevel();
		if (depth <= 0) return;
		if (depth == undefined) return;
		var type = !reversedDirection ? 'src' : 'dst'
		var { lines, systems, sysId } = this.levelClickContents[`level${depth - 1}`]
		var lineResult = lines.find(line => {
			return line[type + "Port"] == Number(obj.props.Port ?? 1) && line[type].id == sysId;
		})
		var branches;
		if (reversedDirection) {
			branches = LineUtil.getAllBranch(lines, []);
		}

		lineResult.root = obj
		lineResult = this.state.spreadInfor(
			lineResult, systems, lines, branches
			, undefined, undefined, depth - 1
		)

		this.state.addStack(lineResult)


	},

	async handleModelReference(s, obj, reversedDirection) {
		if (s.stringId == 'system49') {
			console.log();
		}
		s.lineInSubsys = obj;
		s.needHighLight && this.state.highlight(s);
		s.__proto__.__proto__.setIsTrackLine(true);
		this.state.drawOnSignalTree(s);
		this.state.foundBlocks.push(s);

		const keyVal = `${s.name}${s.stringId}${s.getTraceDeepLevel()}`;
		if (!this.map[keyVal]) {
			var { newSystems, newLines, response } = await this.state.fetchingFileContent(s);
			this.map[keyVal] = { newSystems, newLines, response };
		} else {
			var { newSystems, newLines, response } = this.map[keyVal];
		}

		

		var sIn = this.state.findSystemByPort(newSystems, obj, reversedDirection)
		if (!sIn) return;
		if (reversedDirection) {
			var newBranchDraws = LineUtil.getAllBranch(newLines, []);
		}

		sIn.initListObjDraws(newSystems, newLines, newBranchDraws);
		sIn.setParentSystem(s);
		
		sIn.setCurrentDeepLevel(undefined);
		sIn.setTraceDeepLevel(s.getTraceDeepLevel() + 1);
		sIn.setFileContent(response)
		sIn.setRootSysId(0);

		this.state.addStack(sIn)

	},
	async handleSubsystem(s, obj, reversedDirection) {
		s.lineInSubsys = obj;

		s.needHighLight && this.state.highlight(s);
		s.__proto__.__proto__.setIsTrackLine(true);
		this.state.drawOnSignalTree(s);
		this.state.foundBlocks.push(s);
		const response = obj.getFileContent();

		var fileContent = response != null ? response : this.modelDraw.fileContent;
		const keyVal = `${s.name}${s.stringId}${s.getTraceDeepLevel()}`;
		if (!this.map[keyVal]) {
			var { systemDraws: newSystems, lineDraws: newLines } = this.modelDraw.initDrawEntities(
				false,
				fileContent,
				s.id
			)
			this.map[keyVal] = { newSystems, newLines };
		} else {
			var { newSystems, newLines } = this.map[keyVal];
		}

		

		var type = reversedDirection ? 'srcPort' : 'dstPort'
		if (obj[type] == "enable") {
			var enableSys = newSystems.find(system => {
				return system.blockType == 'EnablePort';
			})
			enableSys.needHighLight && this.state.highlight(enableSys);
			this.state.foundBlocks.push(enableSys);
			this.state.addStack(enableSys);
			return;
		}
		var sIn = this.state.findSystemByPort(newSystems, obj, reversedDirection)
		if (!sIn) {
			return;
		}

		if (reversedDirection) {
			var newBranchDraws = LineUtil.getAllBranch(newLines, []);
		}
		sIn.initListObjDraws(newSystems, newLines, newBranchDraws);
		sIn.setParentSystem(s);

		sIn.setCurrentDeepLevel(undefined);
		sIn.setTraceDeepLevel(s.getTraceDeepLevel() + 1);
		sIn.setFileContent(response)
		sIn.setRootSysId(0);
		this.state.addStack(sIn)
	},

	createNewObj(obj, rootObj) {
		var newObj = Object.create(obj);

		newObj.rootObj = rootObj;
		newObj.addToRootObjArr(rootObj);
		return newObj;
	},

	findPortResult(obj, reversedDirection) {
		var parentSystem = obj.getParentSystem();
		if (parentSystem != null) {

			var lineResult = this.state.findLineByPort(obj, parentSystem.lineDraws, reversedDirection, parentSystem);
			this.processPortResult(lineResult, parentSystem);

			if (reversedDirection) {
				var branchResult = this.state.findBranchByPort(obj, parentSystem.branchDrawsLevel, reversedDirection, parentSystem);
				this.processPortResult(branchResult, parentSystem)
			}
		}
	},
	processLoopObj(obj, reversedDirection) {


		var loopObj = null;
		if (obj.type == 'system') {
			loopObj = this.state.foundBlocks.find(el => el.stringId == obj.stringId && el.getTraceDeepLevel() == obj.getTraceDeepLevel())
		}
		if (obj.type == 'line') {
		
			loopObj = this.state.foundLines.find(
					el => el.stringId == obj.stringId
						&& el.getTraceDeepLevel() == obj.getTraceDeepLevel()
			)


		}

		if (obj.type == 'branch') {
			loopObj = this.state.foundBranches.find(
				el => el.stringId == obj.stringId
					&& el.getTraceDeepLevel() == obj.getTraceDeepLevel()
			)
		}
		this.traceBackOutports(loopObj, loopObj.getOutports());

		this.state.drawOnSignalTree(obj);
		const parentSystem = obj.getParentSystem();

		if (parentSystem) {

			loopObj.outports.forEach(outport => {
				if (!parentSystem.tracedOutport?.includes(outport)) {
					var lineResult = this.state.findLineByPortNumber(outport, parentSystem.lineDraws, reversedDirection, parentSystem);
					this.processPortResult(lineResult, parentSystem);

					if (reversedDirection) {

						var branchResult = this.state.findBranchByPort(obj, parentSystem.branchDrawsLevel, reversedDirection, parentSystem);
						this.processPortResult(branchResult,parentSystem)

					}

					if (!parentSystem.tracedOutport) {
						parentSystem.tracedOutport = [];
					}
					parentSystem.tracedOutport.push(outport);
				}

			})
		}
	},

	traceBackOutports(obj, outports, set = []) {
		const rootObjArr = obj.getRootObjArr();
		if (rootObjArr.length == 0) {
			return;
		}
		if (obj.blockType == 'SubSystem' || (SystemUtil.isRefToFile(obj.blockType, obj.sourceFile) && obj.sourceFile != '')) {
			const lineInSubsys = obj.lineInSubsys;

			if (!set.includes(lineInSubsys.stringId)) {
				for (var outport of outports) {
					lineInSubsys.addToOutports(outport);
				}
				set.push(lineInSubsys.stringId);
				this.traceBackOutports(lineInSubsys, outports, set);
			}
		}
		else {
			rootObjArr.forEach(rootObj => {
				if (!set.includes(rootObj.stringId)) {
					for (var outport of outports) {
						rootObj.addToOutports(outport);
					}
					set.push(rootObj.stringId);
					this.traceBackOutports(rootObj, outports, set);
				}
			})
		}

	}

}

export default Algorithm