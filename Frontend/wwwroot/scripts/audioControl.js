window.muteAudio = function (audioElement) {
    if (audioElement) {
        audioElement.muted = true;
    }
};

window.unmuteAudio = function (audioElement) {
    if (audioElement) {
        audioElement.muted = false;
    }
};