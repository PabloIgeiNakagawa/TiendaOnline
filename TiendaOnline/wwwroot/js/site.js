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


    if (!(window.bootstrap && bootstrap.Toast)) return;

    // Inicializa todos los toasts y sincroniza la barra de progreso con el timeout
    var toastEls = Array.from(document.querySelectorAll('.toast'));
    toastEls.forEach(function (toastEl) {
        var bsDelayAttr = toastEl.getAttribute('data-bs-delay');
        var delay = bsDelayAttr ? parseInt(bsDelayAttr, 10) : 5000; // ms fallback

        var toast = new bootstrap.Toast(toastEl, { autohide: true, delay: delay });
        var progressBar = toastEl.querySelector('.progress-bar');

        // Animación de la barra: usa requestAnimationFrame para fluidez
        function startProgress() {
            if (!progressBar) return;
            var start = performance.now();
            function frame(now) {
                var elapsed = now - start;
                var pct = Math.max(0, 1 - (elapsed / delay));
                progressBar.style.width = (pct * 100).toFixed(2) + '%';
                if (elapsed < delay) {
                    requestAnimationFrame(frame);
                } else {
                    progressBar.style.width = '0%';
                }
            }
            // inicial
            progressBar.style.width = '100%';
            requestAnimationFrame(frame);
        }

        // Cuando se muestra el toast, iniciar la animación
        toastEl.addEventListener('show.bs.toast', function () {
            startProgress();
        });

        // finalmente mostrar
        toast.show();
    });

    // Logout seguro (POST con anti-forgery)
    document.querySelectorAll('.btn-logout').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var form = document.getElementById('logoutForm');
            if (form) form.submit();
        });
    });
});