import System from './index.js'

var Ground = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		this.points =
			`${this.left},${this.top} ` +
			`${this.right},${this.top} ` +
			`${this.right},${this.bottom} ` +
			`${this.left},${this.bottom}`

		this.shape = this.outerShape
			.append('polygon')
			.attr('points', this.points)
			.attr('stroke', this.foregroundColor)
			.attr('fill', this.backgroundColor)

		const innerHeight = this.height * 0.9
		const innerWidth = this.width * 0.9

		this.icon = this.outerShape
			.append('svg')
			.attr('x', this.left + (this.width - innerWidth) / 2)
			.attr('y', this.top + (this.height - innerHeight) / 2)
			.attr('width', innerWidth)
			.attr('height', innerHeight)
			.attr('fill', this.foregroundColor)
			.attr('viewBox', '0 0 464 215')
			.append('g')
			.attr('transform', 'translate(0,215) scale(0.1,-0.1)')

		this.icon
			.append('path')
			.attr(
				'd',
				'M2300 1665 l0 -345 -1150 0 -1150 0 0 -25 0 -25 2320 0 2320 0 0 25 0 25 -1145 0 -1145 0 0 345 0 345 -25 0 -25 0 0 -345z'
			)

		this.icon.append('path').attr('d', 'M800 835 l0 -25 1520 0 1520 0 0 25 0 25 -1520 0 -1520 0 0 -25z')

		this.icon.append('path').attr('d', 'M1600 375 l0 -25 720 0 720 0 0 25 0 25 -720 0 -720 0 0 -25z')

		this.icon.append('path').attr('d', 'M1980 25 l0 -25 340 0 340 0 0 25 0 25 -340 0 -340 0 0 -25z')
	}
}

export default Ground
