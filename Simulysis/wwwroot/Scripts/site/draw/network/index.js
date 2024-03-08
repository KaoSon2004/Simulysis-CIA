import FileUtil from '../../utils/file.js'
import FileRelationshipsUtil from '../../utils/fileRelationships.js'
import Draw from '../index.js'
import FileRect from './draw-entities/fileRect.js'

import { noop, noopGenerator } from '../../noop.js'
import FunctionGroup from './draw-entities/functionGroup.js'

var NetworkDraw = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: Draw,

	/*
	 * PROPERTIES
	 */
	mainFileId: null,
	mainFile: null,
	containingFolder: null,
	relationshipsObj: null,
	viewType: null,
	mainRelationshipsToDisplay: [],
	relationshipsBetweenMainDisplayFiles: [],
	allConnections: [],
	files: [],
	fileCountPerLevel: [],
	angleEachPerLevel: [],
	subLevelCount: 0,
	globalDrawConst: null,
	fileRectStatuses: {},
	lineStatuses: {},
	highlightedFiles: [],
	selectAllFiles: false,
	hideOnRender: false,
	allowToggleVisibility: true,
	displayEquals: true,
	displayChildLibraries: true,
	displayParents: true,
	displayChildren: false,
	displaySubChildren: [],
	showMainLinesOnDisplay: true,
	allowChangeFuncGrLevel: true,
	isSwitchingFuncGroupLevel: false,
	functionGroupLevel: null,
	maxFunctionGroupLevel: 6,
	isCurrentlyInLastFunctionGroupLevel: false,
	functionGroups: {},
	currentFunctionGroups: {},
	lineClickHandler: noop,
	fileRectClickHandlerGenerator: noopGenerator,
	subSysClickHandlerGenerator: noopGenerator,
	onToggleFileLines: noop,
	onToggleLine: noop,
	onDisplayChange: noop,
	parentSysClickHandler: noop,

	/*
	 * METHODS
	 */

	/* ===== OVERRIDE ===== */
	init(ids, relationshipsObj, type, files, mainFileId, clickHandlers, options = {}) {
		const { viewId, drawId, whId } = ids

		super.init(viewId, drawId, whId, {
			...options,
			networkProps: { displayIcon: true },
			popUpUrlUpdateFunc: this.appendAllOptionsToUrl.bind(this)
		})

		this.mainFileId = mainFileId
		this.mainFile = FileUtil.file(files, this.mainFileId)
		if (FileUtil.isFake(this.mainFile))
			this.mainFile.containingFilePath = FileUtil.getContainingFilePath(files, this.mainFile)

		// init options and properties
		this.initSpecificOptions(options, type)
		this.initProps(relationshipsObj, type, files)

		// init for drawings
		this.initFunctionGroups(options)
		this.initContainingFolder()
		this.initRelationshipLists()
		this.initGlobDrawingConst()

		// listener and other work
		this.initClickHandlers(clickHandlers)
		this.initDisplayOptions()
		this.centerViewToMainCircle()
		this.addGoUpFunctionGroupLevelListener()

		return this
	},
	reInit() {
		this.initContainingFolder()
		this.initRelationshipLists()
		this.initGlobDrawingConst()
		this.initInteractingProps()
	},
	getRectAndConnectionLineStrokeColor(params) {
		// Level 0 is main file, level 1 is children/equal of main file. And so on
		const childrenFromLvl2ColourPalette = ['#390099', '#9e0059', '#F4B400', '#2364aa', '#3da5d9', '#e2c2ff', '#ac3d16']

		var { childrenLvl2Index, isSub, isParentChild, isParent, isChild } = params
		if (childrenLvl2Index != null && childrenLvl2Index != undefined && childrenLvl2Index >= 0) {
			return childrenFromLvl2ColourPalette[childrenLvl2Index]
		}

		return isSub ? (isParentChild ? '#ff595e' : '#ffca3a') : isParent ? '#8ac926' : isChild ? '#1982c4' : '#6a4c93'
	},
	/* ===== OVERRIDE ===== */
	draw() {
		const {
			angle,
			centerCoord,
			ratioPerLevel,
			ySpacing,
			subFileH,
			totalLength,
			subWHRatio,
			mainFileH,
			WHRatio
		} = this.globalDrawConst

		if (!this.isCurrentlyInLastFunctionGroupLevel) {
			let currentLvlFuncGroups = this.getCurrentLevelFunctionGroups()

			const funcGroupEdge = 100
			const funcGroupRad = funcGroupEdge * Math.SQRT2 + 5 // 5 is padding between two funcGroups
			const angleBetweenTwoFolders = (2 * Math.PI) / currentLvlFuncGroups.length

			// we use cosine law to calculate the radius of the outer circle
			// which is the edge of the isosceles triangle
			const outerRad =
				currentLvlFuncGroups.length > 1
					? Math.sqrt(Math.pow(funcGroupRad, 2) / (2 * (1 - Math.cos(angleBetweenTwoFolders))))
					: 0

			let functionGroups = currentLvlFuncGroups.map(([funcGroup, { count, children }], i) => {
				const funcGroupX = centerCoord + outerRad * Math.sin(angleBetweenTwoFolders * i)
				const funcGroupY = centerCoord - outerRad * Math.cos(angleBetweenTwoFolders * i)
				const lastGroupLevel = Object.keys(children).length == 0

				var position = { x: funcGroupX, y: funcGroupY }

				return Object.create(FunctionGroup).init(
					this.parent,
					funcGroup,
					count,
					position,
					lastGroupLevel,
					this.getFunctionGroupClickHandler.bind(this),
					{
						edgeLength: funcGroupEdge
					}
				)
			})

			functionGroups.forEach(fg => fg.draw())
		} else {
			// init drawing constants
			const mainFileName = this.mainFile.name
			const mainFileIsSubsys = FileUtil.isFake(this.mainFile)
			const realMainFileName = mainFileIsSubsys
				? FileUtil.getRealFileNameFromFake(this.mainFile)
				: mainFileName

			const mainCircleRadius = ratioPerLevel[0]
			const mainCircleOffset = centerCoord - mainCircleRadius

			// draw other file rectangle and connection lines to main file
			let fileRects = this.mainRelationshipsToDisplay.map((relationship, i) => {
				// calculate line and rectangle positions
				const currentAngle = angle * i
				const dstX = centerCoord + mainCircleRadius * Math.sin(currentAngle)
				const dstY = centerCoord - mainCircleRadius * Math.cos(currentAngle)
				var lineData = [
					[centerCoord, centerCoord],
					[dstX, dstY]
				]

				var rectCenter = { x: dstX, y: dstY }
				if (currentAngle > 0 && currentAngle < Math.PI) {
					const newDstY = subFileH / 2 + (subFileH + ySpacing) * i + mainCircleOffset
					lineData.push([dstX, newDstY], [dstX + (subFileH * subWHRatio) / 2, newDstY])
					rectCenter = {
						x: dstX + (subFileH * subWHRatio) / 2,
						y: newDstY
					}
				} else if (currentAngle > Math.PI && currentAngle < 2 * Math.PI) {
					const newDstY = subFileH / 2 + (subFileH + ySpacing) * (totalLength - i) + mainCircleOffset
					lineData.push([dstX, newDstY], [dstX - (subFileH * subWHRatio) / 2, newDstY])
					rectCenter = {
						x: dstX - (subFileH * subWHRatio) / 2,
						y: newDstY
					}
				}

				const subFileId = FileRelationshipsUtil.getSubFileId(relationship, this.mainFileId)
				var subFile = FileUtil.file(this.files, subFileId)
				const subFileName = subFile.name

				const isChild = FileRelationshipsUtil.isChild(relationship, this.mainFileId)
				const isParent = FileRelationshipsUtil.isParent(relationship, this.mainFileId)

				const subSysIsParent = isParent && relationship.system2

				const subFileIsSubSys = (isChild && relationship.system1) || subSysIsParent

				// we need this to uniquify the key
				// because subsystem's name can be the same
				const statusKey = subFileIsSubSys ? `subsys${subFileId}` : subFileName

				this.fileRectStatuses[statusKey] = {
					id: subFileId,
					showLines: false,
					show: !this.hideOnRender,
					key: statusKey
				}

				const [fromName, toName] = !subFileIsSubSys
					? FileRelationshipsUtil.getFromToName(mainFileName, subFileName, relationship, this.mainFileId)
					: [mainFileName, statusKey]

				const lineParams = {
					lineData,
					isSub: false,
					displayNone: this.hideOnRender,
					isParent,
					isChild
				}

				const strokeColor = this.getRectAndConnectionLineStrokeColor(lineParams)

				this.createConnectionLine(
					fromName,
					toName,
					relationship.uniCount ?? 1,
					relationship.name,
					`${this.mainFileId}~${subFileId}`,
					strokeColor,
					lineParams
				)

				const containingFileName = subSysIsParent ? FileUtil.getRealFileNameFromFake(subFile) : null
				const containingFileId = subSysIsParent
					? FileUtil.findIdFromName(this.files, containingFileName)
					: null

				const susSysFromDiffFile = subSysIsParent && containingFileName != realMainFileName

				return Object.create(FileRect).init(subFileId, rectCenter, subFileName, this.parent, this, {
					width: subFileH * subWHRatio,
					height: subFileH,
					handler: subFileIsSubSys
						? // if the subsystem is parent then it may come from a different file
						  // here we navigate to that file with the initial rootSysId = subSysId
						  subSysIsParent
							? susSysFromDiffFile
								? this.fileRectClickHandlerGenerator({
										initialRootSysId: relationship.system2,
										containingFileId
								  })
								: this.parentSysClickHandler // else we just go up level
							: this.subSysClickHandlerGenerator(
									{ blockType: 'SubSystem' },
									{ sysId: Number(relationship.system1), relaFakeFileId: subFileId },
									'down'
							  )
						: mainFileIsSubsys && isParent // if main file is subsys and parent is a file => the file containing subsys => go up level
						? this.parentSysClickHandler
						: this.fileRectClickHandlerGenerator(),
					hideOnRender: this.hideOnRender,
					isSub: true,
					strokeColor: strokeColor,
					statusKey
				})
			})

			// draw main file rectangle
			Object.create(FileRect)
				.init(this.mainFileId, { x: centerCoord, y: centerCoord }, mainFileName, this.parent, this, {
					width: mainFileH * WHRatio,
					height: mainFileH,
					handler: noop,
					hideOnRender: false,
					isSub: false
				})
				.draw()

			// draw sub connection lines (between files that are not main)
			fileRects
				.filter(fileRect => typeof fileRect.id != 'string')
				.forEach((fileRect, i) => {
					this.relationshipsBetweenMainDisplayFiles[i].forEach(relationship => {
						if (FileRelationshipsUtil.isNotInRelationship(relationship, fileRect.id)) return

						const otherFileId = FileRelationshipsUtil.getSubFileId(relationship, fileRect.id)

						var otherFileRect = fileRects.find(f => f.stringId == `file${otherFileId}`)

						var lineData = [[fileRect.center.x, fileRect.center.y]]

						// Avoid crossing the line through main line
						// Make the curved line avoid the main circle and pad from it a bit
						if (
							Math.abs(fileRect.center.x - otherFileRect.center.x) <= 0.001 &&
							Math.abs(fileRect.center.x - centerCoord) <= 0.001
						) {
							lineData.push([
								fileRect.center.x + (mainFileH * WHRatio) / 2 + (mainCircleRadius * 1) / 5,
								(fileRect.center.y + otherFileRect.center.y) / 2
							])
						} else if (
							Math.abs(fileRect.center.y - otherFileRect.center.y) <= 0.001 &&
							Math.abs(fileRect.center.y - centerCoord) <= 0.001
						) {
							lineData.push([
								(fileRect.center.x + otherFileRect.center.x) / 2,
								fileRect.center.y + mainFileH / 2 + (mainCircleRadius * 1) / 5
							])
						}

						lineData.push([otherFileRect.center.x, otherFileRect.center.y])

						const subsystemId = relationship.system1

						const [fromName, toName] = !subsystemId
							? FileRelationshipsUtil.getFromToName(
									fileRect.name,
									otherFileRect.name,
									relationship,
									fileRect.id
							  )
							: [fileRect.statusKey, otherFileRect.statusKey]

						const lineParams = {
							lineData,
							isSub: true,
							opacity: 0.5,
							displayNone: true,
							isParentChild: FileRelationshipsUtil.isParentChild(relationship)
						}

						const strokeColor = this.getRectAndConnectionLineStrokeColor(lineParams)

						this.createConnectionLine(
							fromName,
							toName,
							relationship.uniCount,
							relationship.name,
							`${fileRect.id}~${otherFileId}`,
							strokeColor,
							lineParams
						)
					})
				})

			if (this.displayChildren) {
				let childrenTreeDrawQueue = []

				this.mainRelationshipsToDisplay.forEach((relationship, i) => {
					const subfileId = FileRelationshipsUtil.getSubFileId(relationship, this.mainFileId)

					if (FileRelationshipsUtil.isChild(relationship, this.mainFileId) || relationship.type == 0) {
						childrenTreeDrawQueue.push([subfileId, 0, angle * i, angle])
					}
				})

				let { subRelaObj } = this.relationshipsObj

				while (childrenTreeDrawQueue.length != 0) {
					let [fileId, level, startingAngle] = childrenTreeDrawQueue.shift()
					if (!this.displaySubChildren[level] ?? true) {
						continue
					}

					let childrenRelationships = FileRelationshipsUtil.bothInFolder(
						FileRelationshipsUtil.sub(subRelaObj[this.viewType], fileId),
						this.files,
						this.containingFolder,
						this.mainFile
					)

					let relaLength = childrenRelationships.length
					if (level == 0) {
						// Mix with different relationships
						relaLength = 0
						childrenRelationships.forEach(rela => FileRelationshipsUtil.isChild(rela, fileId) && relaLength++)
					}

					const myRect = fileRects.find(r => r.stringId == `file${fileId}`)
					const angleForEach = this.angleEachPerLevel[level]

					let indexOfChild = 0
					const startingAngleNew =
						relaLength % 2 != 0
							? startingAngle - angleForEach * Math.floor(relaLength / 2)
							: startingAngle - angleForEach / 2 - angleForEach * (Math.floor(relaLength / 2) - 1)

					childrenRelationships.forEach(childRelationship => {
						if (!FileRelationshipsUtil.isChild(childRelationship, fileId)) {
							return
						}

						const childFileId = FileRelationshipsUtil.getSubFileId(childRelationship, fileId)
						const currentAngle = startingAngleNew + angleForEach * indexOfChild

						if (level + 1 < this.subLevelCount) {
							childrenTreeDrawQueue.push([childFileId, level + 1, currentAngle, angleForEach])
						}

						const dstX = centerCoord + ratioPerLevel[level + 1] * Math.sin(currentAngle)
						const dstY = centerCoord - ratioPerLevel[level + 1] * Math.cos(currentAngle)

						var childFile = FileUtil.file(this.files, childFileId)
						const childName = childFile.name

						var rectCenter = { x: dstX, y: dstY }

						const strokeColor = this.getRectAndConnectionLineStrokeColor({
							childrenLvl2Index: level
						})

						const subsystemId = childRelationship.system1
						const statusKey = subsystemId ? `subsys${childFileId}` : childName

						const [fromName, toName] = !subsystemId
							? FileRelationshipsUtil.getFromToName(myRect.statusKey, childName, childRelationship, myRect.id)
							: [myRect.statusKey, statusKey]

						var lineData = [
							[myRect.center.x, myRect.center.y],
							[rectCenter.x, rectCenter.y]
						]

						this.fileRectStatuses[statusKey] = {
							id: childFileId,
							showLines: false,
							show: !this.hideOnRender,
							key: statusKey
						}

						const lineParams = {
							lineData,
							isSub: false,
							displayNone: this.hideOnRender,
							isParent: false,
							isChild: true
						}

						this.createConnectionLine(
							fromName,
							toName,
							childRelationship.uniCount,
							childRelationship.name,
							`${myRect.id}~${childFileId}`,
							strokeColor,
							lineParams
						)

						// get the name of the file containing the subsystem
						// if that subsystem comes from a different file
						// then we treat it as a reference block
						// so that when we click on it we will go to the other file
						const fileContainSubsys = subsystemId ? FileUtil.getRealFileNameFromFake(childFile) : null
						const subsysAsRef = fileContainSubsys && fileContainSubsys != realMainFileName

						fileRects.push(
							Object.create(FileRect).init(childFileId, rectCenter, childName, this.parent, this, {
								width: subFileH * subWHRatio,
								height: subFileH,
								handler: subsystemId
									? this.subSysClickHandlerGenerator(
											subsysAsRef
												? { blockType: 'Reference', sourceFile: fileContainSubsys }
												: { blockType: 'SubSystem' },
											{ sysId: parseInt(subsystemId), relaFakeFileId: childFileId },
											'down',
											subsysAsRef
									  )
									: this.fileRectClickHandlerGenerator(),
								hideOnRender: this.hideOnRender,
								isSub: true,
								strokeColor,
								statusKey
							})
						)

						indexOfChild++
					})
				}
			}

			fileRects.forEach(fileRect => fileRect.draw())
			// raise the tooltips so that they are on top of other elements
			d3.selectAll("g[class^='line-tooltip']").raise()
		}
	},
	centerViewToMainCircle() {
		const { centerCoord, ratioPerLevel } = this.globalDrawConst

		// Transform the parent so that the main circle is centered
		// Increase the main circle draw area a bit so the scale will leave a little padding in the view
		var scaleToFitFactor = this.height / (ratioPerLevel[0] * 2.5)

		this.canvas.call(this.zoom.translateTo, centerCoord, centerCoord)
		this.canvas.call(this.zoom.scaleTo, scaleToFitFactor)
	},
	initProps(relationshipsObj, type, files) {
		// fundamental props
		this.relationshipsObj = relationshipsObj
		this.viewType = type
		this.files = files.map(file => {
			if (FileUtil.isFake(file)) file.containingFilePath = FileUtil.getContainingFilePath(files, file)
			return file
		})

		this.initInteractingProps()

		// function groups props
		this.functionGroups = {}
		this.currentFunctionGroups = {}
		this.isSwitchingFuncGroupLevel = false
		this.maxFunctionGroupLevel = 6 // default and could be change when init function groups
	},
	initInteractingProps() {
		// props for interacting with file rects
		this.fileRectStatuses = {}
		this.lineStatuses = {}
		this.highlightedFiles = []
		this.selectAllFiles = false
	},
	initFunctionGroups(options) {
		const mainFileParentFolder = FileUtil.parentFolder(this.mainFile)
		var subFolders = this.files
			.filter(file => !FileUtil.isFake(file) && FileUtil.isInFolder(file, mainFileParentFolder))
			.map(file => file.path.substring(file.path.indexOf(mainFileParentFolder)).split('\\')[1] ?? '')
			.filter((path, i, self) => self.indexOf(path) == i && !path.endsWith('.slx') && !path.endsWith('.mdl'))

		// if there is no sub-folder then the main file is at the final level of function group
		// default maximum is 6
		// additionally, we are disabling grouping when there's only one folder
		if (subFolders.length <= 1) {
			this.maxFunctionGroupLevel = -1
			this.functionGroupLevel = this.maxFunctionGroupLevel
			this.isCurrentlyInLastFunctionGroupLevel = true
		} else {
			this.disablePopupBtn()
			this.functionGroupLevel = 1
			this.isCurrentlyInLastFunctionGroupLevel = false

			var addFunctionGroup = (groupObj, groupLevelArr, level) => {
				if (level >= this.maxFunctionGroupLevel) return

				if (groupObj[groupLevelArr[level - 1]]) {
					groupObj[groupLevelArr[level - 1]].count++
				} else {
					groupObj[groupLevelArr[level - 1]] = { count: 1, children: {} }
				}

				var children = groupObj[groupLevelArr[level - 1]].children
				addFunctionGroup(children, groupLevelArr, level + 1)
			}

			subFolders.forEach(folder => {
				// default is 5 groups for 5 levels
				// else we update the maximum to fit project

				// String in the blacklist array that is at the end of the folder name will not be sliced separately
				// For example: ABC_XYZ_SPC, since SPC in blacklist, only two group will be partitioned, ABC and XYZ_SPC
				// Put by search priority, and upper-cased
				// Blacklist search is performed case-insensitive (!! dont know if this is the requirement)
				const blackListEndPartition = ['SPC_NOT', 'SPC']
				const folderNameUppered = folder.toUpperCase()

				// Append the blacklist partitioned to the last group that will be sliced using split
				var lastGroupNameAppend = ''

				blackListEndPartition.every(blacklistString => {
					if (folderNameUppered.endsWith('_' + blacklistString)) {
						folder = folder.substring(0, folder.length - blacklistString.length - 1)
						lastGroupNameAppend = '_' + blacklistString

						return false
					}

					return true
				})

				var groupLevelArr = folder.split('_')

				if (lastGroupNameAppend != '') groupLevelArr[groupLevelArr.length - 1] += lastGroupNameAppend

				this.maxFunctionGroupLevel = groupLevelArr.length + 1

				addFunctionGroup(this.functionGroups, groupLevelArr, 1)
			})
		}

		// if we receive a initial props
		// then we reinit function groups to this
		const { currentFunctionGroups, functionGroupLevel, isCurrentlyInLastFunctionGroupLevel } = options
		if (currentFunctionGroups && functionGroupLevel) {
			let currentFunctionGroupsArr = currentFunctionGroups.split('_')

				this.functionGroupLevel = functionGroupLevel
				this.isCurrentlyInLastFunctionGroupLevel = isCurrentlyInLastFunctionGroupLevel;
				if (isCurrentlyInLastFunctionGroupLevel) {
					this.enablePopupBtn();
				} else {
					this.disablePopupBtn();
				}
				for (let level = 1; level < functionGroupLevel; level++) {
					this.currentFunctionGroups[`lv${level}`] = currentFunctionGroupsArr[level - 1]
				}
			}
		},
		initContainingFolder() {
			this.containingFolder = Object.values(this.currentFunctionGroups).join('_')
			if (!this.containingFolder) this.containingFolder = FileUtil.parentFolder(this.mainFile)
		},
		initRelationshipLists() {
			var { mainRelaObj, subRelaObj, fileCountPerLevelObj } = this.relationshipsObj

		// get all the relationships that the subfile is in a particular folder
		// based on selected function groups
		// and all the relationships related to main file and its subsystems
		var mainRelationships = FileRelationshipsUtil.parentToTop(
			FileRelationshipsUtil.subInFolder(
				FileRelationshipsUtil.distinct(mainRelaObj[this.viewType]),
				this.files,
				this.containingFolder,
				this.mainFile
			),
			this.mainFileId
		)

		this.mainRelationshipsToDisplay = FileRelationshipsUtil.getDisplayRelationships(
			mainRelationships,
			this.displayChildren,
			this.displayEquals,
			this.displayParents,
			this.displayChildLibraries,
			// this.showSubSystemAsChild,
			this.mainFileId,
			this.files
		)

		this.relationshipsBetweenMainDisplayFiles = FileRelationshipsUtil.getRelationshipsBetweenMainDisplayFiles(
			this.mainRelationshipsToDisplay,
			subRelaObj[this.viewType],
			this.mainFileId
		).map(relationships =>
			FileRelationshipsUtil.bothInFolder(relationships, this.files, this.containingFolder, this.mainFile)
		)

		this.fileCountPerLevel = fileCountPerLevelObj
		this.subLevelCount = 0

		while (this.fileCountPerLevel[this.viewType][this.subLevelCount] != 0) {
			this.subLevelCount++
		}

		this.allConnections = FileRelationshipsUtil.distinct(
			FileRelationshipsUtil.all(this.mainRelationshipsToDisplay, this.relationshipsBetweenMainDisplayFiles)
		)

		if (this.displayChildren) {
			this.allConnections = FileRelationshipsUtil.distinct(
				this.allConnections.concat(
					FileRelationshipsUtil.bothInFolder(
						FileRelationshipsUtil.getRecursiveChildrenRelationships(
							this.mainRelationshipsToDisplay,
							subRelaObj[this.viewType],
							this.displaySubChildren,
							this.displayChildLibraries,
							this.mainFileId,
							this.files
						).flat(),
						this.files,
						this.containingFolder,
						this.mainFile
					)
				)
			)
		}
	},
	initGlobDrawingConst() {
		const totalLength = this.mainRelationshipsToDisplay.length
		const baseRadiusAdd = 250

		var ratioPerLevel = [Math.min(0.6 + totalLength * 0.002, 10) * baseRadiusAdd]
		var currentLevel = 0

		this.angleEachPerLevel = []

		if (this.fileCountPerLevel != null) {
			while (this.fileCountPerLevel[this.viewType][currentLevel] != 0) {
				const fcount = this.fileCountPerLevel[this.viewType][currentLevel]

				// Random adjusted math))
				ratioPerLevel.push(
					ratioPerLevel[ratioPerLevel.length - 1] +
						Math.min(0.6 + fcount * 0.002 + currentLevel * 0.001, 10) * (baseRadiusAdd - currentLevel * 0.005)
				)
				this.angleEachPerLevel.push(
					Math.min(
						((Math.PI / 2) * 4) / 5 - currentLevel * 0.05,
						(Math.PI * 2) / (fcount + Math.min(fcount / 10, 10))
					)
				)

				currentLevel++
			}
		}

		const centerCoord = ratioPerLevel[ratioPerLevel.length - 1]

		const angle = (2 * Math.PI) / totalLength

		const WHRatio = 2.5
		const subWHRatio =
			totalLength > 25
				? WHRatio + 0.15 * totalLength
				: totalLength > 15
				? WHRatio + 0.1 * totalLength
				: WHRatio

		const mainFileH = 40

		const subFileH = Math.min((ratioPerLevel[0] * 2) / (Math.floor(totalLength / 2) + 1), mainFileH * 0.8)

		const ySpacing =
			totalLength > 2
				? (ratioPerLevel[0] * 2 - subFileH * (Math.floor(totalLength / 2) + 1)) / Math.floor(totalLength / 2)
				: 0

		// Display children (recursive) will always have uniCount = 1, don't check
		const maxRelationshipCount =
			this.mainRelationshipsToDisplay.length != 0
				? Math.max(...FileRelationshipsUtil.toCountList(this.mainRelationshipsToDisplay))
				: 1

		this.globalDrawConst = {
			totalLength,
			centerCoord,
			ratioPerLevel,
			angle,
			subFileH,
			ySpacing,
			maxRelationshipCount,
			mainFileH,
			subWHRatio,
			WHRatio
		}
	},
	initClickHandlers(handlers) {
		var {
			lineClickHandler = noop,
			fileRectClickHandlerGenerator = noopGenerator,
			subSysClickHandlerGenerator = noopGenerator,
			parentSysClickHandler = noop,
			onToggleFileLines = noop,
			onToggleLine = noop,
			onDisplayChange = noop
		} = handlers
		this.lineClickHandler = lineClickHandler
		this.fileRectClickHandlerGenerator = fileRectClickHandlerGenerator
		this.subSysClickHandlerGenerator = subSysClickHandlerGenerator
		this.onToggleFileLines = onToggleFileLines
		this.onToggleLine = onToggleLine
		this.onDisplayChange = onDisplayChange
		this.parentSysClickHandler = parentSysClickHandler
	},
	initSpecificOptions(options, type) {
		const {
			hideOnRender = false,
			allowToggleVisibility = true,
			displayParents = ($('#displayParents').val() ?? 'true') == 'true',
			displayEquals = ($('#displayEquals').val() ?? 'true') == 'true',
			displayChildLibraries = ($('#displayChildLibraries').val() ?? 'true') == 'true',
			displayChildren = ($('#displayChildren').val() ?? 'false') == 'true',
			allowChangeFuncGrLevel = true
			// showSubSystemAsChild = false,
		} = options
		this.hideOnRender = hideOnRender
		this.allowToggleVisibility = allowToggleVisibility
		this.displayEquals = displayEquals
		this.displayParents = displayParents
		this.displayChildren = this.isCalibrationType(type) ? false : displayChildren
		this.displayChildLibraries = displayChildLibraries
		this.allowChangeFuncGrLevel = allowChangeFuncGrLevel
		//this.showSubSystemAsChild = showSubSystemAsChild

		const displaySubChildrenVal = $('#displaySubChildren').val() ?? ''
		for (let i = 0; i < displaySubChildrenVal.length; i++) {
			this.displaySubChildren.push(displaySubChildrenVal[i] == '1')
		}
	},
	createDisplayOptionCheckbox(checkboxId, labelText, showOption, correspondValue, disabled) {
		return $('<div>').append(
			$(`<input type="checkbox" id="${checkboxId}" />`)
				.change(e => {
					correspondValue = e.target.checked

					if (correspondValue) {
						this.showDisplay(showOption)
					} else {
						this.hideDisplay(showOption)
					}
				})
				.attr('checked', correspondValue)
				.prop('disabled', disabled ?? false),
			$(`<label for="${checkboxId}"></label>`).text(labelText)
		)
	},
	initDisplayOptions() {
		// create dropdown containing options
		var dropdown = $('<div class="network-dropdown"></div>').append(
			$('<span>').text('Display modes'),
			this.createDisplayOptionCheckbox('displayParentsCheck', 'Parents', 'parents', this.displayParents),
			this.createDisplayOptionCheckbox('displayEqualsCheck', 'Equals', 'equals', this.displayEquals),
			this.createDisplayOptionCheckbox(
				'displayChildrenCheck',
				'Children',
				'children',
				this.displayChildren,
				this.isCalibrationType(this.viewType)
			),
			this.createDisplayOptionCheckbox(
				'displayChildLibrariesCheck',
				'Duplicate libraries',
				'libraries',
				this.displayChildLibraries,
				!this.displayChildren
			)
		)

		while (this.displaySubChildren.length < this.subLevelCount) {
			this.displaySubChildren.push(false)
		}

		if (this.subLevelCount > 0) {
			dropdown.append($('<hr>'))

			for (let i = 0; i < this.subLevelCount; i++) {
				dropdown.append(
					this.createDisplayOptionCheckbox(
						`displayChildrenLvl${i + 2}Check`,
						`Children Lvl.${i + 2}`,
						`subChildren${i}`,
						this.displaySubChildren[i],
						this.displayChildren == false
							? true
							: !(i == 0 || this.displaySubChildren[i - 1] == true ? true : false)
					)
				)
			}
		}

		$('.main-container').append(dropdown)

		const y = `top: ${this.allowPopUp ? 25 : 5}px`
		const x = 'right: 5px'

		const btnPath = `
				<path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z" style="transform: scale(50.5) translateY(-0.75px);" />
				<path style="transform: scale(0.85) translate(184px, 178px);" d="M528 0h-480C21.5 0 0 21.5 0 48v320C0 394.5 21.5 416 48 416h192L224 464H152C138.8 464 128 474.8 128 488S138.8 512 152 512h272c13.25 0 24-10.75 24-24s-10.75-24-24-24H352L336 416h192c26.5 0 48-21.5 48-48v-320C576 21.5 554.5 0 528 0zM512 352H64V64h448V352z" />
			`

		var dropdownVisible = false

		function showDropDown(e) {
			if (dropdownVisible) return

			dropdown
				.css('display', 'block')
				.css('top', e.clientY + 10)
				.css('left', e.clientX - 170)
			dropdownVisible = true

			function outsideClickListener(event) {
				var $target = $(event.target)
				if (!$target.closest('.network-dropdown').length && dropdown.is(':visible')) {
					dropdown.css('display', 'none')
					dropdownVisible = false
					removeClickListener()
				}
			}

			function removeClickListener() {
				$(document).off('click', outsideClickListener)
			}

			setTimeout(() => $(document).on('click', outsideClickListener), 0)
		}

		const disabled = !this.isCurrentlyInLastFunctionGroupLevel
		const id = 'displayModes'

		$(`#${this.viewId}`).append(
			this.createBtn({
				btn: { x, y, clickFn: showDropDown, path: btnPath, viewBoxWH: '818 736', disabled, id },
				tooltip: { x: -140, y: 20, msg: 'Display options' }
			})
		)
	},
	destroyDisplayOptDropdown() {
		$('.network-dropdown').remove()
	},
	createConnectionLine(fromName, toName, count, name, fileIds, strokeColor, props) {
		const { mainFileH, maxRelationshipCount } = this.globalDrawConst
		const {
			isSub,
			displayNone = false,
			opacity = 1,
			isParentChild,
			isParent,
			isChild,
			explicitColour = null
		} = props
		var { lineData } = props
		const strokeWidth = this.getStrokeWidth(count, maxRelationshipCount)

		const lineClass = `${FileUtil.nameWithoutExt(fromName)}_${FileUtil.nameWithoutExt(toName)}`

		this.lineStatuses[lineClass] = { show: !displayNone, highlight: false, triggerHidden: false, name }

		var lineTooltip = this.parent
			.append('g')
			.attr('class', `line-tooltip-${lineClass.replace(' ', '')}`)
			.style('opacity', 0)
			.style('display', 'none')
			.style('pointer-events', 'none')

		const wScale = 2.5
		var lineTooltipRect = lineTooltip
			.append('rect')
			.attr('height', mainFileH)
			.attr('width', mainFileH * wScale)
			.attr('stroke', 'black')
			.attr('fill', 'white')

		const fontSize = mainFileH / 2 - (isParentChild ? 2 : 0)
		const tooltipContent = isParent
			? 'Parent'
			: isChild
			? 'Child'
			: isParentChild
			? 'Parent-child'
			: `Count: ${count}`

		var lineTooltipText = lineTooltip
			.append('text')
			.attr('text-anchor', 'middle')
			.attr('dominant-baseline', 'central')
			.attr('font-size', fontSize)
			.attr('fill', 'black')
			.text(tooltipContent)

		this.parent
			.append('path')
			.attr('class', `network-line ${lineClass}${isSub ? ' network-line--sub' : ''}`)
			.attr('d', d3.line()(lineData))
			.attr('stroke', strokeColor)
			.attr('stroke-width', strokeWidth)
			.attr('stroke-linejoin', 'round')
			.attr('fill', 'none')
			.attr('count', count)
			.attr('name', name)
			.attr('opacity', opacity)
			.style('display', displayNone ? 'none' : 'block')
			.on('click', () => {
				if (isChild || isParent || isParentChild) return
				const allowHighlight = this.lineClickHandler(fileIds)
				allowHighlight && this.highlightLine({ className: lineClass }, false)
			})
			.on('mouseover', e => {
				const [mouseX, mouseY] = d3.pointer(e)
				const tooltipPadding = 10

				lineTooltipRect
					.attr('x', mouseX - (mainFileH * wScale) / 2 - tooltipPadding)
					.attr('y', mouseY - mainFileH / 2 - tooltipPadding)
				lineTooltipText.attr('x', mouseX - tooltipPadding).attr('y', mouseY - tooltipPadding)

				lineTooltip.style('display', 'block')
				lineTooltip.transition().delay(300).duration(200).style('opacity', 1)
			})
			.on('mouseleave', () => {
				lineTooltip.transition().duration(200).style('opacity', 0)
				lineTooltip.style('display', 'none')
			})
	},
	getCurrentLevelFunctionGroups() {
		if (this.isCurrentlyInLastFunctionGroupLevel) return []

		var functionGroupsObj = this.functionGroups
		for (let i = 1; i < this.functionGroupLevel; i++) {
			functionGroupsObj = functionGroupsObj[this.currentFunctionGroups[`lv${i}`]].children
		}

		return Object.entries(functionGroupsObj)
	},
	getFunctionGroupClickHandler(functionGroup, lastGroupLevel) {
		return () => {
			if (!this.allowChangeFuncGrLevel || this.isSwitchingFuncGroupLevel) return

			this.isSwitchingFuncGroupLevel = true

			this.currentFunctionGroups[`lv${this.functionGroupLevel}`] = functionGroup
			this.functionGroupLevel++
			this.parent.html(null)

			this.isCurrentlyInLastFunctionGroupLevel = lastGroupLevel

			// we initialize the display relationships based on the selected function groups
			// and enable buttons
			if (this.isCurrentlyInLastFunctionGroupLevel) {
				this.reInit()
				this.enablePopupBtn()
				this.enableDrawingUtilBtn('displayModes')
			}
			this.draw()

			this.isSwitchingFuncGroupLevel = false
		}
	},
	addGoUpFunctionGroupLevelListener() {
		this.canvas.on('wheel', e => {
			if (!this.allowChangeFuncGrLevel || this.isSwitchingFuncGroupLevel) return
			const delta = e.wheelDelta ?? -e.detail

			if (!e.ctrlKey && delta > 0) {
				if (this.functionGroupLevel <= 1 || Object.keys(this.functionGroups).length == 0) return

				this.isSwitchingFuncGroupLevel = true

				this.functionGroupLevel--
				this.currentFunctionGroups[`lv${this.functionGroupLevel}`] = null
				this.parent.html(null)

				// these buttons are not available in function group view
				// so we disable them when we enter the function group view
				if (this.isCurrentlyInLastFunctionGroupLevel) {
					this.disablePopupBtn()
					this.disableDrawingUtilBtn('displayModes')

					// close the dropdown if it is open
					$(`#${this.drawId}`).click()

					this.isCurrentlyInLastFunctionGroupLevel = false
				}

				this.draw()

				this.isSwitchingFuncGroupLevel = false
			}
		})
	},
	showDisplay(mode) {
		switch (mode) {
			case 'parents':
				this.displayParents = true
				break
			case 'children':
				this.displayChildren = true
				$(`#displayChildLibrariesCheck`).prop('disabled', false)
				break
			case 'equals':
				this.displayEquals = true
				break
			case 'libraries':
				this.displayChildLibraries = true
				break
			default:
				break
		}

		var startLevelEnable = mode == 'children' ? 0 : -1

		if (mode.startsWith('subChildren')) {
			startLevelEnable = parseInt(mode.substring(11))
			this.displaySubChildren[startLevelEnable++] = true
		}

		if (startLevelEnable >= 0) {
			while (startLevelEnable < this.displaySubChildren.length) {
				$(`#displayChildrenLvl${startLevelEnable + 2}Check`).prop(
					'disabled',
					!(startLevelEnable == 0 || this.displaySubChildren[startLevelEnable - 1] == true)
				)
				startLevelEnable++
			}
		}

		this.parent.html(null)
		this.reInit()
		this.draw()

		this.onDisplayChange()
	},
	hideDisplay(mode) {
		switch (mode) {
			case 'parent':
				this.displayParents = false
				break
			case 'children':
				this.displayChildren = false
				$(`#displayChildLibrariesCheck`).prop('disabled', true)
				break
			case 'equals':
				this.displayEquals = false
				break
			case 'libraries':
				this.displayChildLibraries = false
				break
			default:
				break
		}

		var startLevelDisable = mode == 'children' ? 0 : -1

		if (mode.startsWith('subChildren')) {
			startLevelDisable = parseInt(mode.substring(11))
			this.displaySubChildren[startLevelDisable++] = false
		}

		if (startLevelDisable >= 0) {
			while (startLevelDisable < this.displaySubChildren.length) {
				$(`#displayChildrenLvl${startLevelDisable + 2}Check`).prop('disabled', true)
				startLevelDisable++
			}
		}

		this.parent.html(null)
		this.reInit()
		this.draw()

		this.onDisplayChange()
	},
	tempHighlightFileLines(fileName) {
		const nameWithoutExt = FileUtil.nameWithoutExt(fileName)
		for (const lineClass in this.lineStatuses) {
			if (lineClass.endsWith(`_${nameWithoutExt}`) || lineClass.startsWith(`${nameWithoutExt}_`)) {
				this.highlightLine({ className: lineClass }, true)
			}
		}
	},
	tempUnHighlightFileLines(fileName) {
		const nameWithoutExt = FileUtil.nameWithoutExt(fileName)
		for (const lineClass in this.lineStatuses) {
			if (lineClass.endsWith(`_${nameWithoutExt}`) || lineClass.startsWith(`${nameWithoutExt}_`)) {
				this.unHighlightLine({ className: lineClass }, true)
			}
		}
	},
	highlightLine({ className, name }, tempHL) {
		const lineClass =
			className ?? (Object.entries(this.lineStatuses).find(([, status]) => status.name == name) ?? [])[0]

		if (!this.lineStatuses[lineClass]) {
			return false
		}

		d3.selectAll(`.network-line.${lineClass}`).attr('filter', 'drop-shadow(0px 0px 3px #3ae6f0)')
		if (!tempHL) {
			this.lineStatuses[lineClass].highlight = true
		}

		return true
	},
	unHighlightLine({ className, name }, tempHL) {
		const lineClass =
			className ?? (Object.entries(this.lineStatuses).find(([, status]) => status.name == name) ?? [])[0]

		if (!this.lineStatuses[lineClass]) {
			return false
		}

		if (tempHL) {
			if (!this.lineStatuses[lineClass].highlight) {
				d3.selectAll(`.network-line.${lineClass}`).attr('filter', 'none')
			}
		} else {
			d3.selectAll(`.network-line.${lineClass}`).attr('filter', 'none')
			this.lineStatuses[lineClass].highlight = false
		}

		return true
	},
	unHighlightAllLines() {
		Object.keys(this.lineStatuses).forEach(lineClass => this.unHighlightLine({ className: lineClass }, false))
	},
	toggleFileSubLinesVisibility(fileName) {
		if (this.fileRectStatuses[fileName].showLines) {
			this.hideFileLines(fileName)
		} else {
			this.showFileLines(fileName)
		}
	},
	updateLinesWidth() {
		var displayLines = $('.network-line:not([style*="display: none"])')
		const newMaxCount = Math.max(
			...displayLines.map(function () {
				return Number($(this).attr('count'))
			})
		)

		var thisDraw = this

		displayLines.each(function () {
			const lineCount = $(this).attr('count')
			$(this).attr('stroke-width', thisDraw.getStrokeWidth(lineCount, newMaxCount))
		})
	},
	showLine({ className, name }, tempShow, isSub) {
		const lineClass =
			className ?? (Object.entries(this.lineStatuses).find(([, status]) => status.name == name) ?? [])[0]

		if (!this.lineStatuses[lineClass]) {
			return false
		}

		d3.selectAll(`.network-line.${lineClass}${isSub ? '.network-line--sub' : ''}`).style('display', 'block')

		this.updateLinesWidth()

		if (!tempShow) {
			this.lineStatuses[lineClass].show = true
			this.onToggleLine()
		}

		return true
	},
	hideLine({ className, name }, tempShow, isSub) {
		const lineClass =
			className ?? (Object.entries(this.lineStatuses).find(([, status]) => status.name == name) ?? [])[0]

		if (!this.lineStatuses[lineClass]) {
			return false
		}

		var subConnectionLines = d3.selectAll(`.network-line.${lineClass}${isSub ? '.network-line--sub' : ''}`)
		this.updateLinesWidth()

		if (tempShow) {
			if (!this.lineStatuses[lineClass].show) {
				subConnectionLines.style('display', 'none')
			}
		} else {
			subConnectionLines.style('display', 'none')
			this.lineStatuses[lineClass].show = false
			this.onToggleLine()
		}

		return true
	},
	showFileLines(fileName) {
		const nameWithoutExt = FileUtil.nameWithoutExt(fileName)
		for (const lineClass in this.lineStatuses) {
			if (!lineClass.includes(nameWithoutExt)) {
				continue
			}

			this.showLine({ className: lineClass }, false, true)
		}

		this.fileRectStatuses[fileName].showLines = true
		this.onToggleFileLines()
	},
	hideFileLines(fileName) {
		const nameWithoutExt = FileUtil.nameWithoutExt(fileName)
		for (const lineClass in this.lineStatuses) {
			if (!lineClass.includes(nameWithoutExt)) {
				continue
			}
			const otherFileName = lineClass.replace(nameWithoutExt, '').replace('_', '')

			if (
				this.fileRectStatuses[`${otherFileName}.mdl`]?.showLines ||
				this.fileRectStatuses[`${otherFileName}.slx`]?.showLines
			) {
				continue
			}

			this.hideLine({ className: lineClass }, false, true)
		}

		this.fileRectStatuses[fileName].showLines = false
		this.onToggleFileLines()
	},
	showFile(fileName, tempShow) {
		var status =
			this.fileRectStatuses[fileName] ??
			this.fileRectStatuses[`${fileName}.mdl`] ??
			this.fileRectStatuses[`${fileName}.slx`]

		if (!status) {
			return
		}

		d3.selectAll(`.file${status.id}`).style('display', 'block')
		if (!tempShow) {
			status.show = true
		}
	},
	hideFile(fileName, tempHide) {
		var status =
			this.fileRectStatuses[fileName] ??
			this.fileRectStatuses[`${fileName}.mdl`] ??
			this.fileRectStatuses[`${fileName}.slx`]

		if (!status) {
			return
		}

		var fileRects = d3.selectAll(`.file${status.id}`)
		fileName = status.key

		if (tempHide) {
			if (!status.show) {
				fileRects.style('display', 'none')
			}
		} else if (this.isAllFileLinesHidden(fileName)) {
			fileRects.style('display', 'none')
			status.show = false
		}
	},
	addHighlightFile(fileId) {
		this.highlightedFiles.push(fileId)
	},
	removeHighlightFile(fileId) {
		this.highlightedFiles.splice(this.highlightedFiles.indexOf(fileId), 1)
	},
	getSelectedFiles() {
		return this.selectAllFiles || this.highlightedFiles.length == 0
			? Object.values(this.fileRectStatuses)
					.filter(status => typeof status.id != 'string')
					.map(status => status.id)
					.concat([this.mainFileId])
			: this.highlightedFiles
	},
	isAllLinesShown() {
		return Object.values(this.lineStatuses)
			.filter(status => !status.triggerHidden)
			.every(status => status.show)
	},
	isAllSubLinesShown() {
		return Object.values(this.fileRectStatuses)
			.filter(status => typeof status.id != 'string')
			.every(status => status.showLines)
	},
	isAllSubLinesHiden() {
		return Object.values(this.fileRectStatuses)
			.filter(status => typeof status.id != 'string')
			.every(status => !status.showLines)
	},
	isAllFileLinesHidden(fileName) {
		return Object.entries(this.lineStatuses)
			.filter(([lineClass]) => lineClass.includes(fileName))
			.every(([, status]) => !status.show)
	},
	hideAllSubLines() {
		Object.keys(this.fileRectStatuses)
			.filter(status => typeof status.id != 'string')
			.forEach(fileName => this.hideFileLines(fileName))
	},
	showAllSubLines() {
		Object.keys(this.fileRectStatuses)
			.filter(status => typeof status.id != 'string')
			.forEach(fileName => this.showFileLines(fileName))
	},
	setLineTriggerStatus(lineClass, status) {
		this.lineStatuses[lineClass].triggerHidden = status
	},
	getStrokeWidth(count, max) {
		const { totalLength, subFileH } = this.globalDrawConst
		return Math.max(((subFileH * (0.8 + totalLength * 0.003)) / max) * count, 1.5)
	},
	getDisplayOptions() {
		return {
			displayChildren: this.displayChildren,
			displayEquals: this.displayEquals,
			displayParents: this.displayParents,
			displayChildLibraries: this.displayChildLibraries,
			displaySubChildren: this.displaySubChildren.map(value => (value ? 1 : 0)).join('')
		}
	},
	getCurrentFunctionGroups() {
		return {
			functionGroupLevel: this.functionGroupLevel,
			currentFunctionGroups: Object.values(this.currentFunctionGroups)
				.filter(funcGr => funcGr)
				.join('_'),
			isCurrentlyInLastFunctionGroupLevel: this.isCurrentlyInLastFunctionGroupLevel
		}
	},
	getAppendOptionsToUrlFunc(optionGetters = []) {
		return url => {
			optionGetters = Array.isArray(optionGetters) ? optionGetters : [optionGetters]
			optionGetters = optionGetters.map(getter => getter.bind(this))

			return (
				url +
				Object.entries({
					...optionGetters.reduce((prev, getter) => ({ ...prev, ...getter() }), {})
				}).reduce((prev, [key, value]) => `${prev}&${key}=${value}`, '')
			)
		}
	},
	get appendDisplayOptionsToUrl() {
		return this.getAppendOptionsToUrlFunc(this.getDisplayOptions)
	},
	get appendAllOptionsToUrl() {
		return this.getAppendOptionsToUrlFunc([this.getDisplayOptions, this.getCurrentFunctionGroups])
	},
	isCalibrationType(type) {
		return type == 'calibrationRelationships'
	}
}
export default NetworkDraw
