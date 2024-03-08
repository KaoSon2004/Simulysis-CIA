import { BASE_URL } from './config.js'

var FileContentsAPI = {
	REQUEST_URL: `${BASE_URL}/filecontents`,
	async getFileContentById(fileId) {
		try {
			let response = await $.get(`${this.REQUEST_URL}/${fileId}`)

			return { success: true, response }
		} catch (error) {
			console.log(error)
			return { success: false, message: "Failed to get the file's content." }
		}
	},

	// data: { projId, fileName }
	async getFileContentByName(data) {
		try {
			let response = await $.get(`${this.REQUEST_URL}`, data)

			return { success: true, response }
		} catch (error) {
			console.log(error)
			return { success: false, message: "Failed to get the file's content." }
		}
	}
}

export default FileContentsAPI
