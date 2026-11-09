// Atención Requerida (panel admin): el listado viene armado del server (_AtencionRequeridaLista);
// este script solo repagina. El refresco usa la maquinaria de site.js — el
// esqueleto y el aviso de error son <template> de la vista (IndexAdmin.cshtml),
// acá no hay HTML de ninguno de los dos.

let paginaActual = 1;
let totalPaginas = 1;

document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('pedidos-estancados-container');

    if (container) {
        paginaActual = parseInt(container.dataset.paginaActual) || 1;
        totalPaginas = parseInt(container.dataset.totalPaginas) || 1;
    }

    // Sin carga inicial: el server ya entregó la página 1 rendereada.

    // Event delegation: los pedazos se recrean en cada página
    document.addEventListener('click', function (e) {
        const prevBtn = e.target.closest('#btn-anterior');
        const nextBtn = e.target.closest('#btn-siguiente');
        const pageLink = e.target.closest('.page-number');
        const reintentar = e.target.closest('.js-reintentar');

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
        } else if (reintentar) {
            e.preventDefault();
            cargarPagina(paginaActual);
        }
    });
});

// El techo de páginas ya lo calcula el server (data-total-paginas): acá no hay
// números mágicos desincronizables.

async function cargarPagina(pagina) {
    const container = document.getElementById('pedidos-estancados-container');
    if (!container) return;

    // Se marca la página pedida ANTES del fetch: si el refresco falla, el
    // Reintentar repega la página que el usuario quería, no la que quedó.
    paginaActual = pagina;

    await cargarFragmentoConEsqueleto(
        container,
        `/admin/pedidos-estancados?pagina=${pagina}`,
        'tpl-esqueleto-filas',
        'tpl-error-refresco',
        (contenedor) => {
            // El fragmento trae el estado (contrato data-*): paginación y badge
            // siguen al server, sin estados paralelos a mano.
            const fragmento = contenedor.querySelector('[data-total-registros]');
            if (fragmento) {
                paginaActual = parseInt(fragmento.dataset.paginaActual) || paginaActual;
                totalPaginas = parseInt(fragmento.dataset.totalPaginas) || totalPaginas;
                const badge = document.querySelector('.card-header .badge.bg-danger');
                if (badge) badge.textContent = fragmento.dataset.totalRegistros;
            }
        }
    );
}
