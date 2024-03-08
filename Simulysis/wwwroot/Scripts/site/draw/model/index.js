import Draw from '../index.js'
import System from './draw-entities/system/index.js'
import Clock from './draw-entities/system/clock.js'
import CombinatorialLogic from './draw-entities/system/combinatorialLogic.js'
import Constant from './draw-entities/system/constant.js'
import Gain from './draw-entities/system/gain.js'
import Ground from './draw-entities/system/ground.js'
import Mux from './draw-entities/system/mux.js'
import Port from './draw-entities/system/port.js'
import Reference from './draw-entities/system/reference.js'
import Scope from './draw-entities/system/scope.js'
import Sin from './draw-entities/system/sin.js'
import SubSystem from './draw-entities/system/subSystem.js'
import Sum from './draw-entities/system/sum.js'
import Terminator from './draw-entities/system/terminator.js'
import ToWorkspace from './draw-entities/system/toWorkspace.js'
import ModelReference from './draw-entities/system/modelReference.js'
import From from './draw-entities/system/from.js'
import Goto from './draw-entities/system/goto.js'
import Logic from './draw-entities/system/logic.js'
import RelationalOperator from './draw-entities/system/relationalOperator.js'
import ManualSwitch from "./draw-entities/system/manualSwitch.js";
import Switch from "./draw-entities/system/switch.js";
import PMIOPort from "./draw-entities/system/pmIOPort.js";
import Line from './draw-entities/line.js'
import Markers from './draw-entities/markers.js'
import SystemUtil from '../../utils/system.js'
import { noopGenerator } from '../../noop.js'

var ModelDraw = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: Draw,

	/*
	 * PROPERTIES
	 */
	fileContent: null,
	levelContent: null,
	drawEntities: null,
	rootSysId: null,
	systemHandlerGenerator: noopGenerator, // noop generator
	viewManager: {},
	/*
	 * METHODS
	 */
	/* ===== OVERRIDE ===== */
	init(ids, rootSysId, fileContent, systemHandlerGenerator, options , viewManager) {
		const { viewId, drawId, whId } = ids
		this.viewManager = viewManager;
		super.init(viewId, drawId, whId, options)
		Markers.defineMarkers(this.canvas)
		this.systemHandlerGenerator = systemHandlerGenerator
		this.initLevelContent(fileContent, rootSysId)
		this.initDrawEntities()
		this.reInitViewBox()

		return this
	},
	/* ===== OVERRIDE ===== */
	draw() {
		var { systemDraws, lineDraws } = this.drawEntities

		systemDraws.forEach(system => system.draw())
		lineDraws.forEach(line => line.draw())
	},
	initDragDropEvent() {
		var { systemDraws } = this.drawEntities
		systemDraws.forEach(system => system.initDragHandler());
	},
	reInitViewBox() {
		var { systemDraws } = this.drawEntities
		if (systemDraws.length == 0) {
			return
		}

		var left, top, right, bottom

		systemDraws.forEach(systemDraw => {
			if (!left || left > systemDraw.left) {
				left = systemDraw.left
			}

			if (!right || right < systemDraw.right) {
				right = systemDraw.right
			}

			if (!top || top > systemDraw.top) {
				top = systemDraw.top
			}

			if (!bottom || bottom < systemDraw.bottom) {
				bottom = systemDraw.bottom
			}
		})

		const padding = 50
		this.canvas.attr(
			'viewBox',
			`${left - padding} ${top - padding} ${right - left + padding * 2} ${bottom - top + padding * 2}`
		)
	},
	initLevelContent(fileContent, rootSysId) {
		this.fileContent = fileContent
		this.rootSysId = rootSysId
		this.levelContent = SystemUtil.getLevelContent(fileContent, rootSysId)
	},
	initDrawEntities(isCurrentObj = true, fileContent, subsysId = 0) {
		var { systems, branches, lists, instanceDatas } = isCurrentObj ? this.fileContent : fileContent


		var { systems: levelSystems, lines: levelLines } = isCurrentObj
			? this.levelContent
			: SystemUtil.getLevelContent(fileContent, subsysId)


		var systemsPorts = SystemUtil.getAllSystemsPorts(systems)

		var systemDraws = levelSystems.map(system => {
			const { id: sysId, fK_FakeProjectFileId: relaFakeFileId, blockType } = system

			var systemDraw = null

			switch (blockType) {
				case 'Inport':
				case 'InportShadow':
				case 'Outport':
					systemDraw = Object.create(Port)
					break
				case 'Demux':
				case 'Mux':
					systemDraw = Object.create(Mux)
					break
				case 'Math':
					systemDraw = Object.create(System)
					break
				case 'Clock':
					systemDraw = Object.create(Clock)
					break
				case 'CombinatorialLogic':
					systemDraw = Object.create(CombinatorialLogic)
					break
				case 'Constant':
					systemDraw = Object.create(Constant)
					break
				case 'Gain':
					systemDraw = Object.create(Gain)
					break
				case 'Ground':
					systemDraw = Object.create(Ground)
					break
				case 'Scope':
					systemDraw = Object.create(Scope)
					break
				case 'Sin':
					systemDraw = Object.create(Sin)
					break
				case 'SubSystem':
					systemDraw = Object.create(SubSystem)
					break
				case 'Sum':
					systemDraw = Object.create(Sum)
					break
				case 'Terminator':
					systemDraw = Object.create(Terminator)
					break
				case 'ToWorkspace':
					systemDraw = Object.create(ToWorkspace)
					break
				case 'ModelReference':
					systemDraw = Object.create(ModelReference)
					break
				case 'From':
					systemDraw = Object.create(From)
					break
				case 'Goto':
					systemDraw = Object.create(Goto)
					break
				case 'Logic':
					systemDraw = Object.create(Logic)
					break
				case 'RelationalOperator':
					systemDraw = Object.create(RelationalOperator)
					break
				case 'ManualSwitch':
					systemDraw = Object.create(ManualSwitch)
					break
				case 'Switch':
					systemDraw = Object.create(Switch)
					break
				case 'PMIOPort':
					systemDraw = Object.create(PMIOPort)
					break
				case 'Reference':
					systemDraw = Object.create(Reference)
					break
				default:
					/*try {
						systemDraw = Object.create(eval(blockType))
					} catch (error) {*/
						systemDraw = Object.create(System)
						console.warn(`Not supported type of system: ${blockType}`)
					//}
					break
			}
			systemDraw.init(
				this.parent,
				system,
				systemsPorts[sysId],
				this.systemHandlerGenerator(system, { sysId, relaFakeFileId }, 'down'),
				SystemUtil.getSystemLists(lists, sysId),
				SystemUtil.getSystemInstanceDatas(instanceDatas, sysId),
				this.viewManager,
			)

			return systemDraw;
			
		})

		var sidNameObj = SystemUtil.systemDrawsToSidNameObj(systemDraws)
		var branchObj = SystemUtil.branchesToObj(branches)
		console.log(this.viewManager);
		var lineDraws = levelLines.map(line => Object.create(Line).init(this.parent, line, sidNameObj, branchObj, undefined, undefined, this.viewManager))

		if (isCurrentObj) this.drawEntities = { systemDraws, lineDraws }

		return { systemDraws, lineDraws }
	},

	initListObjDraws() {
		const { systemDraws, lineDraws } = this.drawEntities;
		systemDraws.forEach(system => system.initListObjDraws(systemDraws, lineDraws));
		lineDraws.forEach(line => line.initListObjDraws(systemDraws, lineDraws));

	},
}

export default ModelDraw
