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

});