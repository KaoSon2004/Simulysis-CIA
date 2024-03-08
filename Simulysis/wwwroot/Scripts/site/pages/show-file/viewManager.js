import FilesAPI from '../../api/files.js'
import FileContentsAPI from '../../api/fileContents.js'
import FileRelationshipsAPI from '../../api/fileRelationships.js'
import LogAPI from '../../api/log.js'
import NetworkDraw from '../../draw/network/index.js'
import TreeDraw from '../../draw/tree/index.js'
import TreeFolderDraw from '../../draw/folderTree/index.js'
import ModelDraw from '../../draw/model/index.js'
import SystemUtil from '../../utils/system.js'
import FileUtil from '../../utils/file.js'
import SystemLevelUtil from '../../utils/systemLevel.js'
import PopUpUtils from '../../utils/popUp.js'
import { noop } from '../../noop.js'

import TraceSignalTree from '../../draw/traceSignalTree/index.js'
import Utils from "../trackline/utils.js"
import Algorithm from '../trackline/algorithm.js'

var ViewManager = {
	/*
	 * PROPERTIES
	 */
	mainViewId: 'mainView',
	subViewId: 'subView',
	networkDrawId: 'networkDraw',
	modelDrawId: 'modelDraw',
	initialLevel: -1,
	currentLevel: -1,
	rootSysId: -1,
	allLevelContents: {},
	subsysRelationships: {},
	projectId: -1,
	originalFileId: -1,
	currentFileId: -1,
	currentSubsysFileId: -1,
	viewType: null,
	swapped: false,
	fullNet: false,
	networkDraw: null,
	modelDraw: null,
	treeDraw: null,
	treeFolderDraw: null,
	files: null,
	isSwitchingLevel: false,
	isNavigating: false,
	isSearching: false,
	hasNetworkView: true,
	showTree: true,
	modelViewPopUp: true,
	goUpDownCallback: noop, // custom callback to run after go up/down
	tempSideView: null,
	numClick: 0,

	levelClickContents: {},
	traceSignalTree: {},
	utils: {},
	algorithm: {},


	/*
	 * METHODS
	 */
	async init(options = {}) {
		// start loading
		this.startLoadingViews()

		this.initOptions(options)
		const rootSysId = Number($('#rootSysId').val() ?? 0)
		await this.updateContent()
		this.createModelDraw(rootSysId)

		if (this.hasNetworkView) {
			if (rootSysId > 0) {
				let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
					system => system.id == rootSysId
				)

				this.currentSubsysFileId = subsys.fK_FakeProjectFileId
				let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response
				console.log(3000)
				if (this.showTree) {
					this.createTreeDraw({ explicitRela })
					this.createTreeFolderDraw({ explicitRela })
				} else {
					this.createNetworkDraw({ explicitRela })
				}
			} else {
				if (this.showTree) {
					this.createTreeDraw()
					this.createTreeFolderDraw()
				} else {
					this.createNetworkDraw()
				}
			}

			this.updateToggleDrawButtonText()
		}

		this.disallowUserSelect()
		if (this.hasNetworkView) {
			this.addEventHandlers()
			this.adjustViewToMatchSwap()
			this.allowSearch()
		}
		this.addAllowSelectEvent()

		// stop loading
		this.stopLoadingViews()


		//For Trace Signal
		this.numClick = 0;
		$("#numClick").val(this.numClick);
		this.levelClickContents = {};
		this.traceSignalTree = Object.create(TraceSignalTree);

		this.traceSignalTree.init(this.modelDraw.drawEntities, this);

		this.utils = Object.create(Utils);
		this.utils.init(this.modelDraw, this.traceSignalTree);

		this.algorithm = Object.create(Algorithm);
		this.algorithm.init(
			this.utils, this.modelDraw
			, this.traceSignalTree, this.levelClickContents
		);
	},
	initOptions({ hasNetworkView = true, modelViewPopUp = true, goUpDownCallback = noop, }) {
		this.initialLevel = SystemLevelUtil.mapNameToLevel($('#systemLevelText').text())
		this.currentLevel = this.initialLevel
		this.originalFileId = Number($('#fileId').val())
		this.currentFileId = this.originalFileId
		this.projectId = Number($('#projectId').val())
		this.viewType = $('#viewType').val()
		this.swapped = $('#swapView').val() == 'true'
		this.fullNet = $('#fullNet').val() == 'true'
		this.showTree = $('#displayTreeView').val() == 'true'

		this.hasNetworkView = hasNetworkView
		this.modelViewPopUp = modelViewPopUp
		this.goUpDownCallback = goUpDownCallback
	},
	createNetworkDraw({ explicitRela, funcGrObj = {} } = {}) {
		// close the dropdown if currently opened
		this.networkDraw?.destroyDisplayOptDropdown()

		// get states for next draw
		var displayOptions = this.networkDraw?.getDisplayOptions()
		const openPopUp = this.networkDraw?.destroy() ?? false
		// const isFunctionLevelOrLower = this.currentLevel >= 2

		var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
		if (explicitRela) {
			relationshipsObj = explicitRela
		}

		this.networkDraw = Object.create(NetworkDraw).init(
			{
				viewId: this.subViewId,
				drawId: this.networkDrawId,
				whId: this.subViewId
			},
			relationshipsObj,
			`${this.viewType}Relationships`,
			this.files,
			explicitRela ? this.currentSubsysFileId : this.currentFileId,
			{
				fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
				subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
				parentSysClickHandler: this.goUpLevel,
				lineClickHandler: this.triggerSignalSearch.bind(this),
				onToggleFileLines: this.updateFullNetBtnText.bind(this),
				onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
			},
			{
				...displayOptions,
				// sub systems props
				// showSubSystemAsChild: isFunctionLevelOrLower,

				// popup props
				openPopUp,
				allowPopUp: true,
				popUpProps: {
					w: 1000,
					h: 800,
					url:
						window.location.href
							.split('?')[0]
							.replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
							.replace(new RegExp('\\bShow\\b'), 'NetworkExtend') +
						// `?viewType=${this.viewType}&showSubSystemAsChild=${isFunctionLevelOrLower}&rootSysId=${this.rootSysId}` +
						`?viewType=${this.viewType}&subSysFileId=${this.currentSubsysFileId}`
				},

				// function group props
				...funcGrObj
			}
		)
		this.networkDraw.draw()

		this.adjustViewToMatchFullNet()

		LogAPI.log(this.networkDraw.globalDrawConst)
	},
	createTreeDraw({ explicitRela } = {}) {
		this.treeDraw?.destroy()

		var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
		if (explicitRela) {
			relationshipsObj = explicitRela
		}

		this.treeDraw = Object.create(TreeDraw).init(
			{
				viewId: this.subViewId,
				drawId: this.networkDrawId,
				whId: this.subViewId
			},
			relationshipsObj,
			`${this.viewType}Relationships`,
			this.files,
			explicitRela ? this.currentSubsysFileId : this.currentFileId,
			{
				fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
				subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
				parentSysClickHandler: this.goUpLevel,
				lineClickHandler: this.triggerSignalSearch.bind(this),
				onToggleFileLines: this.updateFullNetBtnText.bind(this),
				onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
			},
			{
				allowPopUp: false
			}
		)
		this.treeDraw.draw()
	},
	createTreeFolderDraw({ explicitRela } = {}) {

		var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
		if (explicitRela) {
			relationshipsObj = explicitRela
		}

		this.treeFolderDraw = Object.create(TreeFolderDraw).init(
			{
				viewId: this.subViewId,
				drawId: this.networkDrawId,
				whId: this.subViewId
			},
			relationshipsObj,
			`${this.viewType}Relationships`,
			this.files,
			explicitRela ? this.currentSubsysFileId : this.currentFileId,
			{
				fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
				subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
				parentSysClickHandler: this.goUpLevel,
				lineClickHandler: this.triggerSignalSearch.bind(this),
				onToggleFileLines: this.updateFullNetBtnText.bind(this),
				onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
			},
			{
				allowPopUp: false
			}
		)
		this.treeFolderDraw.draw()
	},
	createModelDraw(rootSysId, fileContent) {

		var fileContent = fileContent ? fileContent
			: this.allLevelContents[`level${this.currentLevel}`].fileContent
		if (this.currentFileId == 0) {
			return;
		}

		this.rootSysId = rootSysId
		if (rootSysId == 0 && this.currentLevel == this.initialLevel) {
			$('#upLevelBtn').addClass('toolbar-btn--disabled')
			$('#upLevelBtn').off('click')
		}
		this.modelDraw && this.modelDraw.destroy()
		var popUpProps = {
			w: screen.width,
			h: screen.height,
			url: `${window.location.href
				.replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
				.replace(new RegExp('\\bShow\\b'), 'Trackline')}?rootSysId=${rootSysId}
					`
		}

		this.modelDraw = Object.create(ModelDraw).init(
			{
				viewId: this.mainViewId,
				drawId: this.modelDrawId,
				whId: this.mainViewId
			},
			rootSysId,
			fileContent,
			this.getSystemClickHandler.bind(this),
			{
				additionalDrawListeners: [this.addGoUpLevel.bind(this)],
				allowPopUp: this.modelViewPopUp,
				popUpProps: popUpProps,
			},
			this
		)
		this.modelDraw.draw()
		if (this.hasNetworkView) {
			this.modelDraw.initDragDropEvent();
		}
		return this.modelDraw;
	},
	async updateContent(fileName, blockType, sourceBlock, sysId, isFromSysRef = false) {
		// isFromSysRef = true when the content is updated through clicking on a subsystem on network view
		// which is from a different file
		var { fileContent, relationshipsObj, files } = await this.fetchData(fileName)

		const minSystemId = isFromSysRef
			? sysId
			: blockType
				? SystemUtil.isModelReference(blockType)
					? 0
					: SystemUtil.getReferencedSystemId(sourceBlock, fileContent.systems)
				: 0

		var oldFileContent = this.allLevelContents[`level${this.currentLevel - 1}`]?.fileContent
		const systemContainsRef = isFromSysRef
			? this.rootSysId
			: blockType
				? SystemUtil.getHigherSystemId(oldFileContent.systems, sysId)[0]
				: -1
		this.allLevelContents[`level${this.currentLevel}`] = {
			...this.allLevelContents[`level${this.currentLevel}`],
			fileContent,
			relationshipsObj,
			minSystemId,
			systemContainsRef
		}
		this.files = files

	},
	saveLevelFuncGr(funcGrObj) {
		this.allLevelContents[`level${this.currentLevel}`] = {
			...this.allLevelContents[`level${this.currentLevel}`],
			funcGrObj
		}
	},
	updateLevel(level) {
		this.currentLevel = level
		$('#systemLevelText').text(SystemLevelUtil.mapLevelToName(this.currentLevel))
	},
	async fetchData(fileName) {
		var { response: fileContent } = fileName
			? await FileContentsAPI.getFileContentByName({ projId: this.projectId, fileName })
			: await FileContentsAPI.getFileContentById(this.currentFileId)

		const fileId = fileName ? fileContent.fileId : this.currentFileId
		var { response: relationshipsObj } = await FileRelationshipsAPI.getFileRelationships(this.projectId, fileId)

		var files = this.files ?? (await FilesAPI.getFilesInProject(this.projectId)).response

		return { fileContent, relationshipsObj, files }
	},
	async getSubsysRelationships(fakeProjectFileId) {
		if (!this.subsysRelationships[fakeProjectFileId]) {
			this.subsysRelationships[fakeProjectFileId] = await FileRelationshipsAPI.getFileRelationships(
				this.projectId,
				fakeProjectFileId
			)
		}
		return this.subsysRelationships[fakeProjectFileId]
	},
	addEventHandlers() {
		$('#switchViewBtn').click(this.swapView.bind(this))
		$('#viewNetworkBtn').click(this.toggleNetwork.bind(this))
		$('#viewType').change(this.changeNetworkViewType.bind(this))
		$('#popUpTrackline').click(this.popUpTrackline.bind(this))
		$('#toggleTreeBtn').click(this.toggleSideView.bind(this))
	},
	popUpTrackline() {
		PopUpUtils.popupCenter({
			w: 1000,
			h: 800,
			url: `${window.location.href
				.replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
				.replace(new RegExp('\\bShow\\b'), 'Trackline')}?rootSysId=${this.rootSysId}
			`
		})
	},
	swapView(changeSwapProp = true) {
		var { modelDraw, sideDraw } = this.detachDraws()

		const temp = this.mainViewId
		this.mainViewId = this.subViewId
		this.subViewId = temp

		this.attachDraws(modelDraw, sideDraw)

		if (changeSwapProp) {
			this.swapped = !this.swapped
		}
	},
	toggleNetwork() {
		const allShown = this.networkDraw.isAllSubLinesShown()
		const allHidden = this.networkDraw.isAllSubLinesHiden()

		if (allShown && allHidden) {
			this.updateFullNetBtnText(!this.fullNet)
		} else if (allShown) {
			this.networkDraw.hideAllSubLines()
		} else {
			this.networkDraw.showAllSubLines()
		}
	},
	updateToggleDrawButtonText() {
		if (this.showTree) {
			$('#toggleTreeBtn span').text('Hide tree')
		} else {
			$('#toggleTreeBtn span').text('Show tree')
		}
	},
	async toggleSideView() {
		var previousSideView = this.tempSideView

		if (this.showTree) {
			this.tempSideView = this.treeDraw.detach()
			if (previousSideView) {
				$(`#${this.subViewId}`).append(previousSideView)
			} else {
				var funcGrObj = this.networkDraw ? this.networkDraw.getCurrentFunctionGroups() : null

				if (this.rootSysId > 0) {
					let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
						system => system.id == this.rootSysId
					)

					this.currentSubsysFileId = subsys.fK_FakeProjectFileId
					let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

					this.createNetworkDraw({ explicitRela, funcGrObj })
				} else {
					this.createNetworkDraw({ funcGrObj })
				}
			}
		} else {
			this.tempSideView = this.networkDraw.detach()
			if (previousSideView) {
				$(`#${this.subViewId}`).append(previousSideView)
			} else {
				if (this.rootSysId > 0) {
					let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
						system => system.id == this.rootSysId
					)

					this.currentSubsysFileId = subsys.fK_FakeProjectFileId
					let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

					this.createTreeDraw({ explicitRela })
					this.createTreeFolderDraw({ explicitRela })
				} else {
					this.createTreeDraw()
					this.createTreeFolderDraw()
				}
			}
		}

		this.showTree = !this.showTree;
		this.updateToggleDrawButtonText();
	},
	async changeNetworkViewType(e) {
		this.viewType = e.target.value

		var funcGrObj = this.networkDraw.getCurrentFunctionGroups()

		// TODO: MAKE A FUNCTION TO REUSE THIS CODE
		// DUPLICATE WITH CODE IN INIT() FUNCTION
		if (rootSysId > 0) {
			let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
				system => system.id == rootSysId
			)

			this.currentSubsysFileId = subsys.fK_FakeProjectFileId
			let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

			this.createNetworkDraw({ explicitRela, funcGrObj })
		} else {
			this.createNetworkDraw({ funcGrObj })
		}
	},
	detachDraws() {
		var modelDraw = this.modelDraw.detach()
		var sideDraw = this.showTree ? this.treeDraw.detach() : this.networkDraw.detach()
		return { modelDraw, sideDraw }
	},
	attachDraws(modelDraw, sideDraw) {
		$(`#${this.mainViewId}`).append(modelDraw)
		$(`#${this.subViewId}`).append(sideDraw)
	},
	adjustViewToMatchFullNet() {
		if (this.fullNet) {
			this.networkDraw.showAllSubLines()
		}

		this.updateFullNetBtnText()
	},
	adjustViewToMatchSwap() {
		if (this.swapped) {
			this.swapView(false)
		}
	},
	// prevent double-click lead to all text selected on load
	disallowUserSelect() {
		d3.selectAll('text').style('user-select', 'none')
	},
	addAllowSelectEvent() {
		$(document).click(function allowUserSelect() {
			d3.selectAll('text').style('user-select', 'text')
			$(document).off('click', allowUserSelect)
		})
	},
	allowSearch() {
		$('#signalSearchBtn').prop('disabled', false)
	},
	navigateToNewFileGenerator({ initialRootSysId, containingFileId } = {}) {
		return fileId => {
			if (this.isNavigating || fileId == this.originalFileId) {
				return
			}

			this.isNavigating = true
			// priortize containingFileId (real file that contains the subsys with fake file id)
			const replaceFileId = containingFileId ?? fileId

			var newUrl =
				`${window.location.href
					.split('?')[0]
					.replace(new RegExp(`\\b${this.originalFileId}\\b`), replaceFileId)}` +
				`${this.swapped ? '?swap=true&' : '?'}${this.fullNet ? 'fullNet=true&' : ''}${this.showTree ? 'displayTreeView=true&' : ''}viewType=${this.viewType}`

			initialRootSysId && (newUrl = `${newUrl}&rootSysId=${initialRootSysId}`)

			if (!this.showTree) {
				window.location.href = this.networkDraw.appendDisplayOptionsToUrl(newUrl)
			} else {
				window.location.href = newUrl
			}
		}
	},
	triggerSignalSearch(fileIds) {
		if (this.isSearching) return false

		this.isSearching = true

		$('#relationshipSearch').val(fileIds)
		$('#signalSearchForm').submit()

		return true
	},
	addGoUpLevel(svg) {
		svg.on('wheel', e => {
			const delta = e.wheelDelta ?? -e.detail

			if (!e.ctrlKey && delta > 0) {
				this.goUpLevel()
			}
		})
	},
	goUpLevel() {
		$('#upLevelBtn').click()
	},
	startLoadingViews() {
		$('.main-loader').css('display', 'flex')
		$('.main-container').css('filter', 'blur(2.5px) grayscale(1.4)')
	},
	stopLoadingViews() {
		$('.main-loader').css('display', 'none')
		$('.main-container').css('filter', 'none')
	},
	updateFullNetBtnText(fullNet) {
		if (typeof fullNet != 'undefined') {
			this.fullNet = fullNet

			if (this.fullNet) {
				$('#viewNetworkBtn span').text('Hide network')
			} else {
				$('#viewNetworkBtn span').text('View full network')
			}
		}

		const allShown = true
		const allHidden = false

		// when there is no sub-lines
		if (allShown && allHidden) {
			if ($('#viewNetworkBtn span').text() == 'View remain') {
				$('#viewNetworkBtn span').text('View full network')
			}

			return
		}

		if (allShown) {
			$('#viewNetworkBtn span').text('Hide network')
			this.fullNet = true
		} else if (allHidden) {
			this.fullNet = false
			$('#viewNetworkBtn span').text('View full network')
		} else {
			this.fullNet = false
			$('#viewNetworkBtn span').text('View remain')
		}
	},
	getSystemClickHandler(
		{ blockType, sourceFile, sourceBlock },
		{ sysId, relaFakeFileId },
		direction,
		isFromSysRef = false,
	) {

		return async () => {

			if (this.isSwitchingLevel) {
				return
			}

			if (
				(!SystemUtil.isSubSystem(blockType) && !SystemUtil.isRefToFile(blockType, sourceFile)) ||
				SystemUtil.isCalibration(sourceBlock)
			) {
				return
			}

			// check if the subsystem exist
			if (SystemUtil.isRefToFile(blockType, sourceFile) && sourceFile == '') {
				const isRef = SystemUtil.isReference(blockType)
				alert(
					`This system references to ${isRef ? 'a SubSystem in ' : ''
					}another file. But that file does not exist in the current project.`
				)
				return
			}

			// start loading
			this.startLoadingViews()

			this.isSwitchingLevel = true

			var stillInFile = true
			$('#upLevelBtn').off('click')

			if (direction == 'up') {
				this.numClick--;
				$("#numClick").val(this.numClick);


				let { fileContent, systemContainsRef, minSystemId } =
					this.allLevelContents[`level${this.currentLevel}`]
				// đi lên subsystem, đi từ trong subsystem ra ngoài subsystem
				if (sysId > minSystemId) {
					const [upperSysId, upperFakeFileSysId] = SystemUtil.getHigherSystemId(fileContent.systems, sysId)
					$('#upLevelBtn')
						.click(
							this.getSystemClickHandler(
								{ blockType, sourceFile, sourceBlock },
								{ sysId: upperSysId, relaFakeFileId: upperFakeFileSysId },
								'up'
							)
						)
						.removeClass('toolbar-btn--disabled')
					// chuẩn bị cái số 3
					// đ
				} else if (sysId == minSystemId && this.currentLevel > this.initialLevel) {
					$('#upLevelBtn')
						.click(
							this.getSystemClickHandler(
								{ blockType, sourceFile, sourceBlock },
								{ sysId: -1, relaFakeFileId: -1 },
								'up'
							)
						)
						.removeClass('toolbar-btn--disabled')

					// file A -> file B, fileB thuộc subsystem, đảm bảo up lên file A chứ không up lên subsystem
					// đổi file
				} else if (sysId == -1) {
					stillInFile = false
					this.updateLevel(this.currentLevel - 1)
					sysId = systemContainsRef

					var prefile = fileContent
					let { fileContent: contentAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]
					const { minSystemId: minSysAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]

					var upTitle = $('#pageTitle')
						.text()
						.replace(' > ' + FileUtil.name(this.files, prefile.fileId), '')

					$('#pageTitle').text(upTitle)
					this.currentFileId = contentAfterUpdate.fileId

					if (sysId > minSysAfterUpdate) {
						const [upperSysId, upperRelaFakeId] = SystemUtil.getHigherSystemId(
							contentAfterUpdate.systems,
							sysId
						)

						$('#upLevelBtn')
							.click(
								this.getSystemClickHandler(
									{ blockType, sourceFile, sourceBlock },
									{ sysId: upperSysId, relaFakeFileId: upperRelaFakeId },
									'up'
								)
							)
							.removeClass('toolbar-btn--disabled')
					} else if (this.currentLevel > this.initialLevel) {
						$('#upLevelBtn')
							.click(
								this.getSystemClickHandler(
									{ blockType, sourceFile, sourceBlock },
									{ sysId: -1, relaFakeFileId: -1 },
									'up'
								)
							)
							.removeClass('toolbar-btn--disabled')
					} else {
						$('#upLevelBtn').addClass('toolbar-btn--disabled')
					}
				} else {
					$('#upLevelBtn').addClass('toolbar-btn--disabled')
				}
			} else if (direction == 'down') {

				let upperSysId, upperRelaFakeFileId

				let { fileContent } = this.allLevelContents[`level${this.currentLevel}`]
				this.levelClickContents[`level${this.numClick}`] = {
					systems: this.modelDraw.drawEntities.systemDraws,
					lines: this.modelDraw.drawEntities.lineDraws,
					sysId
				};

				this.numClick++;
				$("#numClick").val(this.numClick);
				if (SystemUtil.isSubSystem(blockType)) {
					;[upperSysId, upperRelaFakeFileId] = SystemUtil.getHigherSystemId(fileContent.systems, sysId)

				} else {

					stillInFile = false

					// get the function groups from network view and save it
					// then later we can use it when we go up level
					if (this.hasNetworkView && !this.showTree) {
						this.saveLevelFuncGr(this.networkDraw.getCurrentFunctionGroups())
					}
					this.updateLevel(this.currentLevel + 1)

					await this.updateContent(sourceFile, blockType, sourceBlock, sysId, isFromSysRef)

					let { fileContent: contentAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]
					const { minSystemId } = this.allLevelContents[`level${this.currentLevel}`]

					upperSysId = -1
					upperRelaFakeFileId = -1

					sysId = minSystemId
					$('#pageTitle').text($('#pageTitle').text() + ' > ' + sourceFile)
					this.currentFileId = contentAfterUpdate.fileId

				}

				$('#upLevelBtn')
					.click(
						this.getSystemClickHandler(
							{ blockType, sourceFile, sourceBlock },
							{ sysId: upperSysId, relaFakeFileId: upperRelaFakeFileId },
							'up'
						)
					)
					.removeClass('toolbar-btn--disabled')
			}

			this.currentSubsysFileId = relaFakeFileId
			const clickSys = this.modelDraw.drawEntities.systemDraws.find(system => system.id == sysId);
			this.createModelDraw(sysId)
			this.updateModelDraw()
			if (this.hasNetworkView) {
				if (stillInFile) {
					// } if (this.currentLevel >= 2) {
					let explicitRela = sysId > 0 ? (await this.getSubsysRelationships(relaFakeFileId)).response : null
					let funcGrObj = this.showTree ? null : this.networkDraw.getCurrentFunctionGroups()
					this.showTree ? this.createTreeDraw({ explicitRela }) : this.createNetworkDraw({ explicitRela, funcGrObj })
					this.createTreeFolderDraw()
				} else if (direction == 'up') {
					let { funcGrObj } = this.allLevelContents[`level${this.currentLevel}`]
					this.showTree ? this.createTreeDraw() : this.createNetworkDraw({ funcGrObj })
					this.createTreeFolderDraw()
				} else {
					this.showTree ? this.createTreeDraw() : this.createNetworkDraw()
					this.createTreeFolderDraw()
				}
			}

			this.goUpDownCallback()
			this.traceSignalTree.reDrawTree(clickSys?.name);
			// stop loading
			this.stopLoadingViews()

			this.isSwitchingLevel = false
		}
	},



	updateModelDraw() {
		this.traceSignalTree.modelDraw = this.modelDraw.drawEntities;
		this.utils.modelDraw = this.modelDraw;
		this.algorithm.modelDraw = this.modelDraw;
	},


}

export default ViewManager
