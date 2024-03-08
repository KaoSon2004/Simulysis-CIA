import { noop } from '../../../../noop.js'
import Common from '../common.js'

var System = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: Common,

	/*
	 *	PROPERTIES
	 */
	name: null,
	blockType: null,
	sid: null,
	parentId: null,
	sourceFile: null,
	width: -1,
	height: -1,
	left: -1,
	top: -1,
	right: -1,
	bottom: -1,
	ports: [],
	lists: [],
	instanceDatas: [],

	numberOfInPorts: 1,
	numberOfOutPorts: 1,
	numberOfLconnPorts: 0,
	numberOfRconnPorts: 0,
	enablePort: 0,
	triggerPort: 0,
	ifactionPort: 0,
	inLandingFace: 'left',
	outLandingFace: 'right',
	enablePortPosition: null,
	triggerPortPosition: null,
	ifactionPortPosition: null,
	inPortsPosition: [],
	outPortsPosition: [],
	lconnPortsPosition: [],
	rconnPortsPosition: [],
	gotoTag: '',

	callback: noop,
	isDragging: false,
	dragOffset: [],
	dragOffsetBase: {x : 0, y : 0},
	
	positionChangedCallbacks: [],

	/*
 	 * For Trace Signal
	 */
	outport: [],
	numResultPort: 1,
	outConnect: [],
	preConnect: [],
	/*
	* For analysing
	*/
	//isAdded = true,
	//isDeleted,
	

	/*
	 * METHODS
	 */
	/* ===== OVERRIDE ===== */
	init(parentElement, system, ports, callback, lists, instanceDatas, viewManager) {
		super.init(parentElement, system.id, 'system', system.properties, viewManager)
		this.name = system.name
		this.blockType = system.blockType
		this.sid = system.sid
		this.parentId = system.fK_ParentSystemId
		this.sourceFile = system.sourceFile

		this.ports = ports
		this.lists = lists
		this.instanceDatas = instanceDatas
		this.gotoTag = system.gotoTag

		this.callback = callback

		var positionArr = this.props.Position ? this.props.Position.slice(1, -1).split(',') :
			this.props.Location.slice(1, -1).split(',')

		this.left = Number(positionArr[0])
		this.top = Number(positionArr[1])
		this.right = Number(positionArr[2])
		this.bottom = Number(positionArr[3])

		this.width = this.right - this.left
		this.height = this.bottom - this.top

		this.initLandingFaces()
		this.initNumberOfPorts()
		this.initInPortsPosition()
		this.initOutPortsPosition()
		this.initLconnPortsPosition()
		this.initRconnPortsPosition()
		this.initSpecialPortsPosition()

		this.outport = []
		this.numResultPort = 1;
		return this
	},
	initLandingFaces() {
		const { Orientation, BlockMirror, BlockRotation } = this.props

		var deg = 180

		if (Orientation) {
			switch (Orientation) {
				case 'right':
					deg += 0
					break
				case 'down':
					deg += 90
					break
				case 'left':
					deg += 180
					break
				case 'up':
					deg += 270
					break
				default:
					throw new Error(`Impossible orientation: ${Orientation}`)
			}
		}

		deg += BlockMirror === 'on' ? 180 : 0

		deg += BlockRotation ? Number(BlockRotation) : 0

		const inDeg = deg % 360
		this.inLandingFace = this.mapDegToOrientation(inDeg)

		const outDeg = (deg - 180) % 360
		this.outLandingFace = this.mapDegToOrientation(outDeg)
	},
	initNumberOfPorts() {
		if (this.props.Ports) {
			let splitPorts = this.props.Ports.slice(1, -1).split(',').map(Number);
			
			const [
				inports = 0,
				outports = 0,
				enable = 0,
				trigger = 0,
				state = 0,
				rconn = 0,
				lconn = 0,
				ifaction = 0
			] = splitPorts;

			this.numberOfInPorts = inports
			this.numberOfOutPorts = outports
			this.enablePort = enable
			this.triggerPort = trigger
			// this.statePort = state
			this.numberOfLconnPorts = rconn
			this.numberOfRconnPorts = lconn
			this.ifactionPort = ifaction
		} else {
			this.numberOfInPorts = 1
			this.numberOfOutPorts = 1
		}
	},
	initInPortsPosition() {
		this.inPortsPosition = this.getPortsPosition(this.numberOfInPorts + this.numberOfLconnPorts, this.inLandingFace).slice(0, this.numberOfInPorts)
	},
	initOutPortsPosition() {
		this.outPortsPosition = this.getPortsPosition(this.numberOfOutPorts + this.numberOfRconnPorts, this.outLandingFace).slice(0, this.numberOfOutPorts)
	},
	initLconnPortsPosition() {
		this.lconnPortsPosition = this.getPortsPosition(this.numberOfInPorts + this.numberOfLconnPorts, this.inLandingFace).slice(this.numberOfInPorts)
	},
	initRconnPortsPosition() {
		this.rconnPortsPosition = this.getPortsPosition(this.numberOfOutPorts + this.numberOfRconnPorts, this.outLandingFace).slice(this.numberOfOutPorts)
	},
	initSpecialPortsPosition() {
		if (!this.enablePort && !this.triggerPort && !this.ifactionPort) return

		let [port1, port2] = this.getPortsPosition(
			this.enablePort + this.triggerPort + this.ifactionPort,
			this.getSpecialPortLandingFace()
		)

		if (!this.ifactionPort) {
			this.enablePortPosition = this.enablePort ? port1 : null
			this.triggerPortPosition = this.triggerPort ? (this.enablePort ? port2 : port1) : null
		}

		this.ifactionPortPosition = this.ifactionPort ? port1 : null
	},
	initDragHandler() {
		this.outerShape = this.outerShape.call(d3.drag()
			.on("start", this.dragStart.bind(this))
			.on("drag", this.dragMove.bind(this))
			.on("end", this.dragEnd.bind(this))
		)
	},
	/* ===== OVERRIDE ===== */
	draw() {
		this.drawWrapper()
		this.drawShape()
		this.drawName()
		this.needHighLight = true;
		/*this.initDragHandler()*/
	},
	drawName(updating=false) {
		if (this.props.ShowName != 'off') {
			const placeAtTop = this.props.NamePlacement === 'alternate' || this.props.NameLocation === 'top'

			const yPlacement = placeAtTop
				? this.top - this.firstLineMargin - this.fontSize
				: this.bottom + this.firstLineMargin

			const lineSpacing = placeAtTop ? -this.fontSize : this.fontSize

			let nameLines = this.name.split('\\n')

			if (updating) {
				var nameElems = this.g2.selectAll('text')

				nameElems.each((d, i, g) => {
					const multiplier = placeAtTop ? nameLines.length - i - 1 : i

					d3.select(g[i])
						.attr('x', this.left + this.width / 2)
						.attr('y', yPlacement + lineSpacing * multiplier)
				})
			}
			else {
				this.g2 = this.outerG.append('g')

				nameLines.forEach((line, i) => {
					const multiplier = placeAtTop ? nameLines.length - i - 1 : i

					this.g2
						.append('text')
						.attr('x', this.left + this.width / 2)
						.attr('y', yPlacement + lineSpacing * multiplier)
						.attr('text-anchor', 'middle')
						.attr('alignment-baseline', 'hanging')
						.attr('font-size', this.fontSize)
						.attr('fill', this.foregroundColor)
						.text(line)
				})
			}
		}
	},
	drawShape(text) {
		this.points = `0,0 ${this.width},0 ${this.width},${this.height} 0 ${this.height}`

		this.drawShapeSvg()
		$(`#${this.stringId}`).dblclick(this.callback)

		this.shape = this.innerShape
			.append('polygon')
			.attr('points', this.points)
			.attr('stroke', this.foregroundColor)
			.attr('fill', this.backgroundColor)

		this.innerShape
			.append('text')
			.attr('x', this.width / 2)
			.attr('y', this.height / 2)
			.attr('text-anchor', 'middle')
			.attr('alignment-baseline', 'middle')
			.attr('font-size', this.fontSize)
			.text(text ?? this.blockType)
	},
	drawShapeSvg() {
		this.innerSvg = this.outerShape
			.append('svg')
			.attr('x', this.left)
			.attr('y', this.top)
			.attr('width', this.width)
			.attr('height', this.height)
			.attr('viewBox', `0 0 ${this.width} ${this.height}`)

		this.innerShape = this.innerSvg.append('g')
	},
	/* ===== OVERRIDE ===== */
	drawDotMarkers() {
		this.drawDotMarker(this.left, this.top)
		this.drawDotMarker(this.right, this.top)
		this.drawDotMarker(this.left, this.bottom)
		this.drawDotMarker(this.right, this.bottom)
	},
	getInPortPosition(portNumber) {
		return this.inPortsPosition[portNumber - 1]
	},
	getOutPortPosition(portNumber) {
		return this.outPortsPosition[portNumber - 1]
	},
	getLConnPortPosition(portNumber) {
		return this.lconnPortsPosition[portNumber - 1]
	},
	getRConnPortPosition(portNumber) {
		return this.rconnPortsPosition[portNumber - 1]
	},
	mapDegToOrientation(deg) {
		switch (deg) {
			case 0:
				return 'right'
			case 90:
				return 'down'
			case 180:
				return 'left'
			case 270:
				return 'up'
			default:
				throw new Error(`Impossible deg = ${deg}`)
		}
	},
	getPortsPosition(numberOfPorts, landingFace) {
		const { width, height, top, left, bottom, right } = this

		const partWidth = width / numberOfPorts
		const partHeight = height / numberOfPorts

		var portsLocation = []

		for (let i = 0; i < numberOfPorts; i++) {
			switch (landingFace) {
				case 'right':
					portsLocation.push(`${right}, ${top + partHeight / 2 + partHeight * i}`)
					break
				case 'down':
					portsLocation.push(`${left + partWidth / 2 + partWidth * i}, ${bottom}`)
					break
				case 'left':
					portsLocation.push(`${left}, ${top + partHeight / 2 + partHeight * i}`)
					break
				case 'up':
					portsLocation.push(`${left + partWidth / 2 + partWidth * i}, ${top}`)
					break
				default:
					throw new Error(`Impossible face: ${landingFace}`)
			}
		}

		return portsLocation
	},
	getSpecialPortLandingFace() {
		switch (this.inLandingFace) {
			case 'right':
				return 'down'
			case 'down':
				return 'left'
			case 'left':
				return 'up'
			case 'up':
				return 'right'
		}
	},
	navigateTo() {
		this.callback()
	},
	/* ==== OVERRIDE ==== */
	dragStart(evt) {
		this.isDragging = true;

		var svgNodes = this.outerShape.selectAll('svg')
		this.dragOffset = []
		if (!svgNodes.nodes().length) {
			this.handleInOutDragStart(evt)
		} else {
			this.handleSvgDragStart(evt, svgNodes)
		}
		

		


	},

	dragMove(evt) {
		if (this.isDragging) {
			
			this.left = evt.x + this.dragOffsetBase.x
			this.top = evt.y + this.dragOffsetBase.y
			this.right = this.left + this.width
			this.bottom = this.top + this.height

			var svgNodes = this.outerShape.selectAll('svg');
			if (!svgNodes.nodes().length) {
				this.handleInOutDragMove(evt);
			}
			else {
				this.handleSvgDragMove(evt, svgNodes);
			}
			// Call port position calculation
			this.initInPortsPosition()
			this.initOutPortsPosition()
			this.initSpecialPortsPosition()
			this.drawName(true)

			this.positionChangedCallbacks.forEach(callback => callback())
		}
	},
	dragEnd() {
		this.isDragging = false;
	},

	handleInOutDragStart(evt) {
		var InOutShapeNode = d3.select(this.outerShape.select("rect").node());
		var InOutTextNode = this.outerShape.select("text").node();

		var posShape = { x: Number(InOutShapeNode.attr("x")), y: Number(InOutShapeNode.attr("y")) }
		
		this.dragOffset.push({ x: posShape.x - evt.x, y: posShape.y - evt.y })
		this.dragOffsetBase = { x: this.left - evt.x, y: this.top - evt.y }
		
	},
	handleSvgDragStart(evt, svgNodes) {
		svgNodes.each((d, i, g) => {
			
			var svgD3Node = d3.select(g[i])
			
			const pos = { x: Number(svgD3Node.attr("x")), y: Number(svgD3Node.attr("y")) }
			
			this.dragOffset.push({ x: pos.x - evt.x, y: pos.y - evt.y })
		})

		this.dragOffsetBase = { x: this.left - evt.x, y: this.top - evt.y }

	},
	handleSvgDragMove(evt, svgNodes) {
		svgNodes.each(
			(d, i, g) => d3.select(g[i]).attr("x", evt.x + this.dragOffset[i].x).attr("y", evt.y + this.dragOffset[i].y));
	},
	handleInOutDragMove(evt) {
		
		var InOutShapeNode = d3.select(this.outerShape.select("rect").node());
		var InOutTextNode = d3.select(this.outerShape.select("text").node());
		InOutShapeNode.attr("x", evt.x + this.dragOffset[0].x).attr("y", evt.y + this.dragOffset[0].y);
		InOutTextNode.attr("x", evt.x + this.dragOffset[0].x + this.width / 2).attr("y", evt.y + this.dragOffset[0].y + this.height / 2);
	},
	initListSystemDraws(systemDraws) {
		this.systemDraws = systemDraws;
	},

	addOutport(outport) {
		let already = false;
		this.outport.forEach(el => {
			if (el.stringId == outport.stringId) {
				already = true;
			}
		})
		if (!already) {
			this.outport.push(outport);
		}
	},

	clearOutport() {
		this.outport = []
	},


}
export default System
