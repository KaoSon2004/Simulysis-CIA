
import LineUtil from '../../Utils/line.js'
import SystemUtil from '../../Utils/system.js'
import ViewManager from '../show-file/viewManager.js'
import Utils from "./Utils.js"
import Algorithm from "./algorithm.js"
import { noop } from '../../noop.js'
var TrackLineViewManager = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: ViewManager,

	/*
	 * PROPERTIES
	 */
	alreadyAlerted: false,
	goUpDownCallback: noop,

	utils: {},
	algorithm: {},
	/*
	 * METHODS
	 */

	/* ===== OVERRIDE ===== */
	async init() {


		await super.init({
			hasNetworkView: false,
			modelViewPopUp: false,
			goUpDownCallback: Utils.reHighlight.bind(this),

		})

		this.addTracklineListener()
		this.allowTrackline()
		this.addClearListener()


		this.traceSignalLevelContents = {};
		this.currentSignalLevel = 0;


	},

	addClearListener() {
		$('#deleteTrack').click(e => {
			$('#trackInput').val('')
			$('#trackSignal').click()

		})
	},

	allowTrackline() {
		$('#trackInput').attr('disabled', false)
		$('#deleteTrack').attr('disabled', false)
		$('#trackSignal').attr('disabled', false)
	},



	// ***TRACK LINE***
	addTracklineListener() {

		$("#handleTraceToSrc").click(async (e) => {
			e.preventDefault();

			$(".custom-menu").hide();
			$("#loaderTraceSignal").html("");
			$("#loaderTraceSignal").addClass("loaderTraceSignal");
			$("#traceSignalResult").text("Searching");

			var { systemDraws, lineDraws } = this.modelDraw.drawEntities;

			var blockStringId = $("#blockStringId").val();
			var block = systemDraws.find(system => system.stringId == blockStringId);

			this.highlightSignal(block, systemDraws, lineDraws, true)

		})
		$("#handleTraceToDes").click(async (e) => {
			e.preventDefault();
			$(".custom-menu").hide(0);
			$("#loaderTraceSignal").html("");
			$("#loaderTraceSignal").addClass("loaderTraceSignal");
			$("#traceSignalResult").text("Searching");

			var { systemDraws, lineDraws } = this.modelDraw.drawEntities;
			var blockStringId = $("#blockStringId").val();

			var block = systemDraws.find(system => system.stringId == blockStringId);

			this.highlightSignal(block, systemDraws, lineDraws, false)

		})

		$('#trackForm').submit(async e => {
			e.preventDefault()
			var { systemDraws: systems, lineDraws: lines } = this.modelDraw.drawEntities
			const input = $('#trackInput').val()


			var s = systems.find(system => {
				return system.name.toLowerCase() == input.toLowerCase()
			})



			if (s == undefined && !this.alreadyAlerted) {

				this.utils.resetState(systems, lines);
				$("#loaderTraceSignal").removeClass("loaderTraceSignal");
				$("#loaderTraceSignal").html("");
				$("#traceSignalResult").text("");
				if (input != '') alert(`No signal named "${input}" in current view!`)
				this.alreadyAlerted = true
				return
			}

			var reversedDirection = s?.blockType != 'Inport' && s?.blockType != 'InportShadow'

			if (input != '') {
				this.alreadyAlerted = false
			}

			if (input.trim() != '') {
				$("#loaderTraceSignal").html("");
				$("#loaderTraceSignal").addClass("loaderTraceSignal");
				$("#traceSignalResult").text("Searching");
			} else {
				$("#loaderTraceSignal").html("");

				$("#traceSignalResult").text("");
			}
			await this.highlightSignal(s, systems, lines, reversedDirection)

		})
	},
	
	async highlightSignal(
		inputSystem,
		systemDraws,
		lineDraws,
		reversedDirection = false,
	) {
		this.traceSignalTree.reversedDirection = reversedDirection;
		this.utils.resetState(systemDraws, lineDraws);
		
		this.depthLevel = this.numClick;
		
		this.traceSignalLevelContents[`level${this.currentSignalLevel++}`]
		var branchDraws = []
		if (reversedDirection) {
			branchDraws = LineUtil.getAllBranch(lineDraws, []);
		}

		$("#trackInput").val(inputSystem.name);
		if ((SystemUtil.isRefToFile(inputSystem.blockType, inputSystem.sourceFile) || inputSystem.blockType == 'SubSystem') && inputSystem.sourceFile != "") {
			this.utils.foundBlocks.push(inputSystem);
			this.utils.highlight(inputSystem);

			this.utils.filterAndInitDraws(lineDraws, systemDraws, lineDraws
				, branchDraws, inputSystem, reversedDirection, this.depthLevel, 0);



		} else {

			inputSystem.initListObjDraws(systemDraws, lineDraws, branchDraws);
			inputSystem.setCurrentDeepLevel(this.depthLevel);
			inputSystem.setCurrentTraceLevel(0);
			inputSystem.setTraceDeepLevel(0);
			inputSystem.setFileContent(this.allLevelContents[`level${this.currentLevel}`]?.fileContent)
			inputSystem.setRootSysId(this.rootSysId);
			this.utils.addStack(inputSystem);
		}


		await this.algorithm.TrackLineLoop(reversedDirection)
	},

}
export default TrackLineViewManager
