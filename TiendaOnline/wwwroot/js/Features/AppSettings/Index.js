document.addEventListener("DOMContentLoaded", function () {
    // Ver/Ocultar
    document.querySelectorAll(".js-toggle-btn").forEach(btn => {
        btn.addEventListener("click", function () {
            const input = document.getElementById(this.dataset.target);
            const icon = this.querySelector("i");
            if (input.type === "password") {
                input.type = "text";
                icon.classList.replace("bi-eye", "bi-eye-slash");
            } else {
                input.type = "password";
                icon.classList.replace("bi-eye-slash", "bi-eye");
            }
        });
    });

    // Copiar
    document.querySelectorAll(".js-copy-btn").forEach(btn => {
        btn.addEventListener("click", async function () {
            // 1. Obtenemos el ID del input desde el atributo 'data-target' del botón
            const targetId = this.dataset.target;
            const input = document.getElementById(targetId);

            // 2. Verificamos que el input exista y tenga algún valor
            if (!input) {
                console.error("No se encontró el input con ID: " + targetId);
                return;
            }

            const valorACopiar = input.value;

            if (!valorACopiar) {
                console.warn("El campo está vacío, nada que copiar.");
                return;
            }

            try {
                // 3. LA MAGIA: Copia directa sin necesidad de select() ni elementos temporales
                await navigator.clipboard.writeText(valorACopiar);

                // 4. Feedback visual (Icono de check)
                const icon = this.querySelector("i");
                const originalClass = icon.className; // Guardamos las clases originales

                icon.classList.replace("bi-clipboard", "bi-check-lg");
                icon.classList.add("text-success");

                setTimeout(() => {
                    icon.className = originalClass; // Restauramos todo a la normalidad
                    icon.classList.remove("text-success");
                }, 1500);

            } catch (err) {
                console.error("Error al copiar al portapapeles:", err);

                // Fallback antiguo solo si la API moderna falla (por ejemplo en navegadores muy viejos)
                input.select();
                document.execCommand("copy");
            }
        });
    });


});