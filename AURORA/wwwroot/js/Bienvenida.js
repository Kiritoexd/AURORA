/* ============================================================
   AURORA — Bienvenida.js
   Page loader · Scroll reveals · Counters · Nav · Modal
   ============================================================ */

(function () {
    'use strict';

    /* ── 1. PAGE LOADER — salida al cargar ─────────────────── */
    const loader = document.getElementById('pageLoader');

    function hideLoader() {
        if (!loader) return;
        loader.classList.add('hidden');
        // Disparar reveal de hero
        document.querySelectorAll('.reveal').forEach(el => el.classList.add('in'));
    }

    // Esperar a que la barra de carga termine (aprox 1.2s) y luego ocultar
    window.addEventListener('load', () => {
        setTimeout(hideLoader, 1000);
    });
    // Fallback por si load ya pasó
    if (document.readyState === 'complete') {
        setTimeout(hideLoader, 1000);
    }

    /* ── 2. PAGE TRANSITION — pantalla al navegar ──────────── */
    function showLoaderForNav() {
        if (!loader) return;
        loader.classList.remove('hidden');
        // Resetear barra
        const fill = loader.querySelector('.loader-bar-fill');
        if (fill) {
            fill.style.animation = 'none';
            fill.offsetWidth; // reflow
            fill.style.animation = '';
        }
        // Resetear letras
        loader.querySelectorAll('.loader-wordmark span').forEach(s => {
            s.style.animation = 'none';
            s.offsetWidth;
            s.style.animation = '';
        });
    }

    document.querySelectorAll('a.page-link, .nav-link').forEach(link => {
        link.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            // Solo links internos reales (no #, no vacíos)
            if (!href || href.startsWith('#') || href.startsWith('javascript')) return;
            // Links con asp-* generan href normales en el HTML renderizado
            showLoaderForNav();
        });
    });

    /* ── 3. SCROLL REVEAL ───────────────────────────────────── */
    const scrollEls = document.querySelectorAll('.scroll-reveal');
    if (scrollEls.length) {
        const revealObs = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                const delay = entry.target.dataset.delay || 0;
                setTimeout(() => entry.target.classList.add('in'), parseInt(delay));
                revealObs.unobserve(entry.target);
            });
        }, { threshold: 0.12 });

        scrollEls.forEach(el => revealObs.observe(el));
    }

    /* ── 4. COUNTER ANIMATION ───────────────────────────────── */
    const counterEls = document.querySelectorAll('.stat-num[data-count]');
    if (counterEls.length) {
        const countObs = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                const el = entry.target;
                const target = parseInt(el.dataset.count, 10);
                const duration = 1400;
                const start = performance.now();

                function tick(now) {
                    const elapsed = now - start;
                    const progress = Math.min(elapsed / duration, 1);
                    // Ease out cubic
                    const eased = 1 - Math.pow(1 - progress, 3);
                    el.textContent = Math.round(eased * target);
                    if (progress < 1) requestAnimationFrame(tick);
                    else el.textContent = target;
                }

                requestAnimationFrame(tick);
                countObs.unobserve(el);
            });
        }, { threshold: 0.3 });

        counterEls.forEach(el => countObs.observe(el));
    }

    /* ── 5. NAV MOBILE ──────────────────────────────────────── */
    const hamburger = document.getElementById('navHamburger');
    const navLinks = document.getElementById('navLinks');

    if (hamburger && navLinks) {
        hamburger.addEventListener('click', () => {
            const open = navLinks.classList.toggle('open');
            hamburger.classList.toggle('open', open);
            hamburger.setAttribute('aria-expanded', open);
            document.body.style.overflow = open ? 'hidden' : '';
        });

        // Cerrar al hacer clic fuera
        document.addEventListener('click', (e) => {
            if (!hamburger.contains(e.target) && !navLinks.contains(e.target)) {
                navLinks.classList.remove('open');
                hamburger.classList.remove('open');
                document.body.style.overflow = '';
            }
        });

        // Cerrar al hacer clic en un link
        navLinks.querySelectorAll('a').forEach(a => {
            a.addEventListener('click', () => {
                navLinks.classList.remove('open');
                hamburger.classList.remove('open');
                document.body.style.overflow = '';
            });
        });
    }

    /* ── 6. NAV SCROLL EFFECT ───────────────────────────────── */
    const heroNav = document.getElementById('heroNav');
    if (heroNav) {
        let ticking = false;
        window.addEventListener('scroll', () => {
            if (ticking) return;
            requestAnimationFrame(() => {
                heroNav.classList.toggle('scrolled', window.scrollY > 60);
                ticking = false;
            });
            ticking = true;
        }, { passive: true });
    }

    /* ── 7. MODAL TÉRMINOS ──────────────────────────────────── */
    const modal = document.getElementById('terminosModal');
    const openBtn = document.getElementById('openTerminos');
    const closeBtn = document.getElementById('closeTerminos');
    const closeBtn2 = document.getElementById('closeTerminos2');

    if (modal && openBtn) {
        const open = () => modal.classList.add('open');
        const close = () => modal.classList.remove('open');

        openBtn.addEventListener('click', open);
        closeBtn?.addEventListener('click', close);
        closeBtn2?.addEventListener('click', close);
        modal.addEventListener('click', e => { if (e.target === modal) close(); });
        document.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });
    }

})();