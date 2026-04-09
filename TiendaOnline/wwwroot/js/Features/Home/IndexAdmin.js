let paginaActual = 1;
let totalPaginas = 1;

document.addEventListener('DOMContentLoaded', function() {
    const container = document.getElementById('pedidos-estancados-container');

    if (container) {
        paginaActual = parseInt(container.dataset.paginaActual) || 1;
        totalPaginas = parseInt(container.dataset.totalPaginas) || 1;
    }

    // Carga inicial
    cargarPagina(paginaActual);

    // Event delegation on document for pagination (elements are recreated dynamically)
    document.addEventListener('click', function(e) {
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
    });
});

function crearPedidoCard(pedido) {
    const claseColor = pedido.horasTranscurridas > 72 ? 'danger' : 'warning';
    return `
        <div class="card bg-body-tertiary border-0 shadow-sm mb-2 border-start border-3 border-${claseColor}">
            <div class="card-body py-2 px-2">
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <span class="fw-semibold small">Pedido #${pedido.pedidoId}</span>
                    <span class="badge bg-${claseColor} small">${Math.floor(pedido.horasTranscurridas)} hs</span>
                </div>
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <small class="text-muted"><i class="bi bi-person me-1"></i>${pedido.cliente}</small>
                </div>
                <div class="d-flex justify-content-end">
                    <a href="/Pedidos/Detalles/${pedido.pedidoId}" class="btn btn-sm btn-outline-primary" data-bs-toggle="tooltip" title="Revisar pedido">
                        <i class="bi bi-eye me-1"></i>Revisar
                    </a>
                </div>
            </div>
        </div>`;
}

function crearPaginacion(paginaActual, totalPaginas) {
    const radio = 2;
    let html = '<nav aria-label="Paginación de pedidos estancados"><ul class="pagination justify-content-center mb-0">';

    // Previous
    html += `<li class="page-item ${paginaActual <= 1 ? 'disabled' : ''}">
        <a class="page-link" href="#" id="btn-anterior" aria-label="Anterior">
            <i class="bi bi-chevron-left"></i>
        </a>
    </li>`;

    // Pages
    for (let i = 1; i <= totalPaginas; i++) {
        if (i === 1 || i === totalPaginas || (i >= paginaActual - radio && i <= paginaActual + radio)) {
            html += `<li class="page-item ${i === paginaActual ? 'active' : ''}">
                <a class="page-link page-number" href="#" data-pagina="${i}">${i}</a>
            </li>`;
        } else if (i === paginaActual - radio - 1 || i === paginaActual + radio + 1) {
            html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    // Next
    html += `<li class="page-item ${paginaActual >= totalPaginas ? 'disabled' : ''}">
        <a class="page-link" href="#" id="btn-siguiente" aria-label="Siguiente">
            <i class="bi bi-chevron-right"></i>
        </a>
    </li>`;

    html += '</ul></nav>';
    return html;
}

function cargarPagina(pagina) {
    fetch(`/admin/pedidos-estancados?pagina=${pagina}`, {
        credentials: 'same-origin'
    })
    .then(response => {
        console.log('Response status:', response.status);
        if (!response.ok) {
            throw new Error('HTTP error: ' + response.status);
        }
        return response.json();
    })
    .then(data => {
            const container = document.getElementById('pedidos-estancados-container');

            if (data.pedidos.length === 0) {
                container.innerHTML = `
                    <div class="text-center py-4">
                        <i class="bi bi-check2-circle fs-1 text-success opacity-50"></i>
                        <p class="text-muted mt-2 mb-0">Todo al día. No hay pedidos demorados.</p>
                    </div>`;
            } else {
                container.innerHTML = data.pedidos.map(crearPedidoCard).join('');
            }

            paginaActual = data.paginaActual;
            totalPaginas = data.totalPaginas;

            // Update Bootstrap pagination
            const footerExistente = document.querySelector('.card-footer');
            if (footerExistente) footerExistente.remove();

            if (data.totalRegistros > 5) {
                const card = document.getElementById('pedidos-estancados-container').closest('.card');
                const footerDiv = document.createElement('div');
                footerDiv.className = 'card-footer bg-body-tertiary border-0 py-2';
                footerDiv.innerHTML = crearPaginacion(data.paginaActual, data.totalPaginas);
                card.appendChild(footerDiv);
            }

            const headerBadge = document.querySelector('.card-header .badge.bg-danger');
            if (headerBadge) headerBadge.textContent = data.totalRegistros;
        })
        .catch(error => console.error('Error:', error));
}
