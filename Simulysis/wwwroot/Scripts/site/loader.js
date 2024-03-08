var LoaderHandler = {
	pageLoaded: false,
	interval: null,
	timeStep: 200,

	init() {
		this.pageLoaded = false
		this.interval = setInterval(this.stopLoading.bind(this), this.timeStep)
	},
	setPageLoaded() {
		setTimeout(() => {
			this.pageLoaded = true
		}, this.timeStep * 2)
	},
	stopLoading() {
		if (this.pageLoaded) {
			$('#loaderBar').css('display', 'none')
			clearInterval(this.interval)
		}
	}
}

export default LoaderHandler
