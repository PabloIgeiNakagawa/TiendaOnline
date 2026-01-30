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