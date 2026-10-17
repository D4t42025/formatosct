function Tran(msg, mod) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg,
        confirmButtonText: "Gracias",
        showCancelButton: false
    }).then(function () {
        if (mod) {
            window.location = mod;
        }
    });
}

function Alerta(msg, mod) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg,
        confirmButtonText: "Ok",
        showCancelButton: false
    }).then(function () {
        window.close();
    });
}

function Alerta2(msg, mod) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg,
        confirmButtonText: "Guardar",
        cancelButtonText: "Editar más detalles",
        showCancelButton: true
    }).then(function () {
        // Réplica fiel del legado: el callback de v1 no distinguía confirmar/cancelar acá,
        // así que cualquiera de los dos botones navegaba a "mod". Función sin uso hoy en
        // Contingencia (solo Alerta está cableada desde Opciones.razor).
        window.location = mod;
    });
}

function Enviar() {
    window.open("Formatos.aspx", "_blank", "toolbar=yes,scrollbars=yes,resizable=yes,width=800,height=800");
}

function Tran2(msg, msg2) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg + "<br/>" + msg2,
        confirmButtonText: "Gracias",
        showCancelButton: false
    });
}
