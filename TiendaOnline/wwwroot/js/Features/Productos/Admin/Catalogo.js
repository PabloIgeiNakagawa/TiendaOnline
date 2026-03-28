document.addEventListener('DOMContentLoaded', function () {
    // Mostrar imagen en modal
    function mostrarImagenModal(urlImagen, nombreProducto) {
        const modalEl = document.getElementById('modalImagenProducto');
        const modal = new bootstrap.Modal(modalEl);

        document.getElementById('imagenProductoModal').src = urlImagen;
        document.getElementById('imagenProductoModal').alt = nombreProducto;
        document.getElementById('nombreProductoModal').textContent = nombreProducto;

        modal.show();
    }

    // Modal Confirmación Baja
    function darBaja(idProducto, nombreProducto) {
        const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
        const iconoEstado = document.getElementById('iconoEstado');
        const mensajeCambioEstado = document.getElementById('mensajeCambioEstado');
        const botonDerecho = document.getElementById('botonDerecho');

        iconoEstado.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensajeCambioEstado.textContent =
            `¿Estás seguro de que quieres dar de baja el producto ${nombreProducto}?`;

        botonDerecho.className = 'btn btn-danger';
        botonDerecho.textContent = 'Dar de Baja';
        botonDerecho.onclick = () =>
            document.getElementById(`formularioBaja${idProducto}`).submit();

        modal.show();
    }

    // Modal Confirmación Alta
    function darAlta(idProducto, nombreProducto) {
        const modal = new bootstrap.Modal(document.getElementById('modalCambioEstado'));
        const iconoEstado = document.getElementById('iconoEstado');
        const mensajeCambioEstado = document.getElementById('mensajeCambioEstado');
        const botonDerecho = document.getElementById('botonDerecho');

        iconoEstado.className = 'bi bi-check-circle text-success display-4';
        mensajeCambioEstado.textContent =
            `¿Estás seguro de que quieres dar de alta el producto ${nombreProducto}?`;

        botonDerecho.className = 'btn btn-success';
        botonDerecho.textContent = 'Dar de Alta';
        botonDerecho.onclick = () =>
            document.getElementById(`formularioAlta${idProducto}`).submit();

        modal.show();
    }

    // Modal Movimiento Stock
    function abrirModalMovimiento(id, nombre, stock) {
        document.getElementById('mov_ProductoId').value = id;
        document.getElementById('mov_NombreProducto').innerText = nombre;
        document.getElementById('mov_StockActual').innerText = stock + " unidades";

        new bootstrap.Modal(
            document.getElementById('modalMovimientoStock')
        ).show();
    }

    // Delegación de eventos
    document.addEventListener('click', function (e) {

        // DAR BAJA
        const btnBaja = e.target.closest('.btn-dar-baja');
        if (btnBaja) {
            darBaja(btnBaja.dataset.id, btnBaja.dataset.nombre);
            return;
        }

        // DAR ALTA
        const btnAlta = e.target.closest('.btn-dar-alta');
        if (btnAlta) {
            darAlta(btnAlta.dataset.id, btnAlta.dataset.nombre);
            return;
        }

        // MOVIMIENTO STOCK
        const btnStock = e.target.closest('.btn-movimiento-stock');
        if (btnStock) {
            abrirModalMovimiento(
                btnStock.dataset.id,
                btnStock.dataset.nombre,
                btnStock.dataset.stock
            );
            return;
        }

        // IMAGEN AMPLIADA
        const imgBtn = e.target.closest('.btn-imagen-producto');
        if (imgBtn) {
            mostrarImagenModal(
                imgBtn.dataset.url,
                imgBtn.dataset.nombre
            );
        }
    });

    // Formulario Movimiento Stock
    const form = document.getElementById('formStock');
    const select = document.getElementById('selectTipo');
    const inputCantidad = document.getElementById('inputCantidad');
    const inputObs = document.getElementById('inputObs');
    const helpCantidad = document.getElementById('helpCantidad');
    const helpObs = document.getElementById('helpObs');

    if (form && select) {

        const configurarFormulario = () => {
            const opcion = select.options[select.selectedIndex];
            const tipo = select.value;

            form.action = opcion.getAttribute('data-url');

            if (tipo === 'Entrada') {
                inputCantidad.setAttribute('min', '1');
                inputObs.removeAttribute('required');
                inputObs.removeAttribute('minlength');

                helpCantidad.innerText = "Ingresá la cantidad a sumar al stock.";
                helpObs.innerText = "Opcional.";
            } else {
                inputCantidad.removeAttribute('min');
                inputObs.setAttribute('required', 'required');
                inputObs.setAttribute('minlength', '10');

                helpCantidad.innerText = "Ingresá la cantidad a sumar o restar.";
                helpObs.innerText = "Requerido (mín. 10 caracteres).";
            }
        };

        select.addEventListener('change', configurarFormulario);
        configurarFormulario();
    }

});