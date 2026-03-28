// Aumentar cantidad
document.querySelectorAll(".btn-aumentar").forEach(btn => {
    btn.addEventListener("click", function () {
        const productoId = this.dataset.productoId;
        const input = document.getElementById("cantidad-" + productoId);

        let valorActual = parseInt(input.value) || 1;
        input.value = valorActual + 1;
    });
});

// Disminuir cantidad
document.querySelectorAll(".btn-disminuir").forEach(btn => {
    btn.addEventListener("click", function () {
        const productoId = this.dataset.productoId;
        const input = document.getElementById("cantidad-" + productoId);

        let valorActual = parseInt(input.value) || 1;
        if (valorActual > 1) {
            input.value = valorActual - 1;
        }
    });
});

// Confirmación eliminar
document.querySelectorAll(".btn-eliminar").forEach(btn => {
    btn.addEventListener("click", function (e) {
        if (!confirm("¿Eliminar producto?")) {
            e.preventDefault();
        }
    });
});