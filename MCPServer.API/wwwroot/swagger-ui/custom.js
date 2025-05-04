// Custom JavaScript for Swagger UI
(function () {
    // Override fetch to add CORS headers
    const originalFetch = window.fetch;
    window.fetch = function (url, options) {
        if (!options) {
            options = {};
        }
        if (!options.headers) {
            options.headers = {};
        }
        
        // Add CORS headers
        options.headers['X-Requested-With'] = 'XMLHttpRequest';
        options.mode = 'cors';
        options.credentials = 'include';
        
        return originalFetch(url, options);
    };
    
    console.log('Custom Swagger UI JavaScript loaded');
})();
