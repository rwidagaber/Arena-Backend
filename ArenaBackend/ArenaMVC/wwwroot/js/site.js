// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Modern Language Switcher functionality
document.addEventListener('DOMContentLoaded', () => {
    const switcher = document.getElementById('langSwitcher');
    const toggleBtn = document.getElementById('langSwitcherBtn');

    if (switcher && toggleBtn) {
        toggleBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = switcher.classList.toggle('open');
            toggleBtn.setAttribute('aria-expanded', isOpen);
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!switcher.contains(e.target)) {
                switcher.classList.remove('open');
                toggleBtn.setAttribute('aria-expanded', 'false');
            }
        });

        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && switcher.classList.contains('open')) {
                switcher.classList.remove('open');
                toggleBtn.setAttribute('aria-expanded', 'false');
                toggleBtn.focus();
            }
        });
    }
});
