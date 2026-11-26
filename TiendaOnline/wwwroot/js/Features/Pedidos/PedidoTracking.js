(() => {
    'use strict';

    const contenedor = document.getElementById('trackingEntrega');
    const divMapa = document.getElementById('mapaEntrega');
    if (!contenedor || !divMapa || typeof L === 'undefined') return;

    const badge = document.getElementById('trackingEstado');
    const url = contenedor.dataset.url;
    const INTERVALO_MS = 30000;
    const SIN_SENAL_SEGUNDOS = 120;

    const mapa = L.map(divMapa, { scrollWheelZoom: false });
    mapa.setView([-34.6037, -58.3816], 12); // Buenos Aires hasta la primera posición

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(mapa);

    let marcador = null;
    let primeraPosicion = true;
    let huboPosicion = false;

    function escaparHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    }

    function estadoBadge(texto, clase) {
        if (!badge) return;
        badge.textContent = texto;
        badge.className = 'badge ' + clase;
    }

    function antiguedad(segundos) {
        if (segundos < 60) return `hace ${segundos} s`;
        const minutos = Math.floor(segundos / 60);
        return `hace ${minutos} min`;
    }

    function pintarUbicacion(datos) {
        const lat = Number(datos.latitud);
        const lng = Number(datos.longitud);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

        if (!marcador) {
            marcador = L.circleMarker([lat, lng], {
                radius: 9,
                color: '#0d6efd',
                weight: 3,
                fillColor: '#0d6efd',
                fillOpacity: 0.85
            }).addTo(mapa);

            marcador.bindPopup(`<strong>${escaparHtml(datos.repartidorNombre)}</strong><br>Repartidor en camino`);
        } else {
            marcador.setLatLng([lat, lng]);
        }

        if (primeraPosicion) {
            mapa.setView([lat, lng], 16);
            primeraPosicion = false;
        } else if (!mapa.getBounds().pad(-0.2).contains([lat, lng])) {
            mapa.panTo([lat, lng]);
        }

        huboPosicion = true;
    }

    async function consultar() {
        try {
            const respuesta = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!respuesta.ok) throw new Error('consulta fallida');

            const datos = await respuesta.json();

            if (!datos.disponible) {
                estadoBadge(huboPosicion ? 'Sin señal' : 'Esperando señal…', 'text-bg-secondary');
                return;
            }

            const segundos = Number(datos.segundosDesdeActualizacion ?? 0);
            if (segundos > SIN_SENAL_SEGUNDOS) {
                estadoBadge(`Sin señal ${antiguedad(segundos)}`, 'text-bg-warning');
                return;
            }

            pintarUbicacion(datos);
            estadoBadge(`En vivo · ${antiguedad(segundos)}`, 'text-bg-success');
        } catch {
            estadoBadge('Sin conexión', 'text-bg-secondary');
        }
    }

    consultar();
    setInterval(consultar, INTERVALO_MS);

    // Al volver a la pestaña, el mapa se actualiza sin esperar el próximo ciclo.
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') consultar();
    });
})();
