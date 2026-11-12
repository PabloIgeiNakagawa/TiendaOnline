document.addEventListener('DOMContentLoaded', function () {
    // --- SELECTORES ---
    const radioLocal = document.getElementById('radioLocal');
    const radioEnvio = document.getElementById('radioEnvio');
    const seccionDir = document.getElementById('seccionDireccion');
    const infoRetiro = document.getElementById('infoRetiro');
    const textoEnvio = document.getElementById('textoEnvio');
    const textoTotal = document.getElementById('textoTotal');
    const panelResumen = document.querySelector('[data-subtotal]');
    const subtotal = panelResumen ? parseFloat(panelResumen.dataset.subtotal) || 0 : 0;

    const btnContinuar = document.getElementById('btnContinuarAlPago');
    const btnConfirmar = document.getElementById('btnConfirmarCompra');

    const selectDireccionGuardada = document.getElementById('DireccionSeleccionadaId');
    const bloqueNuevaDireccion = document.getElementById('bloqueNuevaDireccion');

    const $provincia = document.getElementById("selectProvincia");
    const $localidad = document.getElementById("selectLocalidad");
    const $codigoPostal = document.querySelector('input[name="NuevaDireccion.CodigoPostal"]');
    const $calle = document.querySelector('input[name="NuevaDireccion.Calle"]');

    // --- 1. VALIDACIÓN DE LA ENTREGA (habilita "Continuar al pago" y el submit) ---
    function entregaCompleta() {
        // Caso A: Retiro en local
        if (radioLocal && radioLocal.checked) return true;

        // Caso B: Envío a domicilio: una guardada O la nueva completa
        if (radioEnvio && radioEnvio.checked) {
            const selectGuardada = document.getElementById('DireccionSeleccionadaId');
            const tieneGuardada = selectGuardada && selectGuardada.value !== "";

            const inputCalle = document.querySelector('input[name="NuevaDireccion.Calle"]');
            const inputNumero = document.querySelector('input[name="NuevaDireccion.Numero"]');
            const inputEtiqueta = document.querySelector('input[name="NuevaDireccion.Etiqueta"]');
            const inputCP = document.querySelector('input[name="NuevaDireccion.CodigoPostal"]');
            const selectProv = document.getElementById("selectProvincia");
            const selectLoc = document.getElementById("selectLocalidad");

            const tieneNuevaCompleta =
                inputCalle && inputCalle.value.trim().length >= 3 &&
                inputNumero && inputNumero.value.trim() !== "" &&
                inputEtiqueta && inputEtiqueta.value.trim() !== "" &&
                inputCP && inputCP.value.trim().length >= 4 && // CP en Argentina tiene min 4
                selectProv && selectProv.value !== "" &&
                selectLoc && selectLoc.value !== "";

            return tieneGuardada || tieneNuevaCompleta;
        }

        return false;
    }

    function validarEstadoBotones() {
        if (btnContinuar) btnContinuar.disabled = !entregaCompleta();
        if (btnConfirmar) btnConfirmar.disabled = !entregaCompleta();
    }

    // --- 2. TOTAL Y ENVÍO EN VIVO ---
    function setTotal(costoEnvio) {
        if (!textoTotal) return;
        if (costoEnvio === null) {
            textoTotal.innerText = "—"; // envío sin cotizar: no inventamos el total
            return;
        }
        const total = subtotal + costoEnvio;
        textoTotal.innerText = "$" + total.toLocaleString("es-AR", { maximumFractionDigits: 0 });
    }

    // --- 3. INTERFAZ (MOSTRAR/OCULTAR) ---
    function actualizarInterfaz() {
        if (radioEnvio && radioEnvio.checked) {
            seccionDir.classList.remove('d-none');
            infoRetiro?.classList.add('d-none');

            // El costo lo cota el server contra la tabla de zonas: en vivo según la provincia.
            const guardadaSeleccionada = selectDireccionGuardada?.selectedOptions[0];
            if (guardadaSeleccionada?.dataset.provincia) {
                cotizarEnvio(guardadaSeleccionada.dataset.provincia);
            } else {
                textoEnvio.innerText = "Elegí una provincia";
                setTotal(null);
            }

            if ($provincia && $provincia.options.length <= 1) {
                cargarProvincias();
            }
        } else if (radioLocal && radioLocal.checked) {
            seccionDir.classList.add('d-none');
            infoRetiro?.classList.remove('d-none');
            textoEnvio.innerText = "Sin costo";
            setTotal(0);
        } else {
            // Sin entrega elegida todavía
            seccionDir.classList.add('d-none');
            infoRetiro?.classList.add('d-none');
            textoEnvio.innerText = "A calcular";
            setTotal(null);
        }
        validarEstadoBotones();
    }

    // --- 3b. COTIZACIÓN EN VIVO (el precio real lo fija igual el server al finalizar) ---
    async function cotizarEnvio(provincia) {
        if (!textoEnvio) return;
        textoEnvio.innerText = "Calculando...";
        try {
            const response = await fetch(`/Checkout/CotizarEnvio?provincia=${encodeURIComponent(provincia)}`);
            if (!response.ok) throw new Error("Error en servidor");

            const data = await response.json();
            if (data.permitida) {
                textoEnvio.innerText = "$" + data.costo.toLocaleString("es-AR", { maximumFractionDigits: 0 });
                setTotal(data.costo);
            } else {
                textoEnvio.innerText = "No llegamos a esta zona"; // Zona deshabilitada en el panel de envíos
                setTotal(null);
            }
        } catch (error) {
            console.error("Error al cotizar envío:", error);
            textoEnvio.innerText = "A calcular";
            setTotal(null);
        }
    }

    // --- 4. DIRECCIÓN GUARDADA ---
    if (selectDireccionGuardada) {
        selectDireccionGuardada.addEventListener('change', function () {
            if (this.value !== "") {
                bloqueNuevaDireccion.classList.add('d-none');
                limpiarCamposNuevaDireccion();
                if (radioEnvio && radioEnvio.checked) {
                    cotizarEnvio(this.selectedOptions[0]?.dataset.provincia ?? "");
                }
            } else {
                bloqueNuevaDireccion.classList.remove('d-none');
            }
            validarEstadoBotones();
        });
    }

    function limpiarCamposNuevaDireccion() {
        bloqueNuevaDireccion.querySelectorAll('input, select').forEach(el => el.value = "");
        $localidad.disabled = true;
        $localidad.innerHTML = '<option value="">Primero elija una provincia...</option>';
    }

    // --- 5. API GEO ---
    async function cargarProvincias() {
        try {
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

            if (radioEnvio && radioEnvio.checked) {
                cotizarEnvio(valorElegido);
            }

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
            validarEstadoBotones();
        });
    }

    // --- 6. EVENTOS Y ARRANQUE ---
    if (radioLocal && radioEnvio) {
        radioLocal.addEventListener('change', actualizarInterfaz);
        radioEnvio.addEventListener('change', actualizarInterfaz);
    }

    // Los cambios de pago solo revalidan el submit (MP viene pre-seleccionado).
    document.querySelectorAll('input[name="MetodoPagoId"]').forEach(function (radio) {
        radio.addEventListener('change', validarEstadoBotones);
    });

    document.querySelector('input[name="NuevaDireccion.Etiqueta"]')?.addEventListener('input', validarEstadoBotones);
    document.querySelector('input[name="NuevaDireccion.CodigoPostal"]')?.addEventListener('input', validarEstadoBotones);
    document.querySelector('input[name="NuevaDireccion.Calle"]')?.addEventListener('input', validarEstadoBotones);
    document.querySelector('input[name="NuevaDireccion.Numero"]')?.addEventListener('input', validarEstadoBotones);

    // Ejecutar al cargar la página
    actualizarInterfaz();
});
