window.confettiBlazor = {
    fire: function (side) {
        const count = 200;
        const defaults = {
            origin: { y: 0.7 },
            spread: 60,
            ticks: 200,
            gravity: 1,
            scalar: 1.2,
        };

        if (side === "left") {
            confetti({
                ...defaults,
                particleCount: count,
                angle: 60,
                origin: { x: 0, y: 0.8 }
            });
        } else if (side === "right") {
            confetti({
                ...defaults,
                particleCount: count,
                angle: 120,
                origin: { x: 1, y: 0.8 }
            });
        } else {
            confetti({
                ...defaults,
                particleCount: count,
                spread: 90,
                startVelocity: 45,
                origin: { y: 0.8 } 
            });
        }
    }
};