import System from './index.js'

var Port = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		const rx = 5
		const ry = this.height / 2

		this.shape = this.outerShape
			.append('rect')
			.attr('x', this.left)
			.attr('y', this.top)
			.attr('width', this.width)
			.attr('height', this.height)
			.attr('rx', rx)
			.attr('ry', ry)
			.attr('fill', this.backgroundColor)
			.attr('stroke', this.foregroundColor)

		// port number
		this.outerShape
			.append('text')
			.attr('x', this.left + this.width / 2)
			.attr('y', this.top + this.height / 2)
			.attr('text-anchor', 'middle')
			.attr('alignment-baseline', 'middle')
			.attr('font-size', this.fontSize)
			.attr('fill', this.foregroundColor)
			.text(this.props.Port ?? 1)
	}
}

export default Port
