import { BASE_URL } from './config.js'

var FilesAPI = {
	REQUEST_URL: `${BASE_URL}/files`,
	async getFilesInProject(projId) {
		try {
			let response = await $.get(`${this.REQUEST_URL}?projId=${projId}`)

			return { success: true, response }
		} catch (error) {
			console.log(error)

			return { success: false, message: "Failed to load the project's files" }
		}
	}
}

export default FilesAPI
