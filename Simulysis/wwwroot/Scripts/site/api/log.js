import { BASE_URL } from './config.js'

var LogAPI = {
	REQUEST_URL: `${BASE_URL}/log`,
	async log(message) {
		try {
			let response = await $.post(`${this.REQUEST_URL}`, message)

			return { success: true, response }
		} catch (error) {
			console.log(error)

			return { success: false, message: 'Failed to post log' }
		}
	}
}

export default LogAPI
