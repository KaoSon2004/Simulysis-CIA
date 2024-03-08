import Common from './common.js'

var Line = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: Common,

	/*
	 * PROPERTIES
	 */
	isBranch: false,
	src: null,
	srcPort: -1,
	srcPortType: null,
	dst: null,
	dstPort: -1,
	dstPortType: null,
	changes: [],
	points: [],
	branchDraws: [],
	shape: null,

	/*
	 * For Trace Signal
	 */
	systemPort: null,

	/*
	 * METHODS
	 */

	/* ===== OVERRIDE ===== */
	init(parentElement, line, sidNameObj, branchObj, startPos, color, viewManager) {

		this.isBranch = typeof startPos == 'string'

		super.init(parentElement, line.id, this.isBranch ? 'branch' : 'line', line.properties, viewManager)

		this.initSource(sidNameObj)
		this.initDestination(sidNameObj)

		this.foregroundColor = color ?? (this.src ? this.src.foregroundColor : 'black')

		this.initChildBranches(sidNameObj, branchObj)

		this.changes = this.props.Points ? this.props.Points.slice(1, -1).split(';') : []

		this.points = [this.stringPositionToNumberArr(this.isBranch ? startPos : this.getStartPosFromSrc())]
		this.systemPort = null;
		return this
	},
	/* ===== OVERRIDE ===== */
	calculateLinePoints() {
		const padding = 5

		this.changes.forEach((point, i) => {
			const [x1, y1] = this.points.at(-1)
			const [px, py] = this.stringPositionToNumberArr(point)

			var x2 = x1 + px
			var y2 = y1 + py

			if (i == 0 && !this.isBranch) {
				x2 += this.srcLandingFace == 'right' ? padding : this.srcLandingFace == '-left' ? -padding : 0
				y2 += this.srcLandingFace == 'down' ? padding : this.srcLandingFace == 'up' ? -padding : 0
			}

			this.points.push([x2, y2])
		})

		if (this.dst) {
			const [currentX, currentY] = this.points.at(-1)
			const [endX, endY] = this.stringPositionToNumberArr(this.getEndPosFromDst())

			const minimumDiff = 6

			if (currentX == endX || currentY == endY) {
				this.points.push([endX, endY])
			} else if (Math.abs(currentX - endX) <= minimumDiff) {
				if (this.dst.blockType == 'Sum' && this.dst.getInPortNeedBreakPoint(this.dstPort)) {
					this.points.push([currentX, this.dst.getBreakPointPositionY(this.dstPort, currentX)])
					this.points.push([endX, endY])
				} else {
					this.points.push([currentX, endY])
				}
			} else if (Math.abs(currentY - endY) <= minimumDiff) {
				if (this.dst.blockType == 'Sum' && this.dst.getInPortNeedBreakPoint(this.dstPort)) {
					this.points.push([this.dst.getBreakPointPositionX(this.dstPort, currentY), currentY])
					this.points.push([endX, endY])
				} else {
					this.points.push([endX, currentY])
				}
			} else {
				const movedHorizontal = this.changes.at(-1) ? Number(this.changes.at(-1).split(',')[0]) != 0 : false

				if (this.dst.blockType == 'Sum' && this.dst.getInPortNeedBreakPoint(this.dstPort)) {
					this.points.push(
						movedHorizontal
							? [currentX, this.dst.getBreakPointPositionY(this.dstPort, currentX)]
							: [this.dst.getBreakPointPositionX(this.dstPort, currentY), currentY]
					)
				} else {
					this.points.push(movedHorizontal ? [currentX, endY] : [(endX + currentX) / 2, currentY])
					this.points.push(movedHorizontal ? [currentX, endY] : [(endX + currentX) / 2, endY])
				}

				this.points.push([endX, endY])
			}
		} else {
			this.branchDraws.forEach(branchDraw => {
				branchDraw.points = [this.points.at(-1)]
			})
		}
	},
	draw() {
		this.drawWrapper()

		this.shape = this.outerShape.append('g')

		this.calculateLinePoints()

		if (!this.dst) {
			this.branchDraws.forEach(branchDraw => {
				branchDraw.draw()
			})
		}

		// for lines that do not have src or dst
		const dashLength = 1.5
		const isHangingLine =
			!this.isBranch && (!this.src || (this.src && !this.dst && this.branchDraws.length == 0))

		isHangingLine && (this.foregroundColor = '#ff2617')
		const isSrcConnection = this.srcPortType === 'lconn' || this.srcPortType === 'rconn'
		const isDstConnection = this.dstPortType === 'lconn' || this.dstPortType === 'rconn'

		this.path = this.shape
			.append('path')
			.attr('d', d3.line()(this.points))
			.attr('stroke', this.foregroundColor)
			.attr('stroke-linejoin', 'round')
			.attr('stroke-dasharray', !isHangingLine ? 0 : dashLength)
			.attr('fill', 'none')
			.attr(
				'marker-end',
				isSrcConnection ? '' : this.dst ? 'url(#filledTriangleMarker)' : isHangingLine ? 'url(#triangleMarker)' : ''
			)
			.attr('marker-start', isDstConnection ? '' : this.isBranch ? 'url(#circleMarker' : !this.src ? 'url(#triangleMarker)' : '')

		this.needHighLight = true;
	},
	initChildBranches(sidNameObj, branchObj) {
		if (this.dst) {
			this.branchDraws = []
			return
		}

		this.branchDraws = (branchObj[this.stringId] ?? []).map(branch =>
			Object.create(Line).init(
				this.parentElement,
				branch,
				sidNameObj,
				branchObj,
				'fake start pos', // just a value for this to be recognized as a branch not a line, we will update it when we draw
				this.foregroundColor
			)
		)


	},
	initSource(sidNameObj) {
		if (this.isBranch) return

		if (this.props.Src) {
			this.src = sidNameObj[this.props.Src.split('#')[0]]
			this.srcPort = Number(this.props.Src.split('#')[1].split(':')[1])
			this.srcPortType = this.props.Src.split('#')[1].split(':')[0]
		} else if (this.props.SrcBlock) {
			this.src = sidNameObj[this.props.SrcBlock]
			this.srcPort = Number(this.props.SrcPort)
		}

		// case special source port
		if (Number.isNaN(this.srcPort)) {
			if (this.props.Src) {
				this.srcPort = this.props.Src.split('#')[1]
			} else if (this.props.SrcBlock) {
				this.srcPort = this.props.SrcPort
			}
		}

		if (this.src) {
			this.src.positionChangedCallbacks.push(() => this.updateLinePositions(true))
		}
	},
	initDestination(sidNameObj) {
		if (this.props.Dst) {
			this.dst = sidNameObj[this.props.Dst.split('#')[0]]
			this.dstPort = Number(this.props.Dst.split('#')[1].split(':')[1])
			this.dstPortType = this.props.Dst.split('#')[1].split(':')[0]
		} else if (this.props.DstBlock) {
			this.dst = sidNameObj[this.props.DstBlock]
			this.dstPort = Number(this.props.DstPort)
		}

		// case special destination port
		if (Number.isNaN(this.dstPort)) {
			if (this.props.Dst) {
				this.dstPort = this.props.Dst.split('#')[1]
			} else if (this.props.DstBlock) {
				this.dstPort = this.props.DstPort
			}
		}

		if (this.dst) {
			this.dst.positionChangedCallbacks.push(() => this.updateLinePositions(true))
		}
	},
	/* ===== OVERRIDE ===== */
	drawDotMarkers() {
		this.points.forEach(point => this.drawDotMarker(point[0], point[1]))
	},
	stringPositionToNumberArr(position) {
		if (!position) {
			console.log();
		} 
		return position.split(',').map(Number)
	},
	getStartPosFromSrc() {
		if (!this.src) return this.changes.shift()

		switch (true) {
			case typeof this.srcPort == 'number':
				if (this.srcPortType === 'lconn') {
					return this.src.getLConnPortPosition(this.srcPort)
				}
				else if (this.srcPortType === 'rconn') {
					return this.src.getRConnPortPosition(this.srcPort)
				}
				else {
					return this.src.getOutPortPosition(this.srcPort)
				}
			// case this.srcPort.includes('LConn'):
			// 	return ''
			// case this.srcPort.includes('RConn'):
			// 	return ''
			default:
				console.error(`Unsupported port type: ${this.srcPort}`)
				return ''
		}
	},
	getEndPosFromDst() {
		switch (true) {
			case typeof this.dstPort == 'number':
				if (this.dstPortType === 'lconn') {
					return this.dst.getLConnPortPosition(this.dstPort)
				}
				else if (this.dstPortType === 'rconn') {
					return this.dst.getRConnPortPosition(this.dstPort)
				}
				else {
					return this.dst.getInPortPosition(this.dstPort)
				}
			// case this.dstPort.includes('LConn'):
			// 	return ''
			// case this.dstPort.includes('RConn'):
			// 	return ''
			case this.dstPort == 'enable':
				return this.dst.enablePortPosition
			case this.dstPort == 'trigger':
				return this.dst.triggerPortPosition
			case this.dstPort == 'ifaction':
				return this.dst.ifactionPortPosition
			default:
				console.error(`Unsupported port type: ${this.dstPort}`)
				return ''
		}
	},
	updateLinePositions(needUpdateInitialPos) {
		if (needUpdateInitialPos) {
			this.points = [this.stringPositionToNumberArr(this.getStartPosFromSrc())]
		}

		this.calculateLinePoints()

		// Update points
		this.path = this.path.attr('d', d3.line()(this.points))

		if (!this.dst) {
			this.branchDraws.forEach(branchDraw => {
				branchDraw.updateLinePositions(false)
			})
		}
	}
}

export default Line