window.getTimeOffset = function() {
    try {
        return 0 - new Date().getTimezoneOffset();
    } catch (err) {
        console.error(err);
        return 0;
    }
}
window.isMobileDevice = function () {
    return window.matchMedia("(orientation: portrait) and (pointer: coarse), (orientation: landscape) and (pointer: coarse)").matches;
};
//returns true if the given element has been rendered
//ensures the server side it is safe to proceed
window.elementExists = function(element){
    return document.getElementById(element) !== null;
}
function scrollToBottomInChat() {
    var element = document.getElementById('message-list');

    if (element) {
        // I have set a timeout because the height of the element in the DOM changes after sending a message
        // Therefore I wait so that the scroll bar can be set to the correct position after extending the chat window with a new message in the chat
        setTimeout(() => element.scrollTop = element.scrollHeight, 1);
    }
}

function scrollToTopInChat() {
    var element = document.getElementById('message-list');

    if (element) {
        setTimeout(() => element.scrollTop = 0, 1);
    }
}

//scroll to message
function scrollToMessage(messageId){
    var element = document.getElementById('message-' + messageId);
    if (element)
        element.scrollIntoView();
}

window.isSystemDarkMode = function() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches
}