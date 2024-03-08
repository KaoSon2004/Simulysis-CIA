import System from './index.js'

var Goto = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		super.drawShape(this.props.GotoTag ?? 'A')
	}
}

export default Goto
