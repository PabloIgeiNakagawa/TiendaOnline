// Función para mostrar imagen en modal
function mostrarImagenModal(urlImagen, nombreProducto) {
    const modal = new bootstrap.Modal(document.getElementById('modalImagenProducto'));
    const imagenModal = document.getElementById('imagenProductoModal');
    const nombreModal = document.getElementById('nombreProductoModal');

    // Establecer imagen y nombre
    imagenModal.src = urlImagen;
    imagenModal.alt = nombreProducto;
    nombreModal.textContent = nombreProducto;

    // Mostrar modal
    modal.show();
}

// Modal de confirmación
function darBaja(idProducto, nombreProducto) {
    const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
    const titulo = document.getElementById('modalTitulo');
    const iconoEstado = document.getElementById('iconoEstado');
    const mensajeCambioEstado = document.getElementById('mensajeCambioEstado');
    const botonDerecho = document.getElementById('botonDerecho');

    iconoEstado.className = 'bi bi-exclamation-triangle text-warning display-4';
    mensajeCambioEstado.textContent = `¿Estás seguro de que quieres dar de baja el producto ${nombreProducto}? El producto no estará disponible para la venta.`;
    botonDerecho.className = 'btn btn-danger';
    botonDerecho.textContent = 'Dar de Baja';
    botonDerecho.onclick = () => {
        document.getElementById(`formularioBaja${idProducto}`).submit();
    };

    modal.show();
}

function darAlta(idProducto, nombreProducto) {
    const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
    const titulo = document.getElementById('modalTitulo');
    const iconoEstado = document.getElementById('iconoEstado');
    const mensajeCambioEstado = document.getElementById('mensajeCambioEstado');
    const botonDerecho = document.getElementById('botonDerecho');

    iconoEstado.className = 'bi bi-check-circle text-success display-4';
    mensajeCambioEstado.textContent = `¿Estás seguro de que quieres dar de alta el producto ${nombreProducto}? El producto estará disponible para la venta.`;
    botonDerecho.className = 'btn btn-success';
    botonDerecho.textContent = 'Dar de Alta';
    botonDerecho.onclick = () => {
        document.getElementById(`formularioAlta${idProducto}`).submit();
    };

    modal.show();
}