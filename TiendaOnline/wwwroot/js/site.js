// Al cargar la página, aplicar el tema guardado o el del sistema
const getStoredTheme = () => localStorage.getItem('theme');
const setStoredTheme = theme => localStorage.setItem('theme', theme);

// Función para aplicar el tema y cambiar el icono/texto
const applyTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);

    const icon = document.getElementById('theme-icon');
    const texto = document.getElementById('modo-actual');

    if (theme === 'dark') {
        if (icon) icon.className = 'bi bi-moon-fill fs-4';
        if (texto) texto.innerText = 'Oscuro';
    } else {
        if (icon) icon.className = 'bi bi-sun-fill fs-4';
        if (texto) texto.innerText = 'Claro';
    }
};

// Inicialización
const savedTheme = getStoredTheme();
if (savedTheme) {
    applyTheme(savedTheme);
}

// Función para el botón
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-bs-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

    applyTheme(newTheme);
    setStoredTheme(newTheme);
}
