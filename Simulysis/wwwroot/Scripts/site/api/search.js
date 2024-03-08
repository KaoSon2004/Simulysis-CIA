import { BASE_URL } from './config.js'

var SearchAPI = {
	REQUEST_URL: `${BASE_URL}/search`,
	async getSearchResults(data) {
		try {
			let response = await $.get(`${this.REQUEST_URL}`, data)

			return { success: true, response }
		} catch (error) {
			console.log(error)

			return {
				success: false,
				message: 'Something wrong when searching for results.'
			}
		}
	}
}

export default SearchAPI
