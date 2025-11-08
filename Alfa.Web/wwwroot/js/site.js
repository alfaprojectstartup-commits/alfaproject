(function () {
    window.Alfa = window.Alfa || {};
    const Alfa = window.Alfa;

    const toastContainerId = 'toast-container';

    Alfa.toast = function (message, type = 'success', title) {
        const container = document.getElementById(toastContainerId) || createToastContainer();
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-bg-${type} alfa-toast`;
        toast.setAttribute('role', 'status');
        toast.setAttribute('aria-live', 'polite');
        toast.setAttribute('aria-atomic', 'true');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${title ? `<div class="fw-semibold mb-1">${title}</div>` : ''}
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button>
            </div>`;
        container.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast, { delay: 3500 });
        toast.addEventListener('hidden.bs.toast', () => toast.remove());
        bsToast.show();
        return bsToast;
    };

    function createToastContainer() {
        const container = document.createElement('div');
        container.id = toastContainerId;
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        document.body.appendChild(container);
        return container;
    }

    document.addEventListener('shown.bs.dropdown', (e) => {
        const menu = e.target.parentElement.querySelector('.dropdown-menu');
        if (menu && !menu.classList.contains('dropdown-fade')) menu.classList.add('dropdown-fade');
    });

    document.addEventListener('hide.bs.dropdown', () => {
        // deixa o Bootstrap cuidar do fechamento; o CSS trata a transição
    });

    document.addEventListener('DOMContentLoaded', () => {
        const tooltipTriggers = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggers.forEach(el => new bootstrap.Tooltip(el));

        const popoverTriggers = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggers.forEach(el => new bootstrap.Popover(el, { container: 'body' }));

        initThemeToggle();
        Alfa.initProcessosLista?.(document.getElementById('processos-root'));
    });

    function initThemeToggle() {
        const storageKey = 'alfa-theme';
        const trigger = document.querySelector('[data-theme-toggle]');
        const docEl = document.documentElement;
        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

        const readStored = () => {
            try {
                return localStorage.getItem(storageKey);
            } catch (err) {
                console.warn('Não foi possível ler a preferência de tema.', err);
                return null;
            }
        };

        const persistTheme = (theme) => {
            try {
                localStorage.setItem(storageKey, theme);
            } catch (err) {
                console.warn('Não foi possível persistir a preferência de tema.', err);
            }
        };

        const updateButton = (theme) => {
            if (!trigger) return;
            const isDark = theme === 'dark';
            trigger.setAttribute('aria-pressed', isDark ? 'true' : 'false');
            trigger.classList.toggle('is-dark', isDark);
            const icon = trigger.querySelector('[data-theme-icon]');
            const label = trigger.querySelector('[data-theme-label]');
            if (icon) icon.textContent = isDark ? '🌞' : '🌙';
            if (label) label.textContent = isDark ? 'Tema claro' : 'Tema escuro';
        };

        const applyTheme = (theme, persist = true) => {
            const normalized = theme === 'dark' ? 'dark' : 'light';
            docEl.setAttribute('data-theme', normalized);
            if (persist) persistTheme(normalized);
            updateButton(normalized);
        };

        const stored = readStored();
        const currentAttr = docEl.getAttribute('data-theme');
        const initialTheme = (stored === 'dark' || stored === 'light')
            ? stored
            : (currentAttr === 'dark' || currentAttr === 'light')
                ? currentAttr
                : prefersDark ? 'dark' : 'light';

        applyTheme(initialTheme, stored !== initialTheme);

        if (trigger) {
            trigger.addEventListener('click', () => {
                const activeTheme = docEl.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
                const nextTheme = activeTheme === 'dark' ? 'light' : 'dark';
                applyTheme(nextTheme, true);
            });
        }
    }

    const empresaId = () => document.body ? document.body.dataset.empresaId : undefined;
    const apiBaseUrl = () => document.body ? document.body.dataset.apiBase : undefined;
    const buildApiUrl = (path) => {
        const base = apiBaseUrl();
        if (base && /^https?:/i.test(base)) {
            const normalizedBase = base.endsWith('/') ? base.slice(0, -1) : base;
            const normalizedPath = path.startsWith('/') ? path.substring(1) : path;
            return `${normalizedBase}/${normalizedPath}`;
        }
        return path;
    };

    Alfa.initProcessosLista = function (root) {
        if (!root) return;

        const items = Array.from(root.querySelectorAll('[data-processo-item]'));
        if (!items.length) return;

        const board = root.querySelector('[data-status-board]');
        const startInput = root.querySelector('[data-filter-start]');
        const endInput = root.querySelector('[data-filter-end]');
        const resetButton = root.querySelector('[data-filter-reset]');
        const searchInput = root.querySelector('[data-filter-search]');
        const emptyState = root.querySelector('[data-empty]');

        const filters = {
            statuses: new Set(),
            start: null,
            end: null,
            query: ''
        };

        const laneMatchers = [
            { key: 'planejamento', patterns: [/backlog/i, /planej/i, /novo/i, /aguard/i, /entrada/i] },
            { key: 'execucao', patterns: [/andamento/i, /execu/i, /progresso/i, /iniciado/i, /curso/i] },
            { key: 'revisao', patterns: [/revis/i, /aprov/i, /pendente/i, /anal[ií]se/i, /bloque/i] },
            { key: 'concluido', patterns: [/conclu/i, /finaliz/i, /complet/i, /encerr/i] }
        ];

        const laneBodies = new Map();
        if (board) {
            board.querySelectorAll('[data-lane]').forEach(col => {
                const body = col.querySelector('[data-lane-body]');
                laneBodies.set(col.dataset.lane || 'outros', body);
            });
        }

        const normalizeText = (value) => {
            if (!value) return '';
            const text = value.toString();
            if (typeof text.normalize === 'function') {
                return text
                    .normalize('NFD')
                    .replace(/[\u0300-\u036f]/g, '')
                    .toLowerCase();
            }
            return text.toLowerCase();
        };

        const statusValues = Array.from(new Set(items
            .map(item => (item.dataset.status || '').trim())
            .filter(Boolean)))
            .sort((a, b) => a.localeCompare(b));

        const resolveLane = (status) => {
            for (const lane of laneMatchers) {
                if (lane.patterns.some(re => re.test(status))) return lane.key;
            }
            return 'outros';
        };

        if (board) {
            statusValues.forEach(status => {
                const laneKey = resolveLane(status);
                const body = laneBodies.get(laneKey) || laneBodies.get('outros');
                if (!body) return;

                const count = items.filter(item => (item.dataset.status || '').trim() === status).length;
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'kanban-chip';
                chip.dataset.status = status;
                chip.setAttribute('data-active', 'false');
                chip.innerHTML = `<span class="text">${status}</span><span class="count">${count}</span>`;
                chip.addEventListener('click', () => {
                    const active = chip.getAttribute('data-active') === 'true';
                    if (active) {
                        chip.setAttribute('data-active', 'false');
                        filters.statuses.delete(status);
                    } else {
                        chip.setAttribute('data-active', 'true');
                        filters.statuses.add(status);
                    }
                    applyFilters();
                });
                body.appendChild(chip);
            });
        }

        const parseDate = (value, endOfDay = false) => {
            if (!value) return null;
            const date = new Date(`${value}T00:00:00`);
            if (Number.isNaN(date.getTime())) return null;
            if (endOfDay) {
                date.setHours(23, 59, 59, 999);
            }
            return date;
        };

        if (startInput) {
            startInput.addEventListener('change', () => {
                filters.start = parseDate(startInput.value);
                applyFilters();
            });
        }

        if (endInput) {
            endInput.addEventListener('change', () => {
                filters.end = parseDate(endInput.value, true);
                applyFilters();
            });
        }

        if (resetButton) {
            resetButton.addEventListener('click', () => {
                filters.statuses.clear();
                filters.start = null;
                filters.end = null;
                filters.query = '';
                if (startInput) startInput.value = '';
                if (endInput) endInput.value = '';
                if (searchInput) searchInput.value = '';
                if (board) {
                    board.querySelectorAll('.kanban-chip').forEach(chip => {
                        chip.setAttribute('data-active', 'false');
                    });
                }
                applyFilters();
            });
        }

        if (searchInput) {
            const handleSearch = () => {
                const raw = searchInput.value || '';
                filters.query = normalizeText(raw.trim());
                applyFilters();
            };
            searchInput.addEventListener('input', handleSearch);
            searchInput.addEventListener('change', handleSearch);
        }

        const applyFilters = () => {
            let visibleCount = 0;

            items.forEach(item => {
                const status = (item.dataset.status || '').trim();
                const createdRaw = item.dataset.created;
                const createdDate = createdRaw ? new Date(createdRaw) : null;
                const title = (item.dataset.title || '').trim();
                const users = (item.dataset.users || '').trim();

                let visible = true;

                if (filters.statuses.size > 0 && !filters.statuses.has(status)) {
                    visible = false;
                }

                if (visible && filters.query) {
                    const normalizedTitle = normalizeText(title);
                    const normalizedUsers = normalizeText(users);
                    if (!normalizedTitle.includes(filters.query) && !normalizedUsers.includes(filters.query)) {
                        visible = false;
                    }
                }

                if (visible && filters.start && createdDate instanceof Date && !Number.isNaN(createdDate.getTime())) {
                    if (createdDate < filters.start) visible = false;
                }

                if (visible && filters.end && createdDate instanceof Date && !Number.isNaN(createdDate.getTime())) {
                    if (createdDate > filters.end) visible = false;
                }

                item.classList.toggle('d-none', !visible);
                if (visible) visibleCount += 1;
            });

            if (emptyState) {
                emptyState.classList.toggle('d-none', visibleCount > 0);
            }
        };

        applyFilters();
    };

    Alfa.initProcessoDetalhes = function (root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId, 10);
        if (!processoId) return;

        const selectors = Array.from(root.querySelectorAll('[data-page-selector]'));
        const sections = Array.from(root.querySelectorAll('[data-page-section]'));

        const own = (obj, key) => Object.prototype.hasOwnProperty.call(obj, key);
        const buildKeyVariants = (key) => {
            if (!key) return [];
            const camel = key.charAt(0).toLowerCase() + key.slice(1);
            const snake = key
                .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
                .replace(/__/g, '_')
                .toLowerCase();
            const lower = key.toLowerCase();
            return Array.from(new Set([key, camel, snake, lower].filter(Boolean)));
        };
        const readProp = (obj, key) => {
            if (!obj || !key) return undefined;
            for (const variant of buildKeyVariants(key)) {
                if (variant && own(obj, variant)) {
                    return obj[variant];
                }
            }
            return undefined;
        };
        const readArray = (obj, key) => {
            const value = readProp(obj, key);
            return Array.isArray(value) ? value : [];
        };
        const readNumber = (obj, key) => {
            const value = readProp(obj, key);
            if (value == null) return undefined;
            if (typeof value === 'number') {
                return Number.isFinite(value) ? value : undefined;
            }
            const normalized = String(value).trim().replace(',', '.');
            if (normalized === '') return undefined;
            const parsed = Number(normalized);
            return Number.isFinite(parsed) ? parsed : undefined;
        };
        const readBoolean = (obj, key) => {
            const value = readProp(obj, key);
            if (typeof value === 'boolean') return value;
            if (typeof value === 'number') return value !== 0;
            if (typeof value === 'string') {
                const normalized = value.trim().toLowerCase();
                if (normalized === 'true') return true;
                if (normalized === 'false') return false;
                if (normalized === '1') return true;
                if (normalized === '0') return false;
            }
            return undefined;
        };
        const readText = (obj, key) => {
            const value = readProp(obj, key);
            if (value == null) return undefined;
            return typeof value === 'string' ? value : String(value);
        };

        function showPage(faseId, pageId) {
            selectors.forEach(btn => {
                const active = btn.dataset.faseId === faseId && btn.dataset.pageId === pageId;
                btn.classList.toggle('active', active);
            });

            sections.forEach(section => {
                const match = section.dataset.faseId === faseId && section.dataset.pageId === pageId;
                section.classList.toggle('d-none', !match);
            });
        }

        selectors.forEach(btn => {
            btn.addEventListener('click', () => {
                showPage(btn.dataset.faseId, btn.dataset.pageId);
            });
        });

        if (selectors.length > 0) {
            const first = selectors[0];
            showPage(first.dataset.faseId, first.dataset.pageId);
        }

        const forms = Array.from(root.querySelectorAll('form[data-resposta-form]'));
        forms.forEach(form => {
            form.addEventListener('submit', async (ev) => {
                ev.preventDefault();
                const faseId = form.dataset.faseId;
                const paginaId = form.dataset.pageId;
                const button = form.querySelector('button[type="submit"]');
                const reloadButton = form.querySelector('[data-recarregar-pagina]');
                const feedback = form.querySelector('[data-saving-feedback]');
                if (feedback) feedback.hidden = false;
                if (button) {
                    button.dataset.originalText = button.dataset.originalText || button.textContent;
                    button.textContent = button.dataset.loadingText || 'Salvando...';
                    button.disabled = true;
                }
                if (reloadButton) reloadButton.disabled = true;

                try {
                    const camposPayload = Array.from(form.querySelectorAll('[data-campo-id]')).map(wrapper => {
                        const campoId = parseInt(wrapper.dataset.campoId, 10);
                        const tipo = wrapper.dataset.tipo || '';
                        const input = wrapper.querySelector('[data-field-input]');
                        if (!campoId || !input) return null;
                        const payload = { CampoInstanciaId: campoId, ValorTexto: null, ValorNumero: null, ValorData: null, ValorBool: null };
                        switch (tipo) {
                            case 'number': {
                                const raw = input.value;
                                if (raw !== '') {
                                    const parsed = parseFloat(raw);
                                    if (!Number.isNaN(parsed)) payload.ValorNumero = parsed;
                                }
                                break;
                            }
                            case 'date':
                                payload.ValorData = input.value || null;
                                break;
                            case 'checkbox':
                                payload.ValorBool = input.checked;
                                break;
                            case 'textarea':
                            case 'select':
                            case 'text':
                            default:
                                payload.ValorTexto = input.value ? input.value : null;
                                break;
                        }
                        return payload;
                    }).filter(Boolean);

                    const payload = {
                        FaseInstanciaId: parseInt(faseId, 10),
                        PaginaInstanciaId: parseInt(paginaId, 10),
                        Campos: camposPayload
                    };

                    const headers = { 'Content-Type': 'application/json' };
                    const empId = empresaId();
                    if (empId) headers['X-Empresa-Id'] = empId;
                    const resp = await fetch(buildApiUrl(`/processos/${processoId}/respostas`), {
                        method: 'POST',
                        headers,
                        body: JSON.stringify(payload)
                    });

                    if (!resp.ok) {
                        throw new Error('Falha ao salvar respostas');
                    }

                    await refreshProcess(paginaId);
                    Alfa.toast('Respostas salvas com sucesso!', 'success');
                } catch (err) {
                    console.error(err);
                    Alfa.toast('Não foi possível salvar as respostas. Tente novamente.', 'danger');
                } finally {
                    if (feedback) feedback.hidden = true;
                    if (button) {
                        button.textContent = button.dataset.originalText || 'Salvar respostas';
                        button.disabled = false;
                    }
                    if (reloadButton) reloadButton.disabled = false;
                }
            });
        });

        root.querySelectorAll('[data-recarregar-pagina]').forEach(btn => {
            btn.addEventListener('click', async () => {
                const form = btn.closest('form[data-resposta-form]');
                if (!form) return;
                const paginaId = form.dataset.pageId;
                btn.disabled = true;
                try {
                    await refreshProcess(paginaId);
                    Alfa.toast('Página atualizada com os dados mais recentes.', 'info');
                } catch (err) {
                    console.error(err);
                    Alfa.toast('Não foi possível recarregar os dados.', 'danger');
                } finally {
                    btn.disabled = false;
                }
            });
        });

        function resolveVariantClasses(value) {
            if (value >= 100) return ['bg-success'];
            if (value >= 70) return ['bg-primary'];
            if (value > 0) return ['bg-warning', 'text-dark'];
            return ['bg-secondary'];
        }

        function updateProgress(element, value, labelText) {
            if (!element) return;
            const numericValue = typeof value === 'number' && Number.isFinite(value)
                ? value
                : Number(readNumber({ tmp: value }, 'tmp') ?? 0);
            const val = Math.max(0, Math.min(100, Math.round(Number.isFinite(numericValue) ? numericValue : 0)));
            element.dataset.progressValue = String(val);
            const bar = element.querySelector('.progress-bar');
            if (!bar) return;
            bar.style.width = `${val}%`;
            bar.setAttribute('aria-valuenow', String(val));
            const hideLabel = element.dataset.progressHideLabel === 'true';
            const label = typeof labelText === 'string' && labelText.trim() !== '' ? labelText : `${val}%`;
            if (hideLabel) {
                let hidden = bar.querySelector('.visually-hidden');
                if (!hidden) {
                    hidden = document.createElement('span');
                    hidden.className = 'visually-hidden';
                    bar.textContent = '';
                    bar.appendChild(hidden);
                }
                hidden.textContent = label;
            } else {
                bar.textContent = label;
            }
            const variants = resolveVariantClasses(val);
            const classes = ['bg-success', 'bg-primary', 'bg-warning', 'bg-secondary', 'text-dark'];
            classes.forEach(cls => bar.classList.remove(cls));
            variants.forEach(cls => bar.classList.add(cls));
        }

        function updateProcessSummary(data) {
            if (!data) return;
            const progressEl = root.querySelector('[data-progresso-processo]');
            const progressoValor = readNumber(data, 'PorcentagemProgresso');
            const progresso = Number.isFinite(progressoValor) ? Math.round(progressoValor) : 0;
            updateProgress(progressEl, progresso, `${progresso}%`);
            const status = root.querySelector('[data-status-processo]');
            const statusText = readText(data, 'Status');
            if (status && typeof statusText === 'string') status.textContent = statusText;
        }

        function updatePhaseCards(fases) {
            if (!Array.isArray(fases)) return;
            fases.forEach(fase => {
                const faseId = readProp(fase, 'Id');
                if (faseId == null) return;
                const card = root.querySelector(`[data-fase-card="${faseId}"]`);
                if (!card) return;
                const statusBadge = card.querySelector(`[data-fase-status="${faseId}"]`);
                const faseStatus = readText(fase, 'Status');
                if (statusBadge && typeof faseStatus === 'string') statusBadge.textContent = faseStatus;
                const faseProgressoValor = readNumber(fase, 'PorcentagemProgresso');
                const faseProgresso = Number.isFinite(faseProgressoValor) ? Math.round(faseProgressoValor) : 0;
                updateProgress(card.querySelector(`#fase-progress-${faseId}`), faseProgresso, `${faseProgresso}%`);

                readArray(fase, 'Paginas').forEach(pagina => {
                    const paginaId = readProp(pagina, 'Id');
                    if (paginaId == null) return;
                    const concluida = readBoolean(pagina, 'Concluida') ?? false;
                    const selector = card.querySelector(`[data-page-selector][data-page-id="${paginaId}"]`);
                    if (selector) {
                        const icon = selector.querySelector('[data-page-status]');
                        if (icon) {
                            icon.textContent = concluida ? '✔' : '•';
                            icon.classList.toggle('text-success', concluida);
                            icon.classList.toggle('text-muted', !concluida);
                        }
                        selector.classList.toggle('completed', concluida);
                    }

                    const badge = root.querySelector(`[data-page-complete-badge="${paginaId}"]`);
                    if (badge) {
                        badge.textContent = concluida ? 'Página concluída' : 'Página pendente';
                        badge.classList.toggle('bg-success', concluida);
                        badge.classList.toggle('bg-secondary', !concluida);
                    }
                });
            });
        }

        function updateFormFields(fases) {
            if (!Array.isArray(fases)) return;
            fases.forEach(fase => {
                readArray(fase, 'Paginas').forEach(pagina => {
                    const paginaId = readProp(pagina, 'Id');
                    if (paginaId == null) return;
                    const form = root.querySelector(`form[data-page-id="${paginaId}"]`);
                    if (!form) return;
                    readArray(pagina, 'Campos').forEach(campo => {
                        const campoId = readProp(campo, 'Id');
                        if (campoId == null) return;
                        const wrapper = form.querySelector(`[data-campo-id="${campoId}"]`);
                        if (!wrapper) return;
                        const input = wrapper.querySelector('[data-field-input]');
                        if (!input) return;
                        switch (wrapper.dataset.tipo) {
                            case 'number':
                                {
                                    const valorNumero = readNumber(campo, 'ValorNumero');
                                    input.value = valorNumero != null ? String(valorNumero) : '';
                                }
                                break;
                            case 'date':
                                {
                                    const valorData = readText(campo, 'ValorData');
                                    input.value = valorData ? valorData.substring(0, 10) : '';
                                }
                                break;
                            case 'checkbox':
                                {
                                    const valorBool = readBoolean(campo, 'ValorBool');
                                    input.checked = Boolean(valorBool);
                                }
                                break;
                            case 'textarea':
                            case 'select':
                            case 'text':
                            default:
                                {
                                    const valorTexto = readText(campo, 'ValorTexto');
                                    input.value = valorTexto != null ? String(valorTexto) : '';
                                }
                                break;
                        }
                    });
                });
            });
        }

        async function refreshProcess(focusPageId) {
            const headers = {};
            const empId = empresaId();
            if (empId) headers['X-Empresa-Id'] = empId;
            const resp = await fetch(buildApiUrl(`/processos/${processoId}`), { headers });
            if (!resp.ok) throw new Error('Erro ao atualizar processo');
            const data = await resp.json();
            updateProcessSummary(data);
            const fases = readArray(data, 'Fases');
            updatePhaseCards(fases);
            updateFormFields(fases);
            if (focusPageId) {
                const active = root.querySelector(`[data-page-selector][data-page-id="${focusPageId}"]`);
                if (active) {
                    showPage(active.dataset.faseId, active.dataset.pageId);
                }
            }
            return data;
        }
    };
})();
