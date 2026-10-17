// Dictado por voz (wizard "Nueva Contingencia") — envoltorio delgado sobre la Web Speech
// API del navegador (SpeechRecognition/webkitSpeechRecognition). Toda la lógica de negocio
// (qué campo llenar con qué texto) vive en C# (NuevaContingencia.razor); este módulo solo
// hace la mecánica de escuchar y devolver texto reconocido.
//
// Requiere Chrome/Edge (Firefox no lo soporta) y un contexto seguro (HTTPS, o localhost que
// el navegador trata como seguro sin HTTPS) — si el sitio real se sirve sin HTTPS, esto
// fallará con error "not-allowed" para todos los usuarios, sin que sea un bug de código.
window.ImperiumDictado = (function () {
    let reconocimiento = null;
    let dotNetRef = null;

    function soportado() {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    }

    function iniciar(referencia, idioma) {
        const Motor = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Motor) {
            referencia.invokeMethodAsync('OnDictadoError', 'no-soportado');
            return;
        }

        dotNetRef = referencia;
        reconocimiento = new Motor();
        reconocimiento.lang = idioma || 'es-CO';
        reconocimiento.continuous = true;
        reconocimiento.interimResults = true;

        reconocimiento.onresult = function (evento) {
            for (let i = evento.resultIndex; i < evento.results.length; i++) {
                const resultado = evento.results[i];
                dotNetRef.invokeMethodAsync('OnDictadoResultado', resultado[0].transcript, resultado.isFinal);
            }
        };

        reconocimiento.onerror = function (evento) {
            dotNetRef.invokeMethodAsync('OnDictadoError', evento.error || 'desconocido');
        };

        reconocimiento.onend = function () {
            dotNetRef.invokeMethodAsync('OnDictadoFin');
        };

        try {
            reconocimiento.start();
        } catch (error) {
            dotNetRef.invokeMethodAsync('OnDictadoError', 'no-se-pudo-iniciar');
        }
    }

    function detener() {
        if (reconocimiento) {
            try {
                reconocimiento.stop();
            } catch (error) {
                // Ya estaba detenido — no hace falta propagar el error.
            }
        }
    }

    return { soportado, iniciar, detener };
})();
