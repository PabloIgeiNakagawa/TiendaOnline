document.addEventListener('DOMContentLoaded', function () {

    const formulario = document.getElementById('formularioEditarUsuario');
    if (!formulario) return;

    const botonGuardar = document.getElementById('botonGuardar');
    const spinner = botonGuardar?.querySelector('.spinner-border');
    const textoBoton = botonGuardar?.querySelector('.texto-boton');
    const botonRestablecer = document.getElementById('btnRestablecer');

    // Guardar valores originales
    const valoresOriginales = {};
    const entradas = formulario.querySelectorAll('input, select');

    entradas.forEach(entrada => {
        valoresOriginales[entrada.name] = entrada.value;
    });

    // Submit con estado de carga
    formulario.addEventListener('submit', function () {
        if (formulario.checkValidity()) {
            botonGuardar?.classList.add('cargando');
            spinner?.classList.remove('d-none');
            if (textoBoton) textoBoton.textContent = 'Guardando...';
            if (botonGuardar) botonGuardar.disabled = true;
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

    // Restablecer formulario
    botonRestablecer?.addEventListener('click', function () {
        entradas.forEach(entrada => {
            entrada.value = valoresOriginales[entrada.name] || '';
            entrada.classList.remove('is-valid', 'is-invalid');
        });

        const resumenValidacion = document.querySelector('.validation-summary-errors');
        if (resumenValidacion) {
            resumenValidacion.style.display = 'none';
        }
    });

    // Formateo teléfono
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

    // Capitalización automática
    ['campoNombre', 'campoApellido'].forEach(id => {
        const entrada = document.getElementById(id);

        if (entrada) {
            entrada.addEventListener('input', function (e) {
                const palabras = e.target.value.split(' ');
                const capitalizadas = palabras.map(p =>
                    p.charAt(0).toUpperCase() + p.slice(1).toLowerCase()
                );
                e.target.value = capitalizadas.join(' ');
            });
        }
    });
});

// Confirmar salida con cambios
window.addEventListener('beforeunload', function (e) {

    const formulario = document.getElementById('formularioEditarUsuario');
    if (!formulario) return;

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