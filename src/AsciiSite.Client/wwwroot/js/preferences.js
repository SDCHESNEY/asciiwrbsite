window.asciiPrefs = window.asciiPrefs || {
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
    },
    getItem: function (key) {
        return window.localStorage.getItem(key);
    },
    setItem: function (key, value) {
        window.localStorage.setItem(key, value);
    },
    removeItem: function (key) {
        window.localStorage.removeItem(key);
    }
};
