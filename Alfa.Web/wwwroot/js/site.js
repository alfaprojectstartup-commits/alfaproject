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
    });

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

    Alfa.initProcessoDetalhes = function (root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId, 10);
        if (!processoId) return;

        const selectors = Array.from(root.querySelectorAll('[data-page-selector]'));
        const sections = Array.from(root.querySelectorAll('[data-page-section]'));

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
            const val = Math.max(0, Math.min(100, Math.round(value)));
            element.dataset.progressValue = String(val);
            const bar = element.querySelector('.progress-bar');
            if (!bar) return;
            bar.style.width = `${val}%`;
            bar.setAttribute('aria-valuenow', String(val));
            const hideLabel = element.dataset.progressHideLabel === 'true';
            if (hideLabel) {
                let hidden = bar.querySelector('.visually-hidden');
                if (!hidden) {
                    hidden = document.createElement('span');
                    hidden.className = 'visually-hidden';
                    bar.textContent = '';
                    bar.appendChild(hidden);
                }
                hidden.textContent = labelText ?? `${val}%`;
            } else {
                bar.textContent = labelText ?? `${val}%`;
            }
            const variants = resolveVariantClasses(val);
            const classes = ['bg-success', 'bg-primary', 'bg-warning', 'bg-secondary', 'text-dark'];
            classes.forEach(cls => bar.classList.remove(cls));
            variants.forEach(cls => bar.classList.add(cls));
        }

        function updateProcessSummary(data) {
            const progressEl = root.querySelector('[data-progresso-processo]');
            updateProgress(progressEl, data.PorcentagemProgresso, `${data.PorcentagemProgresso}%`);
            const status = root.querySelector('[data-status-processo]');
            if (status) status.textContent = data.Status;
        }

        function updatePhaseCards(fases) {
            fases.forEach(fase => {
                const card = root.querySelector(`[data-fase-card="${fase.Id}"]`);
                if (!card) return;
                const statusBadge = card.querySelector(`[data-fase-status="${fase.Id}"]`);
                if (statusBadge) statusBadge.textContent = fase.Status;
                updateProgress(card.querySelector(`#fase-progress-${fase.Id}`), fase.PorcentagemProgresso, `${fase.PorcentagemProgresso}%`);

                (fase.Paginas || []).forEach(pagina => {
                    const selector = card.querySelector(`[data-page-selector][data-page-id="${pagina.Id}"]`);
                    if (selector) {
                        const icon = selector.querySelector('[data-page-status]');
                        if (icon) {
                            icon.textContent = pagina.Concluida ? '✔' : '•';
                            icon.classList.toggle('text-success', pagina.Concluida);
                            icon.classList.toggle('text-muted', !pagina.Concluida);
                        }
                        selector.classList.toggle('completed', pagina.Concluida);
                    }

                    const badge = root.querySelector(`[data-page-complete-badge="${pagina.Id}"]`);
                    if (badge) {
                        badge.textContent = pagina.Concluida ? 'Página concluída' : 'Página pendente';
                        badge.classList.toggle('bg-success', pagina.Concluida);
                        badge.classList.toggle('bg-secondary', !pagina.Concluida);
                    }
                });
            });
        }

        function updateFormFields(fases) {
            fases.forEach(fase => {
                (fase.Paginas || []).forEach(pagina => {
                    const form = root.querySelector(`form[data-page-id="${pagina.Id}"]`);
                    if (!form) return;
                    (pagina.Campos || []).forEach(campo => {
                        const wrapper = form.querySelector(`[data-campo-id="${campo.Id}"]`);
                        if (!wrapper) return;
                        const input = wrapper.querySelector('[data-field-input]');
                        if (!input) return;
                        switch (wrapper.dataset.tipo) {
                            case 'number':
                                input.value = campo.ValorNumero != null ? String(campo.ValorNumero) : '';
                                break;
                            case 'date':
                                input.value = campo.ValorData ? campo.ValorData.substring(0, 10) : '';
                                break;
                            case 'checkbox':
                                input.checked = Boolean(campo.ValorBool);
                                break;
                            case 'textarea':
                            case 'select':
                            case 'text':
                            default:
                                input.value = campo.ValorTexto || '';
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
            if (Array.isArray(data.Fases)) {
                updatePhaseCards(data.Fases);
                updateFormFields(data.Fases);
            }
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
