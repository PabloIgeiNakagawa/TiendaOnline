document.addEventListener('DOMContentLoaded', function () {

    document.addEventListener('click', function (e) {

        const btn = e.target.closest('.password-toggle');
        if (!btn) return;

        const inputId = btn.dataset.target;
        const input = document.getElementById(inputId);
        const icon = btn.querySelector('.password-icon');

        if (!input) return;

        const esPassword = input.type === 'password';

        input.type = esPassword ? 'text' : 'password';

        icon.className = esPassword
            ? 'bi bi-eye-slash password-icon'
            : 'bi bi-eye password-icon';
    });

});