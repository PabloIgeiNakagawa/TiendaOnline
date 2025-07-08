document.addEventListener('DOMContentLoaded', function () {
    const formulario = document.getElementById('formularioRegistro');
    const botonEnviar = document.getElementById('botonEnviar');
    const spinner = botonEnviar.querySelector('.spinner-border');
    const textoBoton = botonEnviar.querySelector('.texto-boton');

    // Validación en tiempo real
    const entradas = formulario.querySelectorAll('input, select');
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

    // Indicador de fuerza de contraseña
    const campoContrasena = document.getElementById('campoContrasena');
    const barraFuerza = document.getElementById('barraFuerza');
    const textoFuerza = document.getElementById('textoFuerza');

    if (campoContrasena) {
        campoContrasena.addEventListener('input', function () {
            const contrasena = this.value;
            const fuerza = calcularFuerzaContrasena(contrasena);

            barraFuerza.className = 'relleno-fuerza';

            if (contrasena.length === 0) {
                textoFuerza.textContent = 'Ingrese una contraseña';
                textoFuerza.style.color = '#6c757d';
            } else if (fuerza < 2) {
                barraFuerza.classList.add('debil');
                textoFuerza.textContent = 'Contraseña débil';
                textoFuerza.style.color = '#dc3545';
            } else if (fuerza < 3) {
                barraFuerza.classList.add('regular');
                textoFuerza.textContent = 'Contraseña regular';
                textoFuerza.style.color = '#fd7e14';
            } else if (fuerza < 4) {
                barraFuerza.classList.add('buena');
                textoFuerza.textContent = 'Contraseña buena';
                textoFuerza.style.color = '#ffc107';
            } else {
                barraFuerza.classList.add('fuerte');
                textoFuerza.textContent = 'Contraseña fuerte';
                textoFuerza.style.color = '#198754';
            }
        });
    }

    function calcularFuerzaContrasena(contrasena) {
        let fuerza = 0;

        if (contrasena.length >= 8) fuerza++;
        if (/[a-z]/.test(contrasena)) fuerza++;
        if (/[A-Z]/.test(contrasena)) fuerza++;
        if (/[0-9]/.test(contrasena)) fuerza++;
        if (/[^A-Za-z0-9]/.test(contrasena)) fuerza++;

        return fuerza;
    }

    // Formateo de teléfono
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

    // Envío de formulario
    formulario.addEventListener('submit', function (e) {
        if (formulario.checkValidity()) {
            botonEnviar.classList.add('cargando');
            spinner.classList.remove('d-none');
            textoBoton.textContent = 'Creando...';
            botonEnviar.disabled = true;
        }
    });

    // Función para limpiar formulario
    window.limpiarFormulario = function () {
        formulario.reset();
        entradas.forEach(entrada => {
            entrada.classList.remove('is-valid', 'is-invalid');
        });

        if (barraFuerza) {
            barraFuerza.className = 'relleno-fuerza';
            textoFuerza.textContent = 'Ingrese una contraseña';
            textoFuerza.style.color = '#6c757d';
        }
    };
});