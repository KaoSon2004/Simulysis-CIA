import System from './index.js'

var Switch = {
    /*
     * PROTOTYPAL LINK (PARENT)
     */
    __proto__: System,

    /* ===== OVERRIDE ===== */
    initNumberOfPorts() {
        this.numberOfInPorts = 3;
        this.numberOfOutPorts = 1;
    }
}

export default Switch
