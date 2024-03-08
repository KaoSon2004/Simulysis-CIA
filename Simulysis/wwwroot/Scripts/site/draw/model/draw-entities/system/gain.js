import System from './index.js'

var Gain = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		const isMirror = this.props.BlockMirror === 'on'

		this.points = isMirror
			? `${this.left},${this.top + this.height / 2} ` +
			  `${this.right},${this.top} ` +
			  `${this.right},${this.bottom}`
			: `${this.left},${this.top} ` +
			  `${this.right},${this.top + this.height / 2} ` +
			  `${this.left},${this.bottom}`

		this.shape = this.outerShape
			.append('polygon')
			.attr('points', this.points)
			.attr('stroke', this.foregroundColor)
			.attr('fill', this.backgroundColor)

		this.outerShape
			.append('text')
			.attr('x', this.left + this.width / 2)
			.attr('y', this.top + this.height / 2)
			.attr('text-anchor', isMirror ? 'start' : 'end')
			.attr('alignment-baseline', 'middle')
			.attr('font-size', this.fontSize)
			.attr('fill', this.foregroundColor)
			.text(this.props.Gain)
	},
	/* ===== OVERRIDE ===== */
	drawDotMarkers() {
		var markerPoints = this.points.split(' ')

		markerPoints.forEach(point => {
			const [x, y] = point.split(',')

			this.drawDotMarker(x, y)
		})
	}
}

export default Gain
