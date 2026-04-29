const { post } = require("jquery");

function stripDiacritics(str) {
    return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

if (window.location.pathname === '/') {
    let link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = 'x-page-style.css';
    document.head.appendChild(link);
}

function convertDate(date) {
    var properDate = date.split(".");
    return properDate[2] + "" + properDate[1] + "" + properDate[0];
}
function validateInputLength(input, isRequired) {

    if (input instanceof HTMLInputElement) {
        const value = input.value.trim();
        if (!isRequired) {
            const isValid = value === '' || value.length >= 6
        }
        else {
            const isValid = value === value.length >= 6
        }
        input.setCustomValidity(isValid ? '' : 'Value not valid');
    }
}

window.onload = function () {
    if (document.getElementById("tabulkaObcanu") != null) {
        if (JSON.parse(localStorage.getItem('isAscending'))) {
            sortTable(Number(localStorage.getItem('sortedTable')));
        }
        else {
            sortTable(Number(localStorage.getItem('sortedTable')));
            sortTable(Number(localStorage.getItem('sortedTable')));
        }
    }
};



function sortTable(n) {
    
    localStorage.setItem('sortedTable', n);
    
    const form = document.createElement('form');
    form.method = 'post';
    form.action = "index?handler=SaveSortPrefs";

    // Add sortby
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = 'sortby';
    input.value = n;
    form.appendChild(input);

    const returnUrl = document.createElement('input');
    returnUrl.type = 'hidden';
    returnUrl.name = 'returnUrl';
    returnUrl.value = window.location.pathname + window.location.search;
    form.appendChild(returnUrl);

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) {
        const token = document.createElement('input');
        token.type = 'hidden';
        token.name = '__RequestVerificationToken';
        token.value = tokenInput.value;
        form.appendChild(token);
    }

    const urlParams = new URLSearchParams(window.location.search);
    const searchQuery = urlParams.get("SearchQuery");
    if (searchQuery) {
        const searchInput = document.createElement('input');
        searchInput.type = 'hidden';
        searchInput.name = 'SearchQuery';
        searchInput.value = searchQuery;
        form.appendChild(searchInput);
    }

    document.body.appendChild(form);
    form.submit();

}

