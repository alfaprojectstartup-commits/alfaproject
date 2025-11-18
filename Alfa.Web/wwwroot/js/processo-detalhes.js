(function () {
    const Alfa = window.Alfa || {};
    const toast = typeof Alfa.toast === 'function' ? Alfa.toast : () => {};
    const fetchJson = typeof Alfa.fetchJson === 'function' ? Alfa.fetchJson : null;
    const postJson = typeof Alfa.postJson === 'function' ? Alfa.postJson : null;
    const initSignaturePads = typeof Alfa.initSignaturePads === 'function' ? Alfa.initSignaturePads : () => {};
    const syncSignatureValue = typeof Alfa.syncSignatureValue === 'function' ? Alfa.syncSignatureValue : () => {};
    if (!fetchJson || !postJson) {
        console.warn('processo-detalhes.js: dependências básicas não disponíveis.');
        return;
    }

    function initProcessoDetalhes(root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId, 10);
        if (!processoId) return;

        const selectors = Array.from(root.querySelectorAll('[data-page-selector]'));
        const jumpers = Array.from(root.querySelectorAll('[data-page-jump]'));
        const sections = Array.from(root.querySelectorAll('[data-page-section]'));
        const linkButtons = Array.from(root.querySelectorAll('[data-link-preenchimento]'));
        const linkEndpoint = root.dataset.linkPreenchimentoUrl;

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

        function scrollToSection(faseId, pageId) {
            const target = root.querySelector(`[data-page-section][data-fase-id="${faseId}"][data-page-id="${pageId}"]`);
            if (!target) return;
            const behavior = 'scrollBehavior' in document.documentElement.style ? 'smooth' : 'auto';
            target.scrollIntoView({ behavior, block: 'start' });
        }

        selectors.forEach(btn => {
            btn.addEventListener('click', () => {
                showPage(btn.dataset.faseId, btn.dataset.pageId);
            });
        });

        jumpers.forEach(btn => {
            btn.addEventListener('click', () => {
                const faseId = btn.dataset.faseId;
                const pageId = btn.dataset.pageId;
                if (!faseId || !pageId) return;
                showPage(faseId, pageId);
                scrollToSection(faseId, pageId);
            });
        });

        linkButtons.forEach(btn => {
            btn.addEventListener('click', async () => {
                if (!linkEndpoint) {
                    toast('Endpoint para geração de link não configurado.', 'danger');
                    return;
                }

                const faseId = parseInt(btn.dataset.faseId || '0', 10);
                const paginaId = parseInt(btn.dataset.pageId || '0', 10);
                if (!faseId || !paginaId) return;

                const originalText = btn.textContent;
                btn.disabled = true;
                btn.textContent = 'Gerando link...';

                try {
                    const data = await postJson(linkEndpoint, {
                        processoId,
                        faseInstanciaId: faseId,
                        paginaInstanciaId: paginaId
                    }, 'Falha ao gerar link.');
                    const link = data?.link;
                    if (!link) {
                        throw new Error('Resposta inválida.');
                    }

                    if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
                        try {
                            await navigator.clipboard.writeText(link);
                            toast('Link copiado para a área de transferência.', 'success', 'Preenchimento externo');
                        } catch (clipboardErr) {
                            console.warn('Não foi possível copiar automaticamente.', clipboardErr);
                            toast(`Link gerado: <br><small>${link}</small>`, 'info', 'Preenchimento externo');
                        }
                    } else {
                        toast(`Link gerado: <br><small>${link}</small>`, 'info', 'Preenchimento externo');
                    }
                } catch (err) {
                    console.error(err);
                    const message = err instanceof Error && err.message ? err.message : 'Não foi possível gerar o link público.';
                    toast(message, 'danger');
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
            initSignaturePads(form);
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
                            case 'signature': {
                                payload.ValorTexto = input && input.value ? input.value : null;
                                break;
                            }
                            case 'image': {
                                const arquivoInput = input;
                                const existente = wrapper.querySelector('[data-existing-file]');
                                const valorPersistido = existente ? (existente.value || null) : null;
                                if (arquivoInput && arquivoInput.files && arquivoInput.files.length > 0) {
                                    const arquivo = arquivoInput.files[0];
                                    if (arquivo.size > (50 * 1024 * 1024)) {
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
                        ProcessoId: processoId,
                        FaseInstanciaId: parseInt(faseId, 10),
                        PaginaInstanciaId: parseInt(paginaId, 10),
                        Campos: camposPayload
                    };

                    await postJson('/Processos/RegistrarRespostas', payload, 'Não foi possível salvar as respostas.');

                    await refreshProcess(paginaId);
                    toast('Respostas salvas com sucesso!', 'success');
                } catch (err) {
                    console.error(err);
                    const message = err instanceof Error && err.message
                        ? err.message
                        : 'Não foi possível salvar as respostas. Tente novamente.';
                    toast(message, 'danger');
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
                    toast('Página atualizada com os dados mais recentes.', 'info');
                } catch (err) {
                    console.error(err);
                    toast('Não foi possível recarregar os dados.', 'danger');
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
                            case 'signature':
                                {
                                    const valorAssinatura = readText(campo, 'ValorTexto') || '';
                                    input.value = valorAssinatura;
                                    const assinaturaContainer = wrapper.querySelector('[data-signature-field]');
                                    if (assinaturaContainer) {
                                        syncSignatureValue(assinaturaContainer, valorAssinatura);
                                    }
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
            const data = await fetchJson(`/Processos/Dados?id=${processoId}`, {
                errorMessage: 'Erro ao atualizar processo'
            });
            updateProcessSummary(data);
            const fases = readArray(data, 'Fases');
            updatePhaseCards(fases);
            updateFormFields(fases);
            initSignaturePads(root);
            if (focusPageId) {
                const active = root.querySelector(`[data-page-selector][data-page-id="${focusPageId}"]`);
                if (active) {
                    showPage(active.dataset.faseId, active.dataset.pageId);
                }
            }
            return data;
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        const root = document.getElementById('processo-detalhes');
        if (root) initProcessoDetalhes(root);
    });
})();
