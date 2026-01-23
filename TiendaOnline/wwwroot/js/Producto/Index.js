document.addEventListener('DOMContentLoaded', function () {
    const productosContainer = document.getElementById('productosContainer');
    const noProductos = document.getElementById('noProductos');
    const productosVisibles = document.getElementById('productosVisibles');
    const busquedaInput = document.getElementById('busquedaProducto');
    const precioMinInput = document.getElementById('precioMin');
    const precioMaxInput = document.getElementById('precioMax');
    const aplicarFiltroPrecios = document.getElementById('aplicarFiltroPrecios');
    const limpiarFiltros = document.getElementById('limpiarFiltros');
    const categoriaItems = document.querySelectorAll('.categoria-item');
    const vistaControles = document.querySelectorAll('[data-vista]');

    let filtros = {
        categoria: '',
        busqueda: '',
        precioMin: null,
        precioMax: null
    };

    // Filtrado de productos
    function filtrarProductos() {
        const productos = document.querySelectorAll('.producto-link');
        let visibles = 0;

        productos.forEach(producto => {
            const categoriaId = producto.dataset.categoria;
            const categoriaPadreId = producto.dataset.categoriaPadre;
            const precio = parseFloat(producto.dataset.precio);
            const nombre = producto.dataset.nombre;

            let mostrar = true;

            if (filtros.categoria) {
                const coincidePropia = categoriaId === filtros.categoria;
                const coincidePadre = categoriaPadreId === filtros.categoria;
                if (!coincidePropia && !coincidePadre) mostrar = false;
            }

            if (filtros.busqueda && !nombre.includes(filtros.busqueda.toLowerCase())) {
                mostrar = false;
            }

            if (filtros.precioMin && precio < filtros.precioMin) mostrar = false;
            if (filtros.precioMax && precio > filtros.precioMax) mostrar = false;

            producto.style.display = mostrar ? 'block' : 'none';

            if (mostrar) visibles++;
        });

        // Actualizar UI
        productosVisibles.textContent = visibles;
        if (visibles === 0) {
            productosContainer.style.display = 'none';
            noProductos.style.display = 'block';
        } else {
            // Restaurar el tipo de display según la vista activa
            const esGrid = document.querySelector('[data-vista="grid"]').classList.contains('active');
            productosContainer.style.display = esGrid ? 'grid' : 'flex';
            noProductos.style.display = 'none';
        }
    }

    // Event listeners
    busquedaInput.addEventListener('input', function () {
        filtros.busqueda = this.value;
        filtrarProductos();
    });

    aplicarFiltroPrecios.addEventListener('click', function () {
        filtros.precioMin = precioMinInput.value ? parseFloat(precioMinInput.value) : null;
        filtros.precioMax = precioMaxInput.value ? parseFloat(precioMaxInput.value) : null;
        filtrarProductos();
    });

    categoriaItems.forEach(item => {
        item.addEventListener('click', function () {
            categoriaItems.forEach(i => i.classList.remove('active'));
            this.classList.add('active');
            filtros.categoria = this.dataset.categoria;
            filtrarProductos();
        });
    });

    limpiarFiltros.addEventListener('click', function () {
        filtros = {
            categoria: '',
            busqueda: '',
            precioMin: null,
            precioMax: null
        };

        busquedaInput.value = '';
        precioMinInput.value = '';
        precioMaxInput.value = '';

        categoriaItems.forEach(i => i.classList.remove('active'));
        categoriaItems[0].classList.add('active');

        filtrarProductos();
    });

    // Cambio de vista
    vistaControles.forEach(control => {
        control.addEventListener('click', function () {
            const vista = this.dataset.vista;

            vistaControles.forEach(c => c.classList.remove('active'));
            this.classList.add('active');

            if (vista === 'list') {
                productosContainer.className = 'productos-list';
            } else {
                productosContainer.className = 'productos-grid';
            }
        });
    });

    // Ordenamiento
    document.querySelectorAll('[data-orden]').forEach(item => {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            const orden = this.dataset.orden;
            const productos = Array.from(document.querySelectorAll('.producto-card'));

            productos.sort((a, b) => {
                switch (orden) {
                    case 'nombre':
                        return a.dataset.nombre.localeCompare(b.dataset.nombre);
                    case 'precio-asc':
                        return parseFloat(a.dataset.precio) - parseFloat(b.dataset.precio);
                    case 'precio-desc':
                        return parseFloat(b.dataset.precio) - parseFloat(a.dataset.precio);
                    default:
                        return 0;
                }
            });

            productos.forEach(producto => {
                productosContainer.appendChild(producto);
            });
        });
    });

    // Event listener para el botón de limpiar búsqueda del mensaje "no productos"
    document.getElementById('limpiarBusqueda').addEventListener('click', function () {
        filtros = {
            categoria: '',
            busqueda: '',
            precioMin: null,
            precioMax: null
        };

        busquedaInput.value = '';
        precioMinInput.value = '';
        precioMaxInput.value = '';

        categoriaItems.forEach(i => i.classList.remove('active'));
        categoriaItems[0].classList.add('active');

        filtrarProductos();
    });

    // Aplicar búsqueda automáticamente si hay valor precargado
    if (busquedaInput.value.trim() !== '') {
        filtros.busqueda = busquedaInput.value.trim();
        filtrarProductos();
    }

    // Animación de flechas para subcategorías
    document.querySelectorAll('.collapse').forEach(el => {
        el.addEventListener('show.bs.collapse', () => {
            const icon = document.querySelector(`[data-bs-target="#${el.id}"]`);
            if (icon) icon.style.transform = 'rotate(90deg)';
        });
        el.addEventListener('hide.bs.collapse', () => {
            const icon = document.querySelector(`[data-bs-target="#${el.id}"]`);
            if (icon) icon.style.transform = 'rotate(0deg)';
        });
    });

    // Función para resetear desde el breadcrumb
    window.resetearFiltroCategoria = function () {
        const itemTodas = document.querySelector('.categoria-item[data-categoria=""]');
        if (itemTodas) itemTodas.click();
    };
});