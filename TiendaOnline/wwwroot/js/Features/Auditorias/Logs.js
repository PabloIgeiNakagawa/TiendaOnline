document.addEventListener('DOMContentLoaded', function () {
    // Delegación de eventos
    document.addEventListener('click', function (e) {

        const toggle = e.target.closest('[data-vista-cambios]');
        if (toggle) {
            cambiarVista(toggle.dataset.vistaCambios);
            return;
        }

        const btn = e.target.closest('.btn-ver-detalle');
        if (!btn) return;

        verDetalle(btn.dataset.id);
    });
});

function cambiarVista(vista) {
    document.querySelectorAll('[data-vista-cambios]').forEach(boton => {
        boton.classList.toggle('active', boton.dataset.vistaCambios === vista);
    });
    document.getElementById('vistaDiffCambios').classList.toggle('d-none', vista !== 'diff');
    document.getElementById('vistaJsonCambios').classList.toggle('d-none', vista !== 'json');
}

function verDetalle(id) {
    // Cada apertura arranca en la vista legible, no en la que dejó la anterior.
    cambiarVista('diff');

    const modal = new bootstrap.Modal(document.getElementById('modalCambios'));
    modal.show();

    const cuerpoDiff = document.getElementById('cuerpoDiffCambios');
    const sinDatos = document.getElementById('diffSinDatos');
    const datosAnteriores = document.getElementById('datosAnteriores');
    const datosNuevos = document.getElementById('datosNuevos');

    document.getElementById('subtituloCambios').textContent = '';
    cuerpoDiff.replaceChildren();
    sinDatos.classList.add('d-none');
    datosAnteriores.textContent = 'Cargando...';
    datosNuevos.textContent = 'Cargando...';

    fetch(`/admin/auditorias/ObtenerDetalle/${id}`)
        .then(response => {
            if (!response.ok) throw new Error();
            return response.json();
        })
        .then(data => {
            document.getElementById('subtituloCambios').textContent =
                `${data.accion} · ${data.tablaAfectada} #${data.entidadId}`;

            datosAnteriores.textContent = formatearJson(data.datosAnteriores);
            datosNuevos.textContent = formatearJson(data.datosNuevos);

            renderizarDiff(cuerpoDiff, sinDatos, data.datosAnteriores, data.datosNuevos);
        })
        .catch(() => {
            alert("Error al cargar los detalles.");
        });
}

const formatearJson = (jsonString) => {
    if (!jsonString) return "Sin datos";
    try {
        const obj = typeof jsonString === 'string'
            ? JSON.parse(jsonString)
            : jsonString;

        return JSON.stringify(obj, null, 2);
    } catch {
        return jsonString;
    }
};

// La fecha viaja como ISO: en la tabla se lee dd/MM/yyyy HH:mm.
const formatearFecha = (fecha) => {
    const d = new Date(fecha);
    if (isNaN(d.getTime())) return fecha;

    const dosDigitos = (n) => String(n).padStart(2, '0');
    return `${dosDigitos(d.getDate())}/${dosDigitos(d.getMonth() + 1)}/${d.getFullYear()} `
        + `${dosDigitos(d.getHours())}:${dosDigitos(d.getMinutes())}`;
};

const formatearValor = (valor) => {
    if (valor === undefined) return '—';
    if (valor === null) return 'null';
    if (typeof valor === 'string') {
        return /^\d{4}-\d{2}-\d{2}T/.test(valor) ? formatearFecha(valor) : valor;
    }
    if (typeof valor === 'object') return JSON.stringify(valor);
    return String(valor);
};

const parsearObjeto = (jsonString) => {
    if (!jsonString) return {};
    try {
        const obj = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString;
        return obj && typeof obj === 'object' && !Array.isArray(obj) ? obj : {};
    } catch {
        return {};
    }
};

// Tabla campo / anterior / nuevo: los datos vienen de la base y se escriben
// con textContent (nunca innerHTML): un nombre con marcado es dato, no HTML.
function renderizarDiff(tbody, sinDatos, anterioresJson, nuevosJson) {
    const anteriores = parsearObjeto(anterioresJson);
    const nuevos = parsearObjeto(nuevosJson);
    const claves = [...new Set([...Object.keys(anteriores), ...Object.keys(nuevos)])];

    if (claves.length === 0) {
        sinDatos.classList.remove('d-none');
        return;
    }

    for (const clave of claves) {
        const existiaAntes = Object.hasOwn(anteriores, clave);
        const existeAhora = Object.hasOwn(nuevos, clave);
        const anterior = anteriores[clave];
        const nuevo = nuevos[clave];

        const fila = document.createElement('tr');
        let etiqueta = null;
        let claseFila = '';

        if (!existiaAntes) {
            claseFila = 'table-success';
            etiqueta = 'Nuevo';
        } else if (!existeAhora) {
            claseFila = 'table-danger';
            etiqueta = 'Eliminado';
        } else if (JSON.stringify(anterior) !== JSON.stringify(nuevo)) {
            claseFila = 'table-warning';
            etiqueta = 'Modificado';
        }

        fila.className = claseFila;

        const celdaCampo = document.createElement('td');
        const nombreCampo = document.createElement('span');
        nombreCampo.className = 'fw-semibold';
        nombreCampo.textContent = clave;
        celdaCampo.appendChild(nombreCampo);
        if (etiqueta) {
            const badge = document.createElement('span');
            badge.className = 'badge text-bg-secondary ms-2 fw-normal';
            badge.textContent = etiqueta;
            celdaCampo.appendChild(badge);
        }

        const celdaAnterior = document.createElement('td');
        celdaAnterior.textContent = existiaAntes ? formatearValor(anterior) : '—';

        const celdaNuevo = document.createElement('td');
        celdaNuevo.textContent = existeAhora ? formatearValor(nuevo) : '—';

        fila.append(celdaCampo, celdaAnterior, celdaNuevo);
        tbody.appendChild(fila);
    }
}
