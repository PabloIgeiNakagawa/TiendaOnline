document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('formularioDireccion');
    const btnGuardar = document.getElementById('botonGuardar');
    const btnRestablecer = document.getElementById('btnRestablecer');
    const spinner = btnGuardar.querySelector('.spinner-border');
    const textoBoton = btnGuardar.querySelector('.texto-boton');

    const $provincia = document.getElementById('selectProvincia');
    const $localidad = document.getElementById('selectLocalidad');
    const $codigoPostal = document.querySelector('input[name="CodigoPostal"]');

    const provinciaGuardada = $provincia?.value || '';
    const localidadGuardada = $localidad?.value || '';

    async function cargarProvincias() {
        if (!$provincia) return;
        try {
            const response = await fetch('/api/GeoApi/provincias');
            if (!response.ok) throw new Error('Error al cargar provincias');
            const provincias = await response.json();
            $provincia.innerHTML = '<option value="">Seleccione una provincia...</option>';
            provincias.forEach(p => {
                $provincia.add(new Option(p.nombre, p.nombre));
            });
            if (provinciaGuardada) {
                $provincia.value = provinciaGuardada;
                await cargarLocalidades(provinciaGuardada, localidadGuardada);
            }
        } catch (error) {
            console.error(error);
            $provincia.innerHTML = '<option value="">Error al cargar provincias</option>';
        }
    }

    async function cargarLocalidades(provincia, valorSeleccionado) {
        if (!$localidad) return;
        $localidad.innerHTML = '<option value="">Cargando localidades...</option>';
        $localidad.disabled = true;
        if ($codigoPostal && !valorSeleccionado) $codigoPostal.value = '';
        try {
            const response = await fetch(`/api/GeoApi/localidades?provincia=${encodeURIComponent(provincia)}`);
            if (!response.ok) throw new Error('Error al cargar localidades');
            const localidades = await response.json();
            $localidad.innerHTML = '<option value="">Seleccione una localidad...</option>';
            localidades.forEach(l => {
                $localidad.add(new Option(l.nombre, l.nombre));
            });
            if (valorSeleccionado) {
                $localidad.value = valorSeleccionado;
            }
            $localidad.disabled = false;
        } catch (error) {
            console.error(error);
            $localidad.innerHTML = '<option value="">Error al cargar</option>';
        }
    }

    if ($provincia) {
        $provincia.addEventListener('change', async function () {
            const valorElegido = this.value;
            if (!$localidad) return;
            $localidad.innerHTML = '<option value="">Primero elija una provincia...</option>';
            $localidad.disabled = true;
            if ($codigoPostal) $codigoPostal.value = '';
            if (valorElegido) {
                await cargarLocalidades(valorElegido, '');
            }
        });
    }

    const camposOriginales = {};
    form.querySelectorAll('input, textarea, select').forEach(campo => {
        if (campo.type === 'hidden') return;
        camposOriginales[campo.id] = campo.value;
    });

    form.addEventListener('submit', function () {
        btnGuardar.disabled = true;
        spinner.classList.remove('d-none');
        textoBoton.textContent = 'Guardando...';
    });

    btnRestablecer.addEventListener('click', function () {
        form.querySelectorAll('input, textarea, select').forEach(campo => {
            if (campo.type === 'hidden') return;
            if (campo.type === 'checkbox') {
                campo.checked = camposOriginales[campo.id] === 'true';
            } else {
                campo.value = camposOriginales[campo.id] || '';
            }
        });
    });

    cargarProvincias();
});
