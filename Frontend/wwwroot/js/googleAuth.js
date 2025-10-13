
window.googleAuth = (function () {
    let dotnetRef = null;
    let initialized = false;

    function ensureSdkLoaded() {
        return new Promise((resolve, reject) => {
            if (window.google && window.google.accounts && window.google.accounts.id) {
                resolve();
                return;
            }
            
            const maxWaitMs = 5000;
            const start = Date.now();
            (function waitLoop() {
                if (window.google && window.google.accounts && window.google.accounts.id) {
                    resolve();
                } else if (Date.now() - start > maxWaitMs) {
                    reject(new Error("Google Identity Services SDK not loaded"));
                } else {
                    setTimeout(waitLoop, 50);
                }
            })();
        });
    }

    async function init(clientId, dotnet) {
        await ensureSdkLoaded();
        dotnetRef = dotnet;
        if (initialized) return;
        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (resp) => {
                if (resp && resp.credential && dotnetRef) {
                    dotnetRef.invokeMethodAsync("OnGoogleCredential", resp.credential);
                }
            },
            ux_mode: "popup",         
            auto_select: false
        });
        initialized = true;
    }

    async function renderButton(elementId) {
        await ensureSdkLoaded();
        const el = document.getElementById(elementId || "gsi_btn");
        if (!el) return;
        window.google.accounts.id.renderButton(el, {
            theme: "outline",
            size: "large",
            type: "standard",
            text: "continue_with",
            shape: "pill",
            logo_alignment: "left",
        });
    }

    function prompt() {
        if (window.google && window.google.accounts && window.google.accounts.id) {
            window.google.accounts.id.prompt(); 
        }
    }

    return { init, renderButton, prompt };
})();
