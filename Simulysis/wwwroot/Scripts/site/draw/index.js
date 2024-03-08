import { returnSelf } from '../noop.js'
import ErrorLogger from '../utils/errorLogger.js'
import PopUpUtils from '../utils/popUp.js'
var Draw = {
	/*
	 * PROPERTIES
	 */
	viewId: null,
	drawId: null,
	btnClass: null,
	canvas: null,
	parent: null,
	width: null,
	height: null,
	allowPopUp: false,
	zoomPan: true,
	popUpWindow: null,
	zoom: null,
	networkProps: {},

	/*
	 * METHODS
	 */
	init(
		viewId,
		drawId,
		whId,
		{
			allowPopUp = false,
			zoomPan = true,
			openPopUp = false,
			popUpProps,
			popUpUrlUpdateFunc = returnSelf,
			additionalDrawListeners = [],
			networkProps = {}
		} = {}
	) {
		this.viewId = viewId
		this.drawId = drawId
		this.btnClass = `${drawId}-btn`
		this.allowPopUp = allowPopUp
		this.zoomPan = zoomPan
		this.popUpWindow = null
		this.networkProps = networkProps

		this.createDraw(whId, additionalDrawListeners)
		allowPopUp && this.initPopup(popUpProps, popUpUrlUpdateFunc)
		zoomPan && this.addZoomPan()
		openPopUp && this.openPopUp(popUpProps, popUpUrlUpdateFunc)
	},
	draw() {
		ErrorLogger.methodNotImplemented('draw')
	},
	destroy() {
		$(`#${this.drawId}`).parent().empty()
		return this.closePopUp()
	},
	createDraw(whId, additionalDrawListeners) {
		this.canvas = d3
			.select(`#${this.viewId}`)
			.append('svg')
			.attr('width', '100%')
			.attr('height', '100%')
			.attr('id', this.drawId)

		const { width, height } = d3.select(`#${whId}`).node().getBoundingClientRect()
		this.width = width
		this.height = height

		this.canvas.attr('viewBox', `0 0 ${width} ${height}`)
		this.parent = this.canvas

		additionalDrawListeners.forEach(listener => listener(this.canvas))
	},
	detach() {
		var canvas = $(`#${this.drawId}`).detach()
		var zoomPanBtns = this.zoomPan ? $(`.${this.btnClass}`).detach() : null

		return [canvas, zoomPanBtns]
	},
	// add zoom and pan function to drawing and set g as new parent
	addZoomPan() {
		this.parent = this.canvas.append('g')
		this.zoom = d3
			.zoom()
			.scaleExtent([0.25, 4])
			.filter(e => e.ctrlKey)
			.on('zoom', ({ transform }) => {
				this.parent.attr('transform', transform)
			})
		this.canvas.call(this.zoom)
		const scaleStep = 0.75
		this.canvas.on('wheel.zoom', e => {
			const delta = e.wheelDelta ?? -e.detail

			if (e.ctrlKey) {
				if (delta < 0) {
					this.canvas.call(this.zoom.scaleBy, scaleStep)
				} else if (delta > 0) {
					this.canvas.call(this.zoom.scaleBy, 1 / scaleStep)
				}

				// prevent browser zooming at minimum zoom
				e.stopImmediatePropagation()
			}

			e.preventDefault()
		})

		this.appendZoomPanBtns(scaleStep)
	},
	zoomInPosition(x, y, w, h) {
		// the block position is not the same as canvas position, can improve with width and height
		var scale = 4;
		const initialTransform = d3.zoomIdentity.translate((-x + 100) * scale,
			(-y + 100) * scale).scale(scale)
		this.zoom.transform(this.canvas, initialTransform)
	},
	zoomInPositionRaw(x, y, scale) {
		const initialTransform = d3.zoomIdentity.translate(x, y).scale(scale)
		this.zoom.transform(this.canvas, initialTransform)
	},
	initPopup(popUpProps, popUpUrlUpdateFunc) {
		$(window).on('unload', this.closePopUp.bind(this))
		this.appendPopUpBtn(popUpProps, popUpUrlUpdateFunc)
	},
	appendZoomPanBtns(scaleStep) {
		var btnPaths = [
			` <path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z"/>
				<path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/>
			`,
			`
				<path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z"/>
				<path d="M4 8a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7A.5.5 0 0 1 4 8z"/>
			`,
			'<path fill-rule="evenodd" d="M15 2a1 1 0 0 0-1-1H2a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2zM0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm8.5 9.5a.5.5 0 0 1-1 0V5.707L5.354 7.854a.5.5 0 1 1-.708-.708l3-3a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1-.708.708L8.5 5.707V11.5z"/>',
			'<path fill-rule="evenodd" d="M15 2a1 1 0 0 0-1-1H2a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2zM0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm8.5 2.5a.5.5 0 0 0-1 0v5.793L5.354 8.146a.5.5 0 1 0-.708.708l3 3a.5.5 0 0 0 .708 0l3-3a.5.5 0 0 0-.708-.708L8.5 10.293V4.5z"/>',
			'<path fill-rule="evenodd" d="M15 2a1 1 0 0 0-1-1H2a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2zM0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm4.5 5.5a.5.5 0 0 0 0 1h5.793l-2.147 2.146a.5.5 0 0 0 .708.708l3-3a.5.5 0 0 0 0-.708l-3-3a.5.5 0 1 0-.708.708L10.293 7.5H4.5z"/>',
			'<path fill-rule="evenodd" d="M15 2a1 1 0 0 0-1-1H2a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2zM0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm11.5 5.5a.5.5 0 0 1 0 1H5.707l2.147 2.146a.5.5 0 0 1-.708.708l-3-3a.5.5 0 0 1 0-.708l3-3a.5.5 0 1 1 .708.708L5.707 7.5H11.5z"/>'
		]

		var tooltipsPos = Array(3)
			.fill({ x: -280, y: 20 })
			.concat([
				{ x: -280, y: -65 },
				{ x: -260, y: -65 },
				{ x: 5, y: -65 }
			])

		var instructions = [
			'Press Ctrl + mouse wheel up to zoom in',
			'Press Ctrl + mouse wheel down to zoom out'
		].concat(Array(4).fill('Press Ctrl + drag your mouse to move around'))

		var getZoomFunc = scale => () => {
			this.canvas.call(this.zoom.scaleBy, scale)
		}

		var getMoveFunc = (x, y) => () => {
			this.canvas.call(this.zoom.translateBy, x, y)
		}
		const moveStep = 50
		var fns = [
			getZoomFunc(1 / scaleStep),
			getZoomFunc(scaleStep),
			getMoveFunc(0, moveStep),
			getMoveFunc(0, -moveStep),
			getMoveFunc(-moveStep, 0),
			getMoveFunc(moveStep, 0)
		]

		const initialPos = 5
		const posStep = 20
		const { displayIcon = false } = this.networkProps
		const startY = initialPos + (this.allowPopUp ? posStep : 0) + (displayIcon ? posStep : 0)
		for (let i = 0; i < btnPaths.length; i++) {
			const posX =
				i == 4 ? `right: ${initialPos + posStep}px` : `${i < 4 ? 'right' : 'left'}: ${initialPos}px`
			const posY = i < 3 ? `top: ${startY + posStep * i}px` : `bottom: ${initialPos}px`

			$(`#${this.viewId}`).append(
				this.createBtn({
					btn: { x: posX, y: posY, clickFn: fns[i], path: btnPaths[i] },
					tooltip: { ...tooltipsPos[i], msg: instructions[i] }
				})
			)
		}
	},
	appendPopUpBtn(popUpProps, popUpUrlUpdateFunc) {
		$(`#${this.viewId}`).append(
			this.createBtn({
				btn: {
					id: `${this.drawId}Popup`,
					x: 'right: 5px',
					y: 'top: 5px',
					clickFn: () => this.openPopUp(popUpProps, popUpUrlUpdateFunc),
					path: '<path fill-rule="evenodd" d="M15 2a1 1 0 0 0-1-1H2a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2zM0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm5.854 8.803a.5.5 0 1 1-.708-.707L9.243 6H6.475a.5.5 0 1 1 0-1h3.975a.5.5 0 0 1 .5.5v3.975a.5.5 0 1 1-1 0V6.707l-4.096 4.096z"/>'
				},
				tooltip: {
					x: -80,
					y: 20,
					msg: 'Pop out'
				}
			})
		)
	},
	openPopUp({ w, h, url = '_blank' }, popUpUrlUpdateFunc) {
		if (this.popUpWindow && !this.popUpWindow.closed) {
			window.focus && this.popUpWindow.focus()
		} else {
			url = popUpUrlUpdateFunc(url)
			this.popUpWindow = PopUpUtils.popupCenter({ w, h, url })
		}
	},
	closePopUp() {
		const popUpOpening = this.popUpWindow && !this.popUpWindow.closed
		popUpOpening && this.popUpWindow.close()
		return popUpOpening
	},
	disablePopupBtn() {
		this.disableDrawingUtilBtn(`${this.drawId}Popup`)
	},
	enablePopupBtn() {
		this.enableDrawingUtilBtn(`${this.drawId}Popup`)
	},
	disableDrawingUtilBtn(id) {
		$(`#${id}DrawingUtilBtn`).css('pointer-events', 'none')
		$(`#${id}DrawingUtilBtn svg`).css('fill', 'rgba(0, 0, 0, 0.25)')
	},
	enableDrawingUtilBtn(id) {
		$(`#${id}DrawingUtilBtn`).css('pointer-events', 'auto')
		$(`#${id}DrawingUtilBtn svg`).css('fill', 'rgba(0, 0, 0, 0.5)')
	},
	createBtn({ btn, tooltip }) {
		var $btn = $(
			`<span class="${this.btnClass}" style="position: absolute; ${btn.y}; ${btn.x}; line-height: 0; cursor: pointer;"></span>`
		)

		btn.id && $btn.prop('id', `${btn.id}DrawingUtilBtn`)

		return $btn
			.append(
				$(
					`<svg width="16" height="16" viewBox="0 0 ${btn.viewBoxWH ?? '16 16'}" fill="rgba(0, 0, 0, 0.5)">${btn.path
					}</svg>`
				)
					.hover(
						function (e) {
							$('#helperTooltip')
								.css('left', e.clientX + tooltip.x)
								.css('top', e.clientY + tooltip.y)
								.text(tooltip.msg)
								.css('display', 'block')

							$(this).css('fill', '#1d5193')
						},
						function () {
							$('#helperTooltip').css('display', 'none')
							$(this).css('fill', 'rgba(0, 0, 0, 0.5)')
						}
					)
					.click(e => {
						$('#helperTooltip').css('display', 'none')
						var { clickFn } = btn
						clickFn(e)
					})
					.css('fill', btn.disabled ? 'rgba(0, 0, 0, 0.25)' : 'rgba(0, 0, 0, 0.5)')
			)
			.css('pointer-events', btn.disabled ? 'none' : 'auto')
	},
	centerViewToNode(node) {
		let x = node[0].transform.baseVal.getItem(0).matrix.e;
		let y = node[0].transform.baseVal.getItem(0).matrix.f;

		this.canvas.call(this.zoom.translateTo, x, y)
	}
}

export default Draw