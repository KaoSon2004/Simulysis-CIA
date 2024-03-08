import System from './index.js'

var RelationalOperator = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		super.drawShape(this.props.Operator ?? '>=')
	}
}

export default RelationalOperator
