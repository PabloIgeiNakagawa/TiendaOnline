(() => {
    'use strict';

    const panel = document.getElementById('ubicacionEnVivo');
    if (!panel) return;

    const btnCompartir = document.getElementById('btnCompartirUbicacion');
    const btnDetener = document.getElementById('btnDetenerUbicacion');
    const estado = document.getElementById('ubicacionEstado');
    const token = panel.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const urlUbicacion = panel.dataset.url;
    const urlDetener = panel.dataset.urlDetener;

    let watchId = null;
    let wakeLock = null;
    let ultimoEnvio = 0;

    function mostrarEstado(texto, esError) {
        if (!estado) return;
        estado.textContent = texto;
        estado.classList.toggle('text-danger', !!esError);
        estado.classList.toggle('text-muted', !esError);
    }

    function mostrarCompartiendo(activo) {
        btnCompartir?.classList.toggle('d-none', activo);
        btnDetener?.classList.toggle('d-none', !activo);
        panel.dataset.compartiendo = activo ? 'true' : 'false';

        if (activo) {
            mostrarEstado('Estás compartiendo tu posición. Mantené esta pantalla abierta durante la entrega.');
        } else {
            mostrarEstado('Compartí tu posición para que el cliente vea dónde viene su pedido. El GPS solo corre con esta pantalla abierta.');
        }
    }

    async function pedirWakeLock() {
        if (!('wakeLock' in navigator)) return;
        try {
            wakeLock = await navigator.wakeLock.request('screen');
            wakeLock.addEventListener('release', () => { wakeLock = null; });
        } catch {
            // El navegador puede negar el wake lock (batería baja, pestaña de fondo):
            // el GPS sigue latiendo mientras la pestaña quede visible.
        }
    }

    async function enviarPosicion(position) {
        const ahora = Date.now();
        // watchPosition puede disparar más seguido que el latido del servidor.
        if (ahora - ultimoEnvio < 25000) return;
        ultimoEnvio = ahora;

        try {
            const respuesta = await fetch(urlUbicacion, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    latitud: position.coords.latitude,
                    longitud: position.coords.longitude,
                    precisionMetros: position.coords.accuracy
                })
            });

            if (!respuesta.ok) throw new Error('No se pudo publicar la ubicación');
            mostrarCompartiendo(true);
        } catch {
            mostrarEstado('No se pudo enviar la ubicación. Revisá la conexión.', true);
        }
    }

    function errorGps(error) {
        if (error.code === error.PERMISSION_DENIED) {
            mostrarEstado('Bloqueaste el acceso a la ubicación. Habilitalo en el navegador para compartir tu posición.', true);
            detener();
            return;
        }
        mostrarEstado('Sin señal GPS por ahora. Reintentando…', true);
    }

    function iniciar() {
        if (!navigator.geolocation) {
            mostrarEstado('Este dispositivo no soporta geolocalización.', true);
            return;
        }

        watchId = navigator.geolocation.watchPosition(
            enviarPosicion,
            errorGps,
            { enableHighAccuracy: true, maximumAge: 15000, timeout: 20000 });

        pedirWakeLock();
        mostrarCompartiendo(true);
    }

    function detener(avisarServidor) {
        if (watchId !== null) {
            navigator.geolocation.clearWatch(watchId);
            watchId = null;
        }
        wakeLock?.release().catch(() => { });
        wakeLock = null;

        if (avisarServidor) {
            fetch(urlDetener, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            }).finally(() => window.location.reload());
            return;
        }

        mostrarCompartiendo(false);
    }

    btnCompartir?.addEventListener('click', iniciar);
    btnDetener?.addEventListener('click', () => detener(true));

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible' && watchId !== null && !wakeLock) {
            pedirWakeLock();
        }
    });

    if (panel.dataset.compartiendo === 'true') iniciar();
})();
