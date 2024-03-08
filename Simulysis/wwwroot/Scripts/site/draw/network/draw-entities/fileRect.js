import { noop } from '../../../noop.js'

var FileRect = {
	/*
	 * PROPERTIES
	 */
	id: null,
	parent: null,
	stringId: null,
	isSub: false,
	networkDraw: null,
	center: null,
	width: null,
	height: null,
	name: null,
	statusKey: null,
	isHighlighting: false,

	/*
	 * METHODS
	 */
	init(fileId, center, fileName, parent, networkDraw, props) {
		const { width, height, hideOnRender = false, isSub = false, strokeColor = 'black', statusKey } = props
		var { handler } = props

		this.id = fileId
		this.name = fileName
		this.statusKey = statusKey ?? fileName
		this.parent = parent
		this.isSub = isSub
		this.strokeColor = strokeColor
		this.stringId = this.id ? `${this.isSub ? 'f' : 'mainF'}ile${this.id}` : this.statusKey
		this.center = center
		this.height = height
		this.width = width
		this.networkDraw = networkDraw
		this.isHighlighting = false

		const allowToggle = this.networkDraw?.allowToggleVisibility

		this.wrapper = parent
			.append('g')
			.attr('id', this.stringId)
			.attr('class', `file-rect ${this.stringId}`)
			.style('display', hideOnRender ? 'none' : 'block')
			.style('cursor', this.isSub ? 'pointer' : 'default')
			.on('mouseover', this.showShadow.bind(this))
			.on('mouseleave', this.hideShadow.bind(this))
			.on(
				'click',
				isSub
					? allowToggle
						? this.allowToggleOnClickHandler.bind(this)
						: noop
					: this.onClickHandler.bind(this)
			)

		if (this.isSub) {
			this.wrapper.on('dblclick', () => handler(this.id))

			$(`#${this.stringId}`).on('DOMMouseScroll mousewheel', e => {
				const delta = e.originalEvent.wheelDelta ?? -e.originalEvent.detail
				if (!e.ctrlKey && delta < 0) {
					handler(this.id)
				}

				e.preventDefault()
			})
		}

		return this
	},
	draw() {
		const fontSize =
			(this.width / this.name.length) * (1.75 - this.name.replace(/[^A-Z]/g, '').length * 0.015)

		this.wrapper.raise()

		this.outline = this.wrapper
			.append('rect')
			.attr('x', this.center.x - this.width / 2)
			.attr('y', this.center.y - this.height / 2)
			.attr('height', this.height)
			.attr('width', this.width)
			.attr('stroke', this.strokeColor)
			.attr('stroke-width', '3px')
			.attr('fill', this.isSub ? 'white' : '#fffda8')

		this.wrapper
			.append('text')
			.attr('x', this.center.x)
			.attr('y', this.center.y)
			.attr('text-anchor', 'middle')
			.attr('dominant-baseline', 'central')
			.attr('font-size', fontSize)
			.attr('fill', 'black')
			.text(this.name)
	},
	showShadow() {
		this.outline.attr('filter', 'drop-shadow(0px 0px 3px #1d5193)')
		this.networkDraw.tempHighlightFileLines(this.statusKey)
	},
	hideShadow() {
		if (!this.isHighlighting) {
			this.outline.attr('filter', 'none')
		}
		this.networkDraw.tempUnHighlightFileLines(this.statusKey)
	},
	onClickHandler(e) {
		if (e.ctrlKey) {
			if (!this.isHighlighting) {
				this.networkDraw.addHighlightFile(this.id)
				this.outline.attr('filter', 'drop-shadow(0px 0px 3px #1d5193)')
				this.isHighlighting = true
			} else {
				this.networkDraw.removeHighlightFile(this.id)
				this.outline.attr('filter', 'none')
				this.isHighlighting = false
			}

			this.addOutsideClickHandler()
		}
	},
	allowToggleOnClickHandler(e) {
		if (!e.ctrlKey) {
			this.networkDraw.toggleFileSubLinesVisibility(this.statusKey)
		}
		this.onClickHandler(e)
	},
	addOutsideClickHandler() {
		var outsideClickHandler = e => {
			if (this.isHighlighting) {
				var $target = $(e.target)

				if (
					$target.closest(`#${this.stringId}`).length <= 0 &&
					$target.closest('.file-rect').length <= 0 &&
					$target.closest(`#${this.networkDraw.viewId}`).length > 0
				) {
					this.networkDraw.removeHighlightFile(this.id)
					this.outline.attr('filter', 'none')
					this.isHighlighting = false

					$(document).off('click', outsideClickHandler)
				}
			}
		}

		$(document).click(outsideClickHandler)
	}
}

export default FileRect
