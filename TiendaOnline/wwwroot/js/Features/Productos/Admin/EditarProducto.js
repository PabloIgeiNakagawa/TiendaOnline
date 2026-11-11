// Solo vista previa de nueva imagen y de la galería adicional
document.addEventListener('DOMContentLoaded', function () {
    const imagenInput = document.getElementById('ImagenArchivo');
    const imagePreview = document.getElementById('imagePreview');

    if (imagenInput && imagePreview) {
        imagenInput.addEventListener('change', function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    imagePreview.src = e.target.result;
                    imagePreview.style.display = 'block';
                };
                reader.readAsDataURL(file);
            } else {
                imagePreview.style.display = 'none';
            }
        });
    }

    // Previews de las imágenes adicionales (thumbnails de objeto, sin nombres
    // de archivo: no hay nada que escapar)
    const imagenesAdicionales = document.getElementById('ImagenesAdicionales');
    const previewsAdicionales = document.getElementById('previewsAdicionales');

    imagenesAdicionales?.addEventListener('change', function () {
        previewsAdicionales.innerHTML = '';

        const archivos = Array.from(this.files || []);
        if (archivos.length > 5) {
            mostrarToast('Podés agregar hasta 5 imágenes adicionales (6 en total con la principal).');
            this.value = '';
            return;
        }

        archivos.forEach(archivo => {
            if (!archivo.type.startsWith('image/')) return;
            if (archivo.size > 5 * 1024 * 1024) {
                mostrarToast('Una de las imágenes supera los 5MB y no se agregó: ' + archivo.name);
                return;
            }

            const col = document.createElement('div');
            col.className = 'position-relative';

            const thumb = document.createElement('img');
            thumb.className = 'rounded border';
            thumb.style.width = '72px';
            thumb.style.height = '72px';
            thumb.style.objectFit = 'cover';
            thumb.alt = 'Vista previa';
            // data: y no blob: — el CSP del sitio permite img-src data: pero
            // no blob:, y el preview principal ya usa este mismo mecanismo
            const reader = new FileReader();
            reader.onload = function (e) {
                thumb.src = e.target.result;
            };
            reader.readAsDataURL(archivo);

            col.appendChild(thumb);
            previewsAdicionales.appendChild(col);
        });
    });
});