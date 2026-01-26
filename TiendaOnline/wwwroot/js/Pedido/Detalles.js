document.addEventListener("DOMContentLoaded", () => {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltips = listaTooltips.map((elementoTooltip) => new bootstrap.Tooltip(elementoTooltip))
})
function enviarPedido(pedidoId) {
    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"));
    const mensajeCambioEstado = document.getElementById("mensajeCambioEstado");
    const botonDerecho = document.getElementById("botonDerecho");
    const iconoEstado = document.getElementById("iconoEstado");
    mensajeCambioEstado.textContent = `¿Confirmas que el pedido #${pedidoId.toString().padStart(6, "0")} ha sido enviado?`;
    iconoEstado.className = "bi bi-truck text-info display-4";
    botonDerecho.className = "btn btn-info";
    botonDerecho.textContent = "Marcar como Enviado";
    botonDerecho.onclick = () => {
        // Mostrar loading
        botonDerecho.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...'
        botonDerecho.disabled = true

        // Enviar formulario
        document.getElementById(`enviarPedido${pedidoId}`).submit()
    }

    modal.show()
}

function entregarPedido(pedidoId) {
    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"));
    const mensajeCambioEstado = document.getElementById("mensajeCambioEstado");
    const botonDerecho = document.getElementById("botonDerecho");
    const iconoEstado = document.getElementById("iconoEstado");
    mensajeCambioEstado.textContent = `¿Confirmas que el pedido #${pedidoId.toString().padStart(6, "0")} ha sido entregado?`;
    iconoEstado.className = "bi bi-check-circle text-success display-4";
    botonDerecho.className = "btn btn-success";
    botonDerecho.textContent = "Marcar como Entregado";
    botonDerecho.onclick = () => {
        // Mostrar loading
        botonDerecho.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...'
        botonDerecho.disabled = true

        // Enviar formulario
        document.getElementById(`entregarPedido${pedidoId}`).submit()
    }

    modal.show()
}

function cancelarPedido(pedidoId) {
    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"));
    const mensajeCambioEstado = document.getElementById("mensajeCambioEstado");
    const botonDerecho = document.getElementById("botonDerecho");
    const iconoEstado = document.getElementById("iconoEstado");
    mensajeCambioEstado.textContent = `¿Estás seguro de cancelar el pedido #${pedidoId.toString().padStart(6, "0")}?`;
    iconoEstado.className = "bi bi-x-circle text-danger display-4";
    botonDerecho.className = "btn btn-danger";
    botonDerecho.textContent = "Cancelar Pedido";
    botonDerecho.onclick = () => {
        // Mostrar loading
        botonDerecho.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...'
        botonDerecho.disabled = true

        // Enviar formulario
        document.getElementById(`cancelarPedido${pedidoId}`).submit()
    }

    modal.show()
}

// Imprimir pedido
function imprimirPedido() {
    window.print()
}

// Exportar a PDF (Solo Admin)
function exportarPDF() {
    const boton = document.querySelector('[onclick="exportarPDF()"]')
    if (!boton) return

    const textoOriginal = boton.innerHTML

    boton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Generando PDF...'
    boton.disabled = true

    // Simular generación de PDF (reemplazar con lógica real)
    setTimeout(() => {
        boton.innerHTML = '<i class="bi bi-check-circle me-2"></i>PDF Generado'
        boton.classList.add("btn-success")
        boton.classList.remove("btn-outline-primary")

        // Simular descarga
        const pedidoId = document.querySelector(".breadcrumb-item.active").textContent.trim().replace("Pedido #", "")
        const ahora = new Date()
        const fecha = ahora.toISOString().split("T")[0]
        const nombreArchivo = `pedido_${pedidoId}_${fecha}.pdf`

        // Crear un enlace falso para simular la descarga
        const enlace = document.createElement("a")
        enlace.href = "#"
        enlace.download = nombreArchivo
        enlace.click()

        // Restaurar botón después de un tiempo
        setTimeout(() => {
            boton.innerHTML = textoOriginal
            boton.classList.remove("btn-success")
            boton.classList.add("btn-outline-primary")
            boton.disabled = false
        }, 2000)
    }, 1500)
}

// Copiar número de seguimiento
function copiarSeguimiento() {
    const numeroSeguimiento = document.querySelector("code").textContent
    navigator.clipboard
        .writeText(numeroSeguimiento)
        .then(() => {
            // Mostrar feedback visual
            const boton = document.querySelector('[onclick="copiarSeguimiento()"]')
            const iconoOriginal = boton.innerHTML
            boton.innerHTML = '<i class="bi bi-check"></i>'
            boton.classList.add("btn-success")
            boton.classList.remove("btn-outline-primary")

            setTimeout(() => {
                boton.innerHTML = iconoOriginal
                boton.classList.remove("btn-success")
                boton.classList.add("btn-outline-primary")
            }, 2000)
        })
        .catch(() => {
            alert("No se pudo copiar el número de seguimiento")
        })
}
