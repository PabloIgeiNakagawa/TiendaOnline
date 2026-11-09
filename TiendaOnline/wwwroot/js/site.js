const getStoredTheme = () => localStorage.getItem('theme');
const applyTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);
};

// --- Refrescos con esqueleto (maquinaria compartida) ---
// La FORMA del esqueleto la declara cada vista con un <template>; esta función
// aporta solo el comportamiento: pinta el esqueleto si el refresco tarda más
// que el retardo (configurable: 0 para la carga inicial del caparazón — ahí el
// esqueleto SIEMPRE se ve — y 300ms para refrescos manuales, donde pintarlo
// con respuesta rápida sería un parpadeo incómodo al revés).
const retardoDelEsqueleto = 300;

async function cargarFragmentoConEsqueleto(contenedor, url, idDeEsqueleto, idDeError, alPintar, retardoMs) {
    const retardo = retardoMs === undefined ? retardoDelEsqueleto : retardoMs;

    // Memoria del último origen pedido: el Reintentar del aviso repite la
    // ÚLTIMA página que el usuario pidió — incluida la fallida — no la de
    // inicio del widget.
    contenedor.dataset.ultimaCarga = url;

    let reloj = setTimeout(() => {
        const plantilla = document.getElementById(idDeEsqueleto);
        if (plantilla && contenedor.isConnected) {
            contenedor.replaceChildren(plantilla.content.cloneNode(true));
        }
    }, retardo);

    try {
        const respuesta = await fetch(url, { credentials: 'same-origin' });
        if (!respuesta.ok) throw new Error('HTTP ' + respuesta.status);

        const html = await respuesta.text();
        clearTimeout(reloj);
        contenedor.innerHTML = html;
        if (typeof alPintar === 'function') alPintar(contenedor);

        // El widget puede estar pendiente (badge, contadores): quien pinte se
        // entera por evento (desacopla el loader de la lógica de cada página).
        document.dispatchEvent(new CustomEvent('fragmento:pintado', { detail: { id: contenedor.id, contenedor: contenedor } }));
    } catch {
        clearTimeout(reloj);
        const aviso = document.getElementById(idDeError || 'tpl-error-widget');
        if (aviso) contenedor.replaceChildren(aviso.content.cloneNode(true));
    }
}

// Carga inicial del caparazón: cada contenedor [data-cargar] declara su carga
// con data-cargar / data-esqueleto en el HTML — los JS de página no escriben
// esto. Dos condiciones de arrancar:
//  1. Esqueletos de una: el caparazón completo se ve cargando desde la primera
//     paint (ninguna página en blanco).
//  2. Fetches en fila (no en paralelo): con cache frío, seis fetches a la vez
//     armarían seis veces el mismo caché contra la base. En fila, el primero lo
//     arma y los siguientes lo sirven de una (ventana del cache ya caliente).
function iniciarWidgetsDelCaparazon() {
    const contenedores = Array.from(document.querySelectorAll('[data-cargar]'));

    contenedores.forEach(contenedor => {
        const plantilla = document.getElementById(contenedor.dataset.esqueleto);
        if (plantilla) contenedor.replaceChildren(plantilla.content.cloneNode(true));
    });

    const siguiente = () => {
        const contenedor = contenedores.shift();
        if (!contenedor) return;

        Promise.resolve(
            cargarFragmentoConEsqueleto(
                contenedor,
                contenedor.dataset.cargar,
                contenedor.dataset.esqueleto,
                'tpl-error-widget',
                null,
                0)
        ).then(siguiente, siguiente);
    };
    siguiente();
}

// Reintentar del aviso de error: repite la ÚLTIMA carga del widget (la página
// que el usuario estaba viendo, no la de inicio).
document.addEventListener('click', function (e) {
    const boton = e.target.closest('.js-reintentar-widget');
    if (!boton) return;
    e.preventDefault();

    const contenedor = boton.closest('[data-cargar]');
    if (!contenedor) return;

    cargarFragmentoConEsqueleto(
        contenedor,
        contenedor.dataset.ultimaCarga || contenedor.dataset.cargar,
        contenedor.dataset.esqueleto,
        'tpl-error-widget',
        null,
        0);
});

document.addEventListener('DOMContentLoaded', iniciarWidgetsDelCaparazon);

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

        var autohide = toastEl.getAttribute('data-bs-autohide') !== 'false';
        var toast = new bootstrap.Toast(toastEl, { autohide: autohide, delay: delay });
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