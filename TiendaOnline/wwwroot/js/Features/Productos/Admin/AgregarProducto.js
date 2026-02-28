document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('formProducto');
    const imagenInput = document.getElementById('imagenArchivo');
    const imagePreview = document.getElementById('imagePreview');
    const uploadPlaceholder = document.getElementById('uploadPlaceholder');
    const fileInfo = document.getElementById('fileInfo');
    const fileName = document.getElementById('fileName');
    const uploadContainer = document.querySelector('.image-upload-container');
    const btnSubmit = document.getElementById('btnSubmit');
    const btnText = btnSubmit.querySelector('.btn-text');
    const loadingSpinner = btnSubmit.querySelector('.loading-spinner');


    const btnRemove = document.querySelector('.btn-outline-danger.ms-2');
    btnRemove.addEventListener('click', function (e) {
        e.stopPropagation();
        removeImage(e);
    });

    // Contadores de caracteres
    setupCharacterCounter('nombre', 'nombreContador', 50);
    setupCharacterCounter('descripcion', 'descripcionContador', 150);

    // Validación en tiempo real
    setupRealTimeValidation();

    // Manejo de imagen
    imagenInput.addEventListener('change', handleImageSelect);

    // Drag and drop
    setupDragAndDrop();

    // Validación del formulario
    form.addEventListener('submit', handleFormSubmit);

    uploadContainer.addEventListener('click', function () {
        imagenInput.click();
    });

    function setupCharacterCounter(inputId, counterId, maxLength) {
        const input = document.getElementById(inputId);
        const counter = document.getElementById(counterId);

        input.addEventListener('input', function () {
            const length = this.value.length;
            counter.textContent = length;

            if (length > maxLength * 0.8) {
                counter.parentElement.classList.add('text-warning');
            } else {
                counter.parentElement.classList.remove('text-warning');
            }

            if (length >= maxLength) {
                counter.parentElement.classList.add('text-danger');
                counter.parentElement.classList.remove('text-warning');
            } else {
                counter.parentElement.classList.remove('text-danger');
            }
        });
    }

    function setupRealTimeValidation() {
        const inputs = form.querySelectorAll('input, select, textarea');

        inputs.forEach(input => {
            input.addEventListener('blur', validateField);
            input.addEventListener('input', function () {
                if (this.classList.contains('is-invalid')) {
                    validateField.call(this);
                }
            });
        });
    }

    function validateField() {
        const field = this;
        const value = field.value.trim();
        let isValid = true;
        let message = '';

        // Validaciones específicas
        switch (field.id) {
            case 'nombre':
                if (!value) {
                    isValid = false;
                    message = 'El nombre es requerido';
                } else if (value.length > 50) {
                    isValid = false;
                    message = 'El nombre no puede exceder 50 caracteres';
                }
                break;

            case 'descripcion':
                if (!value) {
                    isValid = false;
                    message = 'La descripción es requerida';
                } else if (value.length > 150) {
                    isValid = false;
                    message = 'La descripción no puede exceder 150 caracteres';
                }
                break;

            case 'precio':
                const precio = parseFloat(value);
                if (!value) {
                    isValid = false;
                    message = 'El precio es requerido';
                } else if (isNaN(precio) || precio <= 0) {
                    isValid = false;
                    message = 'El precio debe ser mayor a 0';
                } else if (precio > 999999999) {
                    isValid = false;
                    message = 'El precio es demasiado alto';
                }
                break;

            case 'stock':
                const stock = parseInt(value);
                if (!value) {
                    isValid = false;
                    message = 'El stock es requerido';
                } else if (isNaN(stock) || stock < 0) {
                    isValid = false;
                    message = 'El stock debe ser mayor o igual a 0';
                }
                break;

            case 'categoria':
                if (!value) {
                    isValid = false;
                    message = 'Debe seleccionar una categoría';
                }
                break;
        }

        // Aplicar estilos de validación
        const feedback = field.parentElement.querySelector('.invalid-feedback');

        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
            if (feedback) feedback.textContent = '';
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
            if (feedback) feedback.textContent = message;
        }

        return isValid;
    }

    function handleImageSelect(e) {
        const file = e.target.files[0];
        if (file) {
            validateAndPreviewImage(file);
        }
    }

    function validateAndPreviewImage(file) {
        // Validar tipo de archivo
        if (!file.type.startsWith('image/')) {
            alert('Por favor seleccione un archivo de imagen válido');
            return;
        }

        // Validar tamaño (5MB)
        if (file.size > 5 * 1024 * 1024) {
            alert('La imagen no puede ser mayor a 5MB');
            return;
        }

        // Mostrar vista previa
        const reader = new FileReader();
        reader.onload = function (e) {
            imagePreview.src = e.target.result;
            imagePreview.classList.remove('d-none');
            imagePreview.style.display = 'block';
            uploadPlaceholder.style.display = 'none';
            fileInfo.classList.remove('d-none');
            fileInfo.style.display = 'block';
            fileName.textContent = file.name;
            uploadContainer.classList.add('has-image');
        };
        reader.readAsDataURL(file);
    }

    function setupDragAndDrop() {
        uploadContainer.addEventListener('dragover', function (e) {
            e.preventDefault();
            this.classList.add('dragover');
        });

        uploadContainer.addEventListener('dragleave', function (e) {
            e.preventDefault();
            this.classList.remove('dragover');
        });

        uploadContainer.addEventListener('drop', function (e) {
            e.preventDefault();
            this.classList.remove('dragover');

            const files = e.dataTransfer.files;
            if (files.length > 0) {
                imagenInput.files = files;
                validateAndPreviewImage(files[0]);
            }
        });
    }

    function handleFormSubmit(e) {
        e.preventDefault();

        // Validar todos los campos
        const inputs = form.querySelectorAll('input, select, textarea');
        let isFormValid = true;

        inputs.forEach(input => {
            if (!validateField.call(input)) {
                isFormValid = false;
            }
        });

        // Validar imagen
        if (!imagenInput.files[0]) {
            alert('Por favor seleccione una imagen para el producto');
            isFormValid = false;
        }

        if (isFormValid) {
            // Mostrar loading
            btnSubmit.disabled = true;
            btnText.style.display = 'none';
            loadingSpinner.style.display = 'inline';

            // Enviar formulario
            form.submit();
        } else {
            // Scroll al primer error
            const firstError = form.querySelector('.is-invalid');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                firstError.focus();
            }
        }
    }

    // Función global para remover imagen
    window.removeImage = function (e) {
        e.stopPropagation();
        imagenInput.value = '';
        imagePreview.style.display = 'none';
        uploadPlaceholder.style.display = 'block';
        fileInfo.style.display = 'none';
        uploadContainer.classList.remove('has-image');
    };
});

// Animación de spin
const style = document.createElement('style');
style.textContent = `
                .spin {
                    animation: spin 1s linear infinite;
                }

            @keyframes spin {
                    from { transform: rotate(0deg); }
                    to { transform: rotate(360deg); }
                }
            `;
document.head.appendChild(style);