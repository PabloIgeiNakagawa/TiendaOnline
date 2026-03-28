document.addEventListener('DOMContentLoaded', function () {
    // Delegación de eventos (más limpio y escalable)
    document.addEventListener('click', function (e) {

        // DAR BAJA
        if (e.target.closest('.btn-dar-baja')) {
            const btn = e.target.closest('.btn-dar-baja');
            mostrarModalCambioEstado(
                btn.dataset.id,
                btn.dataset.nombre,
                'baja'
            );
        }

        // DAR ALTA
        if (e.target.closest('.btn-dar-alta')) {
            const btn = e.target.closest('.btn-dar-alta');
            mostrarModalCambioEstado(
                btn.dataset.id,
                btn.dataset.nombre,
                'alta'
            );
        }

        // COPIAR
        if (e.target.closest('.btn-copiar')) {
            const btn = e.target.closest('.btn-copiar');
            copiarAlPortapapeles(btn.dataset.texto);
        }
    });

});

function mostrarModalCambioEstado(idUsuario, nombreUsuario, tipo) {

    const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
    const titulo = document.getElementById('modalTitulo');
    const iconoEstado = document.getElementById('iconoEstado');
    const mensaje = document.getElementById('mensajeCambioEstado');
    const botonDerecho = document.getElementById('botonDerecho');

    if (tipo === 'baja') {
        titulo.textContent = 'Dar de baja usuario';
        iconoEstado.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensaje.textContent = `¿Estás seguro de que quieres dar de baja a ${nombreUsuario}?`;
        botonDerecho.className = 'btn btn-danger';
        botonDerecho.textContent = 'Dar de Baja';

        botonDerecho.addEventListener('click', function () {
            document.getElementById(`formularioBaja${idUsuario}`).submit();
        }, { once: true });

    } else {
        titulo.textContent = 'Dar de alta usuario';
        iconoEstado.className = 'bi bi-check-circle text-success display-4';
        mensaje.textContent = `¿Estás seguro de que quieres dar de alta a ${nombreUsuario}?`;
        botonDerecho.className = 'btn btn-success';
        botonDerecho.textContent = 'Dar de Alta';

        botonDerecho.addEventListener('click', function () {
            document.getElementById(`formularioAlta${idUsuario}`).submit();
        }, { once: true });
    }

    modal.show();
}
function copiarAlPortapapeles(texto) {

    navigator.clipboard.writeText(texto).then(function () {

        const toast = document.createElement('div');
        toast.className = 'toast align-items-center text-white bg-success border-0 position-fixed top-0 end-0 m-3';
        toast.style.zIndex = '9999';
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-check-circle me-2"></i>Copiado al portapapeles
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        document.body.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            document.body.removeChild(toast);
        });

    }).catch(function () {
        alert('No se pudo copiar');
    });
}