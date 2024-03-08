import System from './index.js'

var Mux = {
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
			.attr('fill', this.foregroundColor)
	}
}

export default Mux
