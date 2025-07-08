document.addEventListener('DOMContentLoaded', function () {
    // Inicializar tooltips
    var listaTooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltips = listaTooltips.map(function (elementoTooltip) {
        return new bootstrap.Tooltip(elementoTooltip);
    });

    // Funcionalidad de búsqueda
    const campoBusqueda = document.getElementById('campoBusqueda');
    const filtroRol = document.getElementById('filtroRol');
    const filtroEstado = document.getElementById('filtroEstado');
    const limpiarFiltros = document.getElementById('limpiarFiltros');
    const filas = document.querySelectorAll('.fila-usuario');
    const totalUsuarios = document.getElementById('totalUsuarios');

    function filtrarTabla() {
        const terminoBusqueda = campoBusqueda.value.toLowerCase();
        const valorRol = filtroRol.value;
        const valorEstado = filtroEstado.value;
        let contadorVisible = 0;

        filas.forEach(fila => {
            const nombre = fila.dataset.nombre.toLowerCase();
            const email = fila.dataset.email.toLowerCase();
            const rol = fila.dataset.rol;
            const estado = fila.dataset.estado;

            const coincideBusqueda = nombre.includes(terminoBusqueda) || email.includes(terminoBusqueda);
            const coincideRol = !valorRol || rol === valorRol;
            const coincideEstado = !valorEstado || estado === valorEstado;

            if (coincideBusqueda && coincideRol && coincideEstado) {
                fila.style.display = '';
                contadorVisible++;
            } else {
                fila.style.display = 'none';
            }
        });

        totalUsuarios.textContent = contadorVisible;
    }

    campoBusqueda.addEventListener('input', filtrarTabla);
    filtroRol.addEventListener('change', filtrarTabla);
    filtroEstado.addEventListener('change', filtrarTabla);

    limpiarFiltros.addEventListener('click', function () {
        campoBusqueda.value = '';
        filtroRol.value = '';
        filtroEstado.value = '';
        filtrarTabla();
    });

    // Funcionalidad de ordenamiento
    const encabezadosOrdenables = document.querySelectorAll('.ordenable');
    let ordenActual = { columna: -1, direccion: 'asc' };

    encabezadosOrdenables.forEach(encabezado => {
        encabezado.addEventListener('click', function () {
            const columna = parseInt(this.dataset.columna);
            const cuerpoTabla = document.querySelector('#tablaUsuarios tbody');
            const filas = Array.from(cuerpoTabla.querySelectorAll('tr'));

            // Actualizar dirección de ordenamiento
            if (ordenActual.columna === columna) {
                ordenActual.direccion = ordenActual.direccion === 'asc' ? 'desc' : 'asc';
            } else {
                ordenActual.direccion = 'asc';
            }
            ordenActual.columna = columna;

            // Actualizar iconos de encabezado
            encabezadosOrdenables.forEach(h => {
                h.classList.remove('orden-asc', 'orden-desc');
            });
            this.classList.add(ordenActual.direccion === 'asc' ? 'orden-asc' : 'orden-desc');

            // Ordenar filas
            filas.sort((a, b) => {
                const valorA = a.cells[columna].textContent.trim();
                const valorB = b.cells[columna].textContent.trim();

                if (ordenActual.direccion === 'asc') {
                    return valorA.localeCompare(valorB);
                } else {
                    return valorB.localeCompare(valorA);
                }
            });

            // Reordenar filas
            filas.forEach(fila => cuerpoTabla.appendChild(fila));
        });
    });
});

// Modal de confirmación
function confirmarAccion(idUsuario, accion, nombreUsuario) {
    const modal = new bootstrap.Modal(document.getElementById('modalConfirmacion'));
    const iconoConfirmacion = document.getElementById('iconoConfirmacion');
    const mensajeConfirmacion = document.getElementById('mensajeConfirmacion');
    const botonConfirmar = document.getElementById('botonConfirmar');

    if (accion === 'baja') {
        iconoConfirmacion.className = 'bi bi-exclamation-triangle text-warning display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de baja a ${nombreUsuario}?`;
        botonConfirmar.className = 'btn btn-danger';
        botonConfirmar.textContent = 'Dar de Baja';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioBaja${idUsuario}`).submit();
        };
    } else {
        iconoConfirmacion.className = 'bi bi-check-circle text-success display-4';
        mensajeConfirmacion.textContent = `¿Estás seguro de que quieres dar de alta a ${nombreUsuario}?`;
        botonConfirmar.className = 'btn btn-success';
        botonConfirmar.textContent = 'Dar de Alta';
        botonConfirmar.onclick = () => {
            document.getElementById(`formularioAlta${idUsuario}`).submit();
        };
    }

    modal.show();
}

// Función para detectar el separador CSV según la configuración regional
function detectarSeparadorCSV() {
    // Detectar configuración regional del navegador
    const locale = navigator.language || navigator.userLanguage || 'en-US';

    // En países que usan coma como separador decimal, Excel espera punto y coma como separador CSV
    const paisesConComaDecimal = ['es', 'es-ES', 'es-CO', 'es-MX', 'es-AR', 'fr', 'de', 'it', 'pt', 'pt-BR'];

    const usaComaDecimal = paisesConComaDecimal.some(pais => locale.startsWith(pais));

    return usaComaDecimal ? ';' : ',';
}

// Función para limpiar y formatear datos CSV
function limpiarDatoCSV(dato) {
    if (!dato) return '""';

    // Convertir a string y limpiar
    let limpio = dato.toString().trim();

    // Remover saltos de línea y caracteres especiales
    limpio = limpio.replace(/[\r\n]+/g, ' ');
    limpio = limpio.replace(/\s+/g, ' ');

    // Escapar comillas dobles
    limpio = limpio.replace(/"/g, '""');

    // Siempre envolver en comillas para mayor compatibilidad
    return `"${limpio}"`;
}

// Función para mostrar notificación de exportación exitosa
function mostrarNotificacionExportacion(nombreArchivo, cantidadFilas) {
    // Crear toast de notificación
    const toast = document.createElement('div');
    toast.className = 'toast align-items-center text-white bg-success border-0 position-fixed';
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 1055; min-width: 300px;';
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <div class="d-flex align-items-center">
                    <i class="bi bi-download me-2"></i>
                    <div>
                        <strong>Exportación exitosa</strong><br>
                        <small>${cantidadFilas} usuarios exportados</small><br>
                        <small class="text-white-50">${nombreArchivo}</small>
                    </div>
                </div>
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast, { delay: 5000 });
    bsToast.show();

    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

// Exportar a CSV mejorado
function exportarACSV() {
    const tabla = document.getElementById('tablaUsuarios');
    const filasVisibles = tabla.querySelectorAll('tbody tr:not([style*="display: none"])');

    if (filasVisibles.length === 0) {
        alert('No hay usuarios para exportar');
        return;
    }

    // Detectar separador apropiado
    const separador = detectarSeparadorCSV();
    const csv = [];

    // Encabezados
    const encabezados = ['Nombre y Apellido', 'Correo Electrónico', 'Rol', 'Estado'];
    csv.push(encabezados.map(h => limpiarDatoCSV(h)).join(separador));

    // Filas de datos
    filasVisibles.forEach(fila => {
        const celdas = fila.querySelectorAll('td');
        if (celdas.length >= 4) {
            // Extraer datos limpios
            const nombre = celdas[0].textContent.trim();
            const email = celdas[1].textContent.trim();

            // Limpiar rol (remover HTML si hay badges)
            const rolElement = celdas[2].querySelector('.badge') || celdas[2];
            const rol = rolElement.textContent.trim();

            // Limpiar estado (remover HTML de badges)
            const estadoElement = celdas[3].querySelector('.badge') || celdas[3];
            const estado = estadoElement.textContent.trim();

            const datosFila = [
                limpiarDatoCSV(nombre),
                limpiarDatoCSV(email),
                limpiarDatoCSV(rol),
                limpiarDatoCSV(estado)
            ];

            csv.push(datosFila.join(separador));
        }
    });

    // Crear contenido CSV con BOM para UTF-8
    const BOM = '\uFEFF';
    const contenidoCSV = BOM + csv.join('\r\n');

    // Crear blob con tipo MIME específico para CSV
    const blob = new Blob([contenidoCSV], {
        type: 'text/csv;charset=utf-8;'
    });

    // Crear enlace de descarga
    const enlace = document.createElement('a');
    const url = URL.createObjectURL(blob);

    // Nombre de archivo con fecha y hora
    const ahora = new Date();
    const fecha = ahora.toISOString().split('T')[0];
    const hora = ahora.toTimeString().split(' ')[0].replace(/:/g, '-');
    const nombreArchivo = `usuarios_${fecha}_${hora}.csv`;

    enlace.setAttribute('href', url);
    enlace.setAttribute('download', nombreArchivo);
    enlace.style.visibility = 'hidden';

    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);

    // Limpiar URL del objeto
    URL.revokeObjectURL(url);

    // Feedback visual mejorado
    const botonExportar = document.querySelector('[onclick="exportarACSV()"]');
    const textoOriginal = botonExportar.innerHTML;

    botonExportar.innerHTML = '<i class="bi bi-check-circle me-1"></i>¡Exportado!';
    botonExportar.classList.add('btn-success');
    botonExportar.classList.remove('btn-outline-primary');

    setTimeout(() => {
        botonExportar.innerHTML = textoOriginal;
        botonExportar.classList.remove('btn-success');
        botonExportar.classList.add('btn-outline-primary');
    }, 3000);

    // Mostrar notificación adicional
    mostrarNotificacionExportacion(nombreArchivo, filasVisibles.length);
}