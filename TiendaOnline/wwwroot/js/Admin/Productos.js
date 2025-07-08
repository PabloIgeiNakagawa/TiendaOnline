document.addEventListener("DOMContentLoaded", () => {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltips = listaTooltips.map((elementoTooltip) => new bootstrap.Tooltip(elementoTooltip))

    // Elementos del DOM
    const campoBusqueda = document.getElementById("campoBusqueda")
    const filtroCategoria = document.getElementById("filtroCategoria")
    const filtroStock = document.getElementById("filtroStock")
    const filtroEstado = document.getElementById("filtroEstado")
    const limpiarFiltros = document.getElementById("limpiarFiltros")
    const filas = document.querySelectorAll(".fila-producto")
    const totalProductos = document.getElementById("totalProductos")
    const contadorActivos = document.getElementById("contadorActivos")
    const contadorInactivos = document.getElementById("contadorInactivos")

    // Funcionalidad de filtrado
    function filtrarTabla() {
        const terminoBusqueda = campoBusqueda.value.toLowerCase().trim()
        const valorCategoria = filtroCategoria.value
        const valorStock = filtroStock.value
        const valorEstado = filtroEstado.value
        let contadorVisible = 0
        let contadorActivosVisible = 0
        let contadorInactivosVisible = 0

        filas.forEach((fila) => {
            const nombre = fila.dataset.nombre || ""
            const descripcion = fila.dataset.descripcion || ""
            const categoria = fila.dataset.categoria
            const stock = fila.dataset.stock
            const estado = fila.dataset.estado

            // Verificar coincidencias
            const coincideBusqueda =
                !terminoBusqueda || nombre.includes(terminoBusqueda) || descripcion.includes(terminoBusqueda)

            const coincideCategoria = !valorCategoria || categoria === valorCategoria
            const coincideStock = !valorStock || stock === valorStock
            const coincideEstado = !valorEstado || estado === valorEstado

            // Mostrar/ocultar fila
            if (coincideBusqueda && coincideCategoria && coincideStock && coincideEstado) {
                fila.style.display = ""
                contadorVisible++

                // Contar activos e inactivos visibles
                if (estado === "activo") {
                    contadorActivosVisible++
                } else {
                    contadorInactivosVisible++
                }
            } else {
                fila.style.display = "none"
            }
        })

        // Actualizar contadores
        totalProductos.textContent = contadorVisible
        contadorActivos.textContent = contadorActivosVisible
        contadorInactivos.textContent = contadorInactivosVisible

        // Mostrar mensaje si no hay resultados
        mostrarMensajeSinResultados(contadorVisible === 0)
    }

    // Función para mostrar mensaje cuando no hay resultados
    function mostrarMensajeSinResultados(mostrar) {
        const mensajeExistente = document.getElementById("mensajeSinResultados")

        if (mostrar && !mensajeExistente) {
            const tbody = document.querySelector("#tablaProductos tbody")
            const mensaje = document.createElement("tr")
            mensaje.id = "mensajeSinResultados"
            mensaje.innerHTML = `
                <td colspan="6" class="text-center py-5">
                    <div class="text-muted">
                        <i class="bi bi-search display-4 mb-3"></i>
                        <h5>No se encontraron productos</h5>
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
    filtroCategoria.addEventListener("change", filtrarTabla)
    filtroStock.addEventListener("change", filtrarTabla)
    filtroEstado.addEventListener("change", filtrarTabla)

    // Limpiar filtros
    limpiarFiltros.addEventListener("click", function () {
        campoBusqueda.value = ""
        filtroCategoria.value = ""
        filtroStock.value = ""
        filtroEstado.value = ""
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
            const cuerpoTabla = document.querySelector("#tablaProductos tbody")
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

                if (columna === 2) {
                    // Precio
                    valorA = Number.parseFloat(a.dataset.precio) || 0
                    valorB = Number.parseFloat(b.dataset.precio) || 0
                } else if (columna === 3) {
                    // Stock
                    const stockA = a.querySelector("td:nth-child(4)").textContent.match(/\d+/)
                    const stockB = b.querySelector("td:nth-child(4)").textContent.match(/\d+/)
                    valorA = stockA ? Number.parseInt(stockA[0]) : 0
                    valorB = stockB ? Number.parseInt(stockB[0]) : 0
                } else if (columna === 4) {
                    // Estado
                    valorA = a.dataset.estado === "activo" ? 1 : 0
                    valorB = b.dataset.estado === "activo" ? 1 : 0
                } else {
                    valorA = a.cells[columna].textContent.trim().toLowerCase()
                    valorB = b.cells[columna].textContent.trim().toLowerCase()
                }

                if (typeof valorA === "number") {
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

// Modal de confirmación
function confirmarAccion(idProducto, accion, nombreProducto) {
    const modal = new bootstrap.Modal(document.getElementById('modalConfirmacion'));
    const iconoConfirmacion = document.getElementById('iconoConfirmacion');
    const mensajeConfirmacion = document.getElementById('mensajeConfirmacion');
    const botonConfirmar = document.getElementById('botonConfirmar');

    if (accion === 'baja') {
        iconoConfirmacion.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de baja el producto ${nombreProducto}? El producto no estará disponible para la venta.`;
        botonConfirmar.className = 'btn btn-danger';
        botonConfirmar.textContent = 'Dar de Baja';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioBaja${idProducto}`).submit();
        };
    } else {
        iconoConfirmacion.className = 'bi bi-check-circle text-success display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de alta el producto ${nombreProducto}? El producto estará disponible para la venta.`;
        botonConfirmar.className = 'btn btn-success';
        botonConfirmar.textContent = 'Dar de Alta';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioAlta${idProducto}`).submit();
        };
    }

    modal.show();
}

// Función para detectar el separador CSV según la configuración regional
function detectarSeparadorCSV() {
    // Detectar configuración regional del navegador
    const locale = navigator.language || navigator.userLanguage || "en-US"

    // En países que usan coma como separador decimal, Excel espera punto y coma como separador CSV
    const paisesConComaDecimal = ["es", "es-ES", "es-CO", "es-MX", "es-AR", "fr", "de", "it", "pt", "pt-BR"]

    const usaComaDecimal = paisesConComaDecimal.some((pais) => locale.startsWith(pais))

    return usaComaDecimal ? ";" : ","
}

// Función para limpiar y formatear datos CSV
function limpiarDatoCSV(dato) {
    if (!dato) return '""'

    // Convertir a string y limpiar
    let limpio = dato.toString().trim()

    // Remover saltos de línea y caracteres especiales
    limpio = limpio.replace(/[\r\n]+/g, " ")
    limpio = limpio.replace(/\s+/g, " ")

    // Escapar comillas dobles
    limpio = limpio.replace(/"/g, '""')

    // Siempre envolver en comillas para mayor compatibilidad
    return `"${limpio}"`
}

// Exportar a CSV mejorado
function exportarACSV() {
    const tabla = document.getElementById("tablaProductos")
    const filasVisibles = tabla.querySelectorAll('tbody tr:not([style*="display: none"]):not(#mensajeSinResultados)')

    if (filasVisibles.length === 0) {
        alert("No hay productos para exportar")
        return
    }

    // Detectar separador apropiado
    const separador = detectarSeparadorCSV()
    const csv = []

    // Encabezados
    const encabezados = ["Producto", "Descripción", "Categoría", "Precio", "Stock"]
    csv.push(encabezados.map((h) => limpiarDatoCSV(h)).join(separador))

    // Filas de datos
    filasVisibles.forEach((fila) => {
        const celdas = fila.querySelectorAll("td")
        if (celdas.length >= 4) {
            // Extraer datos limpios
            const nombre = fila.dataset.nombre || ""
            const descripcion = fila.dataset.descripcion || ""

            // Limpiar categoría (remover HTML)
            const categoriaElement = celdas[1].querySelector(".badge") || celdas[1]
            const categoria = categoriaElement.textContent.trim()

            // Limpiar precio (remover formato de moneda)
            const precioTexto = celdas[2].textContent.trim()
            const precio = precioTexto.replace(/[^\d.,]/g, "").replace(",", ".")

            // Limpiar stock (extraer solo números)
            const stockTexto = celdas[3].textContent.trim()
            const stockMatch = stockTexto.match(/(\d+)/)
            const stock = stockMatch ? stockMatch[1] : "0"

            const datosFila = [
                limpiarDatoCSV(nombre),
                limpiarDatoCSV(descripcion),
                limpiarDatoCSV(categoria),
                limpiarDatoCSV(precio),
                limpiarDatoCSV(stock),
            ]

            csv.push(datosFila.join(separador))
        }
    })

    // Crear contenido CSV con BOM para UTF-8
    const BOM = "\uFEFF"
    const contenidoCSV = BOM + csv.join("\r\n")

    // Crear blob con tipo MIME específico para CSV
    const blob = new Blob([contenidoCSV], {
        type: "text/csv;charset=utf-8;",
    })

    // Crear enlace de descarga
    const enlace = document.createElement("a")
    const url = URL.createObjectURL(blob)

    // Nombre de archivo con fecha y hora
    const ahora = new Date()
    const fecha = ahora.toISOString().split("T")[0]
    const hora = ahora.toTimeString().split(" ")[0].replace(/:/g, "-")
    const nombreArchivo = `productos_${fecha}_${hora}.csv`

    enlace.setAttribute("href", url)
    enlace.setAttribute("download", nombreArchivo)
    enlace.style.visibility = "hidden"

    document.body.appendChild(enlace)
    enlace.click()
    document.body.removeChild(enlace)

    // Limpiar URL del objeto
    URL.revokeObjectURL(url)

    // Feedback visual mejorado
    const botonExportar = document.querySelector('[onclick="exportarACSV()"]')
    const textoOriginal = botonExportar.innerHTML

    botonExportar.innerHTML = '<i class="bi bi-check-circle me-1"></i>¡Exportado!'
    botonExportar.classList.add("btn-success")
    botonExportar.classList.remove("btn-outline-primary")

    setTimeout(() => {
        botonExportar.innerHTML = textoOriginal
        botonExportar.classList.remove("btn-success")
        botonExportar.classList.add("btn-outline-primary")
    }, 3000)

    // Mostrar notificación adicional
    mostrarNotificacionExportacion(nombreArchivo, filasVisibles.length)
}

// Función para mostrar notificación de exportación exitosa
function mostrarNotificacionExportacion(nombreArchivo, cantidadFilas) {
    // Crear toast de notificación
    const toast = document.createElement("div")
    toast.className = "toast align-items-center text-white bg-success border-0 position-fixed"
    toast.style.cssText = "top: 20px; right: 20px; z-index: 1055; min-width: 300px;"
    toast.setAttribute("role", "alert")
    toast.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">
        <div class="d-flex align-items-center">
          <i class="bi bi-download me-2"></i>
          <div>
            <strong>Exportación exitosa</strong><br>
            <small>${cantidadFilas} productos exportados</small><br>
            <small class="text-white-50">${nombreArchivo}</small>
          </div>
        </div>
      </div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>
  `

    document.body.appendChild(toast)
    const bsToast = new bootstrap.Toast(toast, { delay: 5000 })
    bsToast.show()

    toast.addEventListener("hidden.bs.toast", () => {
        toast.remove()
    })
}
