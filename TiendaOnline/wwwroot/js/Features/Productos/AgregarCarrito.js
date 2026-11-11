// --- Agregar al carrito sin cambiar de página ---
// Los forms del detalle y de las cards del catálogo van marcados con
// data-agregar-carrito: el POST corre por fetch (mismo endpoint, antiforgery
// incluido en el FormData) y el server responde SIEMPRE JSON. El éxito SIEMPRE
// lo dicta el server: el toast de "agregado" solo sale si el service realmente
// agregó; los errores (stock, producto inactivo) vuelven en el mismo JSON con
// su motivo. El mensaje viaja por textContent: nada de HTML interpolado.

// Badges del carrito (desktop y móvil): misma forma que renderiza
// CarritoHeader; de 0 a 1 el span no existe y lo recrea con los mismos
// estilos inline.
function actualizarBadgesDeCarrito(cantidad) {
    document.querySelectorAll('[data-carrito-icono]').forEach(function (icono) {
        var badge = icono.querySelector('[data-carrito-badge]');
        if (cantidad > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'position-absolute badge rounded-pill bg-danger';
                badge.style.fontSize = '0.6rem';
                badge.style.top = '15%';
                badge.style.left = '85%';
                badge.style.transform = 'translate(-50%, -50%)';
                badge.setAttribute('data-carrito-badge', '');
                icono.appendChild(badge);
            }
            badge.textContent = cantidad;
        } else if (badge) {
            badge.remove();
        }
    });
}

document.addEventListener('submit', function (e) {
    var form = e.target.closest('form[data-agregar-carrito]');
    if (!form) return;
    e.preventDefault();

    var boton = form.querySelector('button[type="submit"]');
    if (boton && boton.disabled) return;
    if (boton) boton.disabled = true; // un click = un POST (sin dobles)

    fetch(form.action, {
        method: 'POST',
        body: new FormData(form)
    })
        .then(function (respuesta) {
            return respuesta.ok ? respuesta.json() : Promise.reject(new Error('HTTP ' + respuesta.status));
        })
        .then(function (datos) {
            if (datos.ok) {
                mostrarToast(datos.mensaje, 'exito');
                if (typeof datos.cantidadEnCarrito === 'number') {
                    actualizarBadgesDeCarrito(datos.cantidadEnCarrito);
                }
            } else {
                mostrarToast(datos.mensaje || 'No se pudo agregar el producto.', 'error');
            }
        })
        .catch(function () {
            // Respuesta inesperada (caída, HTML en vez de JSON): aviso genérico,
            // nunca el detalle del error interno.
            mostrarToast('No se pudo agregar el producto. Probá de nuevo.', 'error');
        })
        .finally(function () {
            if (boton) boton.disabled = false;
        });
});
