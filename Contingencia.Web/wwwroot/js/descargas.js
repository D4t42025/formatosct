window.ImperiumDescargas = {
    descargar: function (base64, nombreArchivo, contentType) {
        const binario = atob(base64);
        const bytes = new Uint8Array(binario.length);
        for (let i = 0; i < binario.length; i++) { bytes[i] = binario.charCodeAt(i); }
        const blob = new Blob([bytes], { type: contentType });
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url; enlace.download = nombreArchivo;
        document.body.appendChild(enlace); enlace.click();
        document.body.removeChild(enlace); URL.revokeObjectURL(url);
    }
};
