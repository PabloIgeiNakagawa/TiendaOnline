document.addEventListener('DOMContentLoaded', function () {
    // Delegación de eventos
    document.addEventListener('click', function (e) {

        const btn = e.target.closest('.btn-ver-detalle');
        if (!btn) return;

        const id = btn.dataset.id;
        verDetalle(id);
    });

});

function verDetalle(id) {

    const modal = new bootstrap.Modal(document.getElementById('modalCambios'));
    modal.show();

    const datosAnteriores = document.getElementById('datosAnteriores');
    const datosNuevos = document.getElementById('datosNuevos');

    datosAnteriores.textContent = "Cargando...";
    datosNuevos.textContent = "Cargando...";

    fetch(`/Admin/Auditorias/ObtenerDetalle/${id}`)
        .then(response => {
            if (!response.ok) throw new Error();
            return response.json();
        })
        .then(data => {

            const formatearJSON = (jsonString) => {
                if (!jsonString) return "Sin datos";
                try {
                    const obj = typeof jsonString === 'string'
                        ? JSON.parse(jsonString)
                        : jsonString;

                    return JSON.stringify(obj, null, 2);
                } catch {
                    return jsonString;
                }
            };

            datosAnteriores.textContent = formatearJSON(data.datosAnteriores);
            datosNuevos.textContent = formatearJSON(data.datosNuevos);
        })
        .catch(() => {
            alert("Error al cargar los detalles.");
        });
}