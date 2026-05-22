/* Wishlist Toggle */
function toggleWishlist(productId, btn) {
    const token = document.querySelector('#antiforgery-form input[name="__RequestVerificationToken"]')?.value ||
                  document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Wishlist/CheckIsInWishlist', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId: productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.isInWishlist) {
            removeFromWishlist(productId, btn);
        } else {
            addToWishlist(productId, btn);
        }
    })
    .catch(() => showToast('Error', 'error'));
}

function addToWishlist(productId, btn) {
    const token = document.querySelector('#antiforgery-form input[name="__RequestVerificationToken"]')?.value ||
                  document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Wishlist/Add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId: productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            if (btn) {
                btn.classList.add('btn-primary');
                btn.classList.remove('btn-secondary');
                const icon = btn.querySelector('i') || btn;
                icon.className = 'bi bi-heart-fill';
            }
            showToast('Added to wishlist!', 'success');
        } else {
            showToast(data.message || 'Error', 'error');
        }
    })
    .catch(() => showToast('Error', 'error'));
}

function removeFromWishlist(productId, btn) {
    const token = document.querySelector('#antiforgery-form input[name="__RequestVerificationToken"]')?.value ||
                  document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Wishlist/Remove', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ productId: productId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            if (btn) {
                btn.classList.remove('btn-primary');
                btn.classList.add('btn-secondary');
                const icon = btn.querySelector('i') || btn;
                icon.className = 'bi bi-heart';
            }
            showToast('Removed from wishlist', 'success');
        }
    })
    .catch(() => showToast('Error', 'error'));
}

/* Toast Notification */
function showToast(message, type) {
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 2500);
}

/* Mark Review Helpful */
function markHelpful(reviewId, helpful) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
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

/* Check Wishlist Status on Page Load */
document.addEventListener('DOMContentLoaded', function () {
    const productIdEl = document.getElementById('product-id');
    if (productIdEl) {
        checkWishlistStatus(parseInt(productIdEl.value));
    }
});

function checkWishlistStatus(productId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
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
