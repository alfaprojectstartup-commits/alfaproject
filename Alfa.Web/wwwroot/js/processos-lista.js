(function () {
    const Alfa = window.Alfa || {};
    const toast = typeof Alfa.toast === 'function' ? Alfa.toast : () => {};
    const postJson = typeof Alfa.postJson === 'function' ? Alfa.postJson : null;
    if (!postJson) {
        console.warn('processos-lista.js: Alfa.postJson não está disponível.');
        return;
    }

    function initProcessosLista(root) {
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
                toast('Não foi possível identificar o processo.', 'danger');
                hideContextMenu();
                return;
            }

            if (contextMenu) {
                contextMenu.classList.add('is-loading');
            }

            try {
                await postJson('/Processos/AlterarStatus', { token, status: newStatus }, 'Não foi possível atualizar o status.');

                item.dataset.status = newStatus;
                const badge = item.querySelector('.status-badge');
                if (badge) badge.textContent = newStatus;
                statusSet.add(newStatus);
                hideContextMenu();
                refreshStatusBoard();
                applyFilters();
                toast('Status do processo atualizado.', 'success');
            } catch (err) {
                console.error('Erro ao atualizar status do processo.', err);
                const message = err instanceof Error && err.message ? err.message : 'Não foi possível atualizar o status.';
                toast(message, 'danger');
            } finally {
                if (contextMenu) {
                    contextMenu.classList.remove('is-loading');
                }
            }
        }

        applyFilters();
    }

    document.addEventListener('DOMContentLoaded', () => {
        const root = document.getElementById('processos-root');
        if (root) initProcessosLista(root);
    });
})();
