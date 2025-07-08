// Solo función para mostrar/ocultar contraseña
function togglePassword() {
    var passwordInput = document.getElementById('password');
    var passwordIcon = document.getElementById('passwordIcon');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        passwordIcon.className = 'bi bi-eye-slash';
    } else {
        passwordInput.type = 'password';
        passwordIcon.className = 'bi bi-eye';
    }
}