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

        // ELIMINAR DIRECCION
        if (e.target.closest('.btn-eliminar-direccion')) {
            const btn = e.target.closest('.btn-eliminar-direccion');
            mostrarModalEliminarDireccion(
                btn.dataset.direccionId,
                btn.dataset.direccionEtiqueta,
                btn.dataset.usuarioId
            );
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

        mostrarToast('Copiado al portapapeles', 'exito');
    }).catch(function () {
        mostrarToast('No se pudo copiar');
    });
}

function mostrarModalEliminarDireccion(direccionId, etiqueta, usuarioId) {
    const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
    const titulo = document.getElementById('modalTitulo');
    const iconoEstado = document.getElementById('iconoEstado');
    const mensaje = document.getElementById('mensajeCambioEstado');
    const botonDerecho = document.getElementById('botonDerecho');

    titulo.textContent = 'Eliminar dirección';
    iconoEstado.className = 'bi bi-trash text-danger display-4';
    mensaje.textContent = `¿Estás seguro de que quieres eliminar la dirección "${etiqueta}"?`;
    botonDerecho.className = 'btn btn-danger';
    botonDerecho.textContent = 'Eliminar';

    document.getElementById('eliminarUsuarioId').value = usuarioId;
    document.getElementById('formularioEliminarDireccion').action = '/Usuarios/EliminarDireccion/' + direccionId;

    botonDerecho.addEventListener('click', function () {
        document.getElementById('formularioEliminarDireccion').submit();
    }, { once: true });

    modal.show();
}