function cambiarVista(vista) {
    const container = document.getElementById('productosContainer');

    if (vista === 'list') {
        container.classList.remove('row-cols-md-2', 'row-cols-lg-3');
        container.classList.add('row-cols-1');
    } else {
        container.classList.add('row-cols-md-2', 'row-cols-lg-3');
        container.classList.remove('row-cols-1');
    }
}

document.getElementById("btnGrid")
    ?.addEventListener("click", () => cambiarVista("grid"));

document.getElementById("btnList")
    ?.addEventListener("click", () => cambiarVista("list"));

document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(el => {
    el.addEventListener('click', function () {
        this.classList.toggle('rotate-90');
    });
});