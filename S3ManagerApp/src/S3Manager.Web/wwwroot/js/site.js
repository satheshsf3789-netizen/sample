// S3Manager client-side functionality
(function() {
    'use strict';

    // Get CSRF token from meta tag
    function getAntiForgeryToken() {
        const token = document.querySelector('meta[name="csrf-token"]');
        return token ? token.getAttribute('content') : '';
    }

    // Add CSRF token to fetch requests
    window.fetchWithToken = function(url, options = {}) {
        if (!options.headers) {
            options.headers = {};
        }
        options.headers['X-CSRF-TOKEN'] = getAntiForgeryToken();
        return fetch(url, options);
    };

    // Handle confirmation modals with typed confirmation
    const confirmInputs = document.querySelectorAll('[data-confirm-input]');
    confirmInputs.forEach(input => {
        const form = input.closest('form');
        const submitBtn = form?.querySelector('button[type="submit"]');
        
        if (submitBtn) {
            submitBtn.disabled = true;
            
            input.addEventListener('input', function() {
                const expectedValue = this.dataset.confirmInput;
                submitBtn.disabled = this.value !== expectedValue;
            });
        }
    });

    // Initialize tooltips if Bootstrap tooltips are available
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
            new bootstrap.Tooltip(el);
        });
    }
})();
