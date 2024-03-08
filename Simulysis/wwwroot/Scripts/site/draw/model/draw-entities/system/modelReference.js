import System from './index.js'

var ModelReference = {
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

		const modelNamePadding = 15
		const portPadding = 3

		this.innerShape
			.append('text')
			.attr('x', this.width / 2)
			.attr('y', modelNamePadding)
			.attr('text-anchor', 'middle')
			.attr('fill', this.foregroundColor)
			.attr('font-size', this.fontSize)
			.text(this.props.ModelNameDialog.replace('.mdl', '').replace('.slx', ''))

		var inportNameList = this.lists.find(list => list.props.ListType === 'InputPortNames') ?? this.lists[0]
		var outportNameList = this.lists.find(list => list.props.ListType === 'OutputPortNames') ?? this.lists[1]

		this.drawPortFromNameList(inportNameList, portPadding)
		this.drawPortFromNameList(outportNameList, this.width - portPadding, 'end')
	},
	drawPortFromNameList(portNameList, xPos, textAnchor = 'start') {
		if (!portNameList) return

		var portsObj = portNameList.props

		// FOR .SLX FILES ONLY
		// check if port name is not need to be displayed
		// because we just take the first item from the list
		// so we have to check to make sure we chose the right item
		if (!Object.values(portsObj).every(value => typeof value == 'string' && value != 'off')) return

		var portsObjKeys = Object.keys(portsObj).filter(key => key.startsWith('port'))

		portsObjKeys.forEach(port => {
			const partSize = this.height / portsObjKeys.length
			const portNumber = Number(port.replace('port', ''))

			this.innerShape
				.append('text')
				.attr('x', xPos)
				.attr('y', partSize / 2 + portNumber * partSize)
				.attr('text-anchor', textAnchor)
				.attr('alignment-baseline', 'middle')
				.attr('fill', this.foregroundColor)
				.attr('font-size', this.fontSize)
				.text(portsObj[port])
		})
	}
}

export default ModelReference
