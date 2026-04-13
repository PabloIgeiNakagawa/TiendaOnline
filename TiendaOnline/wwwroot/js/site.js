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
    const iconClass = theme === 'dark' ? 'bi bi-moon fs-5' : 'bi bi-sun fs-5';

    // Header normal de la tienda
    const icon = document.getElementById('theme-icon');
    if (icon) {
        icon.className = theme === 'dark' ? 'bi bi-moon fs-4' : 'bi bi-sun fs-4';
    }

    // Admin layout - desktop
    const iconDesktop = document.getElementById('theme-icon-desktop');
    if (iconDesktop) iconDesktop.className = iconClass;

    // Admin layout - mobile
    const iconMobile = document.getElementById('theme-icon-mobile');
    if (iconMobile) iconMobile.className = theme === 'dark' ? 'bi bi-moon fs-4' : 'bi bi-sun fs-4';
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

    // Toggle sidebar
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            document.querySelector('.sidebar')?.classList.toggle('collapsed');
            document.querySelector('.main-wrapper')?.classList.toggle('sidebar-collapsed');
        });
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
});