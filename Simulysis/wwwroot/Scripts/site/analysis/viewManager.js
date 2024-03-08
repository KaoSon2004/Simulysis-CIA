import FilesAPI from '../api/files.js'
import FileContentsAPI from '../api/fileContents.js'
import FileRelationshipsAPI from '../api/fileRelationships.js'
import LogAPI from '../api/log.js'
import NetworkDraw from '../draw/network/index.js'
import TreeDraw from '../draw/tree/index.js'
import TreeFolderDraw from '../draw/folderTree/index.js'
import AnalysisModelDraw from './analysisModelDraw.js'
import SystemUtil from '../utils/system.js'
import FileUtil from '../utils/file.js'
import SystemLevelUtil from '../utils/systemLevel.js'
import PopUpUtils from '../utils/popUp.js'
import {noop} from '../noop.js'
import ImpactComputer from './impactComputer.js'
import Tracer from '../draw/common/tracer.js'
import ImpactState from './impactState.js'

var ViewManager = {
    /*
     * PROPERTIES
     */
    mainViewId: 'mainView',
    subViewId: 'subView',
    networkDrawId: 'networkDraw',
    modelDrawId: 'modelDraw',
    initialLevel: -1,
    currentLevel: -1,
    rootSysId: -1,
    allLevelContents: {},
    subsysRelationships: {},
    projectId: -1,
    originalFileId: -1,
    currentFileId: -1,
    currentSubsysFileId: -1,
    viewType: null,
    swapped: false,
    fullNet: false,
    networkDraw: null,
    modelDraw: null,
    treeDraw: null,
    treeFolderDraw: null,
    files: null,
    isSwitchingLevel: false,
    isNavigating: false,
    isSearching: false,
    hasNetworkView: true,
    showTree: true,
    modelViewPopUp: true,
    goUpDownCallback: noop, // custom callback to run after go up/down
    tempSideView: null,
    numClick: 0,
    levelClickContents: {},
    isAnalysisApplied: false,
    isAddedApplied: false,
    isChangedApplied: false,
    isDeletedApplied: false,
    tracer: {},
    utils: {},

    /*
     * METHODS
     */
    async init(options = {}) {

        // start loading
        this.startLoadingViews()

        this.initOptions(options)
        const rootSysId = Number($('#rootSysId').val() ?? 0)
        await this.updateContent()
        this.createModelDraw(rootSysId)

        if (this.hasNetworkView) {
            if (rootSysId > 0) {
                let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
                    system => system.id == rootSysId
                )

                this.currentSubsysFileId = subsys.fK_FakeProjectFileId
                let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

                if (this.showTree) {
                    //this.createTreeDraw({ explicitRela })
                    //this.createTreeFolderDraw({ explicitRela })
                } else {
                    this.createNetworkDraw({ explicitRela })
                    this.createTreeFolderDraw({ explicitRela })
                }
            } else {
                if (this.showTree) {
                    this.createTreeDraw()
                    this.createTreeFolderDraw()
                } else {
                    this.createNetworkDraw()
                    this.createTreeFolderDraw()
                }
            }

            this.updateToggleDrawButtonText()
        }

        this.disallowUserSelect()
        if (this.hasNetworkView) {
            this.addEventHandlers();
            this.adjustViewToMatchSwap()
            this.allowSearch()
        }
        this.addAllowSelectEvent()
        this.initChangeHandler()
        // stop loading
        this.stopLoadingViews()

        this.numClick = 0;
        $("#numClick").val(this.numClick);
        this.levelClickContents = {};


        this.utils = Object.create(ImpactState);
        this.utils.init(this.modelDraw)

        this.tracer = Object.create(Tracer);
        this.tracer.init(
            this.utils, this.modelDraw
            , this.traceSignalTree, this.levelClickContents
        );
        this.createSelect();
        

        this.updateCompareChangeProjectFileName();

    },
    createSelect() {
        var myParent = document.getElementById("impactedDigDepth");

        // Create select only in file
        if (!myParent) {
            return;
        }

        //Create array of options to be added
        var array = ["Depth: 2", "Depth: 3", "Depth: 4", "Depth: 5", "Depth: 6"];
        var array2 = ["2", "3", "4", "5", "6"];

        //Create and append select list
        var selectList = document.createElement("select");
        selectList.id = "impactedDig";

        myParent.appendChild(selectList);

        //Create and append the options
        for (var i = 0; i < array.length; i++) {
            var option = document.createElement("option");
            option.value = array2[i];
            option.text = array[i];
            selectList.appendChild(option);
        }
        //selectList.onchange = async () => { await showImpactOnTable(selectList.value); }

        $('#file-impact-analyse-btn').click(async () => {
            await showImpactOnTable(selectList.value);
        })
        
        var showImpactOnTable = async (depth) => {
            var impactedData = await this.findImpactsInCurrentFile(depth).then(data => {
                console.log(data); 
                this.insertImpactedData(data);
                
            });
            

        }
    },
    insertImpactedData(data) {
        var paras = document.getElementsByClassName('impacted');

        while (paras[0]) {
            paras[0].parentNode.removeChild(paras[0]);
        };
        data.forEach(x => {
            var tbodyRef = document.getElementById('changes-list-table').getElementsByTagName('tbody')[0];

            // Insert a row at the end of table
            var newRow = tbodyRef.insertRow();
            newRow.classList.add("impacted")
            newRow.addEventListener('mouseover', () => { newRow.classList.add("highlighted") })
            newRow.addEventListener('mouseout', () => { newRow.classList.remove("highlighted") });
            newRow.addEventListener('click', (e) => this.impactedBlockHighlightSingle(e.currentTarget))
            newRow.style.fontSize = "10px"

            // Insert a cell at the end of the row
            var newCell = newRow.insertCell();
            var newCell1 = newRow.insertCell();
            var newCell2 = newRow.insertCell();
            var newCell3 = newRow.insertCell();
            var newCell4 = newRow.insertCell();
            var newCell5 = newRow.insertCell();

            // Append a text node to the cell
            var newText = document.createTextNode(x.id);
            var newText1 = document.createTextNode(x.name);
            var newText2 = document.createTextNode(x.type);
            var newText4 = document.createTextNode("0");
            var newText5 = document.createTextNode(x.stringId);
            var iconElement = document.createElement('i');
            //var newText3 = document.createTextNode(x.type);

            // Add a class for styling (optional)
            iconElement.classList.add('fas');
            iconElement.classList.add("fa-eye-slash")


            //var newText3 = document.createTextNode(x.stringId);
            newCell.appendChild(newText);
            newCell.id = "id"
            newCell1.appendChild(newText1);
            newCell1.id = "name"
            newCell2.appendChild(newText2);
            newCell4.appendChild(newText4)
            newCell5.appendChild(newText5)


            newCell3.appendChild(iconElement);
            newCell3.id = "visibility"
            newCell4.id = "visibilityValue"
            newCell4.style.visibility = "collapse"
            newCell5.id = "projectFileId"
            newCell5.style.visibility = "collapse"


        })
        if ($('#file-impact-analyse-btn').text() === "Show Impact") { this.setChangesVisiblityImpl('impacted', 'cyan', false, false); }
        else {
            this.setChangesVisiblityImpl('impacted', 'cyan', true, false);
        }
        

    },
    initOptions({ hasNetworkView = true, modelViewPopUp = true, goUpDownCallback = noop, }) {
        this.initialLevel = SystemLevelUtil.mapNameToLevel($('#systemLevelText').text())
        this.currentLevel = this.initialLevel
        this.originalFileId = Number($('#fileId').val())
        this.currentFileId = this.originalFileId
        this.projectId = Number($('#projectId').val())
        this.viewType = $('#viewType').val()
        this.swapped = $('#swapView').val() == 'true'
        this.fullNet = $('#fullNet').val() == 'true'
        this.showTree = $('#displayTreeView').val() == 'true'

        this.isAnalysisApplied = false;
        this.hasNetworkView = hasNetworkView
        this.modelViewPopUp = modelViewPopUp
        this.goUpDownCallback = goUpDownCallback
    },
    createNetworkDraw({ explicitRela, funcGrObj = {} } = {}) {
        // close the dropdown if currently opened
        this.networkDraw?.destroyDisplayOptDropdown()

        // get states for next draw
        var displayOptions = this.networkDraw?.getDisplayOptions()
        const openPopUp = this.networkDraw?.destroy() ?? false
        // const isFunctionLevelOrLower = this.currentLevel >= 2

        var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
        if (explicitRela) {
            relationshipsObj = explicitRela
        }

        this.networkDraw = Object.create(NetworkDraw).init(
            {
                viewId: this.subViewId,
                drawId: this.networkDrawId,
                whId: this.subViewId
            },
            relationshipsObj,
            `${this.viewType}Relationships`,
            this.files,
            explicitRela ? this.currentSubsysFileId : this.currentFileId,
            {
                fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
                subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
                parentSysClickHandler: this.goUpLevel,
                lineClickHandler: this.triggerSignalSearch.bind(this),
                onToggleFileLines: this.updateFullNetBtnText.bind(this),
                onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
            },
            {
                ...displayOptions,
                // sub systems props
                // showSubSystemAsChild: isFunctionLevelOrLower,

                // popup props
                openPopUp,
                allowPopUp: true,
                popUpProps: {
                    w: 1000,
                    h: 800,
                    url:
                        window.location.href
                            .split('?')[0]
                            .replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
                            .replace(new RegExp('\\bShow\\b'), 'NetworkExtend') +
                        // `?viewType=${this.viewType}&showSubSystemAsChild=${isFunctionLevelOrLower}&rootSysId=${this.rootSysId}` +
                        `?viewType=${this.viewType}&subSysFileId=${this.currentSubsysFileId}`
                },

                // function group props
                ...funcGrObj
            }
        )
        this.networkDraw.draw()

        this.adjustViewToMatchFullNet()

        LogAPI.log(this.networkDraw.globalDrawConst)
    },
    createTreeDraw({ explicitRela } = {}) {
        this.treeDraw?.destroy()

        var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
        if (explicitRela) {
            relationshipsObj = explicitRela
        }

        this.treeDraw = Object.create(TreeDraw).init(
            {
                viewId: this.subViewId,
                drawId: this.networkDrawId,
                whId: this.subViewId,
                projectId: this.projectId
            },
            relationshipsObj,
            `${this.viewType}Relationships`,
            this.files,
            explicitRela ? this.currentSubsysFileId : this.currentFileId,
            {
                fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
                subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
                parentSysClickHandler: this.goUpLevel,
                lineClickHandler: this.triggerSignalSearch.bind(this),
                onToggleFileLines: this.updateFullNetBtnText.bind(this),
                onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
            },
            {
                allowPopUp: false
            }
        )
        this.treeDraw.draw()
    },
    createTreeFolderDraw({ explicitRela } = {}) {

        var { relationshipsObj } = this.allLevelContents[`level${this.currentLevel}`]
        if (explicitRela) {
            relationshipsObj = explicitRela
        }

        this.treeFolderDraw = Object.create(TreeFolderDraw).init(
            {
                viewId: this.subViewId,
                drawId: this.networkDrawId,
                whId: this.subViewId
            },
            relationshipsObj,
            `${this.viewType}Relationships`,
            this.files,
            explicitRela ? this.currentSubsysFileId : this.currentFileId,
            {
                fileRectClickHandlerGenerator: this.navigateToNewFileGenerator.bind(this),
                subSysClickHandlerGenerator: this.getSystemClickHandler.bind(this),
                parentSysClickHandler: this.goUpLevel,
                lineClickHandler: this.triggerSignalSearch.bind(this),
                onToggleFileLines: this.updateFullNetBtnText.bind(this),
                onDisplayChange: this.adjustViewToMatchFullNet.bind(this)
            },
            {
                allowPopUp: false
            }
        )
        this.treeFolderDraw.draw()
    },
    decapital(inputObject) {
        const decapitalizedObject = {};
        for (const key in inputObject) {
            if (inputObject.hasOwnProperty(key)) {
                const decapitalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
                decapitalizedObject[decapitalizedKey] = inputObject[key];
            }
        }
        return decapitalizedObject;
    },
    createModelDraw(rootSysId) {
        if (this.currentFileId == 0) {
            return;
        }

        this.rootSysId = rootSysId
        if (rootSysId == 0 && this.currentLevel == this.initialLevel) {
            $('#upLevelBtn').addClass('toolbar-btn--disabled')
            $('#upLevelBtn').off('click')
        }
        this.modelDraw && this.modelDraw.destroy()
        var popUpProps = {
            w: screen.width,
            h: screen.height,
            url: `${window.location.href
                .replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
                .replace(new RegExp('\\bShow\\b'), 'Trackline')}?rootSysId=${rootSysId}
					`
        }

        this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.
            push(this.decapital(deletedSystemFilesDTO[3]));


        this.modelDraw = Object.create(AnalysisModelDraw).init(
            {
                viewId: this.mainViewId,
                drawId: this.modelDrawId,
                whId: this.mainViewId
            },
            rootSysId,
            this.allLevelContents[`level${this.currentLevel}`].fileContent,
            this.getSystemClickHandler.bind(this),
            {
                additionalDrawListeners: [this.addGoUpLevel.bind(this)],
                allowPopUp: this.modelViewPopUp,
                popUpProps: popUpProps,
            }
        )

        let removedSystemsFiltered = []

        for (let i = 0; i < deletedSystemFilesDTO.length; i++) {
            var FK_projectFileId = deletedSystemFilesDTO[i].FK_NewVersionProjectFileID;
            if (deletedSystemFilesDTO[i].Name != "" && FK_projectFileId == this.currentFileId) {
                removedSystemsFiltered.push(this.decapital(deletedSystemFilesDTO[i]));
            }
        }

        this.modelDraw.initRemovedSystems(removedSystemsFiltered)
        this.modelDraw.draw()

        if (this.hasNetworkView) {
            this.modelDraw.initDragDropEvent();
        }

        // Hide all removed by default
        this.modelDraw.drawEntities.removedSystems.forEach((system) => {
            $(`#${system.stringId}`).parent().addClass('removed-change-invisible')
        })

    },
    async updateContent(fileName, blockType, sourceBlock, sysId, isFromSysRef = false) {
        // isFromSysRef = true when the content is updated through clicking on a subsystem on network view
        // which is from a different file
        var { fileContent, relationshipsObj, files } = await this.fetchData(fileName)
        const minSystemId = isFromSysRef
            ? sysId
            : blockType
                ? SystemUtil.isModelReference(blockType)
                    ? 0
                    : SystemUtil.getReferencedSystemId(sourceBlock, fileContent.systems)
                : 0

        var oldFileContent = this.allLevelContents[`level${this.currentLevel - 1}`]?.fileContent
        const systemContainsRef = isFromSysRef
            ? this.rootSysId
            : blockType
                ? SystemUtil.getHigherSystemId(oldFileContent.systems, sysId)[0]
                : -1
        this.allLevelContents[`level${this.currentLevel}`] = {
            ...this.allLevelContents[`level${this.currentLevel}`],
            fileContent,
            relationshipsObj,
            minSystemId,
            systemContainsRef
        }
        this.files = files
    },
    saveLevelFuncGr(funcGrObj) {
        this.allLevelContents[`level${this.currentLevel}`] = {
            ...this.allLevelContents[`level${this.currentLevel}`],
            funcGrObj
        }
    },
    updateLevel(level) {
        this.currentLevel = level
        $('#systemLevelText').text(SystemLevelUtil.mapLevelToName(this.currentLevel))
    },
    async fetchData(fileName) {
        var { response: fileContent } = fileName
            ? await FileContentsAPI.getFileContentByName({ projId: this.projectId, fileName })
            : await FileContentsAPI.getFileContentById(this.currentFileId)

        const fileId = fileName ? fileContent.fileId : this.currentFileId
        var { response: relationshipsObj } = await FileRelationshipsAPI.getFileRelationships(this.projectId, fileId)

        var files = this.files ?? (await FilesAPI.getFilesInProject(this.projectId)).response

        return { fileContent, relationshipsObj, files }
    },
    async getSubsysRelationships(fakeProjectFileId) {
        if (!this.subsysRelationships[fakeProjectFileId]) {
            this.subsysRelationships[fakeProjectFileId] = await FileRelationshipsAPI.getFileRelationships(
                this.projectId,
                fakeProjectFileId
            )
        }
        return this.subsysRelationships[fakeProjectFileId]
    },
    addEventHandlers() {
        const self = this;

        $('#switchViewBtn').click(this.swapView.bind(this))
        $('#viewNetworkBtn').click(this.toggleNetwork.bind(this))
        $('#viewType').change(this.changeNetworkViewType.bind(this))
        $('#popUpTrackline').click(this.popUpTrackline.bind(this))
        $('#toggleTreeBtn').click(this.toggleSideView.bind(this))
        $('#analyseBtn').click(this.analyse.bind(this))

        //$('#addedTag').click(this.addedBlockHighlight.bind(this))
        //$("#changedTag").click(this.changedBlockHighlight.bind(this))
        //$("#deletedTag").click(this.deletedBlockHighlight.bind(this))

        if (document.getElementById('treenode')) {

            console.log("da tim thay")
            $('.added').click((e) => this.addedBlockHighlightSingle(e.currentTarget))
            $('.changed').click((e) => this.changedBlockHighlightSingle(e.currentTarget))
            $('.deleted').click((e) => this.deletedBlockHighlightSingle.bind(this, e.currentTarget)())
            /* $('.impacted').click((e) => this.impactedBlockHighlightSingle(e.currentTarget)) */// this will not working because I initialize .impacted when clicking button after treenode finished built


            $('#displayAllChanges').click((e) => this.setChangesVisibility(true))
            $('#hideAllChanges').click((e) => this.setChangesVisibility(false))
        }
        else {
            console.log("khong tim thay")
        }



        $('#addedItems span[id]').click(function () {
            // Get information in element id.
            var id = $(this).attr('id');
            var nameIdParts = id.split(' ');

            var itemId = nameIdParts[0];
            var itemName = nameIdParts.slice(1, nameIdParts.length - 1).join(' ');
            var itemProjectId = nameIdParts[nameIdParts.length - 1];

            // Toggling the highlighting state of the list element and its block in the model view.
            if (!$(this).hasClass("clicked")) {
                $(this).parent().find("*").addClass("bg-success");
                $(this).parent().find("*").addClass("text-white");
                $(this).addClass("clicked");
                console.log("click: " + itemId + " " + itemName + " " + itemProjectId)
                self.blockLocate(itemId, itemName, itemProjectId, false);
            } else {
                $(this).parent().find("*").removeClass("bg-success");
                $(this).parent().find("*").removeClass("text-white");
                $(this).removeClass("clicked");
                self.blockLocate(itemId, itemName, itemProjectId, false, true);
            }

        });

        $('#changedItems span[id]').click(function () {
            // Get information in element id.
            var id = $(this).attr('id');
            var nameIdParts = id.split(' ');

            var itemId = nameIdParts[0];
            var itemName = nameIdParts.slice(1, nameIdParts.length - 1).join(' ');
            var itemProjectId = nameIdParts[nameIdParts.length - 1];

            // Toggling the highlighting state of the list element and its block in the model view.
            if (!$(this).hasClass("clicked")) {
                $(this).parent().find("*").addClass("bg-warning");
                $(this).parent().find("*").addClass("text-black");
                $(this).addClass("clicked");
                console.log("click: " + itemId + " " + itemName + " " + itemProjectId)
                self.blockLocate(itemId, itemName, itemProjectId, false);
            } else {
                $(this).parent().find("*").removeClass("bg-warning");
                $(this).parent().find("*").removeClass("text-black");
                $(this).removeClass("clicked");
                self.blockLocate(itemId, itemName, itemProjectId, false, true);
            }
        });

        $('#deletedItems span[id]').click(function () {
            // If user clicks on span, draw the deleted buttons and turn on deleted
            if (!self.isDeletedApplied) {
                self.drawDeletedBlocks();
                self.isDeletedApplied = true;

            }
            $("#deletedTag").css("background-color", "rgba(255, 0, 0, 1)");

            // Get information in element id
            var id = $(this).attr('id');
            var nameIdParts = id.split(' ');
            var itemId = nameIdParts[0];
            var itemName = nameIdParts.slice(1, nameIdParts.length - 1).join(' ');
            var itemProjectId = nameIdParts[nameIdParts.length - 1];

            // Toggling the highlighting state of the list element and its block in the model view.
            if (!$(this).hasClass("clicked")) {
                $(this).parent().find("*").addClass("bg-danger");
                $(this).parent().find("*").addClass("text-white");
                $(this).addClass("clicked");
                self.blockLocate(itemId, itemName, itemProjectId, true);
            } else {
                $(this).parent().find("*").removeClass("bg-danger");
                $(this).parent().find("*").removeClass("text-white");
                $(this).removeClass("clicked");
                self.blockLocate(itemId, itemName, itemProjectId, true, true);
            }
        });
    },
    blockLocate(itemId, itemName, itemProjectId, isDelete, willToggle = false) {
        var block;

        // Query the block in model view (case: added & changed vs deleted)
        if (isDelete) {
            var textBlocks = document.getElementsByTagName("text");
            var textBlock;
            for (let i = 0; i < textBlocks.length; i++) {
                if (textBlocks[i].textContent === itemName) {
                    textBlock = textBlocks[i];
                }
            }
            block = textBlock.parentNode.parentNode;
        } else {
            block = document.getElementById(`system${itemId}`);
        }

        // Check if the 'block' is null and handle the exception
        if (block == null) {
            return;
        }

        // Ensuring that the item is associated with the current file
        if (itemProjectId != this.currentFileId) {
            return;
        }

        var polygon = block.querySelector('polygon');
        var rect = block.querySelector('rect');
        var svg = block.querySelector("svg");

        // Toggle function for highlighting or reverting the highlighting of a block.
        if (!willToggle) {
            if (svg) {
                svg.setAttribute('filter', 'drop-shadow(0px 0px 10px #1d5193)');
            }

            if (polygon != null) {
                polygon.setAttribute('filter', 'drop-shadow(0px 0px 10px #1d5193)');
                //polygon.setAttribute('fill', '#BBCADE')
                polygon.setAttribute('stroke', '#3405f2');
                polygon.setAttribute('stroke-width', '3');
            }
            if (rect != null) {
                rect.setAttribute('filter', 'drop-shadow(0px 0px 10px #1d5193)');
                //rect.setAttribute('fill', '#BBCADE')
                rect.setAttribute('stroke', '#3405f2');
                rect.setAttribute('stroke-width', '3');
            }
        }
        else {
            if (svg) {
                svg.setAttribute('filter', '');
            }

            if (polygon != null) {
                polygon.setAttribute('filter', '');
                //polygon.setAttribute('fill', '#BBCADE')
                polygon.setAttribute('stroke', 'black');
                polygon.setAttribute('stroke-width', '1');
            }
            if (rect != null) {
                rect.setAttribute('filter', '');
                //rect.setAttribute('fill', '#BBCADE')
                rect.setAttribute('stroke', 'black');
                rect.setAttribute('strokeWidth', '1');
            }
        }
    },

    analyse() {
        // If < 3 tags are on, analysis would be applied
        // If 3 tags are on, analysis would be unapplied
        if (this.isAddedApplied && this.isDeletedApplied && this.isChangedApplied) {
            this.isAnalysisApplied = false;
        } else {
            this.isAnalysisApplied = true;
        }

        this.addedBlockHighlight(true);
        this.changedBlockHighlight(true);
        this.deletedBlockHighlight(true);

    },
    // Highlight added, changed, deleted
    highlightBlock(block, name, color, isApplied, isDeleted = false) {
        if (!isDeleted) {
            var text = block.parentNode.querySelector("g:nth-child(2) > text");
            if (!text || text.innerHTML != name) {
                return;
            }
        }
        else {
            if (isApplied) {
                $(block).parent().removeClass('removed-change-invisible')
            }
            else {
                $(block).parent().addClass('removed-change-invisible')
            }
        }

        var polygon = block.querySelector('polygon');
        var rect = block.querySelector('rect');
        if (polygon != null) {
            if (isApplied) {
                polygon.setAttribute('fill', color);
                polygon.setAttribute('stroke-width', 3);
            } else {
                polygon.setAttribute('fill', 'white');
                polygon.setAttribute('stroke-width', 1);
            }
        }
        if (rect != null) {
            if (isApplied) {
                rect.setAttribute('fill', color);
                rect.setAttribute('stroke-width', 3);
            } else {
                rect.setAttribute('fill', 'white');
                rect.setAttribute('stroke-width', 1);

            }
        }
    },
    addedBlockHighlightSingle(element) {
        let visbilityElement = $('#visibility', element)
        let visbilityValueElement = $('#visibilityValue', element)
        let nameElement = $('#name', element)
        let idElement = $('#id', element)
        let toTurnOn = false;

        if (visbilityValueElement.html() == 0) {
            visbilityElement.html('<i class="fas fa-eye"></i>')
            visbilityValueElement.html(1)

            toTurnOn = true
        }
        else {
            visbilityElement.html('<i class="fas fa-eye-slash"></i>')
            visbilityValueElement.html(0)
        }

        let id = idElement.html()

        var block = document.getElementById(`system${id}`);
        if (block == null) {
            //console.log(`Cannot found system${id}`);
            return;
        }

        this.highlightBlock(block, nameElement.html(), "green", toTurnOn);
    },
    addedBlockHighlight(analyseClicked) {
        //Solve for addedTag click event and analyse event:
        if (analyseClicked === true) {
            this.isAddedApplied = this.isAnalysisApplied ? true : false
        } else {
            this.isAddedApplied = !this.isAddedApplied;
        }

        if (this.isAddedApplied) {
            $("#addedTag").css("background-color", "rgba(0, 255, 0, 1)");
        } else {
            $("#addedTag").css("background-color", "rgba(0, 255, 0, 0.2)");
        }

        for (let i = 0; i < addedSystemFilesDTO.length; i++) {
            var id = addedSystemFilesDTO[i].Id;
            var blockType = addedSystemFilesDTO[i].BlockType;
            var name = addedSystemFilesDTO[i].Name;
            var FK_projectFileId = addedSystemFilesDTO[i].FK_ProjectFileId;

            if (name == "" || FK_projectFileId != this.currentFileId) {
                continue;
            }
            else {
                var block = document.getElementById(`system${id}`);
                if (block == null) {
                    //console.log(`Cannot found system${id}`);
                    continue;
                }

                this.highlightBlock(block, name, "green", this.isAddedApplied);
            }
        }
    },
    changedBlockHighlight(analyseClicked) {
        if (analyseClicked === true) {
            this.isChangedApplied = this.isAnalysisApplied ? true : false
        } else {
            this.isChangedApplied = !this.isChangedApplied;
        }


        if (this.isChangedApplied) {
            $("#changedTag").css("background-color", "rgba(255, 165, 0, 1)");
        } else {
            $("#changedTag").css("background-color", "rgba(255, 165, 0, 0.2)");
        }

        for (let i = 0; i < changedSystemFilesDTO.length; i++) {
            var id = changedSystemFilesDTO[i].Id;
            var blockType = changedSystemFilesDTO[i].BlockType;
            var name = changedSystemFilesDTO[i].Name;
            var FK_projectFileId = changedSystemFilesDTO[i].FK_ProjectFileId;

            if (name == "" || FK_projectFileId != this.currentFileId) {
                continue;
            }
            else {
                var block = document.getElementById(`system${id}`);

                // Block is not found
                if (block == null) {
                    continue;
                }

                this.highlightBlock(block, name, "orange", this.isChangedApplied);
            }
        }
    },
    changedBlockHighlightSingle(element) {
        console.log("click");
        let visbilityElement = $('#visibility', element)
        let visbilityValueElement = $('#visibilityValue', element)
        let nameElement = $('#name', element)
        let idElement = $('#id', element)
        let toTurnOn = false;

        if (visbilityValueElement.html() == 0) {
            console.log("tada")
            visbilityElement.html('<i class="fas fa-eye"></i>')
            visbilityValueElement.html(1)

            toTurnOn = true
        }
        else {
            visbilityElement.html('<i class="fas fa-eye-slash"></i>')
            visbilityValueElement.html(0)
        }

        let id = idElement.html()

        var block = document.getElementById(`system${id}`);
        if (block == null) {
            //console.log(`Cannot found system${id}`);
            return;
        }

        this.highlightBlock(block, nameElement.html(), "orange", toTurnOn);
    },
    deletedBlockHighlight(analyseClicked) {
        if (analyseClicked === true) {
            if (this.isDeletedApplied && this.isAnalysisApplied) {
                return;
            } else {
                this.isDeletedApplied = this.isAnalysisApplied ? true : false
            }
        } else {
            this.isDeletedApplied = !this.isDeletedApplied;
        }

        if (this.isDeletedApplied) {
            $("#deletedTag").css("background-color", "rgba(255, 0, 0, 1)");
        } else {
            $("#deletedTag").css("background-color", "rgba(255, 0, 0, 0.2)");
        }

        // Redraw the deleted blocks
        if (this.isDeletedApplied) {
            this.drawDeletedBlocks();
        } else {
            console.log(deletedSystemFilesDTO)

            for (let i = 0; i < deletedSystemFilesDTO.length; i++) {
                var FK_projectFileId = deletedSystemFilesDTO[i].FK_NewVersionProjectFileID;
                if (deletedSystemFilesDTO[i].Name != "" && FK_projectFileId == this.currentFileId) {
                    this.modelDraw.levelContent.systems.pop();
                }
            }
        }

        for (let i = 0; i < deletedSystemFilesDTO.length; i++) {
            var id = deletedSystemFilesDTO[i].Id;
            var blockType = deletedSystemFilesDTO[i].BlockType;
            var name = deletedSystemFilesDTO[i].Name;
            var FK_projectFileId = deletedSystemFilesDTO[i].FK_NewVersionProjectFileID;

            if (name == "" || FK_projectFileId != this.currentFileId) {
                continue;
            }
            else {
                var textBlocks = document.getElementsByTagName("text");
                var textBlock;
                for (let i = 0; i < textBlocks.length; i++) {
                    if (textBlocks[i].textContent === name) {
                        textBlock = textBlocks[i];
                    }
                }
                var block = textBlock.parentNode.parentNode

                this.highlightBlock(block, name, "red", this.isDeletedApplied);
            }
        }
    },
    deletedBlockHighlightSingle(element) {
        let visbilityElement = $('#visibility', element)
        let visbilityValueElement = $('#visibilityValue', element)
        let nameElement = $('#name', element)
        let idElement = $('#id', element)
        let toTurnOn = false;

        if (visbilityValueElement.html() == 0) {
            visbilityElement.html('<i class="fas fa-eye"></i>')
            visbilityValueElement.html(1)

            toTurnOn = true
        }
        else {
            visbilityElement.html('<i class="fas fa-eye-slash"></i>')
            visbilityValueElement.html(0)
        }

        let id = idElement.html()

        var block = document.getElementById(`system${id}_removed`);
        if (block == null) {
            //console.log(`Cannot found system${id}`);
            return;
        }

        this.highlightBlock(block, nameElement.html(), "red", toTurnOn, true);
    },
    impactedBlockHighlightSingle(element) {
        console.log("ha")
        let visbilityElement = $('#visibility', element)
        let visbilityValueElement = $('#visibilityValue', element)
        let nameElement = $('#name', element)
        let idElement = $('#id', element)
        let toTurnOn = false;


        if (visbilityValueElement.html() == 0) {
            visbilityElement.html('<i class="fas fa-eye"></i>')
            visbilityValueElement.html(1)

            toTurnOn = true
        }
        else {
            visbilityElement.html('<i class="fas fa-eye-slash"></i>')
            visbilityValueElement.html(0)
        }

        let id = idElement.html()

        var block = document.getElementById(`system${id}`);
        if (block == null) {
            console.log(`Cannot found system${id}`);
            return;
        }

        this.highlightBlock(block, nameElement.html(), "cyan", toTurnOn);
    },
    popUpTrackline() {
        PopUpUtils.popupCenter({
            w: 1000,
            h: 800,
            url: `${window.location.href
                .replace(new RegExp(`\\b${this.originalFileId}\\b`), this.currentFileId)
                .replace(new RegExp('\\bShow\\b'), 'Trackline')}?rootSysId=${this.rootSysId}
			`
        })
    },
    swapView(changeSwapProp = true) {
        var { modelDraw, sideDraw } = this.detachDraws()

        const temp = this.mainViewId
        this.mainViewId = this.subViewId
        this.subViewId = temp

        this.attachDraws(modelDraw, sideDraw)

        if (changeSwapProp) {
            this.swapped = !this.swapped
        }
    },
    toggleNetwork() {
        const allShown = this.networkDraw.isAllSubLinesShown()
        const allHidden = this.networkDraw.isAllSubLinesHiden()

        if (allShown && allHidden) {
            this.updateFullNetBtnText(!this.fullNet)
        } else if (allShown) {
            this.networkDraw.hideAllSubLines()
        } else {
            this.networkDraw.showAllSubLines()
        }
    },
    updateToggleDrawButtonText() {
        if (this.showTree) {
            $('#toggleTreeBtn span').text('Hide tree')
        } else {
            $('#toggleTreeBtn span').text('Show tree')
        }
    },
    async toggleSideView() {
        var previousSideView = this.tempSideView

        if (this.showTree) {
            this.tempSideView = this.treeDraw.detach()
            if (previousSideView) {
                $(`#${this.subViewId}`).append(previousSideView)
            } else {
                var funcGrObj = this.networkDraw ? this.networkDraw.getCurrentFunctionGroups() : null

                if (this.rootSysId > 0) {
                    let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
                        system => system.id == this.rootSysId
                    )

                    this.currentSubsysFileId = subsys.fK_FakeProjectFileId
                    let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

                    this.createNetworkDraw({ explicitRela, funcGrObj })
                } else {
                    this.createNetworkDraw({ funcGrObj })
                }
            }
        } else {
            this.tempSideView = this.networkDraw.detach()
            if (previousSideView) {
                $(`#${this.subViewId}`).append(previousSideView)
            } else {
                if (this.rootSysId > 0) {
                    let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
                        system => system.id == this.rootSysId
                    )

                    this.currentSubsysFileId = subsys.fK_FakeProjectFileId
                    let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

                    this.createTreeDraw({ explicitRela })
                } else {
                    this.createTreeDraw()
                }
            }
        }

        this.showTree = !this.showTree;
        this.updateToggleDrawButtonText();
    },
    async changeNetworkViewType(e) {
        this.viewType = e.target.value

        var funcGrObj = this.networkDraw.getCurrentFunctionGroups()

        // TODO: MAKE A FUNCTION TO REUSE THIS CODE
        // DUPLICATE WITH CODE IN INIT() FUNCTION
        if (rootSysId > 0) {
            let subsys = this.allLevelContents[`level${this.currentLevel}`].fileContent.systems.find(
                system => system.id == rootSysId
            )

            this.currentSubsysFileId = subsys.fK_FakeProjectFileId
            let explicitRela = (await this.getSubsysRelationships(this.currentSubsysFileId)).response

            this.createNetworkDraw({ explicitRela, funcGrObj })
        } else {
            this.createNetworkDraw({ funcGrObj })
        }
    },
    detachDraws() {
        var modelDraw = this.modelDraw.detach()
        var sideDraw = this.showTree ? this.treeDraw.detach() : this.networkDraw.detach()
        return { modelDraw, sideDraw }
    },
    attachDraws(modelDraw, sideDraw) {
        $(`#${this.mainViewId}`).append(modelDraw)
        $(`#${this.subViewId}`).append(sideDraw)
    },
    adjustViewToMatchFullNet() {
        if (this.fullNet) {
            this.networkDraw.showAllSubLines()
        }

        this.updateFullNetBtnText()
    },
    adjustViewToMatchSwap() {
        if (this.swapped) {
            this.swapView(false)
        }
    },
    // prevent double-click lead to all text selected on load
    disallowUserSelect() {
        d3.selectAll('text').style('user-select', 'none')
    },
    addAllowSelectEvent() {
        $(document).click(function allowUserSelect() {
            d3.selectAll('text').style('user-select', 'text')
            $(document).off('click', allowUserSelect)
        })
    },
    allowSearch() {
        $('#signalSearchBtn').prop('disabled', false)
    },
    navigateToNewFileGenerator({ initialRootSysId, containingFileId } = {}) {
        return fileId => {
            if (this.isNavigating || fileId == this.originalFileId) {
                return
            }

            this.isNavigating = true
            // priortize containingFileId (real file that contains the subsys with fake file id)
            const replaceFileId = containingFileId ?? fileId

            var newUrl =
                `${window.location.href
                    .split('?')[0]
                    .replace(new RegExp(`\\b${this.originalFileId}\\b`), replaceFileId)}` +
                `${this.swapped ? '?swap=true&' : '?'}${this.fullNet ? 'fullNet=true&' : ''}${this.showTree ? 'displayTreeView=true&' : ''}viewType=${this.viewType}`

            initialRootSysId && (newUrl = `${newUrl}&rootSysId=${initialRootSysId}`)

            if (!this.showTree) {
                window.location.href = this.networkDraw.appendDisplayOptionsToUrl(newUrl)
            } else {
                window.location.href = newUrl
            }
        }
    },
    triggerSignalSearch(fileIds) {
        if (this.isSearching) return false

        this.isSearching = true

        $('#relationshipSearch').val(fileIds)
        $('#signalSearchForm').submit()

        return true
    },
    addGoUpLevel(svg) {
        svg.on('wheel', e => {
            const delta = e.wheelDelta ?? -e.detail

            if (!e.ctrlKey && delta > 0) {
                this.goUpLevel()
            }
        })
    },
    goUpLevel() {
        $('#upLevelBtn').click()
    },
    startLoadingViews() {
        $('.main-loader').css('display', 'flex')
        $('.main-container').css('filter', 'blur(2.5px) grayscale(1.4)')
    },
    stopLoadingViews() {
        $('.main-loader').css('display', 'none')
        $('.main-container').css('filter', 'none')
    },
    updateFullNetBtnText(fullNet) {
        if (typeof fullNet != 'undefined') {
            this.fullNet = fullNet

            if (this.fullNet) {
                $('#viewNetworkBtn span').text('Hide network')
            } else {
                $('#viewNetworkBtn span').text('View full network')
            }
        }

        const allShown = true
        const allHidden = false

        // when there is no sub-lines
        if (allShown && allHidden) {
            if ($('#viewNetworkBtn span').text() == 'View remain') {
                $('#viewNetworkBtn span').text('View full network')
            }

            return
        }

        if (allShown) {
            $('#viewNetworkBtn span').text('Hide network')
            this.fullNet = true
        } else if (allHidden) {
            this.fullNet = false
            $('#viewNetworkBtn span').text('View full network')
        } else {
            this.fullNet = false
            $('#viewNetworkBtn span').text('View remain')
        }
    },
    getSystemClickHandler(
        { blockType, sourceFile, sourceBlock },
        { sysId, relaFakeFileId },
        direction,
        isFromSysRef = false,
        needDrawDotMakersObj,
        callback,
        arr
    ) {
        return async () => {
            this.isAnalysisApplied = false;
            this.isAddedApplied = false;
            this.isChangedApplied = false;
            this.isDeletedApplied = false;
            $("#addedTag").css("background-color", "rgba(0, 255, 0, 0.2)");
            $("#deletedTag").css("background-color", "rgba(255, 0, 0, 0.2)");
            $("#changedTag").css("background-color", "rgba(255, 165, 0, 0.2)");



            if (this.isSwitchingLevel) {

                return
            }

            if (
                (!SystemUtil.isSubSystem(blockType) && !SystemUtil.isRefToFile(blockType, sourceFile)) ||
                SystemUtil.isCalibration(sourceBlock)
            ) {
                return
            }

            // check if the subsystem exist
            if (SystemUtil.isRefToFile(blockType, sourceFile) && sourceFile == '') {
                const isRef = SystemUtil.isReference(blockType)
                alert(
                    `This system references to ${isRef ? 'a SubSystem in ' : ''
                    }another file. But that file does not exist in the current project.`
                )
                return
            }

            // start loading
            this.startLoadingViews()

            this.isSwitchingLevel = true

            var stillInFile = true
            $('#upLevelBtn').off('click')

            if (direction == 'up') {
                this.numClick--;
                $("#numClick").val(this.numClick);
                let { fileContent, systemContainsRef, minSystemId } =
                    this.allLevelContents[`level${this.currentLevel}`]
                // đi lên subsystem, đi từ trong subsystem ra ngoài subsystem
                if (sysId > minSystemId) {
                    const [upperSysId, upperFakeFileSysId] = SystemUtil.getHigherSystemId(fileContent.systems, sysId)
                    $('#upLevelBtn')
                        .click(
                            this.getSystemClickHandler(
                                { blockType, sourceFile, sourceBlock },
                                { sysId: upperSysId, relaFakeFileId: upperFakeFileSysId },
                                'up'
                            )
                        )
                        .removeClass('toolbar-btn--disabled')
                    // chuẩn bị cái số 3
                    // đ
                } else if (sysId == minSystemId && this.currentLevel > this.initialLevel) {
                    $('#upLevelBtn')
                        .click(
                            this.getSystemClickHandler(
                                { blockType, sourceFile, sourceBlock },
                                { sysId: -1, relaFakeFileId: -1 },
                                'up'
                            )
                        )
                        .removeClass('toolbar-btn--disabled')

                    // file A -> file B, fileB thuộc subsystem, đảm bảo up lên file A chứ không up lên subsystem
                    // đổi file
                } else if (sysId == -1) {
                    stillInFile = false
                    this.updateLevel(this.currentLevel - 1)
                    sysId = systemContainsRef

                    var prefile = fileContent
                    let { fileContent: contentAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]
                    const { minSystemId: minSysAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]

                    var upTitle = $('#pageTitle')
                        .text()
                        .replace(' > ' + FileUtil.name(this.files, prefile.fileId), '')

                    $('#pageTitle').text(upTitle)
                    this.currentFileId = contentAfterUpdate.fileId

                    if (sysId > minSysAfterUpdate) {
                        const [upperSysId, upperRelaFakeId] = SystemUtil.getHigherSystemId(
                            contentAfterUpdate.systems,
                            sysId
                        )

                        $('#upLevelBtn')
                            .click(
                                this.getSystemClickHandler(
                                    { blockType, sourceFile, sourceBlock },
                                    { sysId: upperSysId, relaFakeFileId: upperRelaFakeId },
                                    'up'
                                )
                            )
                            .removeClass('toolbar-btn--disabled')
                    } else if (this.currentLevel > this.initialLevel) {
                        $('#upLevelBtn')
                            .click(
                                this.getSystemClickHandler(
                                    { blockType, sourceFile, sourceBlock },
                                    { sysId: -1, relaFakeFileId: -1 },
                                    'up'
                                )
                            )
                            .removeClass('toolbar-btn--disabled')
                    } else {
                        $('#upLevelBtn').addClass('toolbar-btn--disabled')
                    }
                } else {
                    $('#upLevelBtn').addClass('toolbar-btn--disabled')
                }
            } else if (direction == 'down') {

                let upperSysId, upperRelaFakeFileId

                let { fileContent } = this.allLevelContents[`level${this.currentLevel}`]
                this.levelClickContents[`level${this.numClick}`] = {
                    systems: this.modelDraw.drawEntities.systemDraws,
                    lines: this.modelDraw.drawEntities.lineDraws,
                    sysId
                };

                this.numClick++;
                $("#numClick").val(this.numClick);
                if (SystemUtil.isSubSystem(blockType)) {
                    ;[upperSysId, upperRelaFakeFileId] = SystemUtil.getHigherSystemId(fileContent.systems, sysId)

                } else {

                    stillInFile = false

                    // get the function groups from network view and save it
                    // then later we can use it when we go up level
                    if (this.hasNetworkView && !this.showTree) {
                        this.saveLevelFuncGr(this.networkDraw.getCurrentFunctionGroups())
                    }
                    this.updateLevel(this.currentLevel + 1)

                    await this.updateContent(sourceFile, blockType, sourceBlock, sysId, isFromSysRef)

                    let { fileContent: contentAfterUpdate } = this.allLevelContents[`level${this.currentLevel}`]
                    const { minSystemId } = this.allLevelContents[`level${this.currentLevel}`]

                    upperSysId = -1
                    upperRelaFakeFileId = -1

                    sysId = minSystemId
                    $('#pageTitle').text($('#pageTitle').text() + ' > ' + sourceFile)
                    this.currentFileId = contentAfterUpdate.fileId

                }

                $('#upLevelBtn')
                    .click(
                        this.getSystemClickHandler(
                            { blockType, sourceFile, sourceBlock },
                            { sysId: upperSysId, relaFakeFileId: upperRelaFakeFileId },
                            'up'
                        )
                    )
                    .removeClass('toolbar-btn--disabled')
            }

            this.currentSubsysFileId = relaFakeFileId
            this.createModelDraw(sysId, needDrawDotMakersObj);

            if (this.hasNetworkView) {
                if (stillInFile) {
                    // } if (this.currentLevel >= 2) {
                    let explicitRela = sysId > 0 ? (await this.getSubsysRelationships(relaFakeFileId)).response : null
                    let funcGrObj = this.showTree ? null : this.networkDraw.getCurrentFunctionGroups()
                    this.showTree ? this.createTreeDraw({ explicitRela }) : this.createNetworkDraw({ explicitRela, funcGrObj })
                } else if (direction == 'up') {
                    let { funcGrObj } = this.allLevelContents[`level${this.currentLevel}`]
                    this.showTree ? this.createTreeDraw() : this.createNetworkDraw({ funcGrObj })
                } else {
                    this.showTree ? this.createTreeDraw() : this.createNetworkDraw()
                }
            }

            this.goUpDownCallback()

            // stop loading
            this.stopLoadingViews()

            // this.testImpact()

            this.isSwitchingLevel = false
            if (callback != undefined) {
                callback(needDrawDotMakersObj);
            }

            if (arr?.length == 0) {
                var obj = needDrawDotMakersObj;
                if (obj != undefined) {
                    const parentElement = d3.select('#parentG');
                    if (obj.type == 'system') {
                        this.drawDotMarker(parentElement, obj.left, obj.top)
                        this.drawDotMarker(parentElement, obj.right, obj.top)
                        this.drawDotMarker(parentElement, obj.left, obj.bottom)
                        this.drawDotMarker(parentElement, obj.right, obj.bottom)
                    } else {
                        const line = this.modelDraw.drawEntities.lineDraws.find(line => {
                            return obj.id == line.id;
                        })

                        line.points.forEach(point => this.drawDotMarker(parentElement, point[0], point[1]))
                    }
                }

            }


        }

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

    focusOnFile(fileId) {
        console.log(fileId)
        let node = $(`#node${fileId}`)
        if (node) {
            this.treeDraw.centerViewToNode(node)
        }
    },

    initChangeHandler() {
        if (!document.getElementById('treenode')) {
            $(".added").click((e) => {
                this.focusOnFile($("#projectFileId", e.currentTarget).text())
            });

            $(".changed").click((e) => {
                this.focusOnFile($("#projectFileId", e.currentTarget).text())
            });

            $(".deleted").click((e) => {
                this.focusOnFile($("#projectFileId", e.currentTarget).text())
            });
        }
    },


    async findImpactsInProject(depth) {
        var files = this.files ?? (await FilesAPI.getFilesInProject(this.projectId)).response
        let totalSystems = 0

        for (let i = 0; i < files.length; i++) {
            if (files[i].systemLevel != 'SubSystem') {
                var { response } = await FileContentsAPI.getFileContentById(files[i].id)
                totalSystems += response.systems.length;
            }
        }

        var digDepth = depth;

        let changedByProjectFile = {}
        let totalChanged = changedSystemFilesDTO.length

        for (let i = 0; i < changedSystemFilesDTO.length; i++) {
            let s = changedSystemFilesDTO[i];
            let key = `${s.FK_ProjectFileId}_${s.FK_ParentSystemId - 1}`

            var { newSystems, newLines } = await this.utils.fetchingFileContentById(s.FK_ProjectFileId, s.FK_ParentSystemId - 1)

            if (!changedByProjectFile[key]) {
                changedByProjectFile[key] = {
                    changes: [newSystems.find(x => x.sid == s.SID)],
                    parentId: s.FK_ParentSystemId - 1,
                    projectFileId: s.FK_ProjectFileId
                }
            } else {
                changedByProjectFile[key].changes.push(newSystems.find(x => x.sid == s.SID))
            }
        }

        var impacted = []
        let iteration = 0

        let timeStart = Date.now();

        for (const [key, { parentId, projectFileId, changes }] of Object.entries(changedByProjectFile)) {
            var { newSystems, newLines, response } = await this.utils.fetchingFileContentById(projectFileId, parentId)
            let impactComputer = Object.create(ImpactComputer)

            impactComputer.init(newSystems, newLines, response, parentId, this.tracer, this.utils, 3)
            let finalImpact = await impactComputer.impactSetComputationDetails(newSystems, await impactComputer.coreComputation(newSystems, changes, digDepth));

            finalImpact.forEach((system) => {
                if (!this.utils.checkIfFoundBlocksDetails(system, impacted)) {
                    impacted.push(system)
                }
            })

            //console.log(`Iteration ${iteration} finished`);
            iteration++
        }

        //console.log("Time took: " + (Date.now() - timeStart))

        //console.log(`Total changed: ${totalChanged}, Impacted ${impacted.length}/${totalSystems} (${impacted.length / totalSystems * 100}%)`)
        //console.log(impacted.map(x => x.rightSideChildren))
        //console.log(impacted)

        return impacted;
    },
    
    async findImpactsInCurrentFile(depth) {
        /*
        let fileContent = this.allLevelContents[`level${this.currentLevel}`]?.fileContent
        
        let impactComputer = Object.create(ImpactComputer);
        impactComputer.init(this.modelDraw.drawEntities.systemDraws, this.modelDraw.drawEntities.lineDraws, fileContent, this.rootSysId,
            this.tracer, this.utils, this.currentLevel)
        
        return await impactComputer.impactSetComputation(, depth)*/

        var digDepth = depth;

        let changedByProjectFile = {}
        let changedList = changedSystemFilesDTO.filter(x => x.FK_ProjectFileId == this.currentFileId)

        for (let i = 0; i < changedList.length; i++) {
            let s = changedList[i];
            let key = `${s.FK_ProjectFileId}_${s.FK_ParentSystemId - 1}`

            var { newSystems, newLines } = await this.utils.fetchingFileContentById(s.FK_ProjectFileId, s.FK_ParentSystemId - 1)

            if (!changedByProjectFile[key]) {
                changedByProjectFile[key] = {
                    changes: [newSystems.find(x => x.sid == s.SID)],
                    parentId: s.FK_ParentSystemId - 1,
                    projectFileId: s.FK_ProjectFileId
                }
            } else {
                changedByProjectFile[key].changes.push(newSystems.find(x => x.sid == s.SID))
            }
        }

        var impacted = []

        for (const [key, { parentId, projectFileId, changes }] of Object.entries(changedByProjectFile)) {
            var { newSystems, newLines, response } = await this.utils.fetchingFileContentById(projectFileId, parentId)
            let impactComputer = Object.create(ImpactComputer)

            impactComputer.init(newSystems, newLines, response, parentId, this.tracer, this.utils, 3)
            let finalImpact = await impactComputer.impactSetComputationDetails(newSystems, await impactComputer.coreComputation(newSystems, changes, digDepth));

            finalImpact.forEach((system) => {
                if (!this.utils.checkIfFoundBlocksDetails(system, impacted)) {
                    impacted.push(system)
                }
            })
        }

        //console.log("Time took: " + (Date.now() - timeStart))

        //console.log(`Total changed: ${totalChanged}, Impacted ${impacted.length}/${totalSystems} (${impacted.length / totalSystems * 100}%)`)
        //console.log(impacted.map(x => x.rightSideChildren))
        //console.log(impacted)

        return impacted;
    },


    updateCompareChangeProjectFileName() {
        $('.compareChange').each((index, element) => {
            let projectFileIdElem = $('#projectFileId', element)
            let file = this.files.find((x) => x.id == projectFileIdElem.html())
            if (file) {
                $('#projectFileName', element).html(file.name)
            }
        })

    },

    setChangesVisibility(showAll) {
        if (activeCategory == 'added') {
            this.setChangesVisiblityImpl('added', 'green', showAll, false);
        } else if (activeCategory == 'changed') {
            this.setChangesVisiblityImpl('changed', 'orange', showAll, false);
        } else if (activeCategory == 'deleted') {
            this.setChangesVisiblityImpl('deleted', 'red', showAll, true);
        } else {
            this.setChangesVisiblityImpl('impacted', 'cyan', showAll, false);
        }
    },

    setChangesVisiblityImpl(cls, color, showAll, isHighlightingRemoved) {
        $(`.${cls}`).each((i, element) => {
            console.log("ha")
            console.log(element)
            let visbilityElement = $('#visibility', element)
            let visbilityValueElement = $('#visibilityValue', element)
            let nameElement = $('#name', element)
            let idElement = $('#id', element)

            if (showAll) {
                visbilityElement.html('<i class="fas fa-eye"></i>')
                visbilityValueElement.html(1)
            }
            else {
                visbilityElement.html('<i class="fas fa-eye-slash"></i>')
                visbilityValueElement.html(0)
            }

            let id = idElement.html()

            var block = document.getElementById(isHighlightingRemoved ? `system${id}_removed` : `system${id}`);
            if (!block) {
                //console.log(`Cannot found system${id}`);
                return;
            }

            this.highlightBlock(block, nameElement.html(), color, showAll, isHighlightingRemoved);
        })

    }
}



//var child = block.childNodes.childNodes.firstChild.strokeStyle = "green";

export default ViewManager