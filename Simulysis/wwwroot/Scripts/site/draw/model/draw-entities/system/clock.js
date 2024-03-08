import System from './index.js'

var Clock = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		this.shape = this.outerShape
			.append('circle')
			.attr('cx', this.left + this.width / 2)
			.attr('cy', this.top + this.height / 2)
			.attr('r', this.width / 2)
			.attr('stroke', this.props.ForegroundColor ?? 'black')
			.attr('fill', 'white')

		this.icon = this.outerShape
			.append('svg')
			.attr('x', this.left)
			.attr('y', this.top)
			.attr('width', this.width)
			.attr('height', this.height)
			.attr('fill', this.props.ForegroundColor ?? 'black')
			.attr('viewBox', '0 0 305 305')
			.append('g')
			.attr('transform', 'translate(0,305) scale(0.1,-0.1)')

		this.icon
			.append('path')
			.attr(
				'd',
				'M1250 3030 c-297 -54 -580 -201 -795 -414 -162 -160 -277 -333 -354 -531 -83 -211 -105 -353 -98 -613 4 -156 9 -204 30 -287 76 -299 199 -518 412 -730 224 -225 473 -360 785 -426 142 -30 418 -33 560 -5 311 60 592 208 810	426 513 514 594 1310 192 1915 -224 339 -555 566 -957 657 -134 30 -441 35 -585 8z m580 -58 c361 -84 655 -273 870 -561 128 -169 218 -370 266 -591 15 -67 19 -128 19 -290 0 -178 -3 -219 -23 -310 -65 -290 -192 -521 -401 -731 -176 -175 -352 -284 -581 -359 -160 -53 -285 -72 -460 -72 -176 0 -303 19	-461 72 -223 74 -392 175 -557 334 -131 127 -216 239 -291 387 -113 222 -163 431 -163 679 0 404 145 754 431 1041 238 238 506 370 856 423 17 2 116 3 220 1 145 -3 210 -8 275 -23z'
			)

		this.icon
			.append('path')
			.attr(
				'd',
				'M1580 1990 l0 -530 535 0 c528 0 535 0 535 20 0 20 -7 20 -515 20	l-515 0 0 510 c0 503 0 510 -20 510 -20 0 -20 -7 -20 -530z'
			)
	}
}

export default Clock
