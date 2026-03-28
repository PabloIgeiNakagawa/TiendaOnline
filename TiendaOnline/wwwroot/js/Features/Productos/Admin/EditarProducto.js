// Solo vista previa de nueva imagen
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
});