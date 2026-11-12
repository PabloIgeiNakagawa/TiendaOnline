// Filtro de pedidos por estado sin recargar la página: las tarjetas ya viajan
// al DOM con su data-estado-grupo y los chips (data-filtro) solo ocultan o
// muestran. La URL se mantiene sincronizada con replaceState para que el
// filtro sobreviva a un refresh y los links con ?estado= sigan compartibles:
// al cargar, el server ya dejó marcado el chip activo y pre-ocultó las
// tarjetas que no coinciden, acá solo se re-aplica por si el usuario llegó
// sin ese estado previo.

const CLASE_CHIP_ACTIVO = 'btn-primary';
const CLASE_CHIP_INACTIVO = 'btn-outline-primary';

function aplicarFiltroPedidos(filtro) {
    document.querySelectorAll('[data-filtro]').forEach(chip => {
        const activo = chip.dataset.filtro === filtro;
        chip.classList.toggle(CLASE_CHIP_ACTIVO, activo);
        chip.classList.toggle(CLASE_CHIP_INACTIVO, !activo);
        chip.setAttribute('aria-pressed', activo ? 'true' : 'false');
    });

    let visibles = 0;
    document.querySelectorAll('[data-estado-grupo]').forEach(tarjeta => {
        const mostrar = filtro === 'todos' || tarjeta.dataset.estadoGrupo === filtro;
        tarjeta.classList.toggle('d-none', !mostrar);
        if (mostrar) visibles++;
    });

    document.getElementById('sinPedidosFiltro')?.classList.toggle('d-none', visibles > 0);

    const url = new URL(location.href);
    if (filtro === 'todos') url.searchParams.delete('estado');
    else url.searchParams.set('estado', filtro);
    history.replaceState(null, '', url);
}

document.addEventListener('click', (e) => {
    const chip = e.target.closest('[data-filtro]');
    if (!chip) return;
    aplicarFiltroPedidos(chip.dataset.filtro);
});

// Link compartido o refresh con ?estado=: aplicar el mismo filtro sin recarga.
(function initFiltroPedidos() {
    const filtro = new URLSearchParams(location.search).get('estado');
    if (filtro && document.querySelector(`[data-filtro="${filtro}"]`)) {
        aplicarFiltroPedidos(filtro);
    }
})();
