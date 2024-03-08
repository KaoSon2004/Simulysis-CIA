import SearchAPI from '../api/search.js'
import Pagination from '../pagination.js'
import FileUtil from '../utils/file.js'
import { noop } from '../noop.js'

var SignalSearch = {
	viewManager: null,
	resultType: null,
	previouslyHighlightBlockList: null,
	subsystemNavigateGenerator: noop,
	init(viewManager) {
		this.viewManager = viewManager
		this.subsystemNavigateGenerator = viewManager.getSystemClickHandler.bind(viewManager)

		$('#viewType').change(this.emptyResults.bind(this))
		$('#signalSearchForm').submit(async e => {
			e.preventDefault()
			this.viewManager.networkDraw.unHighlightAllLines()
			this.resetResults()
			var searchIcon = this.changeToLoadingState()

			// if file ids exists => search signal base on relationship between two files (line click)
			const fileIds = $('#relationshipSearch').val()
			$('#relationshipSearch').val('')

			var data
			if (fileIds) {
				const [fileId1, fileId2] = fileIds.split(`~`).map(Number)

				data = await this.search({ fileId1, fileId2, type: this.viewManager.viewType })
			} else {
				data = await this.search(this.getFormValues(e.target))
			}

			this.displayResults(data)

			// restore search button status to normal
			this.changeToNormalState(searchIcon)

			this.viewManager.isSearching = false
		})
	},
	async search(formData) {
		var { response } = await SearchAPI.getSearchResults(formData)

		return response
	},
	getFormValues(form) {
		var formData = new FormData(form)

		return {
			name: formData.get('name'),
			scope: formData.get('scope'),
			projectId: this.viewManager.projectId,
			type: this.viewManager.viewType,
			fileList: `[${this.viewManager.networkDraw.getSelectedFiles()}]`
		}
	},
	resetResults() {
		// empty table
		Pagination.init([], $('#bodyResult'))
		$('#resultCounter').text('Searching...')
	},
	emptyResults() {
		Pagination.init([], $('#bodyResult'))
		$('#resultCounterWrapper').css('display', 'none')
	},
	displayResults(data) {
		var { signalInOut, signalFromGoto, signalCali } = data

		// append results to table
		var dataSource = []

		if (signalInOut && signalFromGoto) {
			this.setTableCol('From', 'To')
			this.resultType = 'signal'
			dataSource = signalInOut.concat(signalFromGoto)
		} else if (signalCali) {
			this.setTableCol('Model 1', 'Model 2')
			this.resultType = 'calibration'
			dataSource = signalCali
		}

		Pagination.init(dataSource, $('#bodyResult'), this.createResultRow.bind(this))

		// show the number of results
		const resLength = dataSource.length
		$('#resultCounter').text(
			`Result: ${resLength == 0 ? 'No result' : `${resLength} ${resLength == 1 ? this.resultType : `${this.resultType}s`}`
			}.`
		)

		$('#resultCounterWrapper').css('display', 'flex')
	},
	changeToLoadingState() {
		var searchIcon = $('#signalSearchBtn > svg').detach()
		$('#signalSearchBtn')
			.prop('disabled', true)
			.append(
				$(
					'<span class="spinner-border" style="height: 0.9rem; width: 0.9rem; border-width: 0.15em" role="loading"></span>'
				)
			)

		return searchIcon
	},
	changeToNormalState(searchIcon) {
		$('#signalSearchBtn').empty().append(searchIcon).prop('disabled', false)
	},
	setTableCol(col1, col2) {
		$('#signalTableFrom').text(col1)
		$('#signalTableTo').text(col2)
	},
	async navigateToContainingFile(sourceSubsystemId, sourceFileId, sourceFileName, sourceFakeFildId) {
		const currentFileId = this.viewManager.modelDraw.fileContent.fileId

		console.log(sourceSubsystemId, sourceFileId, sourceFileName, sourceFakeFildId);

		if (sourceFileId != currentFileId) {
			await this.subsystemNavigateGenerator(
				{ blockType: 'Reference', sourceFile: sourceFileName },
				{ sysId: parseInt(sourceSubsystemId), relaFakeFileId: parseInt(sourceFakeFildId) },
				'down',
				true)()
		} else {
			await this.subsystemNavigateGenerator(
				{ blockType: 'SubSystem' },
				{ sysId: parseInt(sourceSubsystemId), relaFakeFileId: parseInt(sourceFakeFildId) },
				'down',
				false)()
		}
	},
	createResultRow(signal) {
		const [fromFile, fromName, fromParentSysId, fromParentFileId, fromFakeId, fromFileFull] = signal.from.split('|')
		const [toFile, toName, toParentSysId, toParentFileId, toFakeId, toFileFull] = signal.to.split('|')

		const fromFileWithoutExt = FileUtil.nameWithoutExt(fromFile)
		const toFileWithoutExt = FileUtil.nameWithoutExt(toFile)

		const lineClass = `${fromFileWithoutExt}_${toFileWithoutExt}`
		const revLineClass = `${toFileWithoutExt}_${fromFileWithoutExt}`

		return $(`<tr class="search-result-row ${lineClass}" ></tr>`)
			.append(
				$(`<td class="col" style="width: calc(100% / 3)">${signal.name}</td>`),
				$(`<td class="col" style="width: calc(100% / 3)" title="Press Click to highlight or Ctrl+Click to jump to and highlight the &quot;From&quot; block">${fromName ? fromName : fromFileWithoutExt}</td>`),
				$(`<td class="col" style="width: calc(100% / 3)" title="Press Click to highlight or Ctrl+Click to jump to and highlight the &quot;To&quot; block">${toName ? toName : toFileWithoutExt}</td>`)
			)
			.mouseover(e => {
				this.showLineOnHover(lineClass, signal.name)
				this.showLineOnHover(revLineClass, signal.name)
			})
			.mouseout(e => {
				this.hideLineOnOut(lineClass, signal.name)
				this.hideLineOnOut(revLineClass, signal.name)
			})
			.click(async e => {
				if (e.ctrlKey) {
					if (e.target.cellIndex == 1) {
						await this.navigateToContainingFile(fromParentSysId, fromParentFileId, fromFileFull, fromFakeId)
					} else if (e.target.cellIndex == 2) {
						await this.navigateToContainingFile(toParentSysId, toParentFileId, toFileFull, toFakeId);
					}
				}

				let highLightThisList;
				if (this.resultType != 'calibration') {
					highLightThisList = this.viewManager.modelDraw.drawEntities.systemDraws.filter(system => (system.name == signal.name) || (system.gotoTag == signal.name))
				} else {
					highLightThisList = this.viewManager.modelDraw.drawEntities.systemDraws.filter(system => {
						if (system.instanceDatas.length == 0) return false;
						if (system.instanceDatas[0].properties.includes(signal.name)) return true;
					})
				}

				if ((highLightThisList == undefined) || (highLightThisList.length == 0)) {
					alert("The block(s) that you trying to highlight is in a subsystem/reference block")
				} else {
					if (this.previouslyHighlightBlockList) {
						this.previouslyHighlightBlockList.forEach(targetBlock => {
							this.unhighlight(targetBlock)
						})
					}

					highLightThisList.forEach(highLightThis => {
						this.highlight(highLightThis)
					})

					this.previouslyHighlightBlockList = highLightThisList
				}
			}
			)
	},
	showLineOnHover(lineClass, name) {
		this.viewManager.networkDraw.showLine({ className: lineClass }, true)
		// !showByClass && this.viewManager.networkDraw.showLine({ name }, true)

		this.viewManager.networkDraw.highlightLine({ className: lineClass }, true)
		// !showByClass && this.viewManager.networkDraw.highlightLine({ name }, true)
	},
	hideLineOnOut(lineClass, name) {
		this.viewManager.networkDraw.hideLine({ className: lineClass }, true, true)
		// !hideByClass && this.viewManager.networkDraw.hideLine({ name }, true, true)

		this.viewManager.networkDraw.unHighlightLine({ className: lineClass }, true)
		// !hideByClass && this.viewManager.networkDraw.unHighlightLine({ name }, true)
	},
	highlight(entity) {
		entity.highlight('#fc0303')
		this.zoomInEntity(entity)
	},
	unhighlight(entity) {
		entity.unHighlight()
	},
	zoomInEntity(entity) {
		var blockPostion = JSON.parse(entity.props.Position)
		this.viewManager.modelDraw.zoomInPosition(blockPostion[0], blockPostion[1], blockPostion[2] - blockPostion[0], blockPostion[3] - blockPostion[1])
	}
}

export default SignalSearch

