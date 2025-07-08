document.addEventListener("DOMContentLoaded", () => {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltips = listaTooltips.map((elementoTooltip) => new bootstrap.Tooltip(elementoTooltip))
})

// Cambiar estado del pedido (Solo Admin)
function cambiarEstado(pedidoId, nuevoEstado, nombreEstado) {
    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"))
    const mensajeCambioEstado = document.getElementById("mensajeCambioEstado")
    const botonConfirmarEstado = document.getElementById("botonConfirmarEstado")
    const iconoEstado = document.getElementById("iconoEstado")

    // Configurar modal según el estado
    const configuraciones = {
        1: {
            // Enviado
            mensaje: `¿Confirmas que el pedido #${pedidoId.toString().padStart(6, "0")} ha sido enviado?`,
            icono: "bi bi-truck text-info display-4",
            boton: "btn btn-info",
            texto: "Marcar como Enviado",
        },
        2: {
            // Entregado
            mensaje: `¿Confirmas que el pedido #${pedidoId.toString().padStart(6, "0")} ha sido entregado?`,
            icono: "bi bi-check-circle text-success display-4",
            boton: "btn btn-success",
            texto: "Marcar como Entregado",
        },
        3: {
            // Cancelado
            mensaje: `¿Estás seguro de cancelar el pedido #${pedidoId.toString().padStart(6, "0")}?`,
            icono: "bi bi-x-circle text-danger display-4",
            boton: "btn btn-danger",
            texto: "Cancelar Pedido",
        },
    }

    const config = configuraciones[nuevoEstado]
    mensajeCambioEstado.textContent = config.mensaje
    iconoEstado.className = config.icono
    botonConfirmarEstado.className = config.boton
    botonConfirmarEstado.textContent = config.texto

    botonConfirmarEstado.onclick = () => {
        // Mostrar loading
        botonConfirmarEstado.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...'
        botonConfirmarEstado.disabled = true

        // Establecer el nuevo estado y enviar formulario
        document.getElementById(`nuevoEstado${pedidoId}`).value = nuevoEstado
        document.getElementById(`formularioCambioEstado${pedidoId}`).submit()
    }

    modal.show()
}

// Cancelar pedido (Solo Usuario)
function cancelarPedidoUsuario(pedidoId) {
    const modal = new bootstrap.Modal(document.getElementById("modalCancelarPedido"))
    const botonCancelarPedido = document.getElementById("botonCancelarPedido")

    botonCancelarPedido.onclick = () => {
        // Mostrar loading
        botonCancelarPedido.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Cancelando...'
        botonCancelarPedido.disabled = true

        // Enviar formulario
        document.getElementById(`formularioCancelarPedido${pedidoId}`).submit()
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
