document.addEventListener('DOMContentLoaded', function () {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltips = listaTooltips.map(function (elementoTooltip) {
        return new bootstrap.Tooltip(elementoTooltip);
    });

    // Variables para filtros
    const filtroUsuario = document.getElementById('filtroUsuario');
    const filtroAccion = document.getElementById('filtroAccion');
    const filtroFechaDesde = document.getElementById('filtroFechaDesde');
    const filtroFechaHasta = document.getElementById('filtroFechaHasta');
    const limpiarFiltros = document.getElementById('limpiarFiltros');
    const filas = document.querySelectorAll('.fila-auditoria');
    const registrosVisibles = document.getElementById('registrosVisibles');

    // Función para filtrar registros
    function filtrarRegistros() {
        const valorUsuario = filtroUsuario.value;
        const valorAccion = filtroAccion.value;
        const fechaDesde = filtroFechaDesde.value ? new Date(filtroFechaDesde.value) : null;
        const fechaHasta = filtroFechaHasta.value ? new Date(filtroFechaHasta.value) : null;
        let contadorVisible = 0;

        filas.forEach(fila => {
            const usuario = fila.dataset.usuario;
            const accion = fila.dataset.accion;
            const fecha = new Date(fila.dataset.fecha);

            const coincideUsuario = !valorUsuario || usuario === valorUsuario;
            const coincideAccion = !valorAccion || accion === valorAccion;
            const coincideFechaDesde = !fechaDesde || fecha >= fechaDesde;
            const coincideFechaHasta = !fechaHasta || fecha <= fechaHasta;

            if (coincideUsuario && coincideAccion && coincideFechaDesde && coincideFechaHasta) {
                fila.style.display = '';
                contadorVisible++;
            } else {
                fila.style.display = 'none';
            }
        });

        registrosVisibles.textContent = contadorVisible;
    }

    [filtroUsuario, filtroAccion, filtroFechaDesde, filtroFechaHasta].forEach(el => {
        el.addEventListener('input', filtrarRegistros);
    });

    limpiarFiltros.addEventListener('click', function () {
        filtroUsuario.value = '';
        filtroAccion.value = '';
        filtroFechaDesde.value = '';
        filtroFechaHasta.value = '';
        filtrarRegistros();
    });

    // Filtros por período
    document.querySelectorAll('[data-periodo]').forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            const periodo = this.dataset.periodo;
            const hoy = new Date();

            switch (periodo) {
                case 'hoy':
                    filtroFechaDesde.value = hoy.toISOString().split('T')[0];
                    filtroFechaHasta.value = hoy.toISOString().split('T')[0];
                    break;
                case 'semana':
                    const semanaAtras = new Date(hoy.getTime() - 7 * 24 * 60 * 60 * 1000);
                    filtroFechaDesde.value = semanaAtras.toISOString().split('T')[0];
                    filtroFechaHasta.value = hoy.toISOString().split('T')[0];
                    break;
                case 'mes':
                    const mesAtras = new Date(hoy.getTime() - 30 * 24 * 60 * 60 * 1000);
                    filtroFechaDesde.value = mesAtras.toISOString().split('T')[0];
                    filtroFechaHasta.value = hoy.toISOString().split('T')[0];
                    break;
                case 'todos':
                    filtroFechaDesde.value = '';
                    filtroFechaHasta.value = '';
                    break;
            }
            filtrarRegistros();
        });
    });
});

// Función para ver cambios
function verCambios(auditoriaId, datosAnteriores, datosNuevos) {
    document.getElementById('datosAnteriores').textContent = datosAnteriores || 'Sin datos anteriores';
    document.getElementById('datosNuevos').textContent = datosNuevos || 'Sin datos nuevos';

    const modal = new bootstrap.Modal(document.getElementById('modalCambios'));
    modal.show();
}

// Función para ver detalles
function verDetalles(auditoriaId) {
    // Aquí puedes hacer una llamada AJAX para obtener más detalles
    const contenido = `
            <div class="row">
                <div class="col-md-6">
                    <h6>ID de Auditoría</h6>
                    <p>${auditoriaId}</p>
                </div>
                <div class="col-md-6">
                    <h6>Estado</h6>
                    <p><span class="badge bg-success small fw-medium">Completado</span></p>
                </div>
            </div>
            <hr>
            <h6>Información Adicional</h6>
            <p>Registro de auditoría generado automáticamente por el sistema.</p>
        `;

    document.getElementById('contenidoDetalles').innerHTML = contenido;

    const modal = new bootstrap.Modal(document.getElementById('modalDetalles'));
    modal.show();
}

function exportarAuditoriaCSV() {
    const tabla = document.getElementById("tablaAuditoria");
    const filasVisibles = tabla.querySelectorAll('tbody tr:not([style*="display: none"])');

    if (!tabla || filasVisibles.length === 0) {
        alert("No hay registros de auditoría para exportar");
        return;
    }

    const separador = detectarSeparadorCSV();
    const csv = [];

    const encabezados = ["Fecha", "Hora", "Usuario", "Email", "Acción", "Datos Anteriores", "Datos Nuevos"];
    csv.push(encabezados.map((h) => limpiarDatoCSV(h)).join(separador));

    filasVisibles.forEach((fila) => {
        const celdas = fila.querySelectorAll("td");

        if (celdas.length >= 4) {
            // Datos básicos visibles
            const colFecha = celdas[0];
            const fecha = colFecha.querySelector(".fw-semibold")?.textContent.trim() || "";
            const hora = colFecha.querySelector("small")?.textContent.trim() || "";

            const colUsuario = celdas[1];
            const nombreUsuario = colUsuario.querySelector(".fw-semibold")?.textContent.trim() || "";
            const emailUsuario = colUsuario.querySelector("small")?.textContent.trim() || "";

            const accion = celdas[2].querySelector(".badge")?.textContent.trim() || "";

            // --- EXTRACCIÓN DE DATOS OCULTOS (Anteriores / Nuevos) ---
            let datosAnt = "";
            let datosNue = "";

            // Buscamos el botón dentro de la celda de acciones (índice 3)
            const botonVer = celdas[3].querySelector("button[onclick^='verCambios']");

            if (botonVer) {
                // Obtenemos el texto literal del atributo onclick:
                // Ej: verCambios(1, '{&quot;id&quot;:1}', '{&quot;id&quot;:2}')
                const onclickStr = botonVer.getAttribute("onclick");

                // Usamos Regex para capturar lo que está entre las comillas simples
                // Explicación Regex: busca verCambios, salta el ID, captura grupo 1 entre comillas, captura grupo 2 entre comillas
                const matches = onclickStr.match(/verCambios\([^,]+,\s*'([^']*)',\s*'([^']*)'\)/);

                if (matches && matches.length >= 3) {
                    // matches[1] son los datos anteriores, matches[2] los nuevos
                    // Usamos decodeHtmlEntities para convertir &quot; en "
                    datosAnt = decodeHtmlEntities(matches[1]);
                    datosNue = decodeHtmlEntities(matches[2]);
                }
            }

            const datosFila = [
                limpiarDatoCSV(fecha),
                limpiarDatoCSV(hora),
                limpiarDatoCSV(nombreUsuario),
                limpiarDatoCSV(emailUsuario),
                limpiarDatoCSV(accion),
                limpiarDatoCSV(datosAnt),
                limpiarDatoCSV(datosNue)
            ];

            csv.push(datosFila.join(separador));
        }
    });

    // Crear y descargar archivo (Bloque estándar)
    const BOM = "\uFEFF";
    const contenidoCSV = BOM + csv.join("\r\n");
    const blob = new Blob([contenidoCSV], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement("a");

    const ahora = new Date();
    const nombreArchivo = `auditoria_completa_${ahora.toISOString().split("T")[0]}.csv`;

    enlace.setAttribute("href", url);
    enlace.setAttribute("download", nombreArchivo);
    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);
    URL.revokeObjectURL(url);

    // Feedback visual (Opcional, reutilizando el de tu ejemplo anterior)
    darFeedbackBoton();
}

// Función para decodificar caracteres HTML (ej: &quot; -> ")
function decodeHtmlEntities(text) {
    if (!text) return "";
    const textArea = document.createElement('textarea');
    textArea.innerHTML = text;
    return textArea.value;
}

function detectarSeparadorCSV() {
    const locale = navigator.language || "en-US";
    const usaComa = ["es", "fr", "de", "it", "pt"].some(p => locale.startsWith(p));
    return usaComa ? ";" : ",";
}

function limpiarDatoCSV(dato) {
    if (!dato) return '""';
    let limpio = dato.toString().trim();
    limpio = limpio.replace(/[\r\n]+/g, " ");
    limpio = limpio.replace(/"/g, '""');
    return `"${limpio}"`;
}

function darFeedbackBoton() {
    const boton = document.querySelector('[onclick="exportarAuditoriaCSV()"]');
    if (!boton) return;
    const textoOriginal = boton.innerHTML;
    boton.innerHTML = '<i class="bi bi-check-circle me-2"></i>Listo';
    boton.classList.add("btn-success");
    setTimeout(() => {
        boton.innerHTML = textoOriginal;
        boton.classList.remove("btn-success");
    }, 2000);
}

function mostrarToast(mensaje, tipo) {
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${tipo} border-0 position-fixed top-0 end-0 m-3`;
    toast.style.zIndex = '9999';
    toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-check-circle me-2"></i>${mensaje}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;
    document.body.appendChild(toast);

    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => {
        document.body.removeChild(toast);
    });
}