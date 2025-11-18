(function () {
    const Alfa = window.Alfa || {};
    const toast = typeof Alfa.toast === 'function' ? Alfa.toast : () => {};
    const postJson = typeof Alfa.postJson === 'function' ? Alfa.postJson : null;
    const initSignaturePads = typeof Alfa.initSignaturePads === 'function' ? Alfa.initSignaturePads : () => {};
    if (!postJson) {
        console.warn('preenchimento-externo.js: Alfa.postJson não está disponível.');
        return;
    }

    function initPreenchimentoExterno(root) {
        if (!root) return;
        const processoId = parseInt(root.dataset.processoId || '0', 10);
        const faseId = parseInt(root.dataset.faseId || '0', 10);
        const paginaId = parseInt(root.dataset.paginaId || '0', 10);
        const token = root.dataset.token || '';
        if (!processoId || !faseId || !paginaId) return;

        const form = root.querySelector('[data-preenchimento-externo-form]');
        if (!form) return;

        initSignaturePads(form);

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
                        case 'signature':
                            payload.ValorTexto = input.value ? input.value : null;
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
                    ProcessoId: processoId,
                    FaseInstanciaId: faseId,
                    PaginaInstanciaId: paginaId,
                    Campos: camposPayload,
                    Token: token
                };

                await postJson('/Processos/RegistrarRespostasExterno', payload, 'Falha ao enviar respostas');

                toast('Respostas enviadas com sucesso!', 'success');
            } catch (err) {
                console.error(err);
                const message = err instanceof Error && err.message
                    ? err.message
                    : 'Não foi possível enviar as respostas. Tente novamente.';
                toast(message, 'danger');
            } finally {
                if (feedback) feedback.hidden = true;
                if (button) {
                    button.textContent = button.dataset.originalText || 'Enviar respostas';
                    button.disabled = false;
                }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        const root = document.querySelector('[data-preenchimento-externo]');
        if (root) initPreenchimentoExterno(root);
    });
})();
