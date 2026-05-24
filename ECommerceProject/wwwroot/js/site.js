/* ===== Anti-Forgery Token ===== */
function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

/* ===== Wishlist ===== */
function updateWishlistBtn(btn, isInWishlist) {
    if (!btn) return;
    if (isInWishlist) {
        btn.classList.add('btn-primary');
        btn.classList.remove('btn-secondary');
        const icon = btn.querySelector('i') || btn;
        icon.className = 'bi bi-heart-fill';
    } else {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-secondary');
        const icon = btn.querySelector('i') || btn;
        icon.className = 'bi bi-heart';
    }
}

function toggleWishlist(productId, btn) {
    const token = getToken();
    if (!token) return;

    const isAdding = btn ? !btn.classList.contains('btn-primary') : true;
    const url = isAdding ? '/Wishlist/Add' : '/Wishlist/Remove';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId: productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            updateWishlistBtn(btn, isAdding);
            showToast(isAdding ? 'Added to wishlist!' : 'Removed from wishlist', 'success');
        } else {
            showToast(data.message || 'Error', 'error');
        }
    })
    .catch(() => showToast('Network error', 'error'));
}

/* ===== Toast ===== */
let toastIdCounter = 0;
function showToast(message, type) {
    const id = ++toastIdCounter;
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.id = 'toast-' + id;
    toast.innerHTML = `<span>${message}</span><button class="toast-close" onclick="dismissToast('${id}')">&times;</button>`;
    document.body.appendChild(toast);
    setTimeout(() => dismissToast(id), 4000);
}

function dismissToast(id) {
    const el = document.getElementById('toast-' + id);
    if (el) {
        el.classList.add('toast-dismissing');
        setTimeout(() => el.remove(), 300);
    }
}

/* ===== Mark Helpful ===== */
function markHelpful(reviewId, helpful) {
    const token = getToken();
    if (!token) return;

    fetch('/Products/MarkHelpful', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ reviewId: reviewId, helpful: helpful })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            const helpfulEl = document.getElementById('helpful-' + reviewId);
            const notHelpfulEl = document.getElementById('nothelpful-' + reviewId);
            if (helpfulEl) helpfulEl.textContent = data.helpfulCount;
            if (notHelpfulEl) notHelpfulEl.textContent = data.notHelpfulCount;
        }
    })
    .catch(() => {});
}

/* ===== Check Wishlist on Page Load ===== */
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
        body: JSON.stringify({ productId: productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.isInWishlist) {
            const btn = document.getElementById('wishlistBtn');
            if (btn) {
                btn.classList.add('btn-primary');
                btn.classList.remove('btn-secondary');
                btn.innerHTML = '<i class="bi bi-heart-fill"></i>';
            }
        }
    })
    .catch(() => {});
}
