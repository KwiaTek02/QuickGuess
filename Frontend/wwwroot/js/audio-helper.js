

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
let bars = [];
let peaks = [];        
let lastBarCount = 0;


function initAudioChain(audioElement) {
    if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (!source) source = audioCtx.createMediaElementSource(audioElement);
    if (!gainNode) { gainNode = audioCtx.createGain(); source.connect(gainNode); }
    if (!analyser) {
        analyser = audioCtx.createAnalyser();
        analyser.fftSize = 2048;
        analyser.smoothingTimeConstant = 0.82;
        analyser.minDecibels = -90;
        analyser.maxDecibels = -10;
        gainNode.connect(analyser);
        analyser.connect(audioCtx.destination);
    }
}

const USABLE_MAX_HZ = 14000;
function buildLogBuckets(barCount, spectrumLength, sampleRate) {
    const buckets = [];
    const low = 3;

    // Nyquist = sampleRate / 2
    const nyquist = (sampleRate || 44100) / 2;
    const maxHz = Math.min(USABLE_MAX_HZ, nyquist); // nie wyżej niż Nyquist
    let high = Math.floor((maxHz / nyquist) * spectrumLength); // górny bin
    high = Math.max(low + barCount, Math.min(high, spectrumLength)); // bezpieczeństwo

    const edges = new Array(barCount + 1);
    edges[0] = low;

    for (let i = 1; i < barCount; i++) {
        const t = i / barCount;
        let idx = Math.round(low * Math.pow(high / low, t));
        idx = Math.min(idx, high - 1);
        edges[i] = Math.max(edges[i - 1] + 1, idx);
    }
    edges[barCount] = high;

    for (let i = 0; i < barCount; i++) {
        let a = edges[i], b = edges[i + 1];
        if (b <= a) b = Math.min(high, a + 1);
        buckets.push([a, b]);
    }
    return buckets;
}

window.initVisualizer = (barCount = 24) => {
    const audio = document.querySelector("audio");
    if (!audio) return;
    initAudioChain(audio);
    bars = Array.from(document.querySelectorAll("#visualizer .bar"));
    peaks = new Array(bars.length).fill(0);
};

window.startVisualizer = () => {
    if (!audioCtx || !analyser) return;
    if (audioCtx.state === "suspended") audioCtx.resume();

    const freq = new Uint8Array(analyser.frequencyBinCount);
    const buckets = buildLogBuckets(bars.length, freq.length, audioCtx.sampleRate);

    const maxPx = 100, minPx = 4, decay = 0.014;

    cancelAnimationFrame(animationId);
    const draw = () => {
        animationId = requestAnimationFrame(draw);
        analyser.getByteFrequencyData(freq);

        for (let i = 0; i < bars.length; i++) {
            const [a, b] = buckets[i];

            // średnia energii w buckecie
            let sum = 0;
            for (let j = a; j < b; j++) sum += freq[j];
            const denom = Math.max(1, b - a);
            const avg = sum / denom;

            // perceptualny tilt – lekko wzmacnia wyższe słupki
            const hiBoost = 0.85 + 0.55 * Math.pow(i / (bars.length - 1 || 1), 1.15);

            // wysokość w px (półlogarytmicznie) + minimalne tętno
            const h = Math.max(minPx, Math.min(maxPx, Math.sqrt(avg / 255) * maxPx * hiBoost));

            // czapka (peak cap) – łagodny opad
            peaks[i] = Math.max(peaks[i] - maxPx * decay, h);

            const el = bars[i];
            el.style.setProperty('--h', `${h}px`);
            el.style.setProperty('--p', `${peaks[i]}px`);
        }
    };
    draw();
};

window.stopVisualizer = () => {
    if (animationId) cancelAnimationFrame(animationId);
    bars.forEach(el => { el.style.setProperty('--h', `4px`); el.style.setProperty('--p', `4px`); });
};

window.setAudioVolume = (audioElement, volume) => { initAudioChain(audioElement); gainNode.gain.value = volume; };