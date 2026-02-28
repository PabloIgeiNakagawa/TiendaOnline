document.addEventListener("DOMContentLoaded", function () {

    // Delegación para eliminar
    document.addEventListener("click", function (e) {

        const btn = e.target.closest(".btn-eliminar-categoria");
        if (!btn || btn.disabled) return;

        mostrarModalEliminar(
            btn.dataset.id,
            btn.dataset.nombre
        );
    });

    // Filtro en tiempo real
    const campoBusqueda = document.getElementById("campoBusqueda");

    if (campoBusqueda) {
        campoBusqueda.addEventListener("keyup", function () {

            const valor = this.value.toLowerCase();

            document.querySelectorAll(".fila-categoria").forEach(fila => {
                const nombre = fila.dataset.nombre.toLowerCase();
                fila.style.display = nombre.includes(valor) ? "" : "none";
            });

        });
    }

});

function mostrarModalEliminar(categoriaId, nombre) {

    const modal = new bootstrap.Modal(document.getElementById("modalCambioEstado"));
    const titulo = document.getElementById("modalTitulo");
    const mensaje = document.getElementById("mensajeCambioEstado");
    const boton = document.getElementById("botonDerecho");
    const icono = document.getElementById("iconoEstado");

    titulo.textContent = "Eliminar Categoría";
    mensaje.textContent = `¿Estás seguro de que deseas eliminar la categoría "${nombre}"? Esta acción no se puede deshacer.`;
    icono.className = "bi bi-exclamation-triangle text-warning display-4 mb-3";
    boton.className = "btn btn-danger";
    boton.textContent = "Eliminar Definitivamente";

    // Limpiar listeners anteriores
    boton.replaceWith(boton.cloneNode(true));
    const nuevoBoton = document.getElementById("botonDerecho");

    nuevoBoton.addEventListener("click", function () {

        nuevoBoton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Procesando...';
        nuevoBoton.disabled = true;

        document.getElementById(`eliminarCategoria${categoriaId}`).submit();

    }, { once: true });

    modal.show();
}