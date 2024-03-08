import System from './index.js'

var Constant = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		this.points = `0,0 ${this.width},0 ${this.width},${this.height} 0 ${this.height}`

		this.drawShapeSvg()

		this.shape = this.innerShape
			.append('polygon')
			.attr('points', this.points)
			.attr('stroke', this.props.ForegroundColor ?? 'black')
			.attr('fill', 'white')

		this.innerText = this.innerShape
			.append('text')
			.attr('x', this.width / 2)
			.attr('y', this.height / 2)
			.attr('text-anchor', 'middle')
			.attr('alignment-baseline', 'middle')
			.attr('fill', this.props.ForegroundColor ?? 'black')
			.attr('font-size', this.fontSize)
			.text(this.props.Value)

		this.innerText.text(
			this.height > this.fontSize && this.width > this.innerText.node().getComputedTextLength()
				? this.props.Value
				: '-C-'
		)
	}
}

export default Constant
