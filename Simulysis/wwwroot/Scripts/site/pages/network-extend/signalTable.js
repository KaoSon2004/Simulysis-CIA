import Pagination from '../../pagination.js'
import FileUtil from '../../utils/file.js'
import FileRelationshipsUtil from '../../utils/fileRelationships.js'

var SignalTable = {
	networkDraw: null,
	paginationManager: null,
	formData: null,
	fromSortStatus: 'unsorted',
	toSortStatus: 'unsorted',
	folders: {},
	tableBody: $('#bodyResult'),
	select: $('#folder'),
	form: $('#filterForm'),
	submitBtn: $('#filterBtn'),
	eyeIcon: $('#eyeIcon').html(),
	eyeSlashIcon: $('#eyeSlashIcon').html(),

	init(networkDraw) {
		this.networkDraw = networkDraw
		this.formData = new FormData()
		this.addFilter()
		this.addHeaderListener()
		this.allowFilter()
		this.addAdjustNumLinksInputs()
		this.addRowsWithPagination()
		this.addFolderOptions()
	},
	reInit() {
		this.dissalowFilter()
		this.addRowsWithPagination()
		this.addFolderOptions()
		this.allowFilter()
	},
	addHeaderListener() {
		$('#signalTableFrom').click(() => {
			if (this.fromSortStatus == 'unsorted' || this.fromSortStatus == 'desc') {
				this.paginationManager.sort(true, this.getSignalComparer('from'))
				this.changeSortStatus('from', 'asc')
			} else {
				this.paginationManager.sort(false, this.getSignalComparer('from'))
				this.changeSortStatus('from', 'desc')
			}

			this.changeSortStatus('to', 'unsorted')
		})

		$('#signalTableTo').click(() => {
			if (this.toSortStatus == 'unsorted' || this.toSortStatus == 'desc') {
				this.paginationManager.sort(true, this.getSignalComparer('to'))
				this.changeSortStatus('to', 'asc')
			} else {
				this.paginationManager.sort(false, this.getSignalComparer('to'))
				this.changeSortStatus('to', 'desc')
			}

			this.changeSortStatus('from', 'unsorted')
		})

		$('th:last-child').click(() => {
			const allLineShown = this.networkDraw.isAllLinesShown()

			$('tr[id]').each((i, tr) => {
				if (this.networkDraw.lineStatuses[tr.id].show == allLineShown) {
					$(`#${tr.id} td.eye-container-col`).click()
				}
			})
		})
	},
	addFolderOptions() {
		this.select.empty()
		this.select.append($('<option value="all">All</option>'))

		Object.keys(this.folders)
			.sort()
			.forEach(folder => {
				this.select.append($(`<option value="${folder}">${folder}</option>`))
			})
	},
	addRowsWithPagination() {
		if (!this.networkDraw) return

		this.paginationManager = Pagination.init(
			this.networkDraw.allConnections,
			this.tableBody,
			this.createResultRow.bind(this),
			this.formatRelationships.bind(this),
			{
				customFunctions: {
					beforePageRender: this.hideAndExcludeLines.bind(this),
					onPageData: this.unExcludeLines.bind(this)
				}
			}
		)
	},
	getNiceDisplayNameFromFakeFilePath(path) {
		var groups = path.split(':')
		return `${groups[1].split('-')[0]} (${groups[0]}'s subsystem)`
	},
	formatRelationships(connections) {
		const selectedFolder = this.formData.get('folder') ?? 'all'
		const filterFrom = this.formData.get('from')?.toLowerCase() ?? ''
		const filterTo = this.formData.get('to')?.toLowerCase() ?? ''
		const numLinksLower = Number(this.formData.get('linksLower') ?? 0)
		const numLinksUpper = Number(this.formData.get('linksUpper') ?? 0)

		var { files, mainFileId } = this.networkDraw

		return connections
			.map(connection => {
				var { uniCount, fK_ProjectFileId1, fK_ProjectFileId2, system1, system2 } = connection

				if (FileRelationshipsUtil.isParentChild(connection)) {
					if (fK_ProjectFileId1 != mainFileId && system1) {
						;[fK_ProjectFileId1, fK_ProjectFileId2, system1, system2] = [
							fK_ProjectFileId2,
							fK_ProjectFileId1,
							system2,
							system1
						]
					}
				}

				var [from, to] = [FileUtil.file(files, fK_ProjectFileId1), FileUtil.file(files, fK_ProjectFileId2)]
				var [fromName, toName] = [from.name, to.name].map(FileUtil.nameWithoutExt)
				var [niceFromName, niceToName] = [fromName, toName]

				const [fromFolder, toFolder] = [
					from.containingFilePath ?? from.path,
					to.containingFilePath ?? to.path
				].map(FileUtil.toFolder)

				// Only parent child have this kind of thing going on
				if (system1 || system2) {
					if (system1) {
						fromName = `subsys${fK_ProjectFileId1}`
						niceFromName = this.getNiceDisplayNameFromFakeFilePath(from.path)
					}

					if (system2) {
						toName = `subsys${fK_ProjectFileId2}`
						niceToName = this.getNiceDisplayNameFromFakeFilePath(to.path)
					}
				}

				return {
					fromName,
					toName,
					fromFolder,
					toFolder,
					niceFromName,
					niceToName,
					count: uniCount ?? 1
				}
			})
			.filter(signal => {
				const { fromName, toName, fromFolder, toFolder, count } = signal
				const lineClass = `${fromName}_${toName}`

				if (
					(selectedFolder == 'all' || fromFolder == selectedFolder || toFolder == selectedFolder) &&
					fromName.toLowerCase().includes(filterFrom) &&
					toName.toLowerCase().includes(filterTo) &&
					numLinksLower <= count &&
					(count <= numLinksUpper || !numLinksUpper)
				) {
					this.unExcludeLineAndUpdateHeader(lineClass)
					return true
				} else {
					this.hideAndExcludeLine(lineClass, signal)
					return false
				}
			})
	},
	addFilter() {
		this.form.submit(e => {
			e.preventDefault()

			this.formData = new FormData(e.target)
			this.addRowsWithPagination()

			this.changeSortStatus('from', 'unsorted')
			this.changeSortStatus('to', 'unsorted')
		})
	},
	allowFilter() {
		this.submitBtn.attr('disabled', false)
	},
	dissalowFilter() {
		this.submitBtn.attr('disabled', true)
	},
	createResultRow(signal) {
		const { fromName, toName, fromFolder, toFolder, niceFromName, niceToName } = signal

		this.folders[fromFolder] = true
		this.folders[toFolder] = true

		// var names = { from: fromName, to: toName }
		const lineClass = `${fromName}_${toName}`

		var toggler = $(
			`<td class="col eye-container-col" style="width: 20%; cursor: pointer;">
        <span class="col__icon col__icon--center eye-container" style="pointer-events: none;">
          ${this.eyeSlashIcon}
        </span>
      </td>`
		).click(() => {
			const show = this.networkDraw.lineStatuses[lineClass].show

			if (show) {
				$(`tr#${lineClass} .eye-container`).html(this.eyeSlashIcon)
				this.hideLineAndFile(lineClass, signal, false)
			} else {
				$(`tr#${lineClass} .eye-container`).html(this.eyeIcon)
				this.showLineAndFile(lineClass, signal, false)
			}
		})

		return $(`<tr id="${lineClass}"></tr>`)
			.append(
				$(`<td class="col from-col" style="width: 40%;">${niceFromName}</td>`),
				$(`<td class="col to-col" style="width: 40%;">${niceToName}</td>`),
				toggler
			)
			.mouseover(() => {
				this.showLineAndFile(lineClass, signal, true)
			})
			.mouseout(() => {
				this.hideLineAndFile(lineClass, signal, true)
			})
	},
	changeHeaderEyeIcon() {
		const allLineShown = this.networkDraw ? this.networkDraw.isAllLinesShown() : false

		if (allLineShown) {
			$('th .eye-container').html(this.eyeIcon)
		} else {
			$('th .eye-container').html(this.eyeSlashIcon)
		}
	},
	showLineAndFile(lineClass, signal, temp) {
		const { fromName, toName } = signal

		this.networkDraw.showLine({ className: lineClass }, temp, false)
		this.networkDraw.showFile(fromName, temp)
		this.networkDraw.showFile(toName, temp)
	},
	hideLineAndFile(lineClass, signal, temp) {
		const { fromName, toName } = signal

		this.networkDraw.hideLine({ className: lineClass }, temp, false)
		this.networkDraw.hideFile(fromName, temp)
		this.networkDraw.hideFile(toName, temp)
	},
	changeSortStatus(type, value) {
		this[`${type}SortStatus`] = value

		const capitalizedType = type.charAt(0).toUpperCase() + type.slice(1)

		switch (value) {
			case 'unsorted':
				$(`#signalTable${capitalizedType} .col__icon`).html('<i class="fa fa-sort"></i>')
				break
			case 'asc':
				$(`#signalTable${capitalizedType} .col__icon`).html('<i class="fa fa-sort-asc"></i>')
				break
			case 'desc':
				$(`#signalTable${capitalizedType} .col__icon`).html('<i class="fa fa-sort-desc"></i>')
				break
			default:
				break
		}
	},
	addAdjustNumLinksInputs() {
		$('#linksLower').change(function adjustNumLinksUpper() {
			if (Number($(this).val()) > Number($('#linksUpper').val())) {
				$('#linksUpper').val(Number($(this).val()))
			}
		})

		$('#linksUpper').change(function adjustNumLinksLower() {
			if (Number($(this).val()) < Number($('#linksLower').val())) {
				$('#linksLower').val(Number($(this).val()))
			}
		})
	},
	hideAndExcludeLines(signals) {
		signals.forEach(signal => {
			const { fromName, toName } = signal
			this.hideAndExcludeLine(`${fromName}_${toName}`, signal)
		})
	},
	unExcludeLines(signals) {
		signals.forEach(signal => {
			const { fromName, toName } = signal
			this.unExcludeLineAndUpdateHeader(`${fromName}_${toName}`)
		})
	},
	unExcludeLineAndUpdateHeader(lineClass) {
		this.networkDraw.setLineTriggerStatus(lineClass, false)
		this.changeHeaderEyeIcon()
	},
	hideAndExcludeLine(lineClass, signal) {
		this.hideLineAndFile(lineClass, signal, false)
		this.networkDraw.setLineTriggerStatus(lineClass, true)
	},
	getSignalComparer(type) {
		return function signalComparer(signal1, signal2) {
			return signal1[`${type}Name`].localeCompare(signal2[`${type}Name`])
		}
	}
}

export default SignalTable
