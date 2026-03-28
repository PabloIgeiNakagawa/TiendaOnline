document.addEventListener("DOMContentLoaded", () => {
    const modalElement = document.getElementById("modalCambioEstado");
    if (!modalElement) return;

    const modal = new bootstrap.Modal(modalElement);
    const mensaje = document.getElementById("mensajeCambioEstado");
    const botonDerecho = document.getElementById("botonDerecho");
    const icono = document.getElementById("iconoEstado");

    function configurarAccion(pedidoId, config) {

        mensaje.textContent = config.mensaje(pedidoId);
        icono.className = config.icono;
        botonDerecho.className = config.botonClase;
        botonDerecho.textContent = config.texto;

        // Limpiar eventos anteriores
        botonDerecho.replaceWith(botonDerecho.cloneNode(true));
        const nuevoBoton = document.getElementById("botonDerecho");

        nuevoBoton.addEventListener("click", () => {
            nuevoBoton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...';
            nuevoBoton.disabled = true;
            document.getElementById(`${config.formPrefix}${pedidoId}`).submit();
        });

        modal.show();
    }

    // Enviar
    document.querySelectorAll(".btn-enviar").forEach(btn => {
        btn.addEventListener("click", function () {
            const id = this.dataset.pedidoId;
            configurarAccion(id, {
                mensaje: (id) => `¿Confirmas que el pedido #${id.padStart(6, "0")} ha sido enviado?`,
                icono: "bi bi-truck text-info display-4",
                botonClase: "btn btn-info",
                texto: "Marcar como Enviado",
                formPrefix: "enviarPedido"
            });
        });
    });

    // Entregar
    document.querySelectorAll(".btn-entregar").forEach(btn => {
        btn.addEventListener("click", function () {
            const id = this.dataset.pedidoId;
            configurarAccion(id, {
                mensaje: (id) => `¿Confirmas que el pedido #${id.padStart(6, "0")} ha sido entregado?`,
                icono: "bi bi-check-circle text-success display-4",
                botonClase: "btn btn-success",
                texto: "Marcar como Entregado",
                formPrefix: "entregarPedido"
            });
        });
    });

    // Cancelar
    document.querySelectorAll(".btn-cancelar").forEach(btn => {
        btn.addEventListener("click", function () {
            const id = this.dataset.pedidoId;
            configurarAccion(id, {
                mensaje: (id) => `¿Estás seguro de cancelar el pedido #${id.padStart(6, "0")}?`,
                icono: "bi bi-x-circle text-danger display-4",
                botonClase: "btn btn-danger",
                texto: "Cancelar Pedido",
                formPrefix: "cancelarPedido"
            });
        });
    });

    // Copiar seguimiento
    const btnCopiar = document.getElementById("btnCopiarSeguimiento");

    btnCopiar?.addEventListener("click", () => {

        const numeroSeguimiento = document.querySelector("code")?.textContent;
        if (!numeroSeguimiento) return;

        navigator.clipboard.writeText(numeroSeguimiento)
            .then(() => {

                const iconoOriginal = btnCopiar.innerHTML;

                btnCopiar.innerHTML = '<i class="bi bi-check"></i>';
                btnCopiar.classList.add("btn-success");
                btnCopiar.classList.remove("btn-outline-primary");

                setTimeout(() => {
                    btnCopiar.innerHTML = iconoOriginal;
                    btnCopiar.classList.remove("btn-success");
                    btnCopiar.classList.add("btn-outline-primary");
                }, 2000);
            })
            .catch(() => alert("No se pudo copiar el número de seguimiento"));
    });

});