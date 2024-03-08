import FileUtil from '../../utils/file.js'
import FileRelationshipsUtil from '../../utils/fileRelationships.js'
import Draw from '../index.js'
import { noop, noopGenerator } from '../../noop.js'



var TreeDraw = {
    /*
     * PROTOTYPAL LINK (PARENT)
     */
    __proto__: Draw,

    /*
     * PROPERTIES
     */
    mainFileId: null,
    mainFile: null,
    containingFolder: null,
    relationshipsObj: null,
    relationshipTree: null,
    drawTreeHierarchy: null,
    maxFileCountPerLevel: 1,
    projectId: -1,

    /*
     * METHODS
     */

    /* ===== OVERRIDE ===== */
    init(ids, relationshipsObj, type, files, mainFileId, clickHandlers, options = {}) {
        const { viewId, drawId, whId, projectId } = ids

        this.projectId = projectId;

        super.init(viewId, drawId, whId, {
            ...options,
            networkProps: { displayIcon: true }
        })

        this.mainFileId = mainFileId
        this.mainFile = this.mainFileId <= 0 ? null : FileUtil.file(files, this.mainFileId)
        this.relationshipsObj = relationshipsObj
        this.viewType = type;
        this.files = files

        if (this.mainFile && FileUtil.isFake(this.mainFile))
            this.mainFile.containingFilePath = FileUtil.getContainingFilePath(files, this.mainFile)

        this.initClickHandlers(clickHandlers)

        this.relationshipTree = this.buildRelationshipTree()
        this.drawTreeHierarchy = d3.hierarchy(this.relationshipTree, d => d.children)

        return this
    },
    initClickHandlers(handlers) {
        var {
            lineClickHandler = noop,
            fileRectClickHandlerGenerator = noopGenerator,
            subSysClickHandlerGenerator = noopGenerator,
            parentSysClickHandler = noop,
            onToggleFileLines = noop,
            onToggleLine = noop,
            onDisplayChange = noop
        } = handlers
        this.lineClickHandler = lineClickHandler
        this.fileRectClickHandlerGenerator = fileRectClickHandlerGenerator
        this.subSysClickHandlerGenerator = subSysClickHandlerGenerator
        this.onToggleFileLines = onToggleFileLines
        this.onToggleLine = onToggleLine
        this.onDisplayChange = onDisplayChange
        this.parentSysClickHandler = parentSysClickHandler
    },
    buildSubRelationshipTree(fileId, fileName, fileHandler, subRelationshipArr) {
        if (subRelationshipArr[fileId] == null) {
            return null;
        }
        var subRelasForThis = FileRelationshipsUtil.sub(subRelationshipArr, fileId)
        if (subRelasForThis) {
            var tree = {
                value: {
                    name: fileName,
                    id: fileId,
                    handler: fileHandler
                },
                children: []
            }

            subRelasForThis.forEach(subRela => {
                const isChild = FileRelationshipsUtil.isChild(subRela, fileId);
                if (!isChild) {
                    return;
                }

                const childIdInRela = FileRelationshipsUtil.getSubFileId(subRela, fileId);
                const childFile = FileUtil.file(this.files, childIdInRela)
                const subFileIsSubSys = subRela.system1

                const subFileHandler = subFileIsSubSys
                    ? this.subSysClickHandlerGenerator(
                        { blockType: 'SubSystem' },
                        { sysId: Number(subRela.system1), relaFakeFileId: childIdInRela },
                        'down'
                    )
                    : this.fileRectClickHandlerGenerator()

                var subTree = this.buildSubRelationshipTree(childIdInRela, childFile.name, subFileHandler, subRelationshipArr);

                if (subTree != null) {
                    tree.children.push(subTree)
                }
            })

            return tree
        }
        return null
    },
    buildRelationshipTree() {
        var { mainRelaObj, subRelaObj, fileCountPerLevelObj } = this.relationshipsObj
        this.maxFileCountPerLevel = Math.max(1, Math.max(...fileCountPerLevelObj[this.viewType]))

        var tree = {
            value: {
                name: this.mainFileId <= 0 ? "Root" : this.mainFile.name, id: this.mainFileId, handler: null
            },
            children: []
        }


        var mainRelaObjIter = mainRelaObj[this.viewType]
        var subRelaObjIter = subRelaObj[this.viewType]

        mainRelaObjIter.forEach(mainRela => {
            const isChild = this.mainFileId <= 0 ? true : FileRelationshipsUtil.isChild(mainRela, this.mainFileId);
            if (!isChild) {
                return;
            }
            const subFileId = FileRelationshipsUtil.getSubFileId(mainRela, this.mainFileId)
            var subFile = FileUtil.file(this.files, subFileId)
            console.log(subFileId);
            const subFileName = subFile.name

            const subFileIsSubSys = mainRela.system1
            const subFileHandler = subFileIsSubSys
                ? this.subSysClickHandlerGenerator(
                    { blockType: 'SubSystem' },
                    { sysId: Number(mainRela.system1), relaFakeFileId: subFileId },
                    'down'
                )
                : this.fileRectClickHandlerGenerator()

            tree.children.push(this.buildSubRelationshipTree(subFileId, subFileName, subFileHandler, subRelaObjIter))
        })

        return tree
    },
    diagonalPath(s, d, rectWidth, rectHeight) {
        const halfRectWidth = rectWidth / 2.0
        const halfRectHeight = rectHeight / 2.0

        if (s.x == d.x || s.y == d.y) {
            // Draw line
            return `M ${s.x + halfRectWidth} ${s.y + halfRectHeight} L ${d.x + halfRectWidth} ${d.y + halfRectHeight}`
        }

        var mpx = (s.x + d.x) / 2.0 + halfRectWidth
        var mpy = (s.y + d.y) / 2.0 + halfRectHeight

        var theta = Math.atan2(d.x - s.x, d.y - s.y) - Math.PI / 2;

        // distance of control point from mid-point of line:
        var offset = 50;

        // location of control point:
        var c1x = mpx + offset * Math.cos(theta);
        var c1y = mpy + offset * Math.sin(theta);

        const path = `M ${s.x + halfRectWidth} ${s.y + halfRectHeight}
        Q ${c1x} ${c1y} ${d.x + halfRectWidth} ${d.y + halfRectHeight}`

        return path;
    },
    onElementMouseOver(e, d) {
        if (d.isHighlighting) return;

        d3.select(e.currentTarget).style('filter', 'drop-shadow(0px 0px 12px #1d5193)')
        d3.select(e.currentTarget).style('fill', '#1d5193')
        d3.select(e.currentTarget).style('stroke', '#1d5193')

        d.isHighlighting = true
    },
    onNodeMouseLeave(e, d) {
        if (!d.isHighlighting) return;

        d3.select(e.currentTarget).style('filter', 'none')
        d3.select(e.currentTarget).style('fill', 'black')
        d3.select(e.currentTarget).style('stroke', '')

        d.isHighlighting = false
    },
    onLinkMouseLeave(e, d) {
        if (!d.isHighlighting) return;

        d3.select(e.currentTarget).style('filter', 'none')
        d3.select(e.currentTarget).style('fill', 'black')
        d3.select(e.currentTarget).style('stroke', 'rgb(0, 0, 0)')

        d.isHighlighting = false
    },
    draw() {
        var { fileCountPerLevelObj } = this.relationshipsObj
        var fileCountPerLevelArr = fileCountPerLevelObj[this.viewType];

        var floorCount = 0
        while (fileCountPerLevelArr[floorCount++] != 0);

        const rectHeight = 40
        const rectWHRatio = 2.5
        const rectWidth = rectHeight * rectWHRatio;

        const rectInnerPadding = rectWidth / 16.0
        const treeWidth = Math.max(this.width, this.maxFileCountPerLevel * rectWidth + Math.max(this.maxFileCountPerLevel - 1, 0) * rectWidth / 16.0);
        const treeHeight = Math.max(this.height, floorCount * rectHeight + Math.max(floorCount - 1, 0) * rectHeight / 4.0);

        var drawTree = d3.tree().size([400, 400]).nodeSize([rectWidth, 20])
        var treeData = drawTree(this.drawTreeHierarchy)

        var nodes = treeData.descendants()
        var links = treeData.descendants().slice(1)

        const idealScale = Math.max(0.5, this.width / treeWidth)

        var firstNodePos = { x: nodes[0].x + (this.width - rectWidth * idealScale) / 2.0, y: nodes[0].y + rectHeight * idealScale / 2.0 }
        this.zoomInPositionRaw(firstNodePos.x, firstNodePos.y, idealScale)

        console.log(firstNodePos.x)

        // set
        //var block = document.getElementById("system7");
        //if (block != null) {
        //    var polygon = block.querySelector('polygon');
        //    if (polygon != null) {
        //        polygon.setAttribute('stroke', 'green');
        //    }
        //}

        nodes.forEach(d => d.y = d.depth * 100)

        var rectSvgs = this.parent.selectAll('g.node')
            .data(nodes)

        var nodeEnter = rectSvgs.enter().append('g')
            .attr('class', d => 'node')
            .attr('id', d => `node${d.data.value.id}`)
            .attr('transform', d => `translate(${d.x},${d.y})`)
            .style('cursor', 'pointer')

            .on('click', (e, d) => {
                var tempURL = `${$('#wwwroot').val()}/Analysis/Index/${this.projectId}/${d.data.value.id}/TreeNode`;
                window.open(tempURL, 'newwindow', 'width:500,height:500')

            })
            .on('mouseover', this.onElementMouseOver.bind(this))
            .on('mouseleave', this.onNodeMouseLeave.bind(this))

        nodeEnter.append('rect')
            .attr('id', d => d.data.value.id)
            .attr('width', rectWidth)
            .attr('height', rectHeight)
            .style('fill', 'rgb(255, 255, 255)')
            .style('stroke', 'rgb(0, 0, 0)')
            .style('stroke-width', '3px')


        nodeEnter.append('svg')
            .attr('width', rectWidth)
            .attr('height', rectHeight)
            .append('text')
            .attr('x', '50%')
            .attr('y', '50%')
            .attr('alignment-baseline', 'middle')
            .attr('text-anchor', 'middle')
            .attr('textLength', rectWidth - rectInnerPadding * 2)
            .html(d => d.data.value.name)

        var linkSvgs = this.parent.selectAll('path.link')
            .data(links)

        var linkEnter = linkSvgs.enter().insert('path', 'g')
            .attr('class', 'link')
            .attr('d', d => this.diagonalPath(d.parent, d, rectWidth, rectHeight))
            .style('stroke-width', '2px')
            .style('stroke', 'rgb(0, 0, 0)')
            .on('mouseover', this.onElementMouseOver.bind(this))
            .on('mouseleave', this.onLinkMouseLeave.bind(this));

        
    }
}
export default TreeDraw
