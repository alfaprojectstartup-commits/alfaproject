(function () {
    // segurança: garantir que a variável exista
    var usuarios = window.USUARIOS || [];
    var pageSize = window.PAGE_SIZE || 10;
    var currentPage = parseInt(window.INITIAL_PAGE, 10) || 1;

    var filtroInput = document.getElementById('filtro-usuario');
    var tbody = document.getElementById('usuarios-tbody');
    var pagination = document.getElementById('usuarios-pagination');

    // estado do filtro
    var filteredUsuarios = usuarios.slice();

    // debounce helper
    function debounce(fn, delay) {
        var timer = null;
        return function () {
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () {
                fn.apply(null, args);
            }, delay);
        };
    }

    // render da tabela (para a página atual)
    function renderTable(list, page) {
        tbody.innerHTML = '';

        if (!list || list.length === 0) {
            var trEmpty = document.createElement('tr');
            var td = document.createElement('td');
            td.setAttribute('colspan', '4');
            td.className = 'text-center';
            td.textContent = 'Nenhum usuário encontrado.';
            trEmpty.appendChild(td);
            tbody.appendChild(trEmpty);
            return;
        }

        var start = (page - 1) * pageSize;
        var end = start + pageSize;
        var pageItems = list.slice(start, end);

        var fragment = document.createDocumentFragment();

        pageItems.forEach(function (u) {
            var tr = document.createElement('tr');

            var tdNome = document.createElement('td');
            tdNome.textContent = u.nome || u.Nome || '';
            tr.appendChild(tdNome);

            var tdEmail = document.createElement('td');
            tdEmail.textContent = u.email || u.Email || '';
            tr.appendChild(tdEmail);

            var tdAtivo = document.createElement('td');
            var ativoVal = (typeof u.ativo !== 'undefined') ? u.ativo : u.Ativo;
            tdAtivo.textContent = ativoVal ? 'Sim' : 'Não';
            tr.appendChild(tdAtivo);

            var tdAcoes = document.createElement('td');

            // Link editar
            var aEditar = document.createElement('a');
            aEditar.className = 'btn btn-sm btn-primary me-1';
            aEditar.href = '/Usuario/Editar/' + (u.id || u.Id);
            aEditar.textContent = 'Editar';
            tdAcoes.appendChild(aEditar);

            // Link permissões
            var aPerm = document.createElement('a');
            aPerm.className = 'btn btn-sm btn-primary me-1';
            aPerm.href = '/Usuario/Permissoes/' + (u.id || u.Id);
            aPerm.textContent = 'Permissões';
            tdAcoes.appendChild(aPerm);

            tr.appendChild(tdAcoes);

            fragment.appendChild(tr);
        });

        tbody.appendChild(fragment);
    }

    // render da paginação
    function renderPagination(totalItems, currentPageLocal) {
        pagination.innerHTML = '';

        var totalPages = Math.ceil(totalItems / pageSize);
        if (totalPages <= 1) return;

        for (var i = 1; i <= totalPages; i++) {
            var li = document.createElement('li');
            li.className = 'page-item' + (i === currentPageLocal ? ' active' : '');

            var a = document.createElement('a');
            a.className = 'page-link';
            a.href = '#';
            a.dataset.page = i;
            a.textContent = i;

            li.appendChild(a);
            pagination.appendChild(li);
        }
    }

    // aplica o filtro (case-insensitive) por nome ou email, só quando >= 4 caracteres
    function applyFilter(term) {
        term = (term || '').trim().toLowerCase();

        if (term.length >= 4) {
            filteredUsuarios = usuarios.filter(function (u) {
                var nome = (u.nome || u.Nome || '').toString().toLowerCase();
                var email = (u.email || u.Email || '').toString().toLowerCase();
                return nome.indexOf(term) !== -1 || email.indexOf(term) !== -1;
            });
        } else {
            // se menos que 4, volta a lista completa
            filteredUsuarios = usuarios.slice();
        }

        // resetar página para 1 ao aplicar filtro
        currentPage = 1;
        renderTable(filteredUsuarios, currentPage);
        renderPagination(filteredUsuarios.length, currentPage);
    }

    // debounce na função de input
    var debouncedFilter = debounce(function (ev) {
        var term = ev.target.value;
        applyFilter(term);
    }, 250); // 250ms debounce

    // eventos
    if (filtroInput) {
        filtroInput.addEventListener('input', debouncedFilter);
    }

    // delegação de clique na paginação
    pagination.addEventListener('click', function (ev) {
        ev.preventDefault();
        var target = ev.target;
        if (target.tagName.toLowerCase() === 'a' && target.dataset.page) {
            var newPage = parseInt(target.dataset.page, 10);
            if (!isNaN(newPage)) {
                currentPage = newPage;
                renderTable(filteredUsuarios, currentPage);
                renderPagination(filteredUsuarios.length, currentPage);

                // opcional: rolar para a tabela ao trocar de página
                tbody.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }
    });

    // render inicial
    (function init() {
        // se não houver usuários, mostra vazio
        filteredUsuarios = usuarios.slice();
        // garantir que a currentPage seja válida
        var totalPages = Math.max(1, Math.ceil(filteredUsuarios.length / pageSize));
        if (currentPage < 1 || currentPage > totalPages) currentPage = 1;

        renderTable(filteredUsuarios, currentPage);
        renderPagination(filteredUsuarios.length, currentPage);
    })();
})();