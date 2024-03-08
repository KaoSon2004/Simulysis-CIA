import ErrorLogger from '../../../utils/errorLogger.js'

var Common = {
	/*
	 *	PROPERTIES
	 */
	parentElement: null,
	id: null, // unique among same type
	type: null, // one of system | line | branch
	stringId: null, // unique for all types
	props: {},
	outerG: null,
	outerShape: null,
	foregroundColor: 'black',
	backgroundColor: 'white',
	fontSize: 10,
	firstLineMargin: 3,



	/*
	 * For Trace Signal
	 */
	isTrackLine: false,
	needHighLight: false,
	systemDraws: [],
	lineDraws: [],
	branchDrawsLevel: [],
	parentSystem: null,
	currentTraceLevel: -1,
	response: null,
	depth: -1,
	treeCoordinate: {},
	traceDeepLevel: {},


	rootObjArr: [],

	fileContent: {},
	rootSysId: 0,


	viewManager: {}, 
	index: -1,
	viewManager: {},
	tracedLeft: false,
	tracedRight: false,
	leftSideChildren: null,
	rightSideChildren: null,
	outports: [],

	/*
	 * METHODS
	 */
	init(parentElement, id, type, properties, viewManager) {
		this.parentElement = parentElement
		this.parentElement.attr("id", "parentG");
		this.id = id
		this.type = type
		this.stringId = `${type}${id}`
		this.props = JSON.parse(properties)

		// initial css attributes
		this.foregroundColor = this.props.ForegroundColor ?? 'black'
		//if (isAdded) {
		//	this.foregroundColor = this.props.ForegroundColor ?? 'green'
		//} else {
		//	this.foregroundColor = this.props.ForegroundColor ?? 'black'
		//}
		this.foregroundColor = this.props.ForegroundColor ?? 'black'
		this.backgroundColor = this.props.BackgroundColor ?? 'white'
		this.fontSize = this.props.FontSize ? Number(this.props.FontSize) : 10
		this.firstLineMargin = 3
		this.isTrackLine = false

		//For Trace Signal
		this.needHighLight = false
		this.systemDraws = []
		this.lineDraws = []
		this.branchDrawsLevel = []
		this.parentSystem = null
		this.currentTraceLevel = -1;
		this.response = null
		this.depth = -1;
		this.treeCoordinate = {};
		this.traceDeepLevel = -1;
		this.traced = false;
		this.fileContent = {};
		this.rootSysId = 0;
		this.rootObjArr = [];

		this.viewManager = viewManager;
		this.outports = [];
	},
	draw() {
		ErrorLogger.methodNotImplemented('draw')
	},
	drawWrapper() {
		this.outerG = this.parentElement.append('g')
		console.log(this.stringId + " " + this.name)

		this.outerShape = this.outerG
			.append('g')
			.attr('id', this.stringId)
			.style('cursor', 'pointer')
			.on('mouseenter', this.mouseEnterHandler.bind(this))
			.on('mouseleave', this.mouseLeaveHandler.bind(this))
			.on('click', this.mouseClickHandler.bind(this))
			.on('contextmenu', (event) => {
				event.preventDefault();
				$(".custom-menu").finish().toggle(100).

					css({
						top: event.pageY + "px",
						left: event.pageX + "px"
					});
				$("#blockStringId").val(this.stringId);
			});
	},
	drawDotMarkers() {
		ErrorLogger.methodNotImplemented('drawDotMarkers')
	},
	drawDotMarker(x, y) {
		const radius = 3

		this.parentElement
			.append('circle')
			.attr('r', radius)
			.attr('cx', x)
			.attr('cy', y)
			.attr('fill', 'white')
			.attr('stroke', 'black')
			.attr('class', `${this.stringId}dot-marker`)
	},
	mouseEnterHandler(e) {
		if (!this.isTrackLine) this.highlight('#1d5193')
	},
	mouseLeaveHandler() {
		if (!this.isTrackLine) {
			this.unHighlight()
		}
	},
	mouseClickHandler() {
		this.drawDotMarkers()
		this.addMouseClickOutsideHandler()
		this.displayObjDetail();

		const node = d3.select(`.tree${this.stringId}`);
		node.dispatch('click');
		const paddingX = 10;
		const paddingY = 40;

		const rectNode = d3.select(`treerect-${this.stringId}-false`);
		console.log(this.fileContent);
		console.log(this.fileContent);
/*		this.viewManager.traceSignalTree.zoomInPositionRaw((-this.treeCoordinate.cx + paddingX), (-this.treeCoordinate.cy + paddingY), 1)
*/

	},

	displayObjDetail() {
		var object = {};
		if (this.type == 'system') {
			const { backgroundColor, blockType, bottom, left
				, top, right, foregroundColor, gotoTag, height
				, width, id, name, numberOfInPorts, numberOfOutPorts
				, parentId, sourceFile, stringId, type } = this;
			object = {
				backgroundColor, blockType, bottom, left
				, top, right, foregroundColor, gotoTag, height
				, width, id, name, numberOfInPorts, numberOfOutPorts
				, parentId, sourceFile, stringId, type
			}

		} else {
			const { isBranch, dstPort, foregroundColor, srcPort, stringId, id, backgroundColor } = this

			object = { isBranch, dstPort, foregroundColor, srcPort, stringId, id, backgroundColor }
		}


		let str = "";
		for (const property in object) {

			str = str + "<div class=\"w-100 d-flex\">"
				+ "<p class=\"w-50\">" + `${property}` + "</p> "
				+ "<p class=\"border-left pl-3\">" + `${object[property] ?? ""}` + "</p> "
				+ "</div>"
		}

		$("#block-detail").html(str);
	},
	addMouseClickOutsideHandler() {
		const selector = `#${this.stringId}`

		var outsideClickListener = event => {
			var $target = $(event.target)
			if (!$target.closest(selector).length && $(`.${this.stringId}dot-marker`).is(':visible')) {
				$(`.${this.stringId}dot-marker`).remove()
				removeClickListener()
			}
		}

		function removeClickListener() {
			document.removeEventListener('click', outsideClickListener)
		}

		document.addEventListener('click', outsideClickListener)
	},
	highlight(color) {
		this.shape.attr('filter', `drop-shadow(0px 0px 3px ${color})`).attr('stroke', color)

		if (this.innerSvg) {
			this.innerSvg.attr('filter', `drop-shadow(0px 0px 3px ${color})`)
		}

		if (this.path) {
			this.path.attr('stroke', color)
		}
	},
	unHighlight() {
		this.shape.attr('filter', '').attr('stroke', this.foregroundColor)
		this.shape.attr('stroke-width', '1')
		if (this.innerSvg) {
			this.innerSvg.attr('filter', '')
		}

		if (this.path) {
			this.path.attr('stroke', this.foregroundColor)
		}
	},
	setIsTrackLine(isTrackLine) {
		this.isTrackLine = isTrackLine
	},

	initListObjDraws(systemDraws, lineDraws, branchDraws) {
		this.systemDraws = systemDraws;
		this.lineDraws = lineDraws;
		this.branchDrawsLevel = branchDraws;
	},
	initResponse(response) {
		this.response = response;
	}
	,
	getResponse() {
		return this.response
	},
	setParentSystem(parentSystem) {
		this.parentSystem = parentSystem;
	},
	getParentSystem() {
		return this.parentSystem;
	},

	setCurrentTraceLevel(level) {
		this.currentTraceLevel = level;
	},

	getCurrentTraceLevel() {
		return this.currentTraceLevel;
	},


	setCurrentDeepLevel(depth) {
		this.depth = depth;
	},

	getCurrentDeepLevel() {
		return this.depth;
	},
	setTraceDeepLevel(traceDeepLevel) {
		this.traceDeepLevel = traceDeepLevel;
	},
	getTraceDeepLevel() {
		return this.traceDeepLevel;
	},
	setFileContent(fileContent) {
		this.fileContent = fileContent;
	},
	getFileContent() {
		return this.fileContent;
	},
	setRootSysId(rootSysId) {
		this.rootSysId = rootSysId;
	},
	getRootSysId() {
		return this.rootSysId;
	},
	getRootObjArr() {
		return this.rootObjArr;
	},
	addToRootObjArr(rootObj) {
		const obj = this.rootObjArr.find(el => el.stringId == rootObj.stringId);
		if (obj) {
			return;
		}
		this.rootObjArr.push(rootObj);
	},

	addToOutports(outport) {
		if (!this.outports.includes(outport)) {
			this.outports.push(outport);
		}
	},
	getOutports() {
		return this.outports;
	},
	clearOutports() {
		this.outports = [];
	},




}

export default Common
