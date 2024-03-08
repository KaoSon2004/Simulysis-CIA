import System from './index.js'

var Reference = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: System,

	/* ===== OVERRIDE ===== */
	drawShape() {
		try {
			this.drawReferenceShapeWithVal()
		} catch {
			this.drawReferenceShape()
		}
	},
	drawReferenceShape() {
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

		let lines = this.props.SourceBlock.split('/').slice(-1)[0].split('\\n')

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

		this.icon = this.outerShape
			.append('svg')
			.attr('viewBox', '0 0 239 239')
			.attr('x', this.left)
			.attr('y', this.bottom - 15)
			.attr('width', 15)
			.attr('height', 15)
			.append('g')
			.attr('transform', 'translate(0,239) scale(0.1,-0.1)')
			.attr('fill', this.foregroundColor)
			.attr('stroke', 'none')

		this.icon
			.append('path')
			.attr(
				'd',
				'M1340 2090 l0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 200 0 200 0 0 50 0 50 100 0 100 0 0 50 0 50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -100 0 -100 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50	0 -50 -50 0 -50 0 0 -50 0 -50 -150 0 -150 0 0 50 0 50 -100 0 -100 0 0 -50 0	-50 -50 0 -50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 200	0 200 0 0 50 0 50 100 0 100 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 300 0 300 -50 0	-50 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -300 0 -300 0 0 -50z'
			)

		this.icon
			.append('path')
			.attr(
				'd',
				'M940 1590 l0 -50 -100 0 -100 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -300 0 -300 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 100 0 100 0 0 -50 0 -50 50 0 50 0 0 50 0 50 150 0 150 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 -200 0 -200 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -100 0 -100 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -50 0 -50 0 0 100 0 100 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0	50 0 0 50 0 50 50 0 50 0 0 50 0 50 150 0 150 0 0 -50 0 -50 100 0 100 0 0 50	0 50 50 0 50 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -100 0 -100 0 0 50 0 50 -150	0 -150 0 0 -50z'
			)
	},
	drawReferenceShapeWithVal() {
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

		var instanceDataProps = JSON.parse(this.instanceDatas[0].properties)

		var lines
		switch (this.props.SourceType) {
			//case 'SubSystem':
			//	lines = instanceDataProps.ContentPreviewEnabled
			//	break
			case 'MC_BackUpRAM':
				lines = instanceDataProps.x0
				break
			case 'MSK_Saturation':
				lines = instanceDataProps.msk_min + '->' + instanceDataProps.msk_max
				break
			case 'MC_EEPROM':
				lines = instanceDataProps.x0
				break
			case 'MSK_PreProcessorIf':
				lines = instanceDataProps.ifConstant1
				break
			case 'MSK_Table':
				lines = instanceDataProps.Label
				break
			case 'MSK_Map':
				lines = instanceDataProps.Label
				break
			case 'MSK_Table_i':
				lines = instanceDataProps.Label
				break
			case 'MSK_Map_i':
				lines = instanceDataProps.Label
				break
			case 'MSK_Interpolate1D':
				lines = instanceDataProps.Label
				break
			case 'MSK_Interpolate2D':
				lines = instanceDataProps.Label
				break
			case 'MSK_Interpolate1D_i':
				lines = instanceDataProps.Label
				break
			case 'MSK_Interpolate2D_i':
				lines = instanceDataProps.Label
				break
			case 'MSK_Gain':
				lines = instanceDataProps.Gain
				break
			case 'MSK_Constant':
				lines = instanceDataProps.Value
				break
			case 'MSK_Index':
				lines = instanceDataProps.table_name
				break
			default:
				lines = this.props.SourceBlock.split('/').slice(-1)[0].split('\\n')
				break
		}

		//this.innerText = this.innerShape
		//	.append('text')
		//	.attr('transform', `translate(${this.width / 2})`)
		//	.attr('fill', this.foregroundColor)
		//	.attr('font-size', this.fontSize)

		//lines.forEach((line, i) =>
		//	this.innerText
		//		.append('tspan')
		//		.attr('text-anchor', 'middle')
		//		.attr('x', 0)
		//		.attr('dy', i != 0 ? this.fontSize : 0)
		//		.text(line)
		//)
		this.innerText = this.innerShape
			.append('text')
			.attr('x', this.width / 2)
			.attr('y', this.height / 2)
			.attr('text-anchor', 'middle')
			.attr('alignment-baseline', 'middle')
			.attr('fill', this.props.ForegroundColor ?? 'black')
			.attr('font-size', this.fontSize)
			.text(lines)

		this.innerText.attr('y', (this.height - this.innerText.node().getBBox().height) / 2 + this.fontSize)

		this.icon = this.outerShape
			.append('svg')
			.attr('viewBox', '0 0 239 239')
			.attr('x', this.left)
			.attr('y', this.bottom - 15)
			.attr('width', 15)
			.attr('height', 15)
			.append('g')
			.attr('transform', 'translate(0,239) scale(0.1,-0.1)')
			.attr('fill', this.foregroundColor)
			.attr('stroke', 'none')

		this.icon
			.append('path')
			.attr(
				'd',
				'M1340 2090 l0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 200 0 200 0 0 50 0 50 100 0 100 0 0 50 0 50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -100 0 -100 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50	0 -50 -50 0 -50 0 0 -50 0 -50 -150 0 -150 0 0 50 0 50 -100 0 -100 0 0 -50 0	-50 -50 0 -50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 200	0 200 0 0 50 0 50 100 0 100 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 300 0 300 -50 0	-50 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -300 0 -300 0 0 -50z'
			)

		this.icon
			.append('path')
			.attr(
				'd',
				'M940 1590 l0 -50 -100 0 -100 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -50 0 -50 0 0 -300 0 -300 50 0 50 0 0 -50 0 -50 50 0 50 0 0 -50 0 -50 100 0 100 0 0 -50 0 -50 50 0 50 0 0 50 0 50 150 0 150 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 -200 0 -200 0 0 -50 0 -50 -50 0 -50 0 0 -50 0 -50 -100 0 -100 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -50 0 -50 0 0 100 0 100 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0 50 0 0 50 0 50 50 0	50 0 0 50 0 50 50 0 50 0 0 50 0 50 150 0 150 0 0 -50 0 -50 100 0 100 0 0 50	0 50 50 0 50 0 0 50 0 50 -50 0 -50 0 0 50 0 50 -100 0 -100 0 0 50 0 50 -150	0 -150 0 0 -50z'
			)
	}
}

export default Reference
