window.gameSession = {
    abandon: function (sessionId) {
        if (!sessionId) return;

        const url = "https://localhost:7236/api/game/abandon";
        const data = new Blob([JSON.stringify({ sessionId: sessionId })], { type: "application/json" });

        navigator.sendBeacon(url, data);
    },

    registerBeforeUnload: function (sessionId) {
        window.addEventListener('beforeunload', function () {
            window.gameSession.abandon(sessionId);
        });
    }
};