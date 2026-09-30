window.corepayAuth = {
    storageKey: 'corepay.auth.session',

    getSession: function () {
        try {
            return localStorage.getItem(this.storageKey);
        } catch {
            return null;
        }
    },

    setSession: function (json) {
        try {
            localStorage.setItem(this.storageKey, json);
        } catch {
            // Ignore storage failures (private mode, quota, etc.).
        }
    },

    clearSession: function () {
        try {
            localStorage.removeItem(this.storageKey);
        } catch {
            // Ignore storage failures.
        }
    }
};

window.corepaySessionIdle = (function () {
    var dotNetRef = null;
    var timeoutMs = 30 * 60 * 1000;
    var timerId = null;
    var boundReset = null;
    var activityEvents = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

    function resetTimer() {
        if (!dotNetRef) {
            return;
        }

        if (timerId) {
            clearTimeout(timerId);
        }

        timerId = setTimeout(function () {
            dotNetRef.invokeMethodAsync('OnIdleTimeoutAsync');
        }, timeoutMs);
    }

    return {
        start: function (ref, ms) {
            this.stop();
            dotNetRef = ref;
            timeoutMs = ms;
            boundReset = resetTimer;
            activityEvents.forEach(function (eventName) {
                document.addEventListener(eventName, boundReset, { passive: true });
            });
            resetTimer();
        },

        stop: function () {
            if (timerId) {
                clearTimeout(timerId);
                timerId = null;
            }

            if (boundReset) {
                activityEvents.forEach(function (eventName) {
                    document.removeEventListener(eventName, boundReset);
                });
                boundReset = null;
            }

            dotNetRef = null;
        }
    };
})();
