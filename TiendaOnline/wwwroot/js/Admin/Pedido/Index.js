document.addEventListener("DOMContentLoaded", () => {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltips = listaTooltips.map((elementoTooltip) => new bootstrap.Tooltip(elementoTooltip))

    // Elementos del DOM
    const campoBusqueda = document.getElementById("campoBusqueda")
    const filtroEstado = document.getElementById("filtroEstado")
    const filtroFecha = document.getElementById("filtroFecha")
    const filtroMonto = document.getElementById("filtroMonto")
    const limpiarFiltros = document.getElementById("limpiarFiltros")
    const filas = document.querySelectorAll(".fila-pedido")
    const totalPedidos = document.getElementById("totalPedidos")
    const montoTotal = document.getElementById("montoTotal")

    // Elementos de estadísticas
    const statsPendientes = document.getElementById("statsPendientes")
    const statsEnviados = document.getElementById("statsEnviados")
    const statsEntregados = document.getElementById("statsEntregados")
    const statsCancelados = document.getElementById("statsCancelados")

    // Funcionalidad de filtrado
    function filtrarTabla() {
        const terminoBusqueda = campoBusqueda.value.toLowerCase().trim()
        const valorEstado = filtroEstado.value
        const valorFecha = filtroFecha.value
        const valorMonto = filtroMonto.value

        let contadorVisible = 0
        let montoTotalVisible = 0
        const contadores = { pendientes: 0, enviados: 0, entregados: 0, cancelados: 0 }

        filas.forEach((fila) => {
            const id = fila.dataset.id || ""
            const cliente = fila.dataset.cliente || ""
            const estado = fila.dataset.estado
            const fecha = fila.dataset.fecha
            const total = Number.parseFloat(fila.dataset.total) || 0

            // Verificar coincidencias
            const coincideBusqueda = !terminoBusqueda || id.includes(terminoBusqueda) || cliente.includes(terminoBusqueda)

            const coincideEstado = !valorEstado || estado === valorEstado
            const coincideFecha = !valorFecha || verificarFiltroFecha(fecha, valorFecha)
            const coincideMonto = !valorMonto || verificarFiltroMonto(total, valorMonto)

            // Mostrar/ocultar fila
            if (coincideBusqueda && coincideEstado && coincideFecha && coincideMonto) {
                fila.style.display = ""
                contadorVisible++
                montoTotalVisible += total

                // Contar por estado
                switch (estado) {
                    case "0":
                        contadores.pendientes++
                        break
                    case "1":
                        contadores.enviados++
                        break
                    case "2":
                        contadores.entregados++
                        break
                    case "3":
                        contadores.cancelados++
                        break
                }
            } else {
                fila.style.display = "none"
            }
        })

        // Actualizar contadores
        totalPedidos.textContent = contadorVisible
        montoTotal.textContent = montoTotalVisible.toLocaleString("es-AR", {
            style: "currency",
            currency: "ARS",
            minimumFractionDigits: 0,
        })

        // Actualizar estadísticas
        statsPendientes.textContent = contadores.pendientes
        statsEnviados.textContent = contadores.enviados
        statsEntregados.textContent = contadores.entregados
        statsCancelados.textContent = contadores.cancelados

        // Mostrar mensaje si no hay resultados
        mostrarMensajeSinResultados(contadorVisible === 0)
    }

    // Verificar filtro de fecha
    function verificarFiltroFecha(fechaPedido, filtro) {
        const fecha = new Date(fechaPedido)
        const hoy = new Date()
        const ayer = new Date(hoy)
        ayer.setDate(hoy.getDate() - 1)

        switch (filtro) {
            case "hoy":
                return fecha.toDateString() === hoy.toDateString()
            case "ayer":
                return fecha.toDateString() === ayer.toDateString()
            case "semana":
                const inicioSemana = new Date(hoy)
                inicioSemana.setDate(hoy.getDate() - hoy.getDay())
                return fecha >= inicioSemana
            case "mes":
                return fecha.getMonth() === hoy.getMonth() && fecha.getFullYear() === hoy.getFullYear()
            case "trimestre":
                const trimestreActual = Math.floor(hoy.getMonth() / 3)
                const trimestreFecha = Math.floor(fecha.getMonth() / 3)
                return trimestreFecha === trimestreActual && fecha.getFullYear() === hoy.getFullYear()
            default:
                return true
        }
    }

    // Verificar filtro de monto
    function verificarFiltroMonto(total, filtro) {
        switch (filtro) {
            case "bajo":
                return total < 50000
            case "medio":
                return total >= 50000 && total <= 200000
            case "alto":
                return total > 200000
            default:
                return true
        }
    }

    // Función para mostrar mensaje cuando no hay resultados
    function mostrarMensajeSinResultados(mostrar) {
        const mensajeExistente = document.getElementById("mensajeSinResultados")

        if (mostrar && !mensajeExistente) {
            const tbody = document.querySelector("#tablaPedidos tbody")
            const mensaje = document.createElement("tr")
            mensaje.id = "mensajeSinResultados"
            mensaje.innerHTML = `
                <td colspan="7" class="text-center py-5">
                  <div class="text-muted">
                    <i class="bi bi-search display-4 mb-3"></i>
                    <h5>No se encontraron pedidos</h5>
                    <p>Intenta ajustar los filtros de búsqueda</p>
                  </div>
                </td>
              `
            tbody.appendChild(mensaje)
        } else if (!mostrar && mensajeExistente) {
            mensajeExistente.remove()
        }
    }

    // Event listeners para filtros
    campoBusqueda.addEventListener("input", debounce(filtrarTabla, 300))
    filtroEstado.addEventListener("change", filtrarTabla)
    filtroFecha.addEventListener("change", filtrarTabla)
    filtroMonto.addEventListener("change", filtrarTabla)

    // Limpiar filtros
    limpiarFiltros.addEventListener("click", function () {
        campoBusqueda.value = ""
        filtroEstado.value = ""
        filtroFecha.value = ""
        filtroMonto.value = ""
        filtrarTabla()

        // Feedback visual
        this.classList.add("btn-success")
        setTimeout(() => {
            this.classList.remove("btn-success")
        }, 1000)
    })

    // Funcionalidad de ordenamiento
    const encabezadosOrdenables = document.querySelectorAll(".ordenable")
    const ordenActual = { columna: -1, direccion: "asc" }

    encabezadosOrdenables.forEach((encabezado) => {
        encabezado.addEventListener("click", function () {
            const columna = Number.parseInt(this.dataset.columna)
            const cuerpoTabla = document.querySelector("#tablaPedidos tbody")
            const filasVisibles = Array.from(
                cuerpoTabla.querySelectorAll('tr:not([style*="display: none"]):not(#mensajeSinResultados)'),
            )

            // Actualizar dirección de ordenamiento
            if (ordenActual.columna === columna) {
                ordenActual.direccion = ordenActual.direccion === "asc" ? "desc" : "asc"
            } else {
                ordenActual.direccion = "asc"
            }
            ordenActual.columna = columna

            // Actualizar iconos de encabezado
            encabezadosOrdenables.forEach((h) => {
                const icono = h.querySelector(".icono-orden")
                icono.className = "bi bi-chevron-expand ms-auto icono-orden"
            })

            const iconoActual = this.querySelector(".icono-orden")
            iconoActual.className = `bi bi-chevron-${ordenActual.direccion === "asc" ? "up" : "down"} ms-auto icono-orden`

            // Ordenar filas
            filasVisibles.sort((a, b) => {
                let valorA, valorB

                switch (columna) {
                    case 0: // ID
                        valorA = Number.parseInt(a.dataset.id) || 0
                        valorB = Number.parseInt(b.dataset.id) || 0
                        break
                    case 1: // Cliente
                        valorA = a.dataset.cliente
                        valorB = b.dataset.cliente
                        break
                    case 2: // Fecha Pedido
                    case 3: // Fecha Entrega
                        valorA = new Date(a.dataset.fecha)
                        valorB = new Date(b.dataset.fecha)
                        break
                    case 4: // Total
                        valorA = Number.parseFloat(a.dataset.total) || 0
                        valorB = Number.parseFloat(b.dataset.total) || 0
                        break
                    case 5: // Estado
                        valorA = Number.parseInt(a.dataset.estado) || 0
                        valorB = Number.parseInt(b.dataset.estado) || 0
                        break
                    default:
                        valorA = a.cells[columna].textContent.trim().toLowerCase()
                        valorB = b.cells[columna].textContent.trim().toLowerCase()
                }

                if (valorA instanceof Date) {
                    return ordenActual.direccion === "asc" ? valorA - valorB : valorB - valorA
                } else if (typeof valorA === "number") {
                    return ordenActual.direccion === "asc" ? valorA - valorB : valorB - valorA
                } else {
                    return ordenActual.direccion === "asc" ? valorA.localeCompare(valorB) : valorB.localeCompare(valorA)
                }
            })

            // Reordenar filas en el DOM
            const mensajeSinResultados = document.getElementById("mensajeSinResultados")
            if (mensajeSinResultados) {
                mensajeSinResultados.remove()
            }

            filasVisibles.forEach((fila) => cuerpoTabla.appendChild(fila))

            if (mensajeSinResultados) {
                cuerpoTabla.appendChild(mensajeSinResultados)
            }
        })
    })
})

// Función debounce para optimizar búsqueda
function debounce(func, wait) {
    let timeout
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout)
            func(...args)
        }
        clearTimeout(timeout)
        timeout = setTimeout(later, wait)
    }
}

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

// Actualizar pedidos
function actualizarPedidos() {
    const boton = document.querySelector('[onclick="actualizarPedidos()"]')
    const textoOriginal = boton.innerHTML

    boton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Actualizando...'
    boton.disabled = true

    // Simular actualización (reemplazar con llamada real)
    setTimeout(() => {
        location.reload()
    }, 1000)
}

// Exportar a CSV
function exportarACSV() {
    const tabla = document.getElementById("tablaPedidos")
    const filasVisibles = tabla.querySelectorAll('tbody tr:not([style*="display: none"]):not(#mensajeSinResultados)')

    if (filasVisibles.length === 0) {
        alert("No hay pedidos para exportar")
        return
    }

    const separador = detectarSeparadorCSV()
    const csv = []

    // Encabezados
    const encabezados = ["ID Pedido", "Cliente", "Email", "Fecha Pedido", "Fecha Entrega", "Total", "Estado"]
    csv.push(encabezados.map((h) => limpiarDatoCSV(h)).join(separador))

    // Filas de datos
    filasVisibles.forEach((fila) => {
        const celdas = fila.querySelectorAll("td")
        if (celdas.length >= 6) {
            const id = fila.dataset.id
            const clienteInfo = celdas[1].querySelector(".cliente-info")
            const cliente = clienteInfo.querySelector(".fw-semibold").textContent.trim()
            const email = clienteInfo.querySelector("small").textContent.trim()
            const fechaPedido = celdas[2].querySelector(".fw-semibold").textContent.trim()
            const fechaEntrega = celdas[3].textContent.trim()
            const total = celdas[4].textContent.trim()
            const estado = celdas[5].textContent.trim()

            const datosFila = [
                limpiarDatoCSV(id),
                limpiarDatoCSV(cliente),
                limpiarDatoCSV(email),
                limpiarDatoCSV(fechaPedido),
                limpiarDatoCSV(fechaEntrega),
                limpiarDatoCSV(total),
                limpiarDatoCSV(estado),
            ]

            csv.push(datosFila.join(separador))
        }
    })

    // Crear y descargar archivo
    const BOM = "\uFEFF"
    const contenidoCSV = BOM + csv.join("\r\n")
    const blob = new Blob([contenidoCSV], { type: "text/csv;charset=utf-8;" })
    const enlace = document.createElement("a")
    const url = URL.createObjectURL(blob)

    const ahora = new Date()
    const fecha = ahora.toISOString().split("T")[0]
    const hora = ahora.toTimeString().split(" ")[0].replace(/:/g, "-")
    const nombreArchivo = `pedidos_${fecha}_${hora}.csv`

    enlace.setAttribute("href", url)
    enlace.setAttribute("download", nombreArchivo)
    enlace.style.visibility = "hidden"

    document.body.appendChild(enlace)
    enlace.click()
    document.body.removeChild(enlace)

    URL.revokeObjectURL(url)

    // Feedback visual
    const botonExportar = document.querySelector('[onclick="exportarACSV()"]')
    const textoOriginal = botonExportar.innerHTML

    botonExportar.innerHTML = '<i class="bi bi-check-circle me-2"></i>Exportado'
    botonExportar.classList.add("btn-success")
    botonExportar.classList.remove("btn-outline-primary")

    setTimeout(() => {
        botonExportar.innerHTML = textoOriginal
        botonExportar.classList.remove("btn-success")
        botonExportar.classList.add("btn-outline-primary")
    }, 2000)
}

// Funciones auxiliares para CSV
function detectarSeparadorCSV() {
    const locale = navigator.language || navigator.userLanguage || "en-US"
    const paisesConComaDecimal = ["es", "es-ES", "es-CO", "es-MX", "es-AR", "fr", "de", "it", "pt", "pt-BR"]
    const usaComaDecimal = paisesConComaDecimal.some((pais) => locale.startsWith(pais))
    return usaComaDecimal ? ";" : ","
}

function limpiarDatoCSV(dato) {
    if (!dato) return '""'
    let limpio = dato.toString().trim()
    limpio = limpio.replace(/[\r\n]+/g, " ")
    limpio = limpio.replace(/\s+/g, " ")
    limpio = limpio.replace(/"/g, '""')
    return `"${limpio}"`
}
