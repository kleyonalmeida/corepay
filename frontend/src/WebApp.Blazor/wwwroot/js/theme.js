window.corepayTheme = {
    storageKey: 'theme',
    defaultTheme: 'dark',

    normalize: function (value) {
        return value === 'light' || value === 'dark' ? value : this.defaultTheme;
    },

    get: function () {
        try {
            return this.normalize(localStorage.getItem(this.storageKey));
        } catch {
            return this.defaultTheme;
        }
    },

    apply: function (theme) {
        var normalized = this.normalize(theme);
        var root = document.documentElement;

        if (normalized === 'dark') {
            root.classList.add('dark');
        } else {
            root.classList.remove('dark');
        }

        try {
            localStorage.setItem(this.storageKey, normalized);
        } catch {
            // Ignore storage failures (private mode, quota, etc.).
        }

        return normalized;
    },

    bootstrap: function () {
        return this.apply(this.get());
    }
};

window.corepayTheme.bootstrap();
