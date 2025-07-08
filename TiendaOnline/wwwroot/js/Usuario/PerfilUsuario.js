document.addEventListener('DOMContentLoaded', function () {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltips = listaTooltips.map(function (elementoTooltip) {
        return new bootstrap.Tooltip(elementoTooltip);
    });
});

// Modal de confirmación
function confirmarAccion(idUsuario, accion, nombreUsuario) {
    const modal = new bootstrap.Modal(document.getElementById('modalConfirmacion'));
    const iconoConfirmacion = document.getElementById('iconoConfirmacion');
    const mensajeConfirmacion = document.getElementById('mensajeConfirmacion');
    const botonConfirmar = document.getElementById('botonConfirmar');

    if (accion === 'baja') {
        iconoConfirmacion.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de baja a ${nombreUsuario}?`;
        botonConfirmar.className = 'btn btn-danger';
        botonConfirmar.textContent = 'Dar de Baja';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioBaja${idUsuario}`).submit();
        };
    } else {
        iconoConfirmacion.className = 'bi bi-check-circle text-success display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de alta a ${nombreUsuario}?`;
        botonConfirmar.className = 'btn btn-success';
        botonConfirmar.textContent = 'Dar de Alta';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioAlta${idUsuario}`).submit();
        };
    }

    modal.show();
}

// Copiar al portapapeles
function copiarAlPortapapeles(texto) {
    navigator.clipboard.writeText(texto).then(function () {
        // Mostrar notificación de éxito
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
        // Fallback para navegadores que no soportan clipboard API
        const textArea = document.createElement('textarea');
        textArea.value = texto;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);

        alert('Texto copiado al portapapeles');
    });
}