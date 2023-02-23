
(function () {
    const themeStorageKey = "css-colors-theme";
    const lightThemeClass = "light-theme";
    const lightCodeStyle = document.getElementById("light-code-style").sheet;
    const darkCodeStyle = document.getElementById("dark-code-style").sheet;
    const themesMenuToggles = document.getElementsByClassName("themes-menu-toggle");
    const themesMenus = document.getElementsByClassName("themes-menu");
    const darkThemeOptions = document.getElementsByClassName("dark-theme-option");
    const lightThemeOptions = document.getElementsByClassName("light-theme-option");
    const autoThemeOptions = document.getElementsByClassName("auto-theme-option");

    loadPreferredTheme();
    registerThemesClickEvents();

    setTimeout(scrollToc, 500);

    function scrollToc() {
        var activeTocItem = $('.sidebar .sidebar-item.active:last')[0]
    
        if (activeTocItem) {
            activeTocItem.scrollIntoView({ block: "center" });
        }
        else{
            setTimeout(scrollToc, 500);
        }
    }

    function loadPreferredTheme() {
        let storedTheme = localStorage.getItem(themeStorageKey);
        if (storedTheme === "dark") {
            setDarkTheme();
        }
        else if (storedTheme === "light") {
            setLightTheme();
        }
        else {
            setAutoTheme();
        }    
    }

    function registerThemesClickEvents()
    {
        for (let i = 0; i < themesMenuToggles.length; i++) {
            themesMenuToggles[i].addEventListener("click", onToggleThemesMenus);
        }
    
        for (let i = 0; i < darkThemeOptions.length; i++) {
            darkThemeOptions[i].addEventListener("click", onDarkThemeSelected);
        }
    
        for (let i = 0; i < lightThemeOptions.length; i++) {
            lightThemeOptions[i].addEventListener("click", onLightThemeSelected);
        }
    
        for (let i = 0; i < autoThemeOptions.length; i++) {
            autoThemeOptions[i].addEventListener("click", onAutoThemeSelected);
        }
    
        window.addEventListener("click", onCloseThemesMenusClick);    
    }

    function onToggleThemesMenus(e) {
        for (let i = 0; i < themesMenus.length; i++) {
            const menu = themesMenus[i];
            menu.classList.toggle("show");
        }
    }

    function onCloseThemesMenusClick(e) {
        var targetIsThemesToggle = false;

        for (let i = 0; i < themesMenuToggles.length; i++) {
            if(themesMenuToggles[i].contains(e.target)) {
                targetIsThemesToggle = true;
                break;
            }
        }

        if (!targetIsThemesToggle) {
            for (let i = 0; i < themesMenus.length; i++) {
                const menu = themesMenus[i];
                menu.classList.remove("show");
            }
        }
    }

    function onDarkThemeSelected() {
        setDarkTheme();
        localStorage.setItem(themeStorageKey, "dark");
    }

    function onLightThemeSelected() {
        setLightTheme();
        localStorage.setItem(themeStorageKey, "light");
    }

    function onAutoThemeSelected() {
        setAutoTheme();
        localStorage.removeItem(themeStorageKey);
    }

    function setDarkTheme() {
        document.documentElement.classList.remove(lightThemeClass);
        setDarkCodeStyleDisabled(false);
        setLightCodeStyleDisabled(true);

        for (let i = 0; i < darkThemeOptions.length; i++) {
            darkThemeOptions[i].classList.add("active");
        }
    
        for (let i = 0; i < lightThemeOptions.length; i++) {
            lightThemeOptions[i].classList.remove("active");
        }
    
        for (let i = 0; i < autoThemeOptions.length; i++) {
            autoThemeOptions[i].classList.remove("active");
        }
    }

    function setLightTheme() {
        document.documentElement.classList.add(lightThemeClass);
        setDarkCodeStyleDisabled(true);
        setLightCodeStyleDisabled(false);

        for (let i = 0; i < darkThemeOptions.length; i++) {
            darkThemeOptions[i].classList.remove("active");
        }
    
        for (let i = 0; i < lightThemeOptions.length; i++) {
            lightThemeOptions[i].classList.add("active");
        }
    
        for (let i = 0; i < autoThemeOptions.length; i++) {
            autoThemeOptions[i].classList.remove("active");
        }
    }

    function setAutoTheme() {
        const prefersDark = window.matchMedia("(prefers-color-scheme:dark)").matches;
        if (prefersDark) {
            document.documentElement.classList.remove(lightThemeClass);
            setDarkCodeStyleDisabled(false);
            setLightCodeStyleDisabled(true);
        }
        else {
            document.documentElement.classList.add(lightThemeClass);
            setDarkCodeStyleDisabled(true);
            setLightCodeStyleDisabled(false);
        }

        for (let i = 0; i < darkThemeOptions.length; i++) {
            darkThemeOptions[i].classList.remove("active");
        }
    
        for (let i = 0; i < lightThemeOptions.length; i++) {
            lightThemeOptions[i].classList.remove("active");
        }
    
        for (let i = 0; i < autoThemeOptions.length; i++) {
            autoThemeOptions[i].classList.add("active");
        }
    }

    function setLightCodeStyleDisabled(val) {
        if (lightCodeStyle != null) {
            lightCodeStyle.disabled = val;
        }
    }

    function setDarkCodeStyleDisabled(val) {
        if (darkCodeStyle != null) {
            darkCodeStyle.disabled = val;
        }
    }
})();

function toggleMenu() {
               
    var sidebar = document.getElementById("sidebar");
    var blackout = document.getElementById("blackout");

    if (sidebar.style.left === "0px") 
    {
        sidebar.style.left = "-" + sidebar.offsetWidth + "px";
        blackout.classList.remove("showThat");
        blackout.classList.add("hideThat");
    } 
    else 
    {
        sidebar.style.left = "0px";
        blackout.classList.remove("hideThat");
        blackout.classList.add("showThat");
    }
}