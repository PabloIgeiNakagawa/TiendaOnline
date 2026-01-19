// 1. Funciones de persistencia en LocalStorage
const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = theme => localStorage.setItem('theme', theme);

// 2. Función para aplicar el tema y cambiar el icono
const applyTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);

    const icon = document.getElementById('theme-icon');
    const texto = document.getElementById('modo-actual');

    if (theme === 'dark') {
        if (icon) icon.className = 'bi bi-moon';
        if (texto) texto.innerText = 'Oscuro';
    } else {
        if (icon) icon.className = 'bi bi-sun';
        if (texto) texto.innerText = 'Claro';
    }
};

// 3. Inicialización al cargar la página
const initialTheme = getStoredTheme() || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
applyTheme(initialTheme);

// 4. Función que llama el botón
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-bs-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

    applyTheme(newTheme);
    setStoredTheme(newTheme);
}