<script>
window.attachResultHotkeys = () => {
  const handler = (e) => {
    if (e.key === 'Escape') {
        e.preventDefault();
    document.querySelector('.result-close')?.click();
    }
    if (e.key === 'Enter') {
        e.preventDefault();
    // "Dalej" (StartGame) – spróbuj kliknąć przycisk
    document.querySelector('.result-actions .bg-blue-600')?.click();
    }
  };
    window.removeEventListener('keydown', handler);
    window.addEventListener('keydown', handler, {once: true }); // jednorazowo
};
</script>
