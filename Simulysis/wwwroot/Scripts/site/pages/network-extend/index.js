import NetworkDraw from '../../draw/network/index.js'
import FileRelationshipsAPI from '../../api/fileRelationships.js'
import FilesApi from '../../api/files.js'
import SignalTable from './signalTable.js'
import LoaderHandler from '../../loader.js'

var loaderHandler = Object.create(LoaderHandler)
loaderHandler.init()

$(async () => {
	const fileId = $('#fileId').val()
	const projectId = $('#projectId').val()
	const viewType = $('#viewType').val()
	const subSysFileId = $('#subSysFileId').val()
	const functionGroupLevel = Number($('#functionGroupLevel').val())
	const currentFunctionGroups = $('#currentFunctionGroups').val()
	const isCurrentlyInLastFunctionGroupLevel = $('#isCurrentlyInLastFunctionGroupLevel').val()

	var { response: relationshipsObj } = await FileRelationshipsAPI.getFileRelationships(fileId)
	var { response: files } = await FilesApi.getFilesInProject(projectId)

	if (subSysFileId != -1) {
		relationshipsObj = (await FileRelationshipsAPI.getFileRelationships(projectId, subSysFileId)).response
	}

	var networkDraw = Object.create(NetworkDraw)
	var signalTable = Object.create(SignalTable)

	networkDraw.init(
		{
			viewId: 'mainView',
			drawId: 'networkDraw',
			whId: 'mainView'
		},
		relationshipsObj,
		`${viewType}Relationships`,
		files,
		fileId,
		{
			onToggleLine: signalTable.changeHeaderEyeIcon.bind(signalTable),
			onDisplayChange: signalTable.reInit.bind(signalTable)
		},
		{
			allowPopUp: false,
			hideOnRender: true,
			allowToggleVisibility: false,

			functionGroupLevel,
			currentFunctionGroups,
			isCurrentlyInLastFunctionGroupLevel,
			allowChangeFuncGrLevel: false
		}
	)
	networkDraw.draw()

	signalTable.init(networkDraw)

	loaderHandler.setPageLoaded()
})
