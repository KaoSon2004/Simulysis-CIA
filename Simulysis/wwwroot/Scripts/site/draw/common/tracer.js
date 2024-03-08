import SystemUtil from "../../Utils/system.js";
import LineUtil from "../../Utils/line.js";

var Tracer = {

	/*
	* Properties
	*/
	state: {},
	modelDraw: {},
	levelClickContents: {},
	/*
	* Methods
	*/
	init(state, modelDraw, levelClickContents) {
		this.state = state;
		this.modelDraw = modelDraw;
		this.levelClickContents = levelClickContents;
		
	},
	async TrackLineLoop(reversedDirection) {
		if (this.state.isEmptyStack()) {
			this.state.onTraceFinish()
			return;
		}
		var obj = this.state.removeStack();


		if (obj.type == 'system') {

			var s = obj;
			var blockTypeOutGF = reversedDirection ? 'From' : 'Goto'
			let neBlockTypeOutGF = reversedDirection ? 'Goto' : 'From'
			if (s.blockType == blockTypeOutGF) {
				this.highlightGoto(s, s.props.GotoTag, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection)
			} else if (s.blockType == neBlockTypeOutGF) {
				if (s.rootObj && !Array.isArray(s.rootObj))
				{
					this.state.onNewSystemFound(s, s.rootObj)
				}
			}

			var blockType = reversedDirection ? 'Inport' : 'Outport'
			if (s.blockType == blockType) {
				var parentSystem = s.getParentSystem();
				if (parentSystem != null) {
					const port = Number(s.props.Port ?? 1);
					parentSystem.addOutport(s)
					var type = reversedDirection ? 'dst' : 'src'
					var portType = type + 'Port'
					var lineResult = this.state.findLineByPort(parentSystem.lineDraws, type, portType, port, parentSystem);
					this.processPortResult(lineResult, s, parentSystem);

					if (reversedDirection) {
						var branchResult = this.state.findBranchByPort(parentSystem.branchDrawsLevel, type, portType, port, parentSystem);
						this.processPortResult(branchResult, s, parentSystem)
					}
				}
				/*this.handleInOutPort(s, reversedDirection);*/
			} 

			else {
				if (s.loop == false && s.blockType != blockTypeOutGF) {
					var lineResults = this.state.findLineResult(s, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection)

					// If searching in reserverd direction, node nexts to
					// a system mayble a branch

					if (reversedDirection) {
						//if prev of block has both branches and lines,
						// trace level of branch must plus length of lineResult
						// to avoid overlapping
						var plusTraceLevel = lineResults.length;
						var branchResults = this.state.findBranchResult(s, s.systemDraws, s.lineDraws, s.branchDrawsLevel, reversedDirection, plusTraceLevel)

						branchResults.forEach(branchResult => {
							if (!this.state.checkIfFoundBranches(branchResult)) {
								this.state.onNewSystemFound(branchResult, s);
								this.state.addStack(branchResult);
							}
						})
					}

					lineResults.forEach(lineResult => {
						this.state.onNewSystemFound(lineResult, s);
						this.state.addStack(lineResult);
					})
				}
			}

			this.state.foundBlocks.push(s);


		}

		if (obj.type == 'line') {
			var lineResult = obj;
			this.state.foundLines.push(lineResult);
			///if searching in direct direction find prev system and connect 2 nodes on grapj
			if (!reversedDirection) {
				if (lineResult.rootObj) {
					this.state.onNewSystemFound(lineResult, lineResult.rootObj)
				}

				if (lineResult.branchDraws.length != 0) {
					this.highlightBranches(lineResult, lineResult.branchDraws, lineResult.systemDraws, lineResult.lineDraws)
				} else {
					var s = this.state.findSystem(lineResult, lineResult.systemDraws, lineResult.lineDraws, lineResult.branchDrawsLevel, reversedDirection)
					if (s) {
						var newS = Object.create(s);
						this.state.onNewSystemFound(newS, lineResult);
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

			}


			else {
				var s = this.state.findSystem(lineResult, lineResult.systemDraws, lineResult.lineDraws, lineResult.branchDrawsLevel, reversedDirection)
				if (s) {
					var newS = Object.create(s);
					this.state.onNewSystemFound(newS, lineResult);
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

		}
		if (obj.type == 'branch') {

			var branch = obj;
			

			this.state.onNewSystemFound(branch, branch.rootObj)
			if (!reversedDirection) {

				if (branch.loop == false) {

					if (!reversedDirection && branch.branchDraws.length != 0) {
						this.highlightBranches(branch, branch.branchDraws, branch.systemDraws, branch.lineDraws)
					} else {
						var s = this.state.findSystem(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel, reversedDirection)
						if (s) {
							var newS = Object.create(s)
							this.state.onNewSystemFound(newS, branch);
							if (SystemUtil.isRefToFile(newS.blockType, newS.sourceFile) && newS.sourceFile != "") {
								await this.handleModelReference(newS, obj, reversedDirection,)
							}
							else if (newS.blockType == 'SubSystem') {
								await this.handleSubsystem(newS, obj, reversedDirection, obj.getResponse(),)
							} else {
								this.state.addStack(newS);
							}
						}
					}

				}
			} else {
				var branchResult = this.state.findBranchFromBranch(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel);
				if (branchResult) {
					if (branchResult.loop == false) {
						this.state.onNewSystemFound(branchResult, obj);
						this.state.addStack(branchResult);
					}
					branchResult.loop = true;
				}

				var lineResult = this.state.findLineFromBranch(branch, branch.systemDraws, branch.lineDraws, branch.branchDrawsLevel);
				if (lineResult)
				{
					this.state.onNewSystemFound(lineResult, obj);
					this.state.addStack(lineResult);
				}
			}
			this.state.foundBranches.push(branch);

		}
		this.state.onSystemTraceDone(obj);

		setTimeout(async () => {
			await this.TrackLineLoop(reversedDirection);
		}, 0)
	},

	highlightGoto(system, input, systems, lines, branches, reversedDirection) {

		var blockTypeGF = reversedDirection ? 'Goto' : 'From'

		var ses = systems.filter(system => {
			return system.props.GotoTag == input && system.blockType == blockTypeGF
		})
		var num = ses.length;

		for (let s of ses) {

			s = this.state.spreadInfor(
				s, systems, lines, branches, system	);

			if (this.state.checkIfFoundBlocks(s)) {
				s.loop = true;
				const newSys = this.createNewObj(s, system, system.getCurrentTraceLevel() + num)
				num--;
				this.state.addStack(newSys);
				continue;
			}
			const newSys = this.createNewObj(s, system, system.getCurrentTraceLevel() + num)
			num--;

			this.state.addStack(newSys);
		}

	},
	highlightBranches(lineResult, branches, systems, lines) {
		var num = branches.length;
		for (let branch of branches) {
			branch = this.state.spreadInfor(branch, systems, lines, undefined, lineResult
			)
			if (this.state.checkIfFoundBranches(branch)) {
				branch.loop = true;
				const newBranch = this.createNewObj(branch, lineResult, lineResult.getCurrentTraceLevel() + num);
				num--;
				this.state.addStack(newBranch);
				continue;
			}

			const newBranch = this.createNewObj(branch, lineResult, lineResult.getCurrentTraceLevel() + num);
			num--;
			this.state.addStack(newBranch);
		}

	},


	processPortResult(result, systemPort, parentSystem) {
		const { cx: rootCx, cy: rootCy } = parentSystem.treeCoordinate;

		if (result) {
			result = this.state.spreadInfor(
				result, parentSystem.systemDraws, parentSystem.lineDraws
				, parentSystem.branchDrawsLevel, parentSystem
			)
			if (parentSystem.name == 'acc_control') {
				console.log(result);
			}
			var newResult = Object.create(result);
			newResult.systemPort = systemPort;
			newResult.setCurrentTraceLevel(parentSystem.getCurrentTraceLevel() + 1);
			//root obj of lineresult connected to port is subsystem/modelRef
			newResult.rootObj = parentSystem;
			this.state.addStack(newResult);
			this.state.onNewSystemFound(newResult, parentSystem);
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

		const { cx: rootCx, cy: rootCy } = obj.treeCoordinate;
		lineResult.root = obj
		lineResult = this.state.spreadInfor(
			lineResult, systems, lines, branches
			, undefined, undefined, depth - 1
		)

		lineResult.setCurrentTraceLevel(obj.getCurrentTraceLevel() - 1);
		this.state.addStack(lineResult)


	},

	async handleModelReference(s, obj, reversedDirection) {
		//s.needHighLight && this.state.highlight(s);
		s.__proto__.__proto__.setIsTrackLine(true);
		this.state.cpyStack.push(s);
		this.state.foundBlocks.push(s);
		var { newSystems, newLines, response } = await this.state.fetchingFileContent(s);

		var sIn = this.state.findSystemByPort(newSystems, obj, reversedDirection)
		if (!sIn) return;
		if (reversedDirection) {
			var newBranchDraws = LineUtil.getAllBranch(newLines, []);
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

		sIn.initListObjDraws(newSystems, newLines, newBranchDraws);
		sIn.setParentSystem(s);

		sIn.initResponse(response);
		sIn.setCurrentDeepLevel(undefined);
		sIn.setTraceDeepLevel(s.getTraceDeepLevel() + 1);
		sIn.setFileContent(response)
		sIn.setRootSysId(0);

		sIn?.setCurrentTraceLevel(s.getCurrentTraceLevel() + 1);
		
		if (!this.state.checkIfFoundBlocks(sIn)) {
			this.state.addStack(sIn)
		} else {
			this.state.removeStack();
		}

		s.systemIn = sIn;
		this.state.onNewSystemFound(sIn, s);

	},
	async handleSubsystem(s, obj, reversedDirection, response) {
		this.state.cpyStack.push(s);
		//s.needHighLight && this.state.highlight(s);
		s.__proto__.__proto__.setIsTrackLine(true);
		
		this.state.foundBlocks.push(s);
		var fileContent = response != null ? response : this.modelDraw.fileContent;
		var { newSystems, newLines } = this.state.fetchingDrawEntities(
			fileContent,
			s.id
		)

		var type = reversedDirection ? 'srcPort' : 'dstPort'
		if (obj[type] == "enable") {
			var enableSys = newSystems.	find(system => {
				return system.blockType == 'EnablePort';
			})
			//enableSys.needHighLight && this.state.highlight(enableSys);
			if (enableSys) {
				this.state.foundBlocks.push(enableSys);
				enableSys.setCurrentTraceLevel(s.getCurrentTraceLevel() + 1);
				this.state.onNewSystemFound(enableSys, s);
				this.state.addStack(enableSys);
			}
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

		sIn.initResponse(response);
		sIn.setCurrentDeepLevel(undefined);
		sIn.setTraceDeepLevel(s.getTraceDeepLevel() + 1);
		sIn.setFileContent(fileContent)
		sIn.setRootSysId(s.id);


		this.state.onNewSystemFound(sIn, s);

		if (!this.state.checkIfFoundBlocks(sIn)) {
			this.state.addStack(sIn)
		} else {
			this.state.removeStack();
		}
	},
	createNewObj(obj, rootObj, currentTraceLevel) {

		const newObj = Object.create(obj);
		newObj.rootObj = rootObj;
		newObj.setCurrentTraceLevel(currentTraceLevel);

		this.state.onNewSystemFound(obj, rootObj);

		return newObj;
	},

}

export default Tracer