document.addEventListener('DOMContentLoaded', function () {
    // Delegación global
    document.addEventListener('click', function (e) {

        const btn = e.target.closest('.btn-cambio-estado');
        if (!btn) return;

        const id = btn.dataset.id;
        const nombre = btn.dataset.nombre;
        const tipo = btn.dataset.tipo;

        mostrarModalCambioEstado(id, nombre, tipo);
    });

});

function mostrarModalCambioEstado(idUsuario, nombreUsuario, tipo) {

    const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
    const iconoEstado = document.getElementById('iconoEstado');
    const mensaje = document.getElementById('mensajeCambioEstado');
    const botonDerecho = document.getElementById('botonDerecho');

    if (tipo === 'baja') {

        iconoEstado.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensaje.textContent = `¿Estás seguro de que quieres dar de baja a ${nombreUsuario}?`;
        botonDerecho.className = 'btn btn-danger';
        botonDerecho.textContent = 'Dar de Baja';

        botonDerecho.replaceWith(botonDerecho.cloneNode(true));
        const nuevoBoton = document.getElementById('botonDerecho');

        nuevoBoton.addEventListener('click', function () {
            document.getElementById(`formularioBaja${idUsuario}`).submit();
        }, { once: true });

    } else {

        iconoEstado.className = 'bi bi-check-circle text-success display-4';
        mensaje.textContent = `¿Estás seguro de que quieres dar de alta a ${nombreUsuario}?`;
        botonDerecho.className = 'btn btn-success';
        botonDerecho.textContent = 'Dar de Alta';

        botonDerecho.replaceWith(botonDerecho.cloneNode(true));
        const nuevoBoton = document.getElementById('botonDerecho');

        nuevoBoton.addEventListener('click', function () {
            document.getElementById(`formularioAlta${idUsuario}`).submit();
        }, { once: true });
    }

    modal.show();
}