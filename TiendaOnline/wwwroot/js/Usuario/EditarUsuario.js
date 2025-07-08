document.addEventListener('DOMContentLoaded', function () {
    const formulario = document.getElementById('formularioEditarUsuario');
    const botonGuardar = document.getElementById('botonGuardar');
    const spinner = botonGuardar.querySelector('.spinner-border');
    const textoBoton = botonGuardar.querySelector('.texto-boton');

    // Almacenar valores originales para funcionalidad de restablecer
    const valoresOriginales = {};
    const entradas = formulario.querySelectorAll('input, select');
    entradas.forEach(entrada => {
        valoresOriginales[entrada.name] = entrada.value;
    });

    // Envío de formulario con estado de carga
    formulario.addEventListener('submit', function (e) {
        if (formulario.checkValidity()) {
            botonGuardar.classList.add('cargando');
            spinner.classList.remove('d-none');
            textoBoton.textContent = 'Guardando...';
            botonGuardar.disabled = true;
        }
    });

    // Validación en tiempo real
    entradas.forEach(entrada => {
        entrada.addEventListener('blur', function () {
            validarCampo(this);
        });

        entrada.addEventListener('input', function () {
            if (this.classList.contains('is-invalid')) {
                validarCampo(this);
            }
        });
    });

    function validarCampo(campo) {
        const esValido = campo.checkValidity();
        campo.classList.remove('is-valid', 'is-invalid');

        if (campo.value.trim() !== '') {
            campo.classList.add(esValido ? 'is-valid' : 'is-invalid');
        }
    }

    // Función para restablecer formulario
    window.restablecerFormulario = function () {
        entradas.forEach(entrada => {
            entrada.value = valoresOriginales[entrada.name] || '';
            entrada.classList.remove('is-valid', 'is-invalid');
        });

        // Restablecer resumen de validación
        const resumenValidacion = document.querySelector('.validation-summary-errors');
        if (resumenValidacion) {
            resumenValidacion.style.display = 'none';
        }
    };

    // Formateo de número de teléfono
    const campoTelefono = document.getElementById('campoTelefono');
    if (campoTelefono) {
        campoTelefono.addEventListener('input', function (e) {
            let valor = e.target.value.replace(/\D/g, '');
            if (valor.length >= 6) {
                valor = valor.replace(/(\d{3})(\d{3})(\d{4})/, '$1-$2-$3');
            } else if (valor.length >= 3) {
                valor = valor.replace(/(\d{3})(\d{3})/, '$1-$2');
            }
            e.target.value = valor;
        });
    }

    // Capitalización automática de nombres
    const camposNombre = ['campoNombre', 'campoApellido'];
    camposNombre.forEach(id => {
        const entrada = document.getElementById(id);
        if (entrada) {
            entrada.addEventListener('input', function (e) {
                const palabras = e.target.value.split(' ');
                const palabrasCapitalizadas = palabras.map(palabra =>
                    palabra.charAt(0).toUpperCase() + palabra.slice(1).toLowerCase()
                );
                e.target.value = palabrasCapitalizadas.join(' ');
            });
        }
    });

    // Mostrar modal de éxito si el formulario se envió exitosamente
    @if (TempData["MensajeExito"] != null) {
        <text>
            const modalExito = new bootstrap.Modal(document.getElementById('modalExito'));
            modalExito.show();
        </text>
    }
});

// Confirmar navegación con cambios no guardados
window.addEventListener('beforeunload', function (e) {
    const formulario = document.getElementById('formularioEditarUsuario');
    const entradas = formulario.querySelectorAll('input, select');
    let tieneCambios = false;

    entradas.forEach(entrada => {
        if (entrada.type !== 'hidden' && entrada.defaultValue !== entrada.value) {
            tieneCambios = true;
        }
    });

    if (tieneCambios) {
        e.preventDefault();
        e.returnValue = '';
    }
});

// Función para exportar datos del usuario (opcional)
function exportarDatosUsuario() {
    const usuario = {
        id: '@Model.UsuarioId',
        nombre: '@Model.Nombre',
        apellido: '@Model.Apellido',
        email: '@Model.Email',
        telefono: '@Model.Telefono',
        direccion: '@Model.Direccion',
        rol: '@Model.Rol'
    };

    const csv = [
        ['Campo', 'Valor'],
        ['ID', usuario.id],
        ['Nombre', usuario.nombre],
        ['Apellido', usuario.apellido],
        ['Email', usuario.email],
        ['Teléfono', usuario.telefono],
        ['Dirección', usuario.direccion],
        ['Rol', usuario.rol]
    ].map(row => row.map(field => `"${field}"`).join(',')).join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const enlace = document.createElement('a');
    const url = URL.createObjectURL(blob);
    enlace.setAttribute('href', url);
    enlace.setAttribute('download', `usuario_${usuario.id}_${new Date().toISOString().split('T')[0]}.csv`);
    enlace.style.visibility = 'hidden';
    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);
}