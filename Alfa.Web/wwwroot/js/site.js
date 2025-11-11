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
        const antiforgeryInput = root.querySelector('#processos-antiforgery input[name="__RequestVerificationToken"]');
        const historicoUrlTemplate = root.dataset.historicoUrlTemplate || '';

        const contextMenu = root.querySelector('[data-processo-menu]');
        const contextMenuStatusGroup = contextMenu?.querySelector('[data-menu-status]') || null;
        const contextMenuDetailsButton = contextMenu?.querySelector('[data-menu-detalhes]') || null;
        const contextMenuHistoricoButton = contextMenu?.querySelector('[data-menu-historico]') || null;

        const filters = {
            statuses: new Set(),
            start: null,
            end: null,
            query: ''
        };

        const normalizeStatusKey = (status) => (status || '').trim().toLowerCase();

        const defaultStatuses = [
            { name: 'Em Planejamento', lane: 'planejamento' },
            { name: 'Em Andamento', lane: 'execucao' },
            { name: 'Em Revisão', lane: 'revisao' },
            { name: 'Concluído', lane: 'concluido' },
            { name: 'Outros', lane: 'outros' }
        ];

        const statusLaneOverrides = new Map(defaultStatuses.map(s => [normalizeStatusKey(s.name), s.lane]));
        const statusSortOrder = new Map(defaultStatuses.map((s, idx) => [normalizeStatusKey(s.name), idx]));

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

        const statusSet = new Set(defaultStatuses.map(s => s.name));
        items.forEach(item => {
            const status = (item.dataset.status || '').trim();
            if (status) {
                statusSet.add(status);
            }
        });

        const getItemStatus = (item) => (item.dataset.status || '').trim();

        const chipElements = new Map();

        const resolveLane = (status) => {
            const override = statusLaneOverrides.get(normalizeStatusKey(status));
            if (override) return override;
            for (const lane of laneMatchers) {
                if (lane.patterns.some(re => re.test(status))) return lane.key;
            }
            return 'outros';
        };

        const createChip = (status) => {
            if (!board) return null;
            if (chipElements.has(status)) {
                return chipElements.get(status) || null;
            }
            const laneKey = resolveLane(status);
            const body = laneBodies.get(laneKey) || laneBodies.get('outros');
            if (!body) return null;

            const chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'kanban-chip';
            chip.dataset.status = status;
            chip.setAttribute('data-active', filters.statuses.has(status) ? 'true' : 'false');
            chip.innerHTML = `<span class="text">${status}</span><span class="count">0</span>`;
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
            chipElements.set(status, chip);
            return chip;
        };

        const ensureChip = (status) => {
            if (!chipElements.has(status)) {
                statusSet.add(status);
                return createChip(status);
            }
            return chipElements.get(status) || null;
        };

        const compareStatuses = (a, b) => {
            const aKey = normalizeStatusKey(a);
            const bKey = normalizeStatusKey(b);
            const orderA = statusSortOrder.has(aKey) ? statusSortOrder.get(aKey) : null;
            const orderB = statusSortOrder.has(bKey) ? statusSortOrder.get(bKey) : null;
            if (orderA != null && orderB != null) return orderA - orderB;
            if (orderA != null) return -1;
            if (orderB != null) return 1;
            return a.localeCompare(b);
        };

        const refreshStatusBoard = () => {
            if (!board) return;
            const counts = new Map();
            items.forEach(item => {
                const status = getItemStatus(item);
                if (!status) return;
                counts.set(status, (counts.get(status) || 0) + 1);
            });

            counts.forEach((count, status) => {
                const chip = ensureChip(status);
                if (chip) {
                    const countEl = chip.querySelector('.count');
                    if (countEl) countEl.textContent = String(count);
                }
            });

            Array.from(statusSet).forEach(status => {
                if (!counts.has(status)) {
                    const chip = ensureChip(status);
                    if (chip) {
                        const countEl = chip.querySelector('.count');
                        if (countEl) countEl.textContent = '0';
                    }
                }
            });
        };

        if (board) {
            Array.from(statusSet)
                .sort(compareStatuses)
                .forEach(status => {
                    createChip(status);
                });
            refreshStatusBoard();
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
                chipElements.forEach(chip => {
                    chip.setAttribute('data-active', 'false');
                });
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

        let menuActiveItem = null;

        const hideContextMenu = () => {
            if (!contextMenu) return;
            contextMenu.classList.add('d-none');
            contextMenu.classList.remove('is-loading');
            menuActiveItem = null;
        };

        const positionContextMenu = (x, y) => {
            if (!contextMenu) return;
            const padding = 8;
            const rect = contextMenu.getBoundingClientRect();
            const width = rect.width || 240;
            const height = rect.height || 200;
            let left = x;
            let top = y;

            const maxLeft = window.innerWidth - width - padding;
            const maxTop = window.innerHeight - height - padding;

            left = Math.min(Math.max(left, padding), Math.max(padding, maxLeft));
            top = Math.min(Math.max(top, padding), Math.max(padding, maxTop));

            contextMenu.style.left = `${left}px`;
            contextMenu.style.top = `${top}px`;
        };

        const openHistorico = (token) => {
            if (!token || !historicoUrlTemplate) {
                console.warn('URL do log não configurada.');
                return;
            }
            const url = historicoUrlTemplate.replace('__token__', encodeURIComponent(token));
            window.open(url, '_blank', 'noopener');
        };

        const buildContextMenu = (item) => {
            if (contextMenuStatusGroup) {
                contextMenuStatusGroup.innerHTML = '';
                const currentStatus = getItemStatus(item);
                const statuses = Array.from(statusSet).sort(compareStatuses);

                statuses.forEach(status => {
                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'processo-context-menu__item';
                    button.textContent = status;
                    if (status === currentStatus) {
                        button.classList.add('is-active');
                        button.setAttribute('aria-current', 'true');
                    }
                    button.addEventListener('click', () => {
                        changeStatus(item, status);
                    });
                    contextMenuStatusGroup.appendChild(button);
                });
            }

            if (contextMenuDetailsButton) {
                const link = item.querySelector('a.stretched-link');
                contextMenuDetailsButton.disabled = !link;
            }

            if (contextMenuHistoricoButton) {
                const token = (item.dataset.token || '').trim();
                contextMenuHistoricoButton.disabled = !(token && historicoUrlTemplate);
            }
        };

        if (contextMenu) {
            contextMenu.addEventListener('contextmenu', (event) => {
                event.preventDefault();
            });
        }

        if (contextMenuDetailsButton) {
            contextMenuDetailsButton.addEventListener('click', () => {
                if (!menuActiveItem) return;
                const link = menuActiveItem.querySelector('a.stretched-link');
                if (link) {
                    hideContextMenu();
                    window.location.href = link.href;
                }
            });
        }

        if (contextMenuHistoricoButton) {
            contextMenuHistoricoButton.addEventListener('click', () => {
                if (!menuActiveItem) return;
                const token = (menuActiveItem.dataset.token || '').trim();
                if (!token) return;
                hideContextMenu();
                openHistorico(token);
            });
        }

        root.addEventListener('contextmenu', (event) => {
            const item = event.target instanceof Element
                ? event.target.closest('[data-processo-item]')
                : null;
            if (!item) return;

            event.preventDefault();
            menuActiveItem = item;
            buildContextMenu(item);
            if (contextMenu) {
                contextMenu.classList.remove('d-none');
                positionContextMenu(event.clientX, event.clientY);
            }
        });

        document.addEventListener('pointerdown', (event) => {
            if (!contextMenu || contextMenu.classList.contains('d-none')) return;
            if (contextMenu.contains(event.target)) return;
            hideContextMenu();
        });

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                hideContextMenu();
            }
        });

        window.addEventListener('scroll', hideContextMenu, true);
        window.addEventListener('resize', hideContextMenu);

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

        async function changeStatus(item, newStatus) {
            if (!item) return;
            const currentStatus = getItemStatus(item);
            if (currentStatus === newStatus) {
                hideContextMenu();
                return;
            }

            const token = item.dataset.token;
            if (!token) {
                Alfa.toast('Não foi possível identificar o processo.', 'danger');
                hideContextMenu();
                return;
            }

            if (contextMenu) {
                contextMenu.classList.add('is-loading');
            }

            const headers = { 'Content-Type': 'application/json' };
            if (antiforgeryInput && antiforgeryInput.value) {
                headers['RequestVerificationToken'] = antiforgeryInput.value;
            }

            try {
                const resp = await fetch('/Processos/AlterarStatus', {
                    method: 'POST',
                    headers,
                    body: JSON.stringify({ token, status: newStatus })
                });

                if (!resp.ok) {
                    let message = 'Não foi possível atualizar o status.';
                    const raw = await resp.text();
                    if (raw && raw.trim() !== '') {
                        try {
                            const parsed = JSON.parse(raw);
                            if (parsed) {
                                if (typeof parsed.message === 'string' && parsed.message.trim() !== '') {
                                    message = parsed.message.trim();
                                } else if (typeof parsed === 'string' && parsed.trim() !== '') {
                                    message = parsed.trim();
                                } else {
                                    message = raw.trim();
                                }
                            }
                        } catch {
                            message = raw.trim();
                        }
                    }
                    Alfa.toast(message, 'danger');
                    return;
                }

                item.dataset.status = newStatus;
                const badge = item.querySelector('.status-badge');
                if (badge) badge.textContent = newStatus;
                statusSet.add(newStatus);
                hideContextMenu();
                refreshStatusBoard();
                applyFilters();
                Alfa.toast('Status do processo atualizado.', 'success');
            } catch (err) {
                console.error('Erro ao atualizar status do processo.', err);
                Alfa.toast('Não foi possível atualizar o status.', 'danger');
            } finally {
                if (contextMenu) {
                    contextMenu.classList.remove('is-loading');
                }
            }
        }

        applyFilters();
    };

    Alfa.initProcessoDetalhes = function (root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId, 10);
        if (!processoId) return;

        const selectors = Array.from(root.querySelectorAll('[data-page-selector]'));
        const sections = Array.from(root.querySelectorAll('[data-page-section]'));
        const linkButtons = Array.from(root.querySelectorAll('[data-link-preenchimento]'));
        const linkEndpoint = root.dataset.linkPreenchimentoUrl;
        const antiforgeryInput = document.querySelector('#preenchimento-externo-antiforgery input[name="__RequestVerificationToken"]');

        const own = (obj, key) => Object.prototype.hasOwnProperty.call(obj, key);
        const normalizeKey = (key) => !key ? key : key.charAt(0).toLowerCase() + key.slice(1);
        const readProp = (obj, key) => {
            if (!obj || !key) return undefined;
            if (own(obj, key)) return obj[key];
            const camel = normalizeKey(key);
            if (camel && own(obj, camel)) return obj[camel];
            return undefined;
        };
        const readArray = (obj, key) => {
            const value = readProp(obj, key);
            return Array.isArray(value) ? value : [];
        };
        const readNumber = (obj, key) => {
            const value = readProp(obj, key);
            if (typeof value === 'number') return value;
            if (typeof value === 'string' && value.trim() !== '') {
                const parsed = Number(value);
                if (!Number.isNaN(parsed)) return parsed;
            }
            return undefined;
        };
        const readBoolean = (obj, key) => {
            const value = readProp(obj, key);
            if (typeof value === 'boolean') return value;
            if (typeof value === 'number') return value !== 0;
            if (typeof value === 'string') return value.toLowerCase() === 'true';
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

        linkButtons.forEach(btn => {
            btn.addEventListener('click', async () => {
                if (!linkEndpoint) {
                    Alfa.toast('Endpoint para geração de link não configurado.', 'danger');
                    return;
                }

                const faseId = parseInt(btn.dataset.faseId || '0', 10);
                const paginaId = parseInt(btn.dataset.pageId || '0', 10);
                if (!faseId || !paginaId) return;

                const originalText = btn.textContent;
                btn.disabled = true;
                btn.textContent = 'Gerando link...';

                try {
                    const headers = { 'Content-Type': 'application/json' };
                    if (antiforgeryInput && antiforgeryInput.value) {
                        headers['RequestVerificationToken'] = antiforgeryInput.value;
                    }

                    const resp = await fetch(linkEndpoint, {
                        method: 'POST',
                        headers,
                        body: JSON.stringify({
                            processoId,
                            faseInstanciaId: faseId,
                            paginaInstanciaId: paginaId
                        })
                    });

                    if (!resp.ok) {
                        throw new Error('Falha ao gerar link.');
                    }

                    const data = await resp.json();
                    const link = data?.link;
                    if (!link) {
                        throw new Error('Resposta inválida.');
                    }

                    if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
                        try {
                            await navigator.clipboard.writeText(link);
                            Alfa.toast('Link copiado para a área de transferência.', 'success', 'Preenchimento externo');
                        } catch (clipboardErr) {
                            console.warn('Não foi possível copiar automaticamente.', clipboardErr);
                            Alfa.toast(`Link gerado: <br><small>${link}</small>`, 'info', 'Preenchimento externo');
                        }
                    } else {
                        Alfa.toast(`Link gerado: <br><small>${link}</small>`, 'info', 'Preenchimento externo');
                    }
                } catch (err) {
                    console.error(err);
                    Alfa.toast('Não foi possível gerar o link público.', 'danger');
                } finally {
                    btn.disabled = false;
                    btn.textContent = originalText;
                }
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
                    const lerArquivoComoBase64 = (arquivo) => new Promise((resolve, reject) => {
                        const reader = new FileReader();
                        reader.onload = () => {
                            if (typeof reader.result === 'string') {
                                resolve(reader.result);
                            } else {
                                reject(new Error('Não foi possível ler o arquivo selecionado.'));
                            }
                        };
                        reader.onerror = () => reject(reader.error || new Error('Falha ao processar o arquivo.'));
                        reader.readAsDataURL(arquivo);
                    });

                    const wrappers = Array.from(form.querySelectorAll('[data-campo-id]'));
                    const camposPayload = (await Promise.all(wrappers.map(async wrapper => {
                        const campoId = parseInt(wrapper.dataset.campoId, 10);
                        const tipo = (wrapper.dataset.tipo || '').toLowerCase();
                        const input = wrapper.querySelector('[data-field-input]');
                        if (!campoId) return null;
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
                            case 'checkboxlist': {
                                const selecionados = Array.from(wrapper.querySelectorAll('[data-field-input]'))
                                    .filter(el => el instanceof HTMLInputElement && el.checked)
                                    .map(el => el.value)
                                    .filter(valor => typeof valor === 'string' && valor.trim() !== '');
                                payload.ValorTexto = selecionados.length > 0 ? selecionados.join(';') : null;
                                break;
                            }
                            case 'signature':
                            case 'image': {
                                const arquivoInput = input;
                                const existente = wrapper.querySelector('[data-existing-file]');
                                const valorPersistido = existente ? (existente.value || null) : null;
                                if (arquivoInput && arquivoInput.files && arquivoInput.files.length > 0) {
                                    const arquivo = arquivoInput.files[0];
                                    if (tipo === 'image' && arquivo.size > (50 * 1024 * 1024)) {
                                        throw new Error('O arquivo de imagem excede o limite de 50 MB.');
                                    }
                                    payload.ValorTexto = await lerArquivoComoBase64(arquivo);
                                } else {
                                    payload.ValorTexto = valorPersistido;
                                }
                                break;
                            }
                            case 'textarea':
                            case 'select':
                            case 'text':
                            default:
                                if (input) {
                                    payload.ValorTexto = input.value ? input.value : null;
                                }
                                break;
                        }
                        return payload;
                    }))).filter(Boolean);

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
            const progresso = readNumber(data, 'PorcentagemProgresso') ?? 0;
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
                const faseProgresso = readNumber(fase, 'PorcentagemProgresso') ?? 0;
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

    Alfa.initPreenchimentoExterno = function (root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId || '0', 10);
        const faseId = parseInt(root.dataset.faseId || '0', 10);
        const paginaId = parseInt(root.dataset.paginaId || '0', 10);
        const token = root.dataset.token || '';
        if (!processoId || !faseId || !paginaId) return;

        const form = root.querySelector('[data-preenchimento-externo-form]');
        if (!form) return;

        form.addEventListener('submit', async (ev) => {
            ev.preventDefault();

            const button = form.querySelector('button[type="submit"]');
            const feedback = form.querySelector('[data-saving-feedback]');

            if (feedback) feedback.hidden = false;
            if (button) {
                button.dataset.originalText = button.dataset.originalText || button.textContent;
                button.textContent = button.dataset.loadingText || 'Enviando...';
                button.disabled = true;
            }

            try {
                const camposPayload = Array.from(form.querySelectorAll('[data-campo-id]')).map(wrapper => {
                    const campoId = parseInt(wrapper.dataset.campoId || '0', 10);
                    if (!campoId) return null;
                    const tipo = wrapper.dataset.tipo || '';
                    const input = wrapper.querySelector('[data-field-input]');
                    if (!input) return null;

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
                            payload.ValorBool = !!input.checked;
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
                    FaseInstanciaId: faseId,
                    PaginaInstanciaId: paginaId,
                    Campos: camposPayload
                };

                const headers = { 'Content-Type': 'application/json' };
                if (token) headers['X-Preenchimento-Token'] = token;

                const resp = await fetch(buildApiUrl(`/processos/${processoId}/respostas`), {
                    method: 'POST',
                    headers,
                    body: JSON.stringify(payload)
                });

                if (!resp.ok) {
                    throw new Error('Falha ao enviar respostas');
                }

                Alfa.toast('Respostas enviadas com sucesso!', 'success');
            } catch (err) {
                console.error(err);
                Alfa.toast('Não foi possível enviar as respostas. Tente novamente.', 'danger');
            } finally {
                if (feedback) feedback.hidden = true;
                if (button) {
                    button.textContent = button.dataset.originalText || 'Enviar respostas';
                    button.disabled = false;
                }
            }
        });
    };
})();
