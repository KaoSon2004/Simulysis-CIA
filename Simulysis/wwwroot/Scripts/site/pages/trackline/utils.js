import LineUtil from "../../utils/line.js";
import FileContentsAPI from "../../api/fileContents.js";

import TracerState from "../../draw/common/tracerState.js";
import System from "../../draw/model/draw-entities/system/index.js";
import { noop } from "../../noop.js";
import SystemUtil from "../../utils/system.js";

var utils = {
	__proto__: TracerState,

	traceSignalTree: {},


	init(modelDraw, traceSignalTree) {
		super.init(modelDraw)
		this.traceSignalTree = traceSignalTree;
	},
	highlight(entity) {
		if (entity != undefined) {

			entity.highlight('#fc0303')
			entity.shape.attr('stroke-width', '3')
			if (entity.innerSvg) {
				entity.innerShape.select('g').attr('stroke-width', '3')
			}
			//#00896d
			entity.setIsTrackLine(true)
		}
	},
	unHighlight(entity) {
		entity.unHighlight()
		entity.__proto__.setIsTrackLine(false)
	},
	unHighlightAllSystems(systems) {
		systems.forEach(this.unHighlight)
	},
	unHighlightAllLines(lines) {
		if (typeof lines == 'undefined') {
			return
		}
		lines.forEach(line => {
			this.unHighlight(line)
			this.unHighlightAllLines(line.branchDraws)
		})
	},
	startLoadingHighlight() {
		$('#loading-text').text('Highlighting signals, please wait...')
		$('.main-loader').css('display', 'flex')
		$('.main-container').css('filter', 'blur(2.5px) grayscale(1.4)')
	},
	stopLoadingHighlight() {
		$('.main-loader').css('display', 'none')
		$('.main-container').css('filter', 'none')
	},

	onNewSystemFound(obj, rootObj) {
		console.log(obj)
		console.log(rootObj)
	},

	findLineByStringId(lines, type, system) {
		return lines.filter(line => {
			return line[type]?.stringId == system.stringId
		})
	},
	findLineByPort(s, lines, reversedDirection, parentSystem) {
		const port = Number(s.props.Port ?? 1);
		parentSystem.addOutport(s)
		var type = reversedDirection ? 'dst' : 'src'
		var portType = type + 'Port'

		return lines.find(line => {
			return line[portType] == port && parentSystem.stringId == line[type].stringId
		})
	},
	findLineByPortNumber(port, lines, reversedDirection, parentSystem) {
		var type = reversedDirection ? 'dst' : 'src'
		var portType = type + 'Port'

		return lines.find(line => {
			return line[portType] == port && parentSystem.stringId == line[type].stringId
		})
	},
	findBranchByStringId(branches, type, system) {
		return branches.filter(branch => {
			return branch[type]?.stringId == system.stringId
		})
	},
	findBranchByPort(s, branches, reversedDirection, parentSystem) {
		const port = Number(s.props.Port ?? 1);
		parentSystem.addOutport(s)
		var type = reversedDirection ? 'dst' : 'src'
		var portType = type + 'Port'
		return branches.find(branch => {
			return branch[portType] == port && parentSystem.stringId == branch[type].stringId
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
		sys = this.spreadInfor(
			sys, systems, lines, branches, lineResult
		);
		if (!lineResult.getResponse()) {
			console.log();
		}
		var newSys = Object.create(sys);
		newSys.rootObj = lineResult;
		newSys.addToRootObjArr(lineResult);
		return newSys;

	},
	findLineResult(system, systems, lines, branches, reversedDirection = false) {

		var type = reversedDirection ? 'dst' : 'src'

		var lineResults = [];

		var newLineResults = [];
		lineResults = this.findLineByStringId(lines, type, system);

		lineResults.forEach(lineResult => {
			lineResult = this.spreadInfor(
				lineResult, systems, lines, branches, system
			)

			var newLineResult = Object.create(lineResult);

			newLineResult.rootObj = system;
			newLineResult.addToRootObjArr(system);

			newLineResults.push(newLineResult);
		})
		var result = [...newLineResults]

		return [...new Set(result)]


	},
	findBranchResult(system, systems, lines, branches, reversedDirection = false) {
		var type = reversedDirection ? 'dst' : 'src';
		var branchResults = [];
		var newBranchResults = [];
		branchResults = this.findBranchByStringId(branches, type, system);
		branchResults.forEach(branchResult => {

			branchResult = this.spreadInfor(
				branchResult, systems, lines, branches, system
			);

			var newBranchResult = Object.create(branchResult);

			newBranchResult.rootObj = system;
			newBranchResult.addToRootObjArr(system);
			newBranchResults.push(newBranchResult);
		})
		return newBranchResults

	},
	filterAndInitDraws(draws, systemDraws, lineDraws, branchDraws
		, block, reversedDirection, depthLevel) {
		const type = reversedDirection == true ? 'dst' : 'src';
		var filterResults = [];
		filterResults = draws.filter(draw => {
			return draw[type].stringId == block.stringId
		})

		filterResults.forEach(filterResult => {
			filterResult?.initListObjDraws(systemDraws, lineDraws, branchDraws);
			this.addStack(filterResult);

		})
		return filterResults;
	},

	drawOnSignalTree(obj) {


		const rootObj = obj.rootObj;
		this.traceSignalTree.appendNode(obj, false);

		this.traceSignalTree.connect2Node(rootObj, obj);
	},
	onSystemTraceDone(obj) {
		this.highlightCurrentObj(obj)
	},
	highlightCurrentObj(obj) {
		var { systemDraws: systems, lineDraws: lines } = this.modelDraw.drawEntities;
		var branchDraws = LineUtil.getAllBranch(lines, []);

		if (obj.type == 'system') {
			systems.forEach(block => {
				if (
					block != undefined &&
					block.sid == obj.sid &&
					block.id == obj.id &&
					block.name == obj.name &&
					block.props.Position == obj.props.Position
				) {
					this.highlight(block);

					return;
				}
			})

		}
		if (obj.type == 'line') {

			lines.forEach(line => {
				if (
					line.id == obj.id &&
					JSON.stringify(line.points[0]) == JSON.stringify(obj.points[0]) &&
					JSON.stringify(line.points[0]) == JSON.stringify(obj.points[0])
				) {
					this.highlight(line);
					return;
				}
			})
		}

		if (obj.type == 'branch') {

			branchDraws.forEach(branch => {
				if (
					branch.id == obj.id &&
					branch.props.Dst == obj.props.Dst &&
					branch.props.Points == obj.props.Points
				) {
					this.highlight(branch);
					return;
				}
			})

		}
	},
	reHighlightClickedSystems(systems) {
		systems.forEach(system => {
			if (this.checkIfFoundBlocks(system)) {
				this.highlight(system)
			}
		})
	},
	reHighlightClickedLines(lines) {
		if (typeof lines == 'undefined') {
			return
		}
		lines.forEach(line => {
			if (this.checkIfFoundLines(line)) {
				this.highlight(line)
			}
			if (this.checkIfFoundBranches(line)) {
				this.highlight(line)
			}
			this.reHighlightClickedLines(line.branchDraws)
		})
	},

	// re-highlight after going up/down
	reHighlight() {

		var { systemDraws, lineDraws } = this.modelDraw.drawEntities

		this.utils.reHighlightClickedSystems(systemDraws)
		this.utils.reHighlightClickedLines(lineDraws)
	},
	onTraceFinish() {
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
	},
	resetState(systemDraws, lineDraws) {
		super.resetState()
		this.unHighlightAllLines(lineDraws);
		this.unHighlightAllSystems(systemDraws);
		this.traceSignalTree.resetState();

		this.traceSignalTree.stackTrace = [];
	},
	unTraceObj(systemDraws, lineDraws) {
		const branchDraws = LineUtil.getAllBranch(lineDraws, []);

		systemDraws.forEach(system => {
			system.traced = false;
			system.treeCoordinate = {};
			system.clearRootObj();
		})
		lineDraws.forEach(line => {
			line.traced = false;
			line.treeCoordinate = {};
			line.clearRootObj();
		})
		branchDraws.forEach(branch => {
			branch.traced = false;
			branch.treeCoordinate = {};
			branch.clearRootObj();
		})
	}
	,
	findPortResult(obj, reversedDirection) {
		var parentSystem = obj.getParentSystem();
		if (parentSystem != null) {

			var lineResult = this.utils.findLineByPort(obj, parentSystem.lineDraws, reversedDirection, parentSystem);
			this.processPortResult(lineResult, obj, parentSystem);

			if (reversedDirection) {
				var branchResult = this.utils.findBranchByPort(obj, parentSystem.branchDrawsLevel, reversedDirection, parentSystem);
				this.processPortResult(branchResult, obj, parentSystem)
			}
		}
	},
	findBranchFromBranch(branchObj, systems, lines, branches) {
		var branchresult;
		var newBranchResult;
		branches.forEach(branch => {
			if (branch.branchDraws.length != 0) {
				branch.branchDraws.forEach(childBranch => {
					if (childBranch.stringId == branchObj.stringId) {

						branchresult = branch
						branchresult = this.spreadInfor(
							branchresult, systems, lines, branches, branchObj
						)
						newBranchResult = Object.create(branchresult);
						newBranchResult.rootObj = branchObj;
						newBranchResult.addToRootObjArr(branchObj);


					}
				})
			}
		})
		return newBranchResult

	},
	findLineFromBranch(branchObj, systems, lines, branches) {
		var lineResult;
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
						newLineResult.rootObj = branchObj;
						newLineResult.addToRootObjArr(branchObj);
					}
				})
			}
		})
		return newLineResult;

	}
}

export default utils;