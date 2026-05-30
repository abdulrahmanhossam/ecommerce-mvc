/* ===== Anti-Forgery Token ===== */
function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

/* ===== Header scroll shadow ===== */
(function () {
    const header = document.querySelector('.header');
    if (!header) return;
    const onScroll = () => header.classList.toggle('scrolled', window.scrollY > 8);
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
})();

/* ===== Wishlist ===== */
function updateWishlistBtn(btn, isInWishlist) {
    if (!btn) return;
    const icon = btn.querySelector('i') || btn;
    if (isInWishlist) {
        btn.classList.add('btn-primary');
        btn.classList.remove('btn-secondary');
        icon.className = 'bi bi-heart-fill';
    } else {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-secondary');
        icon.className = 'bi bi-heart';
    }
}

function toggleWishlist(productId, btn) {
    const token = getToken();
    if (!token) {
        showToast('Please sign in to use the wishlist', 'error');
        return;
    }

    const isAdding = btn ? !btn.classList.contains('btn-primary') : true;
    const url = isAdding ? '/Wishlist/Add' : '/Wishlist/Remove';

    // Optimistic UI update
    updateWishlistBtn(btn, isAdding);

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            showToast(isAdding ? 'Added to wishlist!' : 'Removed from wishlist', 'success');
        } else {
            // Revert on failure
            updateWishlistBtn(btn, !isAdding);
            showToast(data.message || 'Something went wrong', 'error');
        }
    })
    .catch(() => {
        updateWishlistBtn(btn, !isAdding);
        showToast('Network error', 'error');
    });
}

/* ===== Toast ===== */
let toastIdCounter = 0;

function showToast(message, type = 'success') {
    // Lazily create container
    let container = document.getElementById('toast-container-global');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container-global';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const id = ++toastIdCounter;
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.id = 'toast-' + id;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.innerHTML = `<span>${message}</span><button class="toast-close" onclick="dismissToast(${id})" aria-label="Dismiss">&times;</button>`;
    container.appendChild(toast);
    setTimeout(() => dismissToast(id), 4200);
}

function dismissToast(id) {
    const el = document.getElementById('toast-' + id);
    if (el) {
        el.classList.add('toast-dismissing');
        setTimeout(() => el.remove(), 320);
    }
}

/* ===== Mark Review Helpful ===== */
function markHelpful(reviewId, helpful) {
    const token = getToken();
    if (!token) return;

    fetch('/Products/MarkHelpful', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ reviewId, helpful })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            const helpfulEl    = document.getElementById('helpful-'    + reviewId);
            const notHelpfulEl = document.getElementById('nothelpful-' + reviewId);
            if (helpfulEl)    helpfulEl.textContent    = data.helpfulCount;
            if (notHelpfulEl) notHelpfulEl.textContent = data.notHelpfulCount;
        }
    })
    .catch(() => {});
}

/* ===== Check Wishlist State on Product Detail ===== */
document.addEventListener('DOMContentLoaded', function () {
    const productIdEl = document.getElementById('product-id');
    const token = getToken();
    if (productIdEl && token) {
        checkWishlistStatus(parseInt(productIdEl.value));
    }
});

function checkWishlistStatus(productId) {
    const token = getToken();
    if (!token || !productId) return;

    fetch('/Wishlist/CheckIsInWishlist', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.isInWishlist) {
            const btn = document.getElementById('wishlistBtn');
            if (btn) {
                btn.classList.add('btn-primary');
                btn.classList.remove('btn-secondary');
                const icon = btn.querySelector('i');
                if (icon) icon.className = 'bi bi-heart-fill';
            }
        }
    })
    .catch(() => {});
}
