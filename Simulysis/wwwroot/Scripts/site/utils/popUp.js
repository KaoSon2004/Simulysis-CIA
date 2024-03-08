var PopUpUtils = {
	popupCenter({ w, h, url }) {
		const dualScreenLeft = window.screenLeft ?? window.screenX
		const dualScreenTop = window.screenTop ?? window.screenY

		const width = window.innerWidth
			? window.innerWidth
			: document.documentElement.clientWidth
				? document.documentElement.clientWidth
				: screen.width
		const height = window.innerHeight
			? window.innerHeight
			: document.documentElement.clientHeight
				? document.documentElement.clientHeight
				: screen.height

		const systemZoom = width / window.screen.availWidth
		const left = (width - w) / 2 / systemZoom + dualScreenLeft
		const top = (height - h) / 2 / systemZoom + dualScreenTop

		var newWindow = window.open(
			url,
			undefined,
			`scrollbars=yes, width=${w / systemZoom}, height=${h / systemZoom
			}, top=${top}, left=${left}, location=no`
		)

		return newWindow
	}
}

export default PopUpUtils