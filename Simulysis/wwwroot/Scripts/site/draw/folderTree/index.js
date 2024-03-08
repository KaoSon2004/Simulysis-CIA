import FileUtil from '../../utils/file.js'
import FileRelationshipsUtil from '../../utils/fileRelationships.js'
import Draw from '../index.js'
import { noop, noopGenerator } from '../../noop.js'




var TreeFolderDraw = {


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
    folderTree: [],

    /*
     * METHODS
     */

    /* ===== OVERRIDE ===== */
    init(ids, relationshipsObj, type, files, mainFileId, options = {}) {
        const { viewId, drawId, whId } = ids

        this.mainFileId = mainFileId
        this.mainFile = this.mainFileId <= 0 ? null : FileUtil.file(files, this.mainFileId)
        this.relationshipsObj = relationshipsObj
        this.viewType = type;
        this.files = files

        if (this.mainFile && FileUtil.isFake(this.mainFile))
            this.mainFile.containingFilePath = FileUtil.getContainingFilePath(files, this.mainFile)


        this.folderTree.push(this.buildRelationshipTree())
        console.log(this.folderTree)

        return this
    },

    buildSubRelationshipTree(fileId, fileName, subRelationshipArr) {
        if (subRelationshipArr[fileId] == null) {
            return null;
        }
        var subRelasForThis = FileRelationshipsUtil.sub(subRelationshipArr, fileId)
        if (subRelasForThis) {
            var tree = {

                text: fileName,
                id: "folder" + fileId,
                children: [],
                

            }

            subRelasForThis.forEach(subRela => {
                const isChild = FileRelationshipsUtil.isChild(subRela, fileId);
                if (!isChild) {
                    return;
                }

                const childIdInRela = FileRelationshipsUtil.getSubFileId(subRela, fileId);
                const childFile = FileUtil.file(this.files, childIdInRela)
                const subFileIsSubSys = subRela.system1



                var subTree = this.buildSubRelationshipTree(childIdInRela, childFile.name, subRelationshipArr);

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


            text: this.mainFileId <= 0 ? "Root" : this.mainFile.name, id: "folder" + this.mainFileId, state: {
                opened: false  // is the node open
            },
            children: [],
            

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


            tree.children.push(this.buildSubRelationshipTree(subFileId, subFileName, subRelaObjIter))
        })

        return tree
    },


    draw() {
        console.log(100)
        var folder_jsondata = this.folderTree;
        console.log(folder_jsondata)

        $('#tree-folder').jstree({
            'core': {
                'data': folder_jsondata,

            },


        });
    }


}
export default TreeFolderDraw