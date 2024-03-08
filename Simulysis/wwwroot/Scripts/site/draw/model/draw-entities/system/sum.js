import System from './index.js'

var Sum = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/*
	 * PROPERTIES
	 */
	r: -1,
	cx: -1,
	cy: -1,
	inputsPosition: [],
	inPortsDeg: [],

	/*
	 * METHODS
	 */

	/* ===== OVERRIDE ===== */
	init(parentElement, system, ports, callback, lists, instanceDatas) {
		this.inPortsPosition = []
		this.inPortsDeg = []
		this.inputsPosition = []

		super.init(parentElement, system, ports, callback, lists, instanceDatas)

		this.r = this.width / 2
		this.cx = this.left + this.r
		this.cy = this.top + this.r

		return this
	},
	/* ===== OVERRIDE ===== */
	drawShape() {
		this.shape = this.outerShape
			.append('circle')
			.attr('cx', this.cx)
			.attr('cy', this.cy)
			.attr('r', this.r)
			.attr('stroke', this.foregroundColor)
			.attr('fill', this.backgroundColor)

		const filteredInputs = this.props.Inputs ? this.props.Inputs.replaceAll('|', '') : this.props.Ports.split(',')

		for (let i = 0; i < filteredInputs.length; i++) {
			const pos = this.inputsPosition[i].split(',')

			this.outerShape
				.append('text')
				.attr('x', Number(pos[0]))
				.attr('y', Number(pos[1]))
				.attr('text-anchor', 'middle')
				.attr('dominant-baseline', 'middle')
				.attr('font-size', this.fontSize)
				.attr('fill', this.foregroundColor)
				.text(filteredInputs[i])
		}
	},
	/* ===== OVERRIDE ===== */
	initInPortsPosition() {
		let inputs = this.props.Inputs
		if (!inputs)
		{
			inputs = this.props.Ports.split(',')
		}

		const r = this.width / 2
		const cx = this.left + r
		const cy = this.top + r

		for (let i = 0; i < inputs.length; i++) {
			if (inputs[i] == '|') continue

			let deg = (180 / (inputs.length - 1)) * i

			switch (this.inLandingFace) {
				case 'left':
					deg = 360 - deg
					break
				case 'up':
					deg = -90 + deg
					break
				case 'right':
					break
				case 'down':
					deg = 270 - deg
					break
				default:
					throw new Error(`Impossible face = ${this.inLandingFace}`)
			}

			this.inPortsDeg.push(deg)

			deg = (deg * Math.PI) / 180

			this.inPortsPosition.push(`${cx + r * Math.sin(deg)}, ${cy - r * Math.cos(deg)}`)

			const inputR = r - 5
			this.inputsPosition.push(`${cx + inputR * Math.sin(deg)}, ${cy - inputR * Math.cos(deg)}`)
		}
	},
	getInPortNeedBreakPoint(portNumber) {
		return this.inPortsDeg[portNumber - 1] % 90 != 0
	},
	getBreakPointPositionX(portNumber, currentY) {
		const { cx, cy } = this

		const deg = (this.inPortsDeg[portNumber - 1] * Math.PI) / 180

		const breakPointR = (cy - currentY) / Math.cos(deg)

		return cx + breakPointR * Math.sin(deg)
	},
	getBreakPointPositionY(portNumber, currentX) {
		const { cx, cy } = this

		const deg = (this.inPortsDeg[portNumber - 1] * Math.PI) / 180

		const breakPointR = (currentX - cx) / Math.sin(deg)

		return cy - breakPointR * Math.cos(deg)
	}
}

export default Sum
