window.playAudio = (element) => {
    console.log("Element received:", element);
    if (element && typeof element.play === "function") {
        element.play().catch(err => console.error("Audio autoplay blocked:", err));
    } else {
        console.error("Element is not a valid audio element or does not have .play()");
    }
};

window.stopAudio = (element) => {
    if (element && typeof element.pause === "function") {
        element.pause();
        element.currentTime = 0;
    }
};

window.startTimeout = function (dotNetRef, methodName, callbackName, sessionId, delay) {
    setTimeout(() => {
        dotNetRef.invokeMethodAsync(callbackName, sessionId);
    }, delay);
};