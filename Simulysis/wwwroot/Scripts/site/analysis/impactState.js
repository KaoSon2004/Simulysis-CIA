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
		// Stop analyze block
		let currentLevel = this.getBlockDepthCount(obj)

		if (currentLevel >= this.depthCount) {
			return;
		}

		let realObject = obj;

		if ((realObject.tracedLeft && this.isTracingLeft) || (realObject.tracedRight && !this.isTracingLeft)) {
			let retrieveChildrenList = [{ block: realObject, level: currentLevel }]

			while (retrieveChildrenList.length > 0) {
				let { block, level } = retrieveChildrenList.pop()
				let children = null

				if (block.tracedLeft && this.isTracingLeft) {
					children = block.leftSideChildren
				} else if (block.tracedRight && !this.isTracingLeft) {
					children = block.rightSideChildren
				} else {
					if (!super.checkIfFoundBlocks(block)) {
						super.addStack(block)
					}
				}
				
				if (children) {
					let blockAddedToQueue = false;

					children.forEach(child => {
						if (child.getTraceDeepLevel() == 0 && !super.checkIfFoundBlocksDetails(child, this.foundBlocksCurrentLevel)) {
							this.foundBlocksCurrentLevel.push(child);
						}

						if (child.blockType != 'SubSystem' && child.blockType != 'ModelReference') {
							if (!super.checkIfFoundBlocks(child)) {
								super.foundBlocks.push(child)
							}
						} else {
							// Should check it again, since it has connection with a subsystem/model ref, need to be sure of where
							// the output is
							if (!blockAddedToQueue) {
								if (!super.checkIfFoundBlocks(block)) {
									super.addStack(block)
								}
								blockAddedToQueue = true
							}
						}
					})

					if (block.getTraceDeepLevel() > 0 || (level + 1 < this.depthCount)) {
						children.forEach(child => {
							if (child.blockType != 'SubSystem' && child.blockType != 'ModelReference') {
								retrieveChildrenList.push({ block: child, level: (block.getTraceDeepLevel() > 0) ? level : level + 1 })
							}
						})
					}
				}
			}
		}
		else {
			super.addStack(obj)
		}
	},
	addToChildrenList(rootObj, obj) {
		if (obj.type != 'system') {
			return;
		}


		let parentBlock = (rootObj.type == 'system') ? rootObj : rootObj.originSystem;
		let childBlock = obj

		if (obj.getTraceDeepLevel() == 0 && rootObj.getTraceDeepLevel() > 0) {
			parentBlock = rootObj.upperOriginSystem
		}

		if (this.isTracingLeft) {
			if (!parentBlock.leftSideChildren) {
				parentBlock.leftSideChildren = [childBlock]
			} else {
				if (!this.checkIfFoundBlocksDetails(childBlock, parentBlock.leftSideChildren))
				{
					parentBlock.leftSideChildren.push(childBlock)
				}
			}

			parentBlock.tracedLeft = true
		} else {
			if (!parentBlock.rightSideChildren) {
				parentBlock.rightSideChildren = [childBlock]
			} else {
				if (!this.checkIfFoundBlocksDetails(childBlock, parentBlock.rightSideChildren)) {
					parentBlock.rightSideChildren.push(childBlock)
				}
			}

			parentBlock.tracedRight = true
		}
	},
	onNewSystemFound(obj, rootObj) {
		if (obj.getTraceDeepLevel() == 0 && !super.checkIfFoundBlocksDetails(obj, this.foundBlocksCurrentLevel) && obj.type == 'system') {
			this.foundBlocksCurrentLevel.push(obj);
		}

 		if (rootObj.systemIn == obj) {
			this.setBlockDepthCount(obj, this.getBlockDepthCount(rootObj))

			if (rootObj.getTraceDeepLevel() == 0) {
				obj.upperOriginSystem = rootObj;
			}
			else {
				obj.upperOriginSystem = rootObj.upperOriginSystem;
			}

			return
		}

		if (obj.type == 'line' || obj.type == 'branch') {
			this.setBlockDepthCount(obj, this.getBlockDepthCount(rootObj))

			// Any children found after analyzing subsystems/model refs should be added to the precdent system instead
			if (rootObj.type == 'system' && rootObj.blockType != 'SubSystem' && rootObj.blockType != 'ModelReference') {
				obj.originSystem = rootObj
			}
			else {
				obj.originSystem = rootObj.originSystem ?? rootObj
			}

			if (obj.getTraceDeepLevel() > 0) {
				obj.upperOriginSystem = rootObj.upperOriginSystem
			}
		} else {
			let isUp = (Array.isArray(rootObj) ? 0 : (rootObj?.getTraceDeepLevel() ?? 0)) > (obj?.getTraceDeepLevel() ?? 0)
			
			if (obj.getTraceDeepLevel() > 0) {
				this.setBlockDepthCount(obj, this.getBlockDepthCount(rootObj))
				obj.upperOriginSystem = rootObj.upperOriginSystem
			} else {
				this.setBlockDepthCount(obj, this.getBlockDepthCount(rootObj) + 1)
			}

			this.addToChildrenList(rootObj, obj)
		}
	},
	setBlockDepthCount(obj, depth) {
		obj.blockDepthCount = depth;
	},
	getBlockDepthCount(obj) {
		return obj.blockDepthCount ?? 0;
	},
	onSystemTraceDone(obj) {

	},
	onTraceFinish() {
		this.onTraceFinishCallback()
	}
}

export default ImpactState