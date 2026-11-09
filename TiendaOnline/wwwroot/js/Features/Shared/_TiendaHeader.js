// Sugerencias del buscador de la navbar: mientras el usuario escribe se pide
// el fragmento /Productos/Sugerencias (misma consulta del catálogo, 6 items)
// y se pega debajo del input. La lista la arma Razor (parcial
// _SugerenciasBusqueda) — acá solo fetch y pegar, el JS nunca construye HTML.
// Mejora progresiva: sin JS, el form GET muestra el catálogo filtrado.

const RETARDO_DE_LA_DEBUNCIA = 300;
const MINIMO_DE_CARACTERES = 2;

let relojDeSugerencias = null;
let busquedaEnVuelo = null;

function cerrarSugerencias() {
    const contenedor = document.querySelector('.js-sugerencias');
    if (contenedor) contenedor.replaceChildren();
}

async function pedirSugerencias(termino) {
    const contenedor = document.querySelector('.js-sugerencias');
    if (!contenedor) return;

    // Racha de tecleo: la respuesta de un término viejo no pinta — el fetch
    // anterior se cancela y manda la del último término.
    busquedaEnVuelo?.abort();
    const control = new AbortController();
    busquedaEnVuelo = control;

    try {
        const respuesta = await fetch(contenedor.dataset.urlEndpoint + '?busqueda=' + encodeURIComponent(termino),
            { credentials: 'same-origin', signal: control.signal });
        if (!respuesta.ok) return;

        // Fragmento armado por Razor: pegarlo tal cual.
        const html = await respuesta.text();
        contenedor.replaceChildren();
        contenedor.innerHTML = html;
    } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') return;
        contenedor.replaceChildren();
    }
}

const inputBusquedaNavbar = document.querySelector('.js-busqueda-navbar');
inputBusquedaNavbar?.addEventListener('input', () => {
    const termino = inputBusquedaNavbar.value.trim();
    clearTimeout(relojDeSugerencias);

    // Corto o vacío: sin búsqueda y lo que esté abierto, se cierra.
    if (termino.length < MINIMO_DE_CARACTERES) {
        busquedaEnVuelo?.abort();
        cerrarSugerencias();
        return;
    }

    relojDeSugerencias = setTimeout(() => pedirSugerencias(termino), RETARDO_DE_LA_DEBUNCIA);
});

// Cerrar al hacer clic afuera (el propio input y la listita no cuentan).
document.addEventListener('click', (e) => {
    if (e.target.closest('.js-busqueda-navbar') || e.target.closest('.js-sugerencias')) return;
    cerrarSugerencias();
});

// Escape cierra.
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') cerrarSugerencias();
});
