// The one progressive touch: a button on the How to play page that copies the connection command. Nothing else needs scripts.
(function () {
  var b = document.getElementById('copy-connect');
  if (!b || !navigator.clipboard) return;
  b.hidden = false;
  b.addEventListener('click', function () {
    navigator.clipboard.writeText(b.getAttribute('data-copy')).then(function () { b.textContent = 'Copied'; }, function () { b.textContent = 'Select and copy it by hand'; });
  });
})();
