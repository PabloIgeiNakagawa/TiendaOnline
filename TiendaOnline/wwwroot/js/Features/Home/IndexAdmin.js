// Atención Requerida (panel admin): este script maneja solo la PAGINACIÓN del
// widget. La carga inicial la dispara el caparazón (data-cargar en la vista
// + maquinaria de site.js); acá se repagina con retardo de 300ms (con respuesta
// rápida no se pinta esqueleto: un parpadeo incómodo al revés) y se escucha el
// evento de pintado para refrescar badge y contadores del server.

let paginaActual = 1;
let totalPaginas = 1;

// Sync de estado: el fragmento trae página/paginación/badge (contrato data-* de
// _AtencionRequeridaLista); el badge del encabezado arranca como placeholder y
// toma su número real cuando llegó el fragmento.
document.addEventListener('fragmento:pintado', function (e) {
    if (e.detail.id !== 'pedidos-estancados-container') return;

    const fragmento = e.detail.contenedor.querySelector('[data-total-registros]');
    if (!fragmento) return;

    paginaActual = parseInt(fragmento.dataset.paginaActual) || paginaActual;
    totalPaginas = parseInt(fragmento.dataset.totalPaginas) || totalPaginas;

    const badge = document.getElementById('badge-atencion');
    if (badge) badge.textContent = fragmento.dataset.totalRegistros;
});

// El techo de páginas ya lo calcula el server (data-total-paginas): acá no hay
// números mágicos desincronizables.

document.addEventListener('DOMContentLoaded', function () {
    // Event delegation: los pedazos se recrean en cada página
    document.addEventListener('click', function (e) {
        const prevBtn = e.target.closest('#btn-anterior');
        const nextBtn = e.target.closest('#btn-siguiente');
        const pageLink = e.target.closest('.page-number');

        if (prevBtn && paginaActual > 1) {
            e.preventDefault();
            cargarPagina(paginaActual - 1);
        } else if (nextBtn && paginaActual < totalPaginas) {
            e.preventDefault();
            cargarPagina(paginaActual + 1);
        } else if (pageLink) {
            e.preventDefault();
            const pagina = parseInt(pageLink.dataset.pagina);
            if (pagina && pagina !== paginaActual) {
                cargarPagina(pagina);
            }
        }
        // El Reintentar del aviso lo maneja site.js (maquinaria genérica).
    });
});

async function cargarPagina(pagina) {
    const container = document.getElementById('pedidos-estancados-container');
    if (!container) return;

    // Se marca la página pedida ANTES del fetch: si el refresco falla, el
    // Reintentar de site.js repite la página que el usuario quería.
    paginaActual = pagina;

    await cargarFragmentoConEsqueleto(
        container,
        `/admin/pedidos-estancados?pagina=${pagina}`,
        'tpl-esqueleto-filas',
        'tpl-error-widget',
        null,
        retardoDelEsqueleto);
}
