document.addEventListener('DOMContentLoaded', function () {
    // --- SELECTORES ---
    const radioLocal = document.getElementById('radioLocal');
    const radioEnvio = document.getElementById('radioEnvio');
    const seccionDir = document.getElementById('seccionDireccion');
    const textoEnvio = document.getElementById('textoEnvio');
    const btnConfirmar = document.querySelector('button[type="submit"]');

    const selectDireccionGuardada = document.getElementById('DireccionSeleccionadaId');
    const bloqueNuevaDireccion = document.getElementById('bloqueNuevaDireccion');

    const $provincia = document.getElementById("selectProvincia");
    const $localidad = document.getElementById("selectLocalidad");
    const $codigoPostal = document.querySelector('input[name="NuevaDireccion.CodigoPostal"]');
    const $calle = document.querySelector('input[name="NuevaDireccion.Calle"]');

    // --- 1. LÓGICA DE VALIDACIÓN (BOTÓN) ---
    // --- 1. LÓGICA DE VALIDACIÓN (BOTÓN) ---
    function validarEstadoBoton() {
        const btn = document.getElementById('btnConfirmarPedido');
        if (!btn) return;

        const radioLocal = document.getElementById('radioLocal');
        const radioEnvio = document.getElementById('radioEnvio');
        const selectGuardada = document.getElementById('DireccionSeleccionadaId');

        // Selectores de los campos obligatorios (asegurate que los 'name' coincidan con tu HTML)
        const inputCalle = document.querySelector('input[name="NuevaDireccion.Calle"]');
        const inputNumero = document.querySelector('input[name="NuevaDireccion.Numero"]');
        const inputEtiqueta = document.querySelector('input[name="NuevaDireccion.Etiqueta"]');
        const inputCP = document.querySelector('input[name="NuevaDireccion.CodigoPostal"]');
        const selectProv = document.getElementById("selectProvincia");
        const selectLoc = document.getElementById("selectLocalidad");

        let esValido = false;

        // Caso A: Retiro en local
        if (radioLocal && radioLocal.checked) {
            esValido = true;
        }
        // Caso B: Envío a domicilio
        else if (radioEnvio && radioEnvio.checked) {
            const tieneGuardada = selectGuardada && selectGuardada.value !== "";

            // Verificamos TODOS los obligatorios
            const tieneCalle = inputCalle && inputCalle.value.trim().length >= 3;
            const tieneNumero = inputNumero && inputNumero.value.trim() !== "";
            const tieneEtiqueta = inputEtiqueta && inputEtiqueta.value.trim() !== "";
            const tieneCP = inputCP && inputCP.value.trim().length >= 4; // CP en Argentina tiene min 4
            const tieneProv = selectProv && selectProv.value !== "";
            const tieneLoc = selectLoc && selectLoc.value !== "";

            const tieneNuevaCompleta = tieneCalle && tieneNumero && tieneEtiqueta && tieneCP && tieneProv && tieneLoc;

            // Se habilita si elije una guardada O completa todos los campos de la nueva
            if (tieneGuardada || tieneNuevaCompleta) {
                esValido = true;
            }
        }

        btn.disabled = !esValido;
    }

    // --- 2. LÓGICA DE INTERFAZ (MOSTRAR/OCULTAR) ---
    function actualizarInterfaz() {
        if (radioEnvio && radioEnvio.checked) {
            seccionDir.classList.remove('d-none');
            textoEnvio.innerText = "Calculando...";

            if ($provincia && $provincia.options.length <= 1) {
                cargarProvincias();
            }
        } else {
            seccionDir.classList.add('d-none');
            textoEnvio.innerText = "$0.00";
        }
        validarEstadoBoton();
    }

    // --- 3. LÓGICA DE DIRECCIÓN GUARDADA ---
    if (selectDireccionGuardada) {
        selectDireccionGuardada.addEventListener('change', function () {
            if (this.value !== "") {
                bloqueNuevaDireccion.classList.add('d-none');
                limpiarCamposNuevaDireccion(); 
            } else {
                bloqueNuevaDireccion.classList.remove('d-none');
            }
            validarEstadoBoton();
        });
    }

    function limpiarCamposNuevaDireccion() {
        bloqueNuevaDireccion.querySelectorAll('input, select').forEach(el => el.value = "");
        $localidad.disabled = true;
        $localidad.innerHTML = '<option value="">Primero elija una provincia...</option>';
    }

    // --- 4. LÓGICA DE API (LLAMANDO A TU BACKEND) ---
    async function cargarProvincias() {
        try {
            // Llamamos a TU controller
            const response = await fetch("/api/GeoApi/provincias");
            if (!response.ok) throw new Error("Error en servidor");

            const provincias = await response.json();

            provincias.forEach(p => {
                $provincia.add(new Option(p.nombre, p.nombre));
            });
        } catch (error) {
            console.error("Error al cargar provincias:", error);
            $provincia.innerHTML = '<option value="">Error al cargar provincias</option>';
        }
    }

    if ($provincia) {
        $provincia.addEventListener("change", async function () {
            const valorElegido = this.value;
            if (!$localidad) return;

            $localidad.innerHTML = '<option value="">Cargando localidades...</option>';
            $localidad.disabled = true;
            if ($codigoPostal) $codigoPostal.value = "";

            if (valorElegido) {
                try {
                    const response = await fetch(`/api/GeoApi/localidades?provincia=${encodeURIComponent(valorElegido)}`);
                    if (!response.ok) throw new Error("Error en servidor");

                    const localidades = await response.json();

                    $localidad.innerHTML = '<option value="">Seleccione una localidad...</option>';
                    localidades.forEach(l => {
                        $localidad.add(new Option(l.nombre, l.nombre));
                    });
                    $localidad.disabled = false;
                } catch (error) {
                    console.error("Error al cargar localidades:", error);
                    $localidad.innerHTML = '<option value="">Error al cargar</option>';
                }
            }
            validarEstadoBoton();
        });
    }

    // --- 5. EVENTOS Y ARRANQUE ---
    if (radioLocal && radioEnvio) {
        radioLocal.addEventListener('change', actualizarInterfaz);
        radioEnvio.addEventListener('change', actualizarInterfaz);
    }

    document.querySelector('input[name="NuevaDireccion.Etiqueta"]')?.addEventListener('input', validarEstadoBoton);
    document.getElementById("selectProvincia")?.addEventListener('change', validarEstadoBoton);
    document.getElementById("selectLocalidad")?.addEventListener('change', validarEstadoBoton);
    document.querySelector('input[name="NuevaDireccion.CodigoPostal"]')?.addEventListener('input', validarEstadoBoton);
    document.querySelector('input[name="NuevaDireccion.Calle"]')?.addEventListener('input', validarEstadoBoton);
    document.querySelector('input[name="NuevaDireccion.Numero"]')?.addEventListener('input', validarEstadoBoton);
    
    // Ejecutar al cargar la página
    actualizarInterfaz();
});