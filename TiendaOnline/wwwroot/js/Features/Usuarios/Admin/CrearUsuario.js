document.addEventListener('DOMContentLoaded', function () {

    const formulario = document.getElementById('formularioRegistro');
    const botonLimpiar = document.getElementById('btnLimpiarFormulario');

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

    // Capitalización automática
    ['campoNombre', 'campoApellido'].forEach(id => {
        const entrada = document.getElementById(id);
        if (entrada) {
            entrada.addEventListener('input', function (e) {
                const palabras = e.target.value.split(' ');
                const palabrasCapitalizadas = palabras.map(p =>
                    p.charAt(0).toUpperCase() + p.slice(1).toLowerCase()
                );
                e.target.value = palabrasCapitalizadas.join(' ');
            });
        }
    });

    // Botón limpiar SIN inline JS
    if (botonLimpiar) {
        botonLimpiar.addEventListener('click', function () {
            formulario.reset();
            entradas.forEach(entrada => {
                entrada.classList.remove('is-valid', 'is-invalid');
            });
        });
    }

});