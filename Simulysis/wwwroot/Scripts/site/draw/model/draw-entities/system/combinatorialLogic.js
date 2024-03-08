import System from './index.js'

var CombinatorialLogic = {
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

		const size = this.width > this.height ? this.height * 0.7 : this.width * 0.7

		this.icon = this.outerShape
			.append('svg')
			.attr('x', this.left + (this.width - size) / 2)
			.attr('y', this.top + (this.height - size) / 2)
			.attr('width', size)
			.attr('height', size)
			.attr('fill', this.props.ForegroundColor ?? 'black')
			.attr('viewBox', '0 0 94 94')
			.append('g')
			.attr('transform', 'translate(0,94) scale(0.1,-0.1)')

		this.icon
			.append('path')
			.attr(
				'd',
				'M0 470 l0 -470 80 0 c64 0 80 3 80 15 0 12 -14 15 -65 15 l-65 0 0 440 0 440 65 0 c51 0 65 3 65 15 0 12 -16 15 -80 15 l-80 0 0 -470z'
			)

		this.icon
			.append('path')
			.attr(
				'd',
				'M780 925 c0 -12 14 -15 65 -15 l65 0 0 -440 0 -440 -65 0 c-51 0 -65 -3 -65 -15 0 -12 16 -15 80 -15 l80 0 0 470 0 470 -80 0 c-64 0 -80 -3 -80 -15z'
			)

		this.icon
			.append('path')
			.attr('d', 'M200 675 c0 -42 3 -55 15 -55 12 0 15 13 15 55 0 42 -3 55 -15 55 -12 0 -15 -13 -15 -55z')

		this.icon
			.append('path')
			.attr('d', 'M460 675 c0 -42 3 -55 15 -55 12 0 15 13 15 55 0 42 -3 55 -15 55 -12 0 -15 -13 -15 -55z')

		this.icon
			.append('path')
			.attr('d', 'M720 675 c0 -42 3 -55 15 -55 12 0 15 13 15 55 0 42 -3 55 -15 55 -12 0 -15 -13 -15 -55z')

		this.icon
			.append('path')
			.attr('d', 'M200 470 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')

		this.icon
			.append('path')
			.attr('d', 'M460 470 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')

		this.icon
			.append('path')
			.attr('d', 'M720 470 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')

		this.icon
			.append('path')
			.attr('d', 'M200 260 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')

		this.icon
			.append('path')
			.attr('d', 'M460 260 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')

		this.icon
			.append('path')
			.attr('d', 'M720 260 c0 -47 3 -60 15 -60 12 0 15 13 15 60 0 47 -3 60 -15 60 -12 0 -15 -13 -15 -60z')
	}
}

export default CombinatorialLogic
