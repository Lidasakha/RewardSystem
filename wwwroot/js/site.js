function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('overlay');
    
    // Eğer ekran mobil boyuttaysa (örneğin 768px altı) eski overlay mantığı çalışsın
    if (window.innerWidth <= 768) {
        sidebar.classList.toggle('open');
        overlay.classList.toggle('show');
    } else {
        // Ekran büyükse (Masaüstü) menüyü daralt/genişlet
        sidebar.classList.toggle('collapsed');
    }
}

