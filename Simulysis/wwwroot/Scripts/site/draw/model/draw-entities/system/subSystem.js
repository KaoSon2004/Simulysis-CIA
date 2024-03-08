import System from './index.js'

var SubSystem = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		this.points = `0,0 ${this.width},0 ${this.width},${this.height} 0 ${this.height}`

		this.drawShapeSvg()

		$(`#${this.stringId}`).dblclick(this.callback)
		$(`#${this.stringId}`).on('DOMMouseScroll mousewheel', e => {
			const delta = e.originalEvent.wheelDelta ?? -e.originalEvent.detail
			if (!e.ctrlKey && delta < 0) {
				this.callback()
			}

			e.preventDefault()
		})

		this.shape = this.innerShape
			.append('polygon')
			.attr('points', this.points)
			.attr('stroke', this.foregroundColor)
			.attr('fill', this.backgroundColor)

		if (this.props.MaskDisplay && this.props.MaskDisplay.includes('disp')) {
			let lines = this.props.MaskDisplay.replace("disp('", '').replace("')", '').split('\\\\n')

			this.innerText = this.innerShape
				.append('text')
				.attr('transform', `translate(${this.width / 2})`)
				.attr('fill', this.foregroundColor)
				.attr('font-size', this.fontSize)

			lines.forEach((line, i) =>
				this.innerText
					.append('tspan')
					.attr('text-anchor', 'middle')
					.attr('x', 0)
					.attr('dy', i != 0 ? this.fontSize : 0)
					.text(line)
			)

			this.innerText.attr('y', (this.height - this.innerText.node().getBBox().height) / 2 + this.fontSize)
		} else if (this.props.ShowPortLabels === 'on') {
			let inports = this.ports.filter(port => port.blockType == 'Inport')
			let outports = this.ports.filter(port => port.blockType == 'Outport')
			const portPadding = 3

			inports.forEach(port => {
				const partSize = this.height / inports.length
				const portString = JSON.parse(port.properties).Port
				const portNumber = portString ? Number(portString) - 1 : 0

				this.innerShape
					.append('text')
					.attr('x', portPadding)
					.attr('y', partSize / 2 + portNumber * partSize)
					.attr('alignment-baseline', 'middle')
					.attr('fill', this.foregroundColor)
					.attr('font-size', this.fontSize)
					.text(port.name)
			})

			outports.forEach(port => {
				const partSize = this.height / outports.length
				const portString = JSON.parse(port.properties).Port
				const portNumber = portString ? Number(portString) - 1 : 0

				this.innerShape
					.append('text')
					.attr('x', this.width - portPadding)
					.attr('y', partSize / 2 + portNumber * partSize)
					.attr('text-anchor', 'end')
					.attr('alignment-baseline', 'middle')
					.attr('fill', this.foregroundColor)
					.attr('font-size', this.fontSize)
					.text(port.name)
			})
		}
	}
}

export default SubSystem
