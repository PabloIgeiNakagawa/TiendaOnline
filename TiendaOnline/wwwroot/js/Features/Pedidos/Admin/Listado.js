document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("click", function (e) {

        const btn = e.target.closest(".btn-cambio-estado");
        if (!btn) return;

        const pedidoId = btn.dataset.id;
        const accion = btn.dataset.accion;

        mostrarModalCambioEstado(pedidoId, accion);
    });

});

function mostrarModalCambioEstado(pedidoId, accion) {

    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"));
    const mensaje = document.getElementById("mensajeCambioEstado");
    const boton = document.getElementById("botonDerecho");
    const icono = document.getElementById("iconoEstado");

    const numeroFormateado = pedidoId.toString().padStart(6, "0");

    // Configuración según acción
    const config = {
        enviar: {
            mensaje: `¿Confirmas que el pedido #${numeroFormateado} ha sido enviado?`,
            icono: "bi bi-truck text-info display-4",
            botonClase: "btn btn-info",
            texto: "Marcar como Enviado",
            formId: `enviarPedido${pedidoId}`
        },
        entregar: {
            mensaje: `¿Confirmas que el pedido #${numeroFormateado} ha sido entregado?`,
            icono: "bi bi-check-circle text-success display-4",
            botonClase: "btn btn-success",
            texto: "Marcar como Entregado",
            formId: `entregarPedido${pedidoId}`
        },
        cancelar: {
            mensaje: `¿Estás seguro de cancelar el pedido #${numeroFormateado}?`,
            icono: "bi bi-x-circle text-danger display-4",
            botonClase: "btn btn-danger",
            texto: "Cancelar Pedido",
            formId: `cancelarPedido${pedidoId}`
        }
    };

    const data = config[accion];
    if (!data) return;

    mensaje.textContent = data.mensaje;
    icono.className = data.icono;
    boton.className = data.botonClase;
    boton.textContent = data.texto;

    // Limpiar listeners anteriores
    boton.replaceWith(boton.cloneNode(true));
    const nuevoBoton = document.getElementById("botonDerecho");

    nuevoBoton.addEventListener("click", function () {

        nuevoBoton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...';
        nuevoBoton.disabled = true;

        document.getElementById(data.formId).submit();

    }, { once: true });

    modal.show();
}