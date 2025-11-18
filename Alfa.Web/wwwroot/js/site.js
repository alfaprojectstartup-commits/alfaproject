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

    const getRequestVerificationToken = () => {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        if (input && input.value) return input.value;
        const meta = document.querySelector('meta[name="request-verification-token"]');
        if (meta && meta.content) return meta.content;
        return null;
    };

    Alfa.getRequestVerificationToken = getRequestVerificationToken;

    async function parseJsonResponse(resp, fallbackMessage = 'Falha na operação.') {
        if (!resp.ok) {
            let msg = fallbackMessage;
            const raw = await resp.text();
            if (raw) {
                try {
                    const json = JSON.parse(raw);
                    if (json) {
                        if (typeof json.message === 'string' && json.message.trim() !== '') {
                            msg = json.message.trim();
                        } else if (typeof json === 'string' && json.trim() !== '') {
                            msg = json.trim();
                        } else {
                            msg = raw.trim();
                        }
                    }
                } catch {
                    msg = raw.trim();
                }
            }
            throw new Error(msg);
        }

        try {
            return await resp.json();
        } catch {
            return null;
        }
    }

    async function postJson(url, payload, errorMessage = 'Falha na operação.') {
        const headers = { 'Content-Type': 'application/json' };
        const token = getRequestVerificationToken();
        if (token) headers['RequestVerificationToken'] = token;

        const resp = await fetch(url, {
            method: 'POST',
            headers,
            body: JSON.stringify(payload ?? {})
        });

        return parseJsonResponse(resp, errorMessage);
    }

    Alfa.postJson = postJson;

    async function fetchJson(url, options = {}) {
        const { method = 'GET', headers = {}, body, errorMessage = 'Falha na operação.' } = options;
        const resp = await fetch(url, {
            method,
            headers,
            body
        });

        return parseJsonResponse(resp, errorMessage);
    }

    Alfa.fetchJson = fetchJson;

    function initSignaturePads(scope) {
        if (!scope) return;

        if (scope instanceof NodeList || Array.isArray(scope)) {
            scope.forEach(node => initSignaturePads(node));
            return;
        }

        const root = (scope instanceof Element || scope instanceof Document) ? scope : document;
        if (typeof root.querySelectorAll !== 'function') return;

        const containers = root.querySelectorAll('[data-signature-field]');
        containers.forEach(container => ensureSignaturePad(container));
    }

    function ensureSignaturePad(container) {
        if (!(container instanceof Element)) return;
        if (container.dataset.signatureInitialized === 'true') {
            const api = container.__alfaSignature;
            if (api && typeof api.refresh === 'function') {
                api.refresh();
            }
            return;
        }

        const pad = container.querySelector('[data-signature-pad]');
        const canvas = container.querySelector('[data-signature-canvas]');
        const hiddenInput = container.querySelector('[data-signature-output]');
        if (!pad || !canvas || !hiddenInput) {
            return;
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            return;
        }

        const hint = container.querySelector('[data-signature-hint]');
        const clearBtn = container.querySelector('[data-signature-clear]');
        const removeBtn = container.querySelector('[data-signature-remove]');
        const previewWrapper = container.querySelector('[data-signature-preview]');
        const previewImg = container.querySelector('[data-signature-preview-img]');

        let storedData = hiddenInput.value || '';
        let drawing = false;

        const applyCtxStyles = () => {
            ctx.lineWidth = 2;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.strokeStyle = '#111';
        };

        const updateUi = () => {
            const hasSignature = !!storedData;
            if (previewWrapper) {
                previewWrapper.classList.toggle('d-none', !hasSignature);
            }
            if (previewImg && hasSignature) {
                previewImg.src = storedData;
            }
            if (removeBtn) {
                removeBtn.classList.toggle('d-none', !hasSignature);
            }
            if (hint) {
                hint.classList.toggle('d-none', hasSignature);
            }
        };

        const drawStored = () => {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            if (!storedData) {
                return;
            }

            const image = new Image();
            image.onload = () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                const ratio = Math.min(canvas.width / image.width, canvas.height / image.height, 1);
                const renderWidth = image.width * ratio;
                const renderHeight = image.height * ratio;
                const offsetX = (canvas.width - renderWidth) / 2;
                const offsetY = (canvas.height - renderHeight) / 2;
                ctx.drawImage(image, offsetX, offsetY, renderWidth, renderHeight);
            };
            image.src = storedData;
        };

        const setValue = (value, { redraw = true } = {}) => {
            storedData = value || '';
            hiddenInput.value = storedData;
            updateUi();
            if (redraw) {
                drawStored();
            }
        };

        const resizeCanvas = () => {
            const rect = pad.getBoundingClientRect();
            const width = rect.width > 0 ? rect.width : 600;
            const height = rect.height > 0 ? rect.height : 180;
            canvas.width = width;
            canvas.height = height;
            canvas.style.width = `${width}px`;
            canvas.style.height = `${height}px`;
            applyCtxStyles();
            drawStored();
        };

        const getPoint = (evt) => {
            const rect = canvas.getBoundingClientRect();
            return {
                x: evt.clientX - rect.left,
                y: evt.clientY - rect.top
            };
        };

        const startDrawing = (evt) => {
            if (evt.button === 2) return;
            evt.preventDefault();
            const point = getPoint(evt);
            if (!point) return;
            if (storedData) {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                setValue('', { redraw: false });
            }
            drawing = true;
            ctx.beginPath();
            ctx.moveTo(point.x, point.y);
            hint?.classList.add('d-none');
            if (typeof canvas.setPointerCapture === 'function' && evt.pointerId != null) {
                try {
                    canvas.setPointerCapture(evt.pointerId);
                } catch {
                    // browsers sem suporte a pointer capture podem lançar exceções
                }
            }
        };

        const draw = (evt) => {
            if (!drawing) return;
            evt.preventDefault();
            const point = getPoint(evt);
            if (!point) return;
            ctx.lineTo(point.x, point.y);
            ctx.stroke();
        };

        const stopDrawing = (evt) => {
            if (!drawing) return;
            evt.preventDefault();
            drawing = false;
            if (typeof canvas.releasePointerCapture === 'function' && evt.pointerId != null) {
                try {
                    canvas.releasePointerCapture(evt.pointerId);
                } catch {
                    // ignore falhas ao liberar pointer capture
                }
            }
            ctx.closePath();
            setValue(canvas.toDataURL('image/png'), { redraw: false });
        };

        canvas.style.touchAction = 'none';
        canvas.addEventListener('pointerdown', startDrawing);
        canvas.addEventListener('pointermove', draw);
        canvas.addEventListener('pointerup', stopDrawing);
        canvas.addEventListener('pointerleave', stopDrawing);
        canvas.addEventListener('pointercancel', stopDrawing);

        clearBtn?.addEventListener('click', (evt) => {
            evt.preventDefault();
            setValue('', { redraw: true });
        });

        removeBtn?.addEventListener('click', (evt) => {
            evt.preventDefault();
            setValue('', { redraw: true });
        });

        let resizeObserver = null;
        const resizeHandler = () => resizeCanvas();
        if (window.ResizeObserver) {
            resizeObserver = new ResizeObserver(() => resizeCanvas());
            resizeObserver.observe(pad);
        } else {
            window.addEventListener('resize', resizeHandler);
        }

        container.dataset.signatureInitialized = 'true';
        container.__alfaSignature = {
            setValue: (value, options) => setValue(value, options),
            refresh: () => resizeCanvas(),
            destroy: () => {
                resizeObserver?.disconnect();
                window.removeEventListener('resize', resizeHandler);
            }
        };

        applyCtxStyles();
        resizeCanvas();
        setValue(storedData, { redraw: true });
    }

    function syncSignatureValue(container, value) {
        if (!(container instanceof Element)) return;
        ensureSignaturePad(container);
        const api = container.__alfaSignature;
        if (api && typeof api.setValue === 'function') {
            api.setValue(value || '', { redraw: true });
        }
    }

    Alfa.initSignaturePads = initSignaturePads;
    Alfa.syncSignatureValue = syncSignatureValue;
})();
