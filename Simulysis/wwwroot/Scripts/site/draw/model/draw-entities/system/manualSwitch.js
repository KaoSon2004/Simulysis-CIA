import System from './index.js'

var ManualSwitch = {
    /*
     * PROTOTYPAL LINK (PARENT)
     */
    __proto__: System,

    /* ===== OVERRIDE ===== */
    initNumberOfPorts() {
        this.numberOfInPorts = 2;
        this.numberOfOutPorts = 1;
    }
}

export default ManualSwitch
