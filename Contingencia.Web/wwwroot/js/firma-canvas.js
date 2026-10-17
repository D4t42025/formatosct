// Lienzo de firma a mano alzada para /profesionales — a diferencia del lienzo de
// JuntasFirma.aspx en el legado (drawMouse/drawTouch + WebMethod UploadImage, código
// muerto, nunca alcanzable desde la UI real — ver Juntas.razor), este SÍ está conectado:
// es la implementación real del mismo patrón, para MIP_PROFESIONALES.aspx.
window.ImperiumFirmaCanvas = (function () {
    const lienzos = {};

    function coordenadas(canvas, evento) {
        const rect = canvas.getBoundingClientRect();
        const origen = evento.touches ? evento.touches[0] : evento;
        return {
            x: origen.clientX - rect.left,
            y: origen.clientY - rect.top,
        };
    }

    function iniciar(idCanvas) {
        const canvas = document.getElementById(idCanvas);
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        ctx.lineWidth = 2.5;
        ctx.lineCap = "round";
        ctx.strokeStyle = "#1e293b";

        let dibujando = false;

        const empezar = (e) => {
            dibujando = true;
            const { x, y } = coordenadas(canvas, e);
            ctx.beginPath();
            ctx.moveTo(x, y);
            e.preventDefault();
        };

        const trazar = (e) => {
            if (!dibujando) return;
            const { x, y } = coordenadas(canvas, e);
            ctx.lineTo(x, y);
            ctx.stroke();
            e.preventDefault();
        };

        const terminar = () => {
            dibujando = false;
        };

        canvas.addEventListener("mousedown", empezar);
        canvas.addEventListener("mousemove", trazar);
        canvas.addEventListener("mouseup", terminar);
        canvas.addEventListener("mouseleave", terminar);
        canvas.addEventListener("touchstart", empezar, { passive: false });
        canvas.addEventListener("touchmove", trazar, { passive: false });
        canvas.addEventListener("touchend", terminar);

        lienzos[idCanvas] = { canvas, ctx, empezar, trazar, terminar };
    }

    function limpiar(idCanvas) {
        const lienzo = lienzos[idCanvas];
        if (!lienzo) return;
        lienzo.ctx.clearRect(0, 0, lienzo.canvas.width, lienzo.canvas.height);
    }

    function tieneTrazo(idCanvas) {
        const lienzo = lienzos[idCanvas];
        if (!lienzo) return false;
        const datos = lienzo.ctx.getImageData(0, 0, lienzo.canvas.width, lienzo.canvas.height).data;
        for (let i = 3; i < datos.length; i += 4) {
            if (datos[i] !== 0) return true;
        }
        return false;
    }

    function exportarPng(idCanvas) {
        const lienzo = lienzos[idCanvas];
        if (!lienzo) return null;
        return lienzo.canvas.toDataURL("image/png");
    }

    function destruir(idCanvas) {
        const lienzo = lienzos[idCanvas];
        if (!lienzo) return;
        lienzo.canvas.removeEventListener("mousedown", lienzo.empezar);
        lienzo.canvas.removeEventListener("mousemove", lienzo.trazar);
        lienzo.canvas.removeEventListener("mouseup", lienzo.terminar);
        lienzo.canvas.removeEventListener("mouseleave", lienzo.terminar);
        delete lienzos[idCanvas];
    }

    return { iniciar, limpiar, tieneTrazo, exportarPng, destruir };
})();
