import { noop } from '../../noop.js';
import LineUtil from '../../utils/line.js';
import SystemUtil from '../../utils/system.js';
import Draw from '../index.js';
var TraceSignalTree = {
	/*
	 * PROTOTYPAL LINK (PARENT)
	 */
	__proto__: Draw,
	viewId: null,
	drawId: null,
	whId: null,
	tree: null,
	index: 0,
	scaleY: 30,
	scaleX: 15,
	startX: 15,
	startY: 10,
	radius: 5,
	minLevel: 0,


	modelDraw: {},
	lastClickNode: null,
	parentE: null,
	viewManager: {},
	stackTrace: [],
	reversedDirection: false,
	reDrawedBlocks: [],
	reDrawedLines: [],
	reDrawedBranches: [],

	existingNodes: [],
	existingLines: [],
	init(modelDraw, viewManager) {
		this.viewId = "subView";
		this.drawId = "traceSignalTree";
		this.whId = "subView";




		super.init(this.viewId, this.drawId, this.whId);
		this.parentE = d3.select("#traceSignalTree");
		this.tree = this.parentE.select("g");

		this.resetState();

		this.index = 0;

		this.modelDraw = modelDraw;
		this.lastClickNode = null;
		this.viewManager = viewManager;

		this.minLevel = 0;


	},
	destroy() {
		$(`#${this.drawId}`).parent().empty();
	},


	resetState() {
		this.index = 0;

		this.tree.selectAll("g").remove();
		this.tree.selectAll("line").remove();
		this.tree.selectAll("circle").remove();
		this.tree.selectAll("text").remove();
		this.lastClickNode = null;

		this.reDrawedBlocks = [];
		this.reDrawedLines = [];
		this.reDrawedBranches = [];
		this.minLevel = 0;
		this.existingNodes = [];
		this.existingLines = [];
	},

	appendNode(obj, end = false, redraw = false) {
		if (end == true) {
			var newX = -150;
			var box = this.parentE.attr('viewBox');
			const [x, y, width, height] = box.split(/\s+|,/)
			this.zoomInPositionRaw(0, 0, 1);
			this.parentE.attr("viewBox", `${newX} ${y} ${width} ${height}`)
			return;
		}
		if (obj && !redraw) {
			this.stackTrace.push(obj);
		}

		if (obj?.traceDeepLevel != 0 && !redraw) {
			return { cx: null, cy: null };
		}


		var cx = obj.rootObj ? obj.rootObj.treeCoordinate.cx : this.startX + 0 * this.scaleX;
		
		var cy = this.startY + this.index * this.scaleY;

		if ((obj != null && obj.traced == false) || redraw == true) {
			obj.treeCoordinate = { cx, cy };
		}

		var loop = this.checkIfNeedBlocks(this.reDrawedBlocks, obj)
			|| this.checkIfNeedLines(this.reDrawedLines, obj)
			|| this.checkIfNeedBranches(this.reDrawedBranches, obj)
		obj.loop = loop;
		this.addToRedrawed(obj);



		if (obj.sourceFile != "" && SystemUtil.isRefToFile(obj.blockType, obj.sourceFile)) {
			this.createNode(obj, cx, cy, `${obj.name} (Model Reference)`);
		} else if (obj.blockType == 'SubSystem') {
			this.createNode(obj, cx, cy, `${obj.name} (Sub System)`);
		}
		else {
			this.createNode(obj, cx, cy, `${obj.name ?? obj.stringId}`, loop);
		}


		this.index++;
		this.existingNodes.push({ obj, cx, cy });
		return { cx, cy };
	},
	createNode(obj, cx, cy, text, loop) {
		if (loop) {
			text = text + "(Loop)";
		}
		var nodeEle = this.tree.append("g");
		nodeEle.attr('class', `tree${obj.stringId}_${this.index}`);
		//Redraw when click a node on graph
		nodeEle.on('dblclick', () => {

			if (this.lastClickNode != null) {
				this.lastClickNode.style("fill", "transparent");
			}
			$("[class$='dot-marker']").remove();
			if ((obj.sourceFile != "" && SystemUtil.isRefToFile(obj.blockType, obj.sourceFile)) || obj.blockType == 'SubSystem') {
				$(`#${obj.stringId}`).dblclick()
				this.viewManager.goUpDownCallback()
			}
		})

		nodeEle.style("cursor", "pointer").on("click", () => {
			if (this.lastClickNode != null) {
				this.lastClickNode.style("fill", "transparent");
			}
			const rect = d3.select(`.treerect-${obj.stringId}-false`);
			rect.style("fill", "rgba(123,123,123,0.5)");
			this.lastClickNode = rect;

			$("[class$='dot-marker']").remove();
			this.highlightObjPos(obj);
			obj.displayObjDetail();
		});
		const circleNode = nodeEle.append("circle").attr("cx", cx).attr("cy", cy).attr("r", this.radius).style("fill", "#69b3a2");  //69b3a2
		const textNode = nodeEle.append("text").attr("text-anchor", "end").attr("x", cx - 20).attr("y", cy + this.radius).text(text);
		const width = 20 + textNode.node().getBBox().width;
		const height = 20;
		const className = obj.blockType != 'SubSystem' ? `treerect-${obj.stringId}-${loop}` : `treerect-${obj.stringId}-${loop}`;
		const rectnode = nodeEle.append("rect")
			.attr("x", cx - width - 10)
			.attr("y", cy - this.radius - 5)
			.attr("width", width)
			.attr("height", height)
			.style("fill", "transparent")
			.attr("class", className);

	}
	,


	connect2Node(srcObj, dstObj) {
		if (srcObj && dstObj) {
			const { cx: x1, cy: y1 } = srcObj.treeCoordinate;
			const { cx: x2, cy: y2 } = dstObj.treeCoordinate;
			var indexY1 = (y1 - this.startY) / this.scaleY;
			var indexY2 = (y2 - this.startY) / this.scaleY;
			if (Math.abs(indexY1 - indexY2) <= 1) {
				this.tree.append("line")
					.attr("x1", x1)
					.attr("y1", y1 + this.radius)
					.attr("x2", x2)
					.attr("y2", y2 - this.radius)
					.style("stroke", "gray")
					.style("stroke-width", 2)
					.attr("class", `lineTo${dstObj?.stringId}_${indexY1}-${indexY2}`);
			} else {
				var breakY = (indexY2 - 1) * this.scaleY + this.startY;

				/*
				* check if being drawline overlap existing node
				*/

				let newX1 = x1;

				for (var i = indexY1 + 1; i <= indexY2 - 1; i++) {
					var lineY1 = i * this.scaleY + this.startY
					var newX2 = newX1;
					while (this.checkExistingNode(newX2, lineY1)) {
						newX2 = newX2 + this.scaleX;
					}

					if (i == indexY1 + 1) {
						var curY1 = lineY1 - this.scaleY + this.radius;
						this.tree.append("line")
							.attr("x1", x1)
							.attr("y1", curY1)
							.attr("x2", newX2)
							.attr("y2", lineY1)
							.style("stroke", "gray")
							.style("stroke-width", 2)
						this.existingLines.push({ x1, y1: lineY1 - this.scaleY + this.radius, x2: newX2, y2: lineY1 })
					} else {
						var curY1 = lineY1 - this.scaleY;
						while (this.areCuttingLines({ x1: newX1, y1: lineY1 - this.scaleY, x2: newX2, y2: lineY1 })) {
							newX2 = newX2 + this.scaleX;
						}

						this.tree.append("line")
							.attr("x1", newX1)
							.attr("y1", curY1)
							.attr("x2", newX2)
							.attr("y2", lineY1)
							.style("stroke", "gray")
							.style("stroke-width", 2)
						this.existingLines.push({ x1: newX1, y1: lineY1 - this.scaleY, x2: newX2, y2: lineY1 })
					}

					newX1 = newX2;

				}
				
				const node = this.tree.select(`.tree${dstObj.stringId}_${indexY2}`);
				node.select("circle").attr("cx", newX2);
				node.select("text").attr("text-anchor", "end").attr("x", newX2 - 20);
				

				var widthRect = node.select("rect").node()?.getBoundingClientRect().width;


				node.select("rect").attr("x", newX2 - widthRect - 10);

				const lines = this.tree.select(`.lineTo${dstObj.stringId}`).attr('x2', newX2);

				dstObj.treeCoordinate.cx = newX2;
				this.tree.append("line")
					.attr("x1", newX1)
					.attr("y1", breakY)
					.attr("x2", newX2)
					.attr("y2", y2 - this.radius)
					.style("stroke", "gray")
					.style("stroke-width", 2)
					.attr("class", `.lineTo${dstObj.stringId}`);
				this.existingLines.push({ x1: newX1, y1: breakY, x2: newX2, y2: y2 - this.radius });
        
				const nodeFound = this.existingNodes.find(node => {
					return node.cy == y2;
				})
				if (nodeFound) {
					nodeFound.cx = newX2;
				}

			}
		}

	},

	areCuttingLines(line) {
		var obj = this.existingLines.find(el => el.x2 == line.x2 && el.y2 == line.y2);

		return obj ? true : false;
	},
	drawDotMarker(parentG, x, y) {
		const radius = 3

		parentG
			.append('circle')
			.attr('r', radius)
			.attr('cx', x)
			.attr('cy', y)
			.attr('fill', 'white')
			.attr('stroke', 'black')
			.attr('class', `${this.stringId}dot-marker`)

	},

	highlightObjPos(obj) {

		const parentElement = d3.select('#parentG');


		if (obj.type == 'system') {

			this.drawDotMarker(parentElement, obj.left, obj.top)
			this.drawDotMarker(parentElement, obj.right, obj.top)
			this.drawDotMarker(parentElement, obj.left, obj.bottom)
			this.drawDotMarker(parentElement, obj.right, obj.bottom)
		}
		else if (obj.type == 'line') {


			const line = this.modelDraw.lineDraws.find(line => {
				return obj.id == line.id;
			})
			line.points.forEach(point => this.drawDotMarker(parentElement, point[0], point[1]))
		}
		else if (obj.type == 'branch') {
			const branchDraws = LineUtil.getAllBranch(this.modelDraw.lineDraws, []);
			const branch = branchDraws.find(branch => {
				return obj.id == branch.id;
			});

			branch.points.forEach(point => this.drawDotMarker(parentElement, point[0], point[1]))

		}
		this.getHighlightPos(obj);
	},



	getHighlightPos(obj) {

		const padding = 150;

		const modelDraw = d3.select('#modelDraw');
		var box = modelDraw.attr("viewBox");
		const [modelX, modelY, modelWidth, modelHeight] = box.split(/\s+|,/);

		if (obj.type == 'system') {
			const factor = (+obj.width * +obj.height) / (+modelWidth * +modelHeight);
			let scale = 1;

			if (factor < 1 / 30) {
				scale = 2;
			}
			if (factor < 1 / 250) {
				scale = 3;
			}
			const x = - Number(obj.left) * scale + padding;
			const y = - Number(obj.top) * scale + padding;
			this.viewManager.modelDraw.zoomInPositionRaw(x, y, scale);
			if (obj.blockType == 'Subsystem' && (s.sourceFile != "" && SystemUtil.isRefToFile(s.blockType, s.sourceFile))) {
				return;
			}


		}
		else if (obj.type == 'line') {

			const line = this.modelDraw.lineDraws.find(line => {
				return obj.id == line.id;
			})
			const right = line.dst ? line.dst.left : line.branchDraws[0].points[0][0]
			const length = Math.abs((+right) - (+line.points[0][0]));
			const factor = length / modelWidth;
			let scale = 1;
			if (factor < 1 / 3) {
				scale = 2;
			}
			if (factor < 1 / 10) {
				scale = 3;
			}
			const x = - Number(obj.points[0][0]) * scale + padding;
			const y = - Number(obj.points[0][1]) * scale + padding;
			this.viewManager.modelDraw.zoomInPositionRaw(x, y, scale);

		}
		else if (obj.type == 'branch') {
			const branchDraws = LineUtil.getAllBranch(this.modelDraw.lineDraws, []);
			const branch = branchDraws.find(branch => {
				return obj.id == branch.id;
			});
			const right = branch.dst ? branch.dst.left : branch.branchDraws[0].points[0][0]
			const length = Math.abs((+right) - (+branch.points[0][0]));
			const factor = length / modelWidth;
			let scale = 1;
			if (factor < 1 / 3) {
				scale = 2;
			}
			if (factor < 1 / 10) {
				scale = 3;
			}

			const x = - Number(branch.points[0][0]) * scale + padding;
			const y = - Number(branch.points[0][1]) * scale + padding;

			this.viewManager.modelDraw.zoomInPositionRaw(x, y, scale);


		}
		if (obj.loop) {
			const node = d3.select(`.treerect-${obj.stringId}-false`);
			const cx = node.attr("x");
			const cy = node.attr("y");
			this.zoomInPositionRaw(-cx - 40, -cy + 30, 1);
		}
	},




	reDrawTree(utils) {

		this.resetState();
		var { systemDraws, lineDraws } = this.modelDraw;
		var branchDraws = LineUtil.getAllBranch(this.modelDraw.lineDraws, []);

		for (let i = 0; i < this.stackTrace.length; i++) {

			if (
				this.checkIfNeedBlocks(systemDraws, this.stackTrace[i])
				|| this.checkIfNeedLines(lineDraws, this.stackTrace[i])
				|| this.checkIfNeedBranches(branchDraws, this.stackTrace[i])
			) {

				this.appendNode(this.stackTrace[i], false, true);
				const rootObj = this.stackTrace[i].rootObj;

				this.connect2Node(rootObj, this.stackTrace[i]);


			}

		}
		this.appendNode(null, true, true);


	},
	addToRedrawed(obj) {
		if (obj.type == 'system') {
			this.reDrawedBlocks.push(obj);
		}
		if (obj.type == 'line') {
			this.reDrawedLines.push(obj);
		}
		if (obj.type == 'branch') {
			this.reDrawedBranches.push(obj);
		}
	}
	,
	checkIfNeedBlocks(systemDraws, entity) {
		var ans = false;
		if (entity == undefined || entity.type != 'system') return ans;
		systemDraws.forEach(block => {
			if (
				block != undefined &&
				block.sid == entity.sid &&
				block.id == entity.id &&
				block.name == entity.name &&
				block.props.Position == entity.props.Position
			) {
				ans = true
			}
		})
		return ans;
	},

	checkIfNeedLines(lineDraws, entity) {
		let ans = false
		if (typeof entity == 'undefined' || entity.type != 'line') return ans;
		lineDraws.forEach(line => {
			if (
				line.id == entity.id
				&& JSON.stringify(line.points[0]) == JSON.stringify(entity.points[0])
				&& JSON.stringify(line.points[0]) == JSON.stringify(entity.points[0])
			) {
				ans = true
			}
		})
		return ans
	},
	checkIfNeedBranches(branchDraws, entity) {
		let ans = false
		if (typeof entity == 'undefined' || entity.type != 'branch') return ans;
		branchDraws.forEach(branch => {
			if (
				branch.id == entity.id &&
				branch.props.Dst == entity.props.Dst &&
				branch.props.Points == entity.props.Points
			) {
				ans = true
			}
		})
		return ans;
	},

	checkExistingNode(x, y) {
		for (const node of this.existingNodes) {
			if (x == node.cx && y == node.cy) {
				return true;
			}
		}
		return false;
	}

}

export default TraceSignalTree;