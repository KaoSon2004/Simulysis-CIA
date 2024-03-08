// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


var project = document.getElementsByClassName("project")[0];
var analysis = document.getElementsByClassName("analysis")[0];
var about = document.getElementsByClassName("about")[0];


var header = document.getElementsByClassName("navList")[0];
var btns = header.getElementsByClassName("navItem");
var logoPart = document.getElementsByClassName("logoPart")[0];
logoPart.addEventListener('click', function (e) {
    var current = document.getElementsByClassName("onPage");
    current[0].classList.remove('onPage');
    btns[0].classList.add('onPage');
    var array = [];
    array.push(btns);
    sessionStorage.setItem('element', btns[0].innerText.toLowerCase().toString());

})


for (var i = 0; i < btns.length; i++) {
    btns[i].addEventListener('click', function (e) {
        var current = document.getElementsByClassName("onPage");
        if (current) {
            current[0].classList.remove('onPage');
        }        
        this.classList.add('onPage');
        var array = [];
        array.push(btns);
        sessionStorage.setItem('element', this.innerText.toLowerCase().toString());


    });
}

function setActivatedItem() {
    var item = sessionStorage.getItem('element');

    if (item) {
        for (var a = 0; a < btns.length; a++) {
            if (btns[a].innerText.toLowerCase() == item) {
                btns[a].classList.add('onPage');
            } else {
                btns[a].classList.remove('onPage');
            }
        }
    }
}

window.onload == setActivatedItem();
mainContainer = document.getElementsByClassName('main-container')[0];
main = document.getElementsByClassName('main')[0];
navbar = document.getElementById("navbar-simulysis")




analysePage = document.getElementById("analyse-page");
uploadViewPage = document.getElementById("uploadView-page");
projectComparision = document.getElementById("projectComparision")
homeIndex = document.getElementById("homeIndex")
fileAnalyse = document.getElementById("file-analyse")




var directory = document.URL.split("/")
if (analysePage) {
        mainContainer.classList.add("analysize");
}

if (uploadViewPage) {
    mainContainer.classList.add("uploadView");
        main.classList.add("uploadView");
}
if (projectComparision) {
        mainContainer.classList.add("projectForm");
        main.classList.add("projectForm");
    }


if (homeIndex) {
        mainContainer.classList.add("homeIndex");
        main.classList.add("homeIndex");
    console.log(homeIndex)
}
if (fileAnalyse) {
    navbar.style.display = "none";
}
