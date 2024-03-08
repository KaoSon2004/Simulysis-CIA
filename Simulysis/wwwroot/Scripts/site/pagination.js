import { noop, returnSelf } from './noop.js'

var Pagination = {
	currentPage: 1,
	container: null,
	formattedData: null,
	config: null,
	toHtml: noop,
	beforePageRender: noop,
	onPageData: noop,

	init(
		dataSrc,
		container,
		toHtml = noop,
		formatResult = returnSelf,
		{
			pageSize = Number($('#tablePagiSize')?.val() ?? 10),
			pageRange = 1,
			autoHideNext = true,
			autoHidePrevious = true,
			customFunctions = {}
		} = {}
	) {
		this.config = { pageSize, pageRange, autoHideNext, autoHidePrevious }
		this.formattedData = formatResult(dataSrc)
		this.currentPage = 1
		this.toHtml = toHtml
		this.container = container

		var { beforePageRender = noop, onPageData = noop } = customFunctions
		this.beforePageRender = beforePageRender
		this.onPageData = onPageData

		this.addPageSizeChangeListener()
		this.addPagination()

		return this
	},
	addPagination() {
		this.beforePageRender(this.formattedData)

		$('#signalPagination').pagination({
			...this.config,
			dataSource: this.formattedData,
			pageNumber: this.currentPage,
			beforePreviousOnClick: this.beforePageChangeWithData.bind(this),
			beforeNextOnClick: this.beforePageChangeWithData.bind(this),
			beforePageOnClick: this.beforePageChangeWithData.bind(this),
			callback: data => {
				this.container.html(this.template(data))
				this.onPageData(data)
			}
		})
	},
	addPageSizeChangeListener() {
		$('#tablePagiSize').change(e => {
			this.config = { ...this.config, pageSize: e.target.value }
			this.currentPage = 1
			this.addPagination()
		})
	},
	sort(desc, compareFunc) {
		this.formattedData.sort(compareFunc)
		desc && this.formattedData.reverse()
		this.addPagination()
	},
	template(data) {
		return data.map(this.toHtml)
	},
	beforePageChangeWithData(event, newPage) {
		const newPageNum = Number(newPage)

		var pageRange = (
			newPageNum > this.currentPage
				? [this.currentPage - 1, newPageNum - 1]
				: [this.currentPage - 1, this.currentPage]
		).map(this.toPageIndex.bind(this))

		this.beforePageRender(this.formattedData.slice(...pageRange))
		this.currentPage = newPageNum
	},
	toPageIndex(page) {
		return page * this.config.pageSize
	}
}

export default Pagination
