import { BASE_URL } from './config.js'

var FileRelationshipsAPI = {
	REQUEST_URL: `${BASE_URL}/filerelationships`,
	async getFileRelationships(projectId, fileId) {
		try {
			let response = await $.get(`${this.REQUEST_URL}?projectId=${projectId}&fileId=${fileId}`)

			return { success: true, response }
		} catch (error) {
			console.log(error)

			return {
				success: false,
				message: "Failed to load in-view files' relationships"
			}
		}
	}
}

export default FileRelationshipsAPI
