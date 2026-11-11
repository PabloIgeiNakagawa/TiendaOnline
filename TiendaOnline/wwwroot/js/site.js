const getStoredTheme = () => localStorage.getItem('theme');
const applyTheme = (theme) => {
    document.documentElement.setAttribute('data-bs-theme', theme);
};

// --- Toast armado del lado del cliente (misma forma que _MensajeTempData) ---
// Para avisos que nacen en JS (validaciones de formularios, etc.). El mensaje
// viaja por textContent: nada de HTML interpolado con datos del usuario.
// Regla de la casa: error persiste hasta cerrarlo, éxito se va solo a los 5s.
function mostrarToast(mensaje, tipo = 'error') {
    if (!(window.bootstrap && bootstrap.Toast)) return;

    var contenedor = document.querySelector('.toast-container[data-toast-cliente]');
    if (!contenedor) {
        contenedor = document.createElement('div');
        // Medio abajo, por encima del header sticky (1100 = el mismo z-index que
        // usan los dropdowns para superar la barra fija). En móvil el mb-5 lo deja
        // justo arriba de la barra de tabs fixed-bottom.
        contenedor.className = 'toast-container position-fixed bottom-0 start-50 translate-middle-x p-3 mb-5 mb-lg-0';
        contenedor.style.zIndex = '1100';
        contenedor.setAttribute('data-toast-cliente', '');
        document.body.appendChild(contenedor);
    }

    var esError = tipo === 'error';
    var toast = document.createElement('div');
    toast.className = 'toast align-items-center ' + (esError ? 'text-bg-danger' : 'text-bg-success') + ' border-0';
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    if (esError) toast.setAttribute('data-bs-autohide', 'false');

    var fila = document.createElement('div');
    fila.className = 'd-flex';

    var cuerpo = document.createElement('div');
    cuerpo.className = 'toast-body';
    var icono = document.createElement('i');
    icono.className = esError ? 'bi bi-x-circle-fill me-2' : 'bi bi-check-circle-fill me-2';
    cuerpo.appendChild(icono);
    cuerpo.appendChild(document.createTextNode(mensaje));

    var cierre = document.createElement('button');
    cierre.type = 'button';
    cierre.className = 'btn-close ' + (esError ? 'btn-close-white' : '') + ' me-2 m-auto';
    cierre.setAttribute('data-bs-dismiss', 'toast');
    cierre.setAttribute('aria-label', 'Cerrar');

    fila.appendChild(cuerpo);
    fila.appendChild(cierre);
    toast.appendChild(fila);
    contenedor.appendChild(toast);

    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove(); // sin autotoasts fantasma acumulados en el DOM
    });

    new bootstrap.Toast(toast, { autohide: !esError, delay: 5000 }).show();
}

// --- Refrescos de sección (maquinaria compartida) ---
// La FORMA del esqueleto la declara cada vista con un <template>; esta función
// aporta solo el comportamiento de una sección suelta: pinta el esqueleto si
// el refresco tarda más que el retardo (300ms — con respuesta rápida sería un
// parpadeo incómodo al revés). La carga inicial del caparazón no pasa por acá:
// se pinta de una sin retardo (cargarElPanelDelCaparazon, más abajo).
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

// Caparazón del panel, TODO DE GOLPE: la página declara UNA carga para todo el
// cuerpo del panel (data-cargar en el raíz) y cada contenedor declara
// data-seccion (la clave del envase que arma el server) + data-esqueleto (su
// forma). Un único fetch trae todas las secciones juntas: los esqueletos se
// pintan de una al primer paint y el reparto pinta el panel completo en el
// mismo borrón — ninguna sección aparece antes que las demás.
async function cargarElPanelDelCaparazon() {
    const contenedores = Array.from(document.querySelectorAll('[data-seccion]'));
    if (!contenedores.length) return;

    const url = contenedores[0].closest('[data-cargar]')?.dataset.cargar;
    if (!url) return;

    contenedores.forEach(contenedor => {
        const plantilla = document.getElementById(contenedor.dataset.esqueleto);
        if (plantilla) contenedor.replaceChildren(plantilla.content.cloneNode(true));
    });

    try {
        const respuesta = await fetch(url, { credentials: 'same-origin' });
        if (!respuesta.ok) throw new Error('HTTP ' + respuesta.status);

        const cuerpo = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
        const avisoDelError = document.getElementById('tpl-error-widget');

        // Reparto por clave y pintado EN LA MISMA pasada: badge/contadores se
        // enteran por evento, igual que cuando las secciones vuelan sueltas.
        contenedores.forEach(contenedor => {
            const seccion = cuerpo.querySelector(`[data-seccion="${contenedor.dataset.seccion}"]`);
            if (seccion) {
                contenedor.innerHTML = seccion.innerHTML;
            } else if (avisoDelError) {
                contenedor.replaceChildren(avisoDelError.content.cloneNode(true));
            }
            document.dispatchEvent(new CustomEvent('fragmento:pintado', { detail: { id: contenedor.id, contenedor: contenedor } }));
        });
    } catch {
        const aviso = document.getElementById('tpl-error-widget');
        if (aviso) contenedores.forEach(contenedor => contenedor.replaceChildren(aviso.content.cloneNode(true)));
    }
}

document.addEventListener('DOMContentLoaded', cargarElPanelDelCaparazon);

// Reintentar del aviso de error. Dos caminos:
//  - El error lo dejó la PAGINACIÓN (cargarPagina guarda su URL en
//    dataset.ultimaCarga): repite ESA página, no vuelve al inicio.
//  - La caída fue del envase completo: se pide todo el panel de nuevo, con
//    esqueletos y pintado juntos otra vez.
document.addEventListener('click', function (e) {
    const boton = e.target.closest('.js-reintentar-widget');
    if (!boton) return;
    e.preventDefault();

    const contenedor = boton.closest('[data-seccion]');
    if (!contenedor) return;

    if (contenedor.dataset.ultimaCarga) {
        cargarFragmentoConEsqueleto(
            contenedor,
            contenedor.dataset.ultimaCarga,
            contenedor.dataset.esqueleto,
            'tpl-error-widget',
            null,
            0);
        return;
    }

    cargarElPanelDelCaparazon();
});

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