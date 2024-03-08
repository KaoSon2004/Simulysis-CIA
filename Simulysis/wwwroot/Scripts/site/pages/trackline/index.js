import LoaderHandler from '../../loader.js'
import TrackLineViewManager from './trackLineViewManager.js'

var loaderHandler = Object.create(LoaderHandler)
loaderHandler.init()

$(async () => {
	var trackLineViewManager = Object.create(TrackLineViewManager)
	await trackLineViewManager.init()
	loaderHandler.setPageLoaded()
})
