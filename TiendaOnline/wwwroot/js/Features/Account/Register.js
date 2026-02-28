document.addEventListener('DOMContentLoaded', function () {

    const formulario = document.getElementById('formularioRegistro');
    const botonEnviar = document.getElementById('botonEnviar');
    const botonLimpiar = document.getElementById('btnLimpiar');

    if (!formulario) return;

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

    if (botonLimpiar) {
        botonLimpiar.addEventListener('click', function () {
            formulario.reset();
            entradas.forEach(entrada => {
                entrada.classList.remove('is-valid', 'is-invalid');
            });
        });
    }
});