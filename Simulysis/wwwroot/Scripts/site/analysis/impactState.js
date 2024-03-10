import TracerState from "../draw/common/tracerState.js";
import { noop } from "../noop.js";

var ImpactState = {
	__proto__: TracerState,
	depthCount: 1,
	onTraceFinishCallback: noop,
	isTracingLeft: false,
	foundBlocksCurrentLevel: [],

	init(modelDraw) {
		super.init(modelDraw)
	},

	resetState(depthCount, isTracingLeft) {
		super.resetState()
		this.depthCount = depthCount
		this.isTracingLeft = isTracingLeft
		this.foundBlocksCurrentLevel = []
	},

	addStack(obj) {
		if (!obj) {
			return;
		}

		// Stop analyze block
		let currentLevel = obj.depthLevel ?? 0

		if (currentLevel >= this.depthCount) {
			return;
		}

		let parentLevel = obj.rootObj ? obj.rootObj.depthLevel : 0

		if (parentLevel >= this.depthCount) {
			return;
		}

		let portType = this.isTracingLeft ? 'Outport' : 'Inport'
		let needTraceQueue = [obj]

		while (needTraceQueue.length > 0) {
			let targetObj = needTraceQueue.pop()

			if (this.checkIfFoundBlocks(targetObj)) {
				continue;
			}

			if (targetObj.depthCount >= this.depthCount) {
				continue;
			}

			var parentSystem = targetObj.getParentSystem()

			if (targetObj.blockType == portType && parentSystem && parentSystem.depthLevel > 0) {
				let port = targetObj.props.Port ?? 1
				let oppositePortListMap = this.isTracingLeft ? parentSystem.outInPortMap : parentSystem.inOutPortMap
				let oppositePortList = oppositePortListMap ? oppositePortListMap[port] : null

				if (oppositePortList) {
					if (!this.checkIfFoundBlocks(parentSystem)) {
						this.foundBlocks.push(parentSystem)
					}

					if (parentSystem.getTraceDeepLevel() == 0 && !super.checkIfFoundBlocksDetails(parentSystem, this.foundBlocksCurrentLevel)) {
						this.foundBlocksCurrentLevel.push(parentSystem);
					}

					for (const oppositePort of oppositePortList) {
						let childrenList = this.isTracingLeft ? (parentSystem.inChildren ? parentSystem.inChildren[oppositePort.port] : null) :
							(parentSystem.outChildren ? parentSystem.outChildren[oppositePort.port] : null)

						if (childrenList) {
							for (var child of childrenList) {
								if (targetObj.getTraceDeepLevel() == 0) {
									child.depthLevel = parentSystem.depthLevel + 1
								}
								child.rootObj = parentSystem

								needTraceQueue.push(...childrenList)
							}
						} else {
							super.addStack(oppositePort.portSystem)
						}
					}
				} else {
					// Need to trace
					super.addStack(targetObj)
				}
			} else if (targetObj.blockType == 'SubSystem' || targetObj.blockType == 'ModelReference') {
				var connectLine = targetObj.lineInSubsys

				if (connectLine) {
					let port = this.isTracingLeft ? connectLine.srcPort : connectLine.dstPort
					let oppositePortListMap = this.isTracingLeft ? targetObj.outInPortMap : targetObj.inOutPortMap
					let oppositePortList = oppositePortListMap ? oppositePortListMap[port] : null

					if (oppositePortList) {
						if (!this.checkIfFoundBlocks(targetObj)) {
							this.foundBlocks.push(targetObj)
						}

						if (targetObj.getTraceDeepLevel() == 0 && !super.checkIfFoundBlocksDetails(targetObj, this.foundBlocksCurrentLevel)) {
							this.foundBlocksCurrentLevel.push(targetObj);
						}

						for (const oppositePort of oppositePortList) {
							let childrenList = this.isTracingLeft ? (targetObj.inChildren ? targetObj.inChildren[oppositePort.port] : null) :
								(targetObj.outChildren ? targetObj.outChildren[oppositePort.port] : null)

							if (!childrenList) {
								super.addStack(oppositePort.portSystem)
							} else {
								for (var child of childrenList) {
									if (targetObj.getTraceDeepLevel() == 0) {
										child.depthLevel = targetObj.depthLevel + 1
									}
									child.rootObj = targetObj

									needTraceQueue.push(...childrenList)
								}
							}
						}
					} else {
						// Need to trace
						super.addStack(targetObj)
					}
				} else {
					// Need to trace
					super.addStack(targetObj)
				}
			} else {
				let childrenFromPortList = this.isTracingLeft ? targetObj.inChildren : targetObj.outChildren

				if (childrenFromPortList) {
					if (!this.checkIfFoundBlocks(targetObj)) {
						this.foundBlocks.push(targetObj)
					}

					if (targetObj.getTraceDeepLevel() == 0 && !this.checkIfFoundBlocksDetails(targetObj, this.foundBlocksCurrentLevel)) {
						this.foundBlocksCurrentLevel.push(targetObj);
					}

					for (const [port, childrenList] of Object.entries(childrenFromPortList)) {
						for (var child of childrenList) {
							child.depthLevel = targetObj.depthLevel + 1
							child.rootObj = targetObj

							needTraceQueue.push(child)
						}
					}
				} else {
					super.addStack(targetObj)
				}
			}
		}
	},
	addToChildrenListV2(rootObj, port, obj, depthLevel) {
		if (obj.getTraceDeepLevel() == 0) {
			obj.depthLevel = depthLevel + 1

			if (!super.checkIfFoundBlocksDetails(obj, this.foundBlocksCurrentLevel)) {
				this.foundBlocksCurrentLevel.push(obj);
			}
		}

		if (!this.isTracingLeft) {
			if (!rootObj.outChildren) {
				rootObj.outChildren = {}
			}

			if (!rootObj.outChildren[port]) {
				rootObj.outChildren[port] = [obj]
			} else {
				if (!this.checkIfFoundBlocksDetails(obj, rootObj.outChildren[port])) {
					rootObj.outChildren[port].push(obj)
				}
			}
		} else {
			if (!rootObj.inChildren) {
				rootObj.inChildren = {}
			}

			if (!rootObj.inChildren[port]) {
				rootObj.inChildren[port] = [obj]
			} else {
				if (!this.checkIfFoundBlocksDetails(obj, rootObj.inChildren[port])) {
					rootObj.inChildren[port].push(obj)
				}
			}
		}
	},
	onNewBlockFound(obj, isLoop) {
		if (isLoop) {
			return;
		}
		
		let rootObj = obj.rootObj

		if (!rootObj) {
			return;
		}

		var portType = this.isTracingLeft ? 'Outport' : 'Inport'
		var oppositePortType = this.isTracingLeft ? 'Inport' : 'Outport'

		if (rootObj.blockType == portType) {
			let containingSystem = rootObj.getParentSystem()

			if (containingSystem) {
				containingSystem.currentTracingRootPort = rootObj
			}
		}
		
		if (obj.type == 'line') {
			obj.rootSystem = {
				port: (this.isTracingLeft ? obj.dstPort : obj.srcPort) ?? 1,
				system: rootObj,
				depthLevel: rootObj.depthLevel ?? 0
			};

			if (obj.getTraceDeepLevel() == 0) {
				obj.depthLevel = obj.rootSystem.depthLevel
			}
		} else if (obj.type == 'branch') {
			let isParentLine = rootObj.type == 'line' || rootObj.type == 'branch'

			if (isParentLine) {
				obj.rootSystem = {
					port: (this.isTracingLeft ? rootObj.dstPort : rootObj.srcPort) ?? 1,
					system: rootObj.rootSystem.system,
					depthLevel: rootObj.rootSystem.depthLevel ?? 0
				};

				if (obj.getTraceDeepLevel() == 0) {
					obj.depthLevel = obj.rootSystem.depthLevel
				}
			} else {
				obj.rootSystem = {
					port: 1,
					system: rootObj,
					depthLevel: rootObj.depthLevel ?? 0
				};

				if (obj.getTraceDeepLevel() == 0) {
					obj.depthLevel = rootObj.depthLevel ?? 0
				}
			}
		} else if (obj.type == 'system') {
			if (obj.blockType == oppositePortType) {
				// Exit port
				let containingSystem = obj.getParentSystem()

				if (containingSystem && containingSystem.currentTracingRootPort) {
					if (!containingSystem.inOutPortMap) {
						containingSystem.inOutPortMap = {}
					}

					if (!containingSystem.outInPortMap) {
						containingSystem.outInPortMap = {}
					}

					let inPort = Number((this.isTracingLeft ? obj.props.Port : containingSystem.currentTracingRootPort.props.Port) ?? 1)
					let outPort = Number((this.isTracingLeft ? containingSystem.currentTracingRootPort.props.Port : obj.props.Port) ?? 1)

					let inSystem = this.isTracingLeft ? obj : containingSystem.currentTracingRootPort
					let outSystem = this.isTracingLeft ? containingSystem.currentTracingRootPort : obj

					if (!this.isTracingLeft) {
						if (!containingSystem.inOutPortMap[inPort]) {
							containingSystem.inOutPortMap[inPort] = [{ port: outPort, portSystem: outSystem }]
						} else {
							if (!containingSystem.inOutPortMap[inPort].find(info => info.port == outPort)) {
								containingSystem.inOutPortMap[inPort].push({ port: outPort, portSystem: outSystem })
							}
						}
					} else {
						if (!containingSystem.outInPortMap[outPort]) {
							containingSystem.outInPortMap[outPort] = [{ port: inPort, portSystem: inSystem }]
						} else {
							if (containingSystem.outInPortMap[outPort].find(info => info.port == inPort)) {
								containingSystem.outInPortMap[outPort].push({ port: inPort, portSystem: inSystem })
							}
						}
					}
				}
			}

			if (!rootObj.rootSystem) {
				if (rootObj.blockType == 'Goto' || rootObj.blockType == 'From') {
					this.addToChildrenListV2(rootObj, 1, obj, rootObj.depthLevel ?? 0)
					obj.rootSystem = rootObj.rootSystem
				} else {
					console.log("WARNING: No valid root system")
				}
			} else {
				this.addToChildrenListV2(rootObj.rootSystem.system, rootObj.rootSystem.port, obj, rootObj.rootSystem.depthLevel)
				obj.rootSystem = rootObj.rootSystem
			}
		} else {
			/** UNREACHABLE **/
		}
	},
	onSystemTraceDone(obj) {

	},
	onTraceFinish() {
		this.onTraceFinishCallback()
	}
}

export default ImpactState