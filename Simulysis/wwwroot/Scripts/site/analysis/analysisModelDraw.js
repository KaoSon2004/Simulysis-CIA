import ModelDraw from "../draw/model/index.js";
import System from '../draw/model/draw-entities/system/index.js'
import Clock from '../draw/model/draw-entities/system/clock.js'
import CombinatorialLogic from '../draw/model/draw-entities/system/combinatorialLogic.js'
import Constant from '../draw/model/draw-entities/system/constant.js'
import Gain from '../draw/model/draw-entities/system/gain.js'
import Ground from '../draw/model/draw-entities/system/ground.js'
import Mux from '../draw/model/draw-entities/system/mux.js'
import Port from '../draw/model/draw-entities/system/port.js'
import Reference from '../draw/model/draw-entities/system/reference.js'
import Scope from '../draw/model/draw-entities/system/scope.js'
import Sin from '../draw/model/draw-entities/system/sin.js'
import SubSystem from '../draw/model/draw-entities/system/subSystem.js'
import Sum from '../draw/model/draw-entities/system/sum.js'
import Terminator from '../draw/model/draw-entities/system/terminator.js'
import ToWorkspace from '../draw/model/draw-entities/system/toWorkspace.js'
import ModelReference from '../draw/model/draw-entities/system/modelReference.js'
import From from '../draw/model/draw-entities/system/from.js'
import Goto from '../draw/model/draw-entities/system/goto.js'
import Logic from '../draw/model/draw-entities/system/logic.js'
import RelationalOperator from '../draw/model/draw-entities/system/relationalOperator.js'
import ManualSwitch from "../draw/model/draw-entities/system/manualSwitch.js";
import Switch from "../draw/model/draw-entities/system/switch.js";
import PMIOPort from "../draw/model/draw-entities/system/pmIOPort.js";
import Line from '../draw/model/draw-entities/line.js'
import Markers from '../draw/model/draw-entities/markers.js'
import SystemUtil from "../utils/system.js";

var AnalysisModelDraw = {
	__proto__: ModelDraw,
	
	initRemovedSystems(systems) {
		var systemsPorts = SystemUtil.getAllSystemsPorts(systems)
		var { lists, instanceDatas } = this.fileContent

		this.fileContent.removedSystems = systems
		this.drawEntities.removedSystems = systems.map(system => {
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
				default:
					try {
						systemDraw = Object.create(eval(blockType))
					} catch (error) {
						systemDraw = Object.create(System)
						console.warn(`Not supported type of system: ${blockType}`)
					}
					break
			}

			systemDraw.init(
				this.parent,
				system,
				systemsPorts[sysId],
				this.systemHandlerGenerator(system, { sysId, relaFakeFileId }, 'down'),
				SystemUtil.getSystemLists(lists, sysId),
				SystemUtil.getSystemInstanceDatas(instanceDatas, sysId)
			)

			systemDraw.stringId += "_removed";

			return systemDraw;

		})

		this.drawEntities.removedSystems.forEach(system => system.initListObjDraws(this.drawEntities.systemDraws, this.drawEntities.lineDraws));
	},

	draw() {
		super.draw()

		if (this.drawEntities.removedSystems) {
			this.drawEntities.removedSystems.forEach(system => system.draw())
		}
	}
}

export default AnalysisModelDraw