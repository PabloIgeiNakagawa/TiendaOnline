// Funciones para incrementar y decrementar cantidad
function incrementQuantity(productoId) {
    var input = document.getElementById('cantidad-' + productoId);
    input.value = parseInt(input.value) + 1;
}

function decrementQuantity(productoId) {
    var input = document.getElementById('cantidad-' + productoId);
    var currentValue = parseInt(input.value);
    if (currentValue > 1) {
        input.value = currentValue - 1;
    }
}