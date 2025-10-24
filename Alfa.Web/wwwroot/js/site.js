document.addEventListener('shown.bs.dropdown', (e) => {
    const menu = e.target.parentElement.querySelector('.dropdown-menu');
    if (menu && !menu.classList.contains('dropdown-fade')) menu.classList.add('dropdown-fade');
});
document.addEventListener('hide.bs.dropdown', (e) => {
    // deixa o Bootstrap cuidar do fechamento; o CSS trata a transição
});