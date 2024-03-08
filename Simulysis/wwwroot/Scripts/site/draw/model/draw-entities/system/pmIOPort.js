import Port from './port.js'

var PMIOPort = {
    /*
     * PROTOTYPAL LINK (PARENT)
     */
    __proto__: Port,

    /* ===== OVERRIDE ===== */
    initNumberOfPorts() {
        this.numberOfRconnPorts = 1;
        this.props.port = "PMIOPort_" + this.props.port;
    }
}

export default PMIOPort
