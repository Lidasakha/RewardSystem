/* TÖS — Site JS v3 */

function toggleSidebar() {
  var sidebar = document.getElementById('sidebar');
  var overlay = document.getElementById('overlay');
  var isOpen  = sidebar.classList.contains('open');

  if (isOpen) {
    sidebar.classList.remove('open');
    overlay.classList.remove('show');
  } else {
    sidebar.classList.add('open');
    overlay.classList.add('show');
  }
}

function closeSidebar() {
  document.getElementById('sidebar').classList.remove('open');
  document.getElementById('overlay').classList.remove('show');
}

document.addEventListener('DOMContentLoaded', function () {
  // Overlay tıklama
  var overlay = document.getElementById('overlay');
  if (overlay) {
    overlay.addEventListener('click', closeSidebar);
  }

  // Ana içerik alanı tıklama — sidebar açıksa kapat
  var main = document.querySelector('.main');
  if (main) {
    main.addEventListener('click', function () {
      var sidebar = document.getElementById('sidebar');
      if (sidebar && sidebar.classList.contains('open')) {
        closeSidebar();
      }
    });
  }

  // ESC ile kapat
  document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeSidebar();
  });
});