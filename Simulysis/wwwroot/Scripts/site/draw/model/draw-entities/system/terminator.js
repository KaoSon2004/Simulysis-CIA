import System from './index.js'

var Terminator = {
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

		const size = this.width > this.height ? this.height * 0.9 : this.width * 0.9
		this.outerShape
			.append('svg')
			.attr('x', this.left + this.width / 2 - size / 2)
			.attr('y', this.top + this.height / 2 - size / 2)
			.attr('width', size)
			.attr('height', size)
			.attr('viewBox', '0 0 172 172')
			.attr('preserveAspectRatio', 'xMidYMid meet')
			.append('g')
			.attr('transform', 'translate(0,172) scale(0.1,-0.1)')
			.attr('fill', '#000')
			.attr('stroke', 'none')
			.append('path')
			.attr(
				'd',
				'M570 1355 l0 -25 450 0 450 0 0 -225 0 -225 -700 0 -700 0 0 -25 0 -25 700 0 700 0 0 -225 0 -225 -460 0 -460 0 0 -25 0 -25 485 0 485 0 0 525 0 525 -475 0 -475 0 0 -25z'
			)
	}
}

export default Terminator
