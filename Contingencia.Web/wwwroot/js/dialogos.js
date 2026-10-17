// Helpers de SweetAlert que en el legado vivían en SiteMipresN.Master (compartidos por
// todas las páginas que usan ese master) — se cargan globalmente desde App.razor, mismo
// motivo que login.js/app-layout.js: una enhanced navigation de Blazor nunca re-ejecuta un
// <script> propio de una página, así que si vivieran en CargarFormato.razor no funcionarían
// la primera vez que alguien llega ahí sin una recarga completa.
//
// Usan SweetAlert2 (window.Swal), no la v1 original (window.swal, deprecada) — a diferencia
// de v1, Swal2 cierra el modal solo al confirmar (no hace falta un closeOnConfirm/close()
// manual), lo que además evita el bug que tenía la v1 acá: antes el modal solo se cerraba
// como efecto colateral de navegar siempre a "mod", incluso con "mod" vacío — con el
// <base href="/"> que exige Blazor eso expulsaba al usuario del formulario a "/" perdiendo
// lo escrito. Con Swal2, Tran solo navega si mod trae algo; si no, el modal ya se cerró solo.

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

function Tran2(msg, msg2) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg + "<br/>" + msg2,
        confirmButtonText: "Gracias",
        showCancelButton: false
    });
}

function TranT(msg, msg2, mod) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg + "<br/>" + msg2,
        confirmButtonText: "Gracias",
        showCancelButton: false
    }).then(function () {
        window.location = mod;
    });
}

// Fin del wizard "Nueva contingencia": ofrece abrir el formulario oficial MIPRES NoPBS ya
// diligenciado (PDF en una pestaña nueva) y luego lleva a la consulta, igual que TranT. El
// window.open sale directo del clic del usuario en el modal, para que el navegador no lo bloquee.
function TranFormulario(msg, msg2, urlPdf, mod) {
    Swal.fire({
        html: '<img src="assets/images/logo.jpg" width="100"><br/>' + msg + "<br/>" + msg2,
        confirmButtonText: "Ver formulario PDF",
        showCancelButton: true,
        cancelButtonText: "Cerrar"
    }).then(function (resultado) {
        if (resultado.isConfirmed) {
            window.open(urlPdf, "_blank");
        }
        window.location = mod;
    });
}
