import ImpactState from "./impactState.js";
import SystemUtil from "../utils/system.js";
import Tracer from "../draw/common/tracer.js";
import LineUtil from "../utils/line.js";

var ImpactComputer = {
	systemDraws: {},
	lineDraws: {},
	depthLevel: 0,
	fileContent: {},
	utils: {},
	tracer: {},

    init(systemDraws, lineDraws, fileContent, rootSysId, tracer, utils, depthLevel) {
		this.systemDraws = systemDraws;
		this.lineDraws = lineDraws;
		this.depthLevel = depthLevel;
		this.fileContent = fileContent;
		this.rootSysId = rootSysId;
		this.tracer = tracer;
		this.utils = utils;
	},

    async coreComputation(callGraph, changeSet, digDepth) {
		let coreSet = []
		let i= 0

		for (let unit of callGraph) {
			if (!this.utils.checkIfFoundBlocksDetails(unit, changeSet)) {
				let timeStart = Date.now();
				let neighborSet = await this.getNeighborSet(unit, digDepth);

				//console.log("Time took: " + (Date.now() - timeStart))

				let neighborSetInChangeSetCount = neighborSet.filter(x => this.utils.checkIfFoundBlocksDetails(x, changeSet)).length

                if (neighborSetInChangeSetCount > 1) {
                    coreSet.push(unit);
                }
			}

			i++
        }

		coreSet.push(...changeSet);
        return coreSet;
    },

    async impactSetComputationDetails(callGraph, coreSet) {
        let impactSet = [...coreSet];

		while (true) {
			let impactSetStabled = true;

			for (let unit of callGraph) {
				if (!this.utils.checkIfFoundBlocksDetails(unit, impactSet)) {
					let callerSet = await this.getCallerSet(unit);
					let calleeSet = await this.getCalleeSet(unit);

					if ((callerSet.filter(x => this.utils.checkIfFoundBlocksDetails(x, impactSet)).length > 0) && (calleeSet.filter(x => this.utils.checkIfFoundBlocksDetails(x, impactSet)).length > 0)) {
						impactSet.push(unit);
						impactSetStabled = false;
					}
				}
			}

			if (impactSetStabled) {
				break;
			}
		}

		return impactSet;
    },
	async doTrace(inputSystem, digDepth, reversedDirection, finishCallback) {
		this.utils.resetState(digDepth, reversedDirection);

		var branchDraws = []
		if (reversedDirection) {
			branchDraws = LineUtil.getAllBranch(this.lineDraws, []);
		}

		this.utils.setBlockDepthCount(inputSystem, 0)

		if ((SystemUtil.isRefToFile(inputSystem.blockType, inputSystem.sourceFile) || inputSystem.blockType == 'SubSystem') && inputSystem.sourceFile != "") {
			this.utils.foundBlocks.push(inputSystem);
			this.utils.filterAndInitDraws(this.lineDraws, this.systemDraws, this.lineDraws
				, branchDraws, inputSystem, reversedDirection, this.depthLevel, 0);
		} else {
			this.utils.foundBlocks.push(inputSystem)

			inputSystem.initListObjDraws(this.systemDraws, this.lineDraws, branchDraws);
			inputSystem.setCurrentDeepLevel(this.depthLevel);
			inputSystem.setCurrentTraceLevel(0);
			inputSystem.setTraceDeepLevel(0);
			inputSystem.setFileContent(this.fileContent)
			inputSystem.setRootSysId(this.rootSysId);
			this.utils.addStack(inputSystem);
		}

		var promise = new Promise((resolve, reject) => {
			this.utils.onTraceFinishCallback = () => {
				resolve(this.normalizeFoundList(inputSystem))
			}
		});

		await this.tracer.TrackLineLoop(reversedDirection)
		return promise
	},
	normalizeFoundList(inputSystem) {
		let result = []

		for (let i = 0; i < this.utils.foundBlocksCurrentLevel.length; i++) {
			if (!this.utils.checkIfFoundBlocksDetails(this.utils.foundBlocksCurrentLevel[i], result)) {
				result.push(this.utils.foundBlocksCurrentLevel[i]);
			}
		}

		let shouldKeepGoing = true

		while (shouldKeepGoing) {
			shouldKeepGoing = false

			for (let i = 0; i < result.length; i++) {
				if (this.utils.checkIfFoundBlocksDetails(inputSystem, result)) {
					result.splice(i, 1);
					shouldKeepGoing = true
					break;
				}
			}
		}

		return result
	},
	async getNeighborSet(unit, digDepth) {
		var neighborSet = []
		neighborSet.push(...await this.doTrace(unit, digDepth, false));
		let reversedNeighbor = await this.doTrace(unit, digDepth, true)

		// Most likely impossible but it's safe to check
		reversedNeighbor.forEach((value) => {
			if (!this.utils.checkIfFoundBlocksDetails(value, neighborSet)) {
				neighborSet.push(value)
			}
		})

		return neighborSet;
    },

	async getCallerSet(unit) {
		return await this.doTrace(unit, 1, true);
    },

	async getCalleeSet(unit) {
		return await this.doTrace(unit, 1, false);
    },

	async impactSetComputation(changeSet, digDepth) {
		var changeSetTransformed = changeSet.map(x => this.systemDraws.find(y => y.sid == x.SID)).filter(x => x);
		if (changeSetTransformed.length == 0) {
			return []
		}
		return await this.impactSetComputationDetails(this.systemDraws, await this.coreComputation(this.systemDraws, changeSetTransformed, digDepth));
    }
}

export default ImpactComputer;