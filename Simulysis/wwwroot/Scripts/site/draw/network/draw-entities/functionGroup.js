import { noopGenerator } from '../../../noop.js'

var FunctionGroup = {
	name: null,
	count: null,
	position: null,
	edgeLength: 100,
	fontSize: 14,
	wrapper: null,
	outlineWrap: null,
	outline: null,
	stringId: null,
	lastGroupLevel: false,

	init(
		parent,
		name,
		count,
		position,
		lastGroupLevel,
		getClickHandler = noopGenerator,
		{ edgeLength = 100, fontSize = 14 } = {}
	) {
		this.name = name
		this.count = count
		this.position = position
		this.lastGroupLevel = lastGroupLevel
		this.edgeLength = edgeLength
		this.fontSize = fontSize
		this.stringId = `funcGroup-${this.name}`

		this.wrapper = parent
			.append('g')
			.attr('id', this.stringId)
			.style('cursor', 'pointer')
			.on('mouseenter', this.showShadow.bind(this))
			.on('mouseleave', this.hideShadow.bind(this))
			.on('dblclick', getClickHandler(name, lastGroupLevel))

		$(`#${this.stringId}`).on('DOMMouseScroll mousewheel', e => {
			const delta = e.originalEvent.wheelDelta ?? -e.originalEvent.detail
			if (!e.ctrlKey && delta < 0) {
				getClickHandler(name, lastGroupLevel)()
			}

			e.preventDefault()
		})

		return this
	},
	draw() {
		const { x, y } = this.position

		// the path is empty in the inside
		// so handler only run when the cursor is on the edge
		// this transparent square is like a mask to make the handler run everywhere
		this.wrapper
			.append('rect')
			.attr('width', this.edgeLength)
			.attr('height', this.edgeLength)
			.attr('x', x - this.edgeLength / 2)
			.attr('y', y - this.edgeLength / 2)
			.attr('fill', 'transparent')

		this.outlineWrap = this.wrapper
			.append('svg')
			.attr('viewBox', '0 0 512 512')
			.attr('width', this.edgeLength)
			.attr('height', this.edgeLength)
			.attr('x', x - this.edgeLength / 2)
			.attr('y', y - this.edgeLength / 2)

		this.outline = this.outlineWrap
			.append('path')
			.attr(
				'd',
				'M501.333,96H10.667C4.779,96,0,100.779,0,106.667v298.667C0,411.221,4.779,416,10.667,416h490.667 c5.888,0,10.667-4.779,10.667-10.667V106.667C512,100.779,507.221,96,501.333,96z M490.667,394.667H21.333V117.333h469.333 V394.667z'
			)

		var innerText = this.wrapper.append('text').attr('font-size', this.fontSize).attr('y', y)

		innerText.append('tspan').attr('text-anchor', 'middle').attr('x', x).attr('dy', 0).text(this.name)

		innerText
			.append('tspan')
			.attr('text-anchor', 'middle')
			.attr('x', x)
			.attr('dy', this.fontSize)
			.text(this.count)
	},
	showShadow() {
		this.outlineWrap.attr('filter', 'drop-shadow(0px 0px 10px #1d5193)')
		this.outline.attr('fill', '#1d5193')
	},
	hideShadow() {
		this.outlineWrap.attr('filter', 'none')
		this.outline.attr('fill', 'black')
	}
}

export default FunctionGroup
