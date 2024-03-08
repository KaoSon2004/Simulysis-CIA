import System from './index.js'

var Sin = {
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

		const innerSize = this.width > this.height ? this.height * 0.9 : this.width * 0.9

		this.outerShape
			.append('svg')
			.attr('x', this.left + (this.width - innerSize) / 2)
			.attr('y', this.top + (this.height - innerSize) / 2)
			.attr('width', innerSize)
			.attr('height', innerSize)
			.attr('fill', this.foregroundColor)
			.attr('viewBox', '0 0 500 500')
			.append('g')
			.attr('transform', 'translate(0,500) scale(0.1,-0.1)')
			.append('path')
			.attr(
				'd',
				'M120 2500 l0 -2370 30 0 30 0 0 1170 0 1170 1154 0 1153 0 43 -112 c507 -1333 673 -1722 849 -1988 113 -170 220 -249 323 -237 200 24 384 325 723 1182 136 342 445 1169 445 1189 0 14 -113 16 -1179 16 l-1178 0 -63 168 c-502 1342 -720 1825 -914 2024 -68 70 -125 98 -196 98 -231 0 -437 -352 -927 -1590 -90 -228 -179 -453 -198 -500 l-34 -85 -1 1118 0 1117 -30 0 -30 0 0 -2370z m1296 2244 c18 -9 63 -47 98 -85 179 -189 388 -654 851 -1888 40 -108 78 -208 84 -223 l11 -28 -1134 0 -1134 0 75 193 c146 377 365 923 454 1131 227 531 409 834 541 899 42 21 111 21 154 1z m3365 -2351 c-250 -676 -485 -1278 -596 -1528 -224 -503 -383 -708 -528 -681 -210 39 -458 532 -1017 2026 -32 85 -67 179 -79 208 l-21 52 1135 0 1134 0 -28 -77z'
			)
	}
}

export default Sin
