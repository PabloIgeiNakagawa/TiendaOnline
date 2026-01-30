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
        totalProductos.textContent = contadorVisible/2 // Dividido dos porque hay 2 vistas una desktop y otra mobile.
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


