
const Timer = (function () {
    const FULL_DASH_ARRAY = 283;
    const WARNING_THRESHOLD = 10;
    const ALERT_THRESHOLD = 5;

    const COLOR_CODES = {
        info: { color: "green" },
        warning: { color: "orange", threshold: WARNING_THRESHOLD },
        alert: { color: "red", threshold: ALERT_THRESHOLD }
    };

    let timeLimit = 20;
    let timePassed = 0;
    let timeLeft = timeLimit;
    let timerInterval = null;
    let remainingPathColor = COLOR_CODES.info.color;
    let dotNetRef = null;
    let sessionId = null;

    function formatTime(time) {
        const minutes = Math.floor(time / 60);
        let seconds = time % 60;
        if (seconds < 10) {
            seconds = `0${seconds}`;
        }
        return `${minutes}:${seconds}`;
    }



    function applyStateClasses() {
        const base = document.getElementById("base-timer");
        const path = document.getElementById("base-timer-path-remaining");
        if (!base || !path) return;

        base.classList.remove("is-warning", "is-alert", "pulse");
        path.classList.remove("orange", "red");
        path.classList.add("green");

        if (timeLeft <= COLOR_CODES.warning.threshold) {
            base.classList.add("is-warning");
            path.classList.remove("green");
            path.classList.add("orange");
        }
        if (timeLeft <= COLOR_CODES.alert.threshold) {
            base.classList.remove("is-warning");
            base.classList.add("is-alert", "pulse"); 
            path.classList.remove("orange");
            path.classList.add("red");
        }
    }

    function calculateTimeFraction() {
        const raw = timeLeft / timeLimit;
        return raw - (1 / timeLimit) * (1 - raw);
    }

    function setCircleDasharray() {
        const arr = `${(calculateTimeFraction() * FULL_DASH_ARRAY).toFixed(0)} 283`;
        const path = document.getElementById("base-timer-path-remaining");
        if (path) path.setAttribute("stroke-dasharray", arr);
    }

    function updateUI() {
        const label = document.getElementById("base-timer-label");
        if (label) label.textContent = formatTime(timeLeft);
        setCircleDasharray();
        applyStateClasses();
    }

    function onTimesUp() {
        clearInterval(timerInterval);
        timerInterval = null;
        if (dotNetRef && sessionId) {
            dotNetRef.invokeMethodAsync("TimeoutGuess", sessionId);
        }
    }

    return {
        start: function (dotnet, session, duration) {
            dotNetRef = dotnet;
            sessionId = session;
            timeLimit = duration || 20;
            timePassed = 0;
            timeLeft = timeLimit;

            updateUI();
            if (timerInterval) clearInterval(timerInterval);

            timerInterval = setInterval(() => {
                timePassed += 1;
                timeLeft = timeLimit - timePassed;
                updateUI();
                if (timeLeft <= 0) onTimesUp();
            }, 1000);
        },
        stop: function () {
            clearInterval(timerInterval);
            timerInterval = null;
        }
    };
})();
window.Timer = Timer;


window.currentSessionId = null;

window.registerSession = function (sessionId) {
    window.currentSessionId = sessionId;
};

window.onbeforeunload = () => {
    if (window.currentSessionId) {
        navigator.sendBeacon("/api/game/abandon",
            JSON.stringify({ sessionId: window.currentSessionId }));
    }
};