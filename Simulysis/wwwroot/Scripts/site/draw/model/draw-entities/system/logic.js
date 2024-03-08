import System from './index.js'

var Logic = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		super.drawShape(this.props.Operator ?? 'AND')
	}
}

export default Logic
