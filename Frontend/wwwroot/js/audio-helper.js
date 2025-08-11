

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

function lerp(a, b, t) { return a + (b - a) * t; }
function lerpColor(c1, c2, t) {
    const r = Math.round(lerp(c1[0], c2[0], t));
    const g = Math.round(lerp(c1[1], c2[1], t));
    const b = Math.round(lerp(c1[2], c2[2], t));
    return { rgb: `rgb(${r},${g},${b})`, rgba: `rgba(${r},${g},${b},0.45)` };
}

// kolory jak na pasku, od niskich do wysokich
const HEIGHT_STOPS = [
    { t: 0.00, c: [255, 255, 255] }, // #ffffff
    { t: 0.20, c: [255, 228, 0] }, // #ffe400
    { t: 0.40, c: [57, 255, 20] }, // #39ff14
    { t: 0.60, c: [56, 189, 248] }, // #38bdf8
    { t: 0.80, c: [255, 32, 151] }, // #ff2097
    { t: 1.00, c: [255, 255, 255] }  // #ffffff
];

function colorAtHeight(t) {
    for (let i = 0; i < HEIGHT_STOPS.length - 1; i++) {
        const a = HEIGHT_STOPS[i], b = HEIGHT_STOPS[i + 1];
        if (t >= a.t && t <= b.t) {
            const u = (t - a.t) / (b.t - a.t || 1);
            return lerpColor(a.c, b.c, u);
        }
    }
    return { rgb: 'rgb(255,255,255)', rgba: 'rgba(255,255,255,0.45)' };
}

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

            // średnia energii
            let sum = 0;
            for (let j = a; j < b; j++) sum += freq[j];
            const denom = Math.max(1, b - a);
            const avg = sum / denom;

            const hiBoost = 0.85 + 0.55 * Math.pow(i / (bars.length - 1 || 1), 1.15);
            const h = Math.max(minPx, Math.min(maxPx, Math.sqrt(avg / 255) * maxPx * hiBoost));

            peaks[i] = Math.max(peaks[i] - maxPx * decay, h);

            const el = bars[i];
            el.style.setProperty('--h', `${h}px`);
            el.style.setProperty('--p', `${peaks[i]}px`);

            // >>> KOLOR Z WYSOKOŚCI (0..1)
            const tNorm = (h - minPx) / (maxPx - minPx); // 0..1
            const col = colorAtHeight(Math.max(0, Math.min(1, tNorm)));

            // ustaw JEDEN kolor dla całego słupka (nadpisuje wszystko)
            el.style.background = col.rgb;
            // pasujący glow (poświata)
            el.style.boxShadow = `0 0 16px ${col.rgba}`;
        }
    };
    draw();
};

window.stopVisualizer = () => {
    if (animationId) cancelAnimationFrame(animationId);
    bars.forEach(el => { el.style.setProperty('--h', `4px`); el.style.setProperty('--p', `4px`); });
};

window.setAudioVolume = (audioElement, volume) => { initAudioChain(audioElement); gainNode.gain.value = volume; };