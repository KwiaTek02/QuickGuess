// wwwroot/js/timer.js

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

    function setRemainingPathColor(timeLeft) {
        const { alert, warning, info } = COLOR_CODES;
        const path = document.getElementById("base-timer-path-remaining");
        if (!path) return;

        if (timeLeft <= alert.threshold) {
            path.classList.remove(warning.color);
            path.classList.add(alert.color);
        } else if (timeLeft <= warning.threshold) {
            path.classList.remove(info.color);
            path.classList.add(warning.color);
        }
    }

    function calculateTimeFraction() {
        const rawTimeFraction = timeLeft / timeLimit;
        return rawTimeFraction - (1 / timeLimit) * (1 - rawTimeFraction);
    }

    function setCircleDasharray() {
        const circleDasharray = `${(
            calculateTimeFraction() * FULL_DASH_ARRAY
        ).toFixed(0)} 283`;
        const path = document.getElementById("base-timer-path-remaining");
        if (path) {
            path.setAttribute("stroke-dasharray", circleDasharray);
        }
    }

    function updateUI() {
        const label = document.getElementById("base-timer-label");
        if (label) {
            label.innerHTML = formatTime(timeLeft);
        }
        setCircleDasharray();
        setRemainingPathColor(timeLeft);
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
                if (timeLeft <= 0) {
                    onTimesUp();
                }
            }, 1000);
        },
        stop: function () {
            clearInterval(timerInterval);
            timerInterval = null;
        }
    };
})();

window.Timer = Timer;