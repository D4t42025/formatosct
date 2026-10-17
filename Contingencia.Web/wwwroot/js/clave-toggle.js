/* Mostrar/ocultar contraseña en la pantalla "Actualizar contraseña" (hay tres campos; login-v2.js solo maneja el primero). */
(function () {
  document.querySelectorAll('.control .toggle').forEach(function (b) {
    b.addEventListener('click', function () {
      var i = b.closest('.control').querySelector('input');
      if (!i) return;
      var mostrar = i.type === 'password';
      i.type = mostrar ? 'text' : 'password';
      b.setAttribute('aria-pressed', mostrar);
      b.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
      i.focus();
    });
  });
})();
