import System from './index.js'

var Scope = {
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

		if (this.height > 10) {
			const innerWidth = this.width * 0.8
			const innerHeight = this.height * 0.5

			this.outerShape
				.append('rect')
				.attr('x', this.left + (this.width - innerWidth) / 2)
				.attr('y', this.top + (this.height - innerHeight) / 4)
				.attr('width', innerWidth)
				.attr('height', innerHeight)
				.attr('stroke', this.foregroundColor)
				.attr('fill', this.backgroundColor)
		}
	}
}

export default Scope
