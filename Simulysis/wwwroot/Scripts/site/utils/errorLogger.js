var ErrorLogger = {
	methodNotImplemented(methodName) {
		throw new Error(`${methodName}() must be implemented on the prototypal-linked (child) object`)
	}
}

export default ErrorLogger
