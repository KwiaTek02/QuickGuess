function initAudioChainIfNeeded(audioElement) {
    if (!audioCtx) {
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }

    if (!source) {
        source = audioCtx.createMediaElementSource(audioElement);
    }

    if (!gainNode) {
        gainNode = audioCtx.createGain();
        source.connect(gainNode);
        gainNode.connect(audioCtx.destination);
    }

    if (!analyser) {
        analyser = audioCtx.createAnalyser();
        analyser.fftSize = 64;
        gainNode.connect(analyser);
    }
}


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

let audioCtx;
let analyser;
let source;
let gainNode;
let animationId;
let visualizerInitialized = false;

window.initVisualizer = () => {
    if (visualizerInitialized) return;
    visualizerInitialized = true;

    const audio = document.querySelector("audio");
    if (!audio) {
        console.warn("Audio element not found");
        return;
    }

    try {
        initAudioChainIfNeeded(audio); // 🔧 użyj naszej funkcji

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        const bars = [];
        for (let i = 0; i < 32; i++) {
            const el = document.getElementById(`bar-${i}`);
            if (el) bars.push(el);
        }

        function draw() {
            animationId = requestAnimationFrame(draw);
            analyser.getByteFrequencyData(dataArray);
            for (let i = 0; i < bars.length; i++) {
                const val = dataArray[i];
                const height = Math.max(4, (val / 255) * 50);
                bars[i].style.height = `${height}px`;
                bars[i].style.opacity = val > 20 ? "1" : "0.5";
            }
        }

        window.startVisualizer = () => {
            if (audioCtx.state === "suspended") {
                audioCtx.resume();
            }
            draw();
        };

        window.stopVisualizer = () => {
            if (animationId) cancelAnimationFrame(animationId);
            bars.forEach(bar => bar.style.height = "4px");
        };
    } catch (err) {
        console.error("Error initializing visualizer:", err);
    }
};

window.setAudioVolume = function (audioElement, volume) {
    if (!audioCtx) {
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }

    if (!source && audioElement) {
        source = audioCtx.createMediaElementSource(audioElement);
    }

    if (!gainNode) {
        gainNode = audioCtx.createGain();
        source.connect(gainNode);
        gainNode.connect(audioCtx.destination);
    }

    if (gainNode) {
        gainNode.gain.value = volume;
        console.log("Volume set to:", volume);
    } else {
        console.warn("GainNode not initialized yet");
    }
};