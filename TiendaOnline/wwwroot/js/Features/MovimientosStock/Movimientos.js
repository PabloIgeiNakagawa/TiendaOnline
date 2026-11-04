document.addEventListener('DOMContentLoaded', function () {
    // Delegación de eventos para imágenes
    document.addEventListener('click', function (e) {

        const img = e.target.closest('.img-producto-modal');
        if (!img) return;

        const url = img.dataset.url;
        const titulo = img.dataset.titulo;

        document.getElementById('imgModalSrc').src = url;
        document.getElementById('imgModalTitle').textContent = titulo;

        new bootstrap.Modal(
            document.getElementById('modalImagenProducto')
        ).show();
    });

    // "Ver 20/50/100": envía el formulario de filtros al cambiar el tamaño de página.
    const selectPaginado = document.querySelector('select[name="RegistrosPorPagina"]');
    if (selectPaginado) {
        selectPaginado.addEventListener('change', function () {
            selectPaginado.closest('form').submit();
        });
    }

});