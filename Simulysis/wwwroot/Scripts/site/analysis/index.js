import SignalSearch from './search.js'
import ViewManager from './viewManager.js'



$(async () => {
	var viewManager = Object.create(ViewManager)
	await viewManager.init()
	Object.create(SignalSearch).init(viewManager)
})



	
