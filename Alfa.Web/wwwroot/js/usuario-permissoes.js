(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('btn-toggle-all');
        if (!btn) return;

        btn.addEventListener('click', function () {
            var checkboxes = document.querySelectorAll('input[name="PermissoesSelecionadas"]');
            if (!checkboxes || checkboxes.length === 0) return;

            // se todos estão marcados, desmarcar; caso contrário marcar todos
            var allChecked = Array.from(checkboxes).every(cb => cb.checked);
            checkboxes.forEach(cb => cb.checked = !allChecked);

            // mudar texto do botão
            btn.textContent = allChecked ? 'Marcar todos' : 'Desmarcar todos';
        });

        // inicializar texto do botão conforme estado inicial
        var checkboxes = document.querySelectorAll('input[name="PermissoesSelecionadas"]');
        if (checkboxes.length > 0) {
            var allChecked = Array.from(checkboxes).every(cb => cb.checked);
            btn.textContent = allChecked ? 'Desmarcar todos' : 'Marcar todos';
        }
    });
})();