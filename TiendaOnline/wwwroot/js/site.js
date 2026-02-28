const getStoredTheme = () => localStorage.getItem('theme');
const applyTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);
};

const savedTheme = getStoredTheme();
if (savedTheme) {
    applyTheme(savedTheme);
}

document.addEventListener('DOMContentLoaded', () => {
    const theme = document.documentElement.getAttribute('data-bs-theme') || 'light';
    updateThemeUI(theme);
});

function updateThemeUI(theme) {
    const icon = document.getElementById('theme-icon');
    const texto = document.getElementById('modo-actual');

    if (!icon) return;

    if (theme === 'dark') {
        icon.className = 'bi bi-moon fs-4';
        if (texto) texto.innerText = 'Oscuro';
    } else {
        icon.className = 'bi bi-sun fs-4';
        if (texto) texto.innerText = 'Claro';
    }
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-bs-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

    localStorage.setItem('theme', newTheme);
    applyTheme(newTheme);
    updateThemeUI(newTheme);
}

document.addEventListener('DOMContentLoaded', () => {
    const theme = document.documentElement.getAttribute('data-bs-theme') || 'light';
    updateThemeUI(theme);

    const btn = document.getElementById('btn-theme');
    if (btn) {
        btn.addEventListener('click', toggleTheme);
    }

    // Inicializar Tooltips
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(t => new bootstrap.Tooltip(t));
});