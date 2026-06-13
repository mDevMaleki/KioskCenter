let idleMs = 60_000; // 60s
let t;

function resetIdle() {
    clearTimeout(t);
    t = setTimeout(() => {
        fetch('/Cart/Clear', { method: 'POST' })
            .finally(() => window.location.href = '/');
    }, idleMs);
}

['click', 'touchstart', 'mousemove', 'keydown'].forEach(e => {
    document.addEventListener(e, resetIdle, { passive: true });
});

resetIdle();
