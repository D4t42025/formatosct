using System.Reflection;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Utils;
using iText.IO.Font.Constants;

namespace Contingencia.Infrastructure.Contingencias.FormularioPdf;

public interface IFormularioContingenciaPdfBuilder
{
    byte[] Generar(DatosFormularioContingencia datos);
}

// Pinta los datos de una contingencia sobre el formulario oficial MIPRES NoPBS (3 páginas, sin
// campos AcroForm — por eso todo va por coordenadas). Se genera una copia de las páginas 1 y 2
// por cada tecnología (cada una con su sección: VI Medicamento, VII/VIII Procedimiento, X
// Dispositivo, XII Producto nutricional, XIV Servicio complementario) y al final, una sola vez,
// la página 3 (instrucciones). Lo que el wizard no captura se deja en blanco.
//
// Compatibilidad Linux: solo iText (managed), fuentes estándar PDF (Helvetica, sin fuentes del
// sistema) y la plantilla incrustada en el ensamblado — sin System.Drawing ni rutas de archivo.
//
// Coordenadas: las constantes están medidas sobre la plantilla con origen ARRIBA-izquierda (eje Y
// hacia abajo, página de 612x1008 pt) y se convierten al eje de PDF en Hoja.Y().
public class FormularioContingenciaPdfBuilder : IFormularioContingenciaPdfBuilder
{
    private const string NombrePlantilla = "formulario-contingencia-mipres.pdf";
    private const float PasoCelda = 11.3f;

    public byte[] Generar(DatosFormularioContingencia d)
    {
        using var salida = new MemoryStream();
        using (var docSalida = new PdfDocument(new PdfWriter(salida)))
        {
            var merger = new PdfMerger(docSalida);

            // Sin tecnologías (no debería pasar) igual sale un formulario con las secciones I a IV.
            IEnumerable<TecnologiaFormulario?> tecnologias =
                d.Tecnologias.Count > 0 ? d.Tecnologias.Cast<TecnologiaFormulario?>() : new TecnologiaFormulario?[] { null };
            foreach (var tecnologia in tecnologias)
            {
                var estampado = Estampar(d, tecnologia);
                using var lector = new PdfDocument(new PdfReader(new MemoryStream(estampado)));
                merger.Merge(lector, 1, 2);
            }

            using var plantilla = new PdfDocument(new PdfReader(AbrirPlantilla()));
            merger.Merge(plantilla, 3, 3);
            merger.Close();
        }

        return salida.ToArray();
    }

    private static Stream AbrirPlantilla() =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(NombrePlantilla)
        ?? throw new InvalidOperationException($"Falta el recurso incrustado '{NombrePlantilla}'.");

    private static byte[] Estampar(DatosFormularioContingencia d, TecnologiaFormulario? t)
    {
        using var ms = new MemoryStream();
        using (var doc = new PdfDocument(new PdfReader(AbrirPlantilla()), new PdfWriter(ms)))
        {
            var fuente = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var p1 = new Hoja(new PdfCanvas(doc.GetPage(1)), fuente);
            var p2 = new Hoja(new PdfCanvas(doc.GetPage(2)), fuente);

            Encabezado(p1, d);

            if (t is not null)
            {
                switch (t.TipoTecnologia)
                {
                    case "Medicamento": Medicamento(p1, d, t); break;
                    case "Procedimiento": Procedimiento(p1, p2, d, t); break;
                    case "Dispositivo": Dispositivo(p2, d, t); break;
                    case "ProductoNutricional": ProductoNutricional(p2, d, t); break;
                    case "ServicioComplementario": ServicioComplementario(p2, d, t); break;
                }
            }
        }

        return ms.ToArray();
    }

    // ---- I a IV: datos comunes a todas las tecnologías --------------------------------------
    private static void Encabezado(Hoja h, DatosFormularioContingencia d)
    {
        switch (d.MotivoCasilla)
        {
            case "DIFICULTAD_TECNICA": h.Marca(167.6f, 96.8f); break;
            case "SIN_CONECTIVIDAD": h.Marca(167.6f, 104.8f); break;
            case "SIN_SERVICIO_ELECTRICO": h.Marca(167.6f, 112.8f); break;
        }
        h.Caja(d.Aclaracion, 196f, 105.2f, 218f, 13.5f, 7f);
        h.Imagen(d.Firma, 424f, 97.5f, 104f, 15f);

        // 2) Fecha y hora de diligenciamiento
        var f = d.FechaDiligenciamiento;
        h.Digitos(f.ToString("yyyy"), 137.3f, 132.3f, 139.8f, 4);
        h.Digitos(f.ToString("MM"), 215.8f, 132.3f, 139.8f, 2);
        h.Digitos(f.ToString("dd"), 272.2f, 132.3f, 139.8f, 2);
        h.Digitos(f.ToString("HH"), 339.9f, 132.3f, 139.8f, 2);
        h.Digitos(f.ToString("mm"), 373.8f, 132.3f, 139.8f, 2);

        // II. IPS
        h.Texto(d.IpsRazonSocial, 126f, 151.3f, 7f, 188f);
        h.Digitos(SoloDigitos(d.IpsNit), 339.9f, 144.5f, 152.5f, 13);
        h.Digitos(SoloDigitos(d.IpsCodigoHabilitacion), 148.6f, 157.3f, 166.3f, 12);

        // III. Profesional (solo CC y CE tienen casilla)
        if (d.ProfesionalTipoDoc == "CC") h.Marca(193.1f, 177.2f);
        else if (d.ProfesionalTipoDoc == "CE") h.Marca(193.1f, 185.2f);
        h.Digitos(d.ProfesionalDocumento, 339.9f, 177.4f, 184.9f, 17);
        Nombres(h, 193.2f, 199.8f, d.ProfesionalPrimerApellido, d.ProfesionalSegundoApellido, d.ProfesionalPrimerNombre, d.ProfesionalSegundoNombre);

        // IV. Datos iniciales — paciente
        var casillaDoc = d.PacienteTipoDoc switch
        {
            "CC" => 103.3f, "RC" => 125.9f, "TI" => 148.4f, "CE" => 170.5f, "PA" => 193.1f,
            "NV" => 215.6f, "CD" => 238.2f, "SC" => 260.8f, "PR" => 283.3f, _ => (float?)null,
        };
        if (casillaDoc is not null) h.Marca(casillaDoc.Value, 234.1f);
        h.Digitos(d.PacienteDocumento, 328.6f, 233.4f, 240.8f, 18);
        Nombres(h, 248.4f, 254.9f, d.PacientePrimerApellido, d.PacienteSegundoApellido, d.PacientePrimerNombre, d.PacienteSegundoNombre);

        // 14) Ámbito de atención
        switch (d.AmbitoCodigo)
        {
            case "12": h.Marca(170.5f, 272.1f); break;
            case "11": h.Marca(259.6f, 272.0f); break;
            case "21": h.Marca(356.6f, 272.0f); break;
            case "22": h.Marca(453.6f, 272.0f); break;
            case "30": h.Marca(512.9f, 273.0f); break;
        }

        // 17) Diagnósticos (15, 16 y 18 — enfermedad huérfana y soporte nutricional — quedan en blanco)
        h.Texto(d.DiagnosticoPrincipal, 142f, 311.2f, 6.5f, 205f);
        h.Texto(d.DiagnosticoRelacionado1, 155f, 318.3f, 6.5f, 190f);
        h.Texto(d.DiagnosticoRelacionado2, 155f, 326.4f, 6.5f, 190f);
    }

    private static void Nombres(Hoja h, float yTopCelda, float yBase, string? apellido1, string? apellido2, string? nombre1, string? nombre2)
    {
        float[] xs = [171.4f, 260.9f, 351.2f, 441.5f];
        string?[] valores = [apellido1, apellido2, nombre1, nombre2];
        for (var i = 0; i < 4; i++)
        {
            if (string.IsNullOrWhiteSpace(valores[i])) continue;
            h.Blanquear(xs[i] + 1f, yTopCelda - 0.4f, 88f, 7.8f);
            h.Texto(valores[i]!.ToUpperInvariant(), xs[i] + 2.5f, yBase, 7f, 85f);
        }
    }

    // ---- Secciones por tipo de tecnología ----------------------------------------------------
    private static void Medicamento(Hoja h, DatosFormularioContingencia d, TecnologiaFormulario t)
    {
        h.Caja(t.PrincipioActivo, 84f, 603.4f, 209f, 14.5f, 7f);
        h.Texto(t.ConcentracionNumero, 309f, 617.6f, 7f, 50f);
        h.Texto(t.ConcentracionUnidad, 365f, 617.6f, 7f, 72f);
        h.Texto(t.FormaFarmaceutica, 84f, 633.5f, 7f, 208f);
        h.Caja(t.Justificacion, 308f, 627.5f, 222f, 23f, 6.5f);
        h.Texto(t.ViaAdministracion, 84f, 649.5f, 7f, 208f);

        h.Texto(t.DosisNumero, 84f, 680.5f, 7f, 28f);
        h.Texto(t.DosisUnidad, 116f, 680.5f, 7f, 58f);
        h.Texto(t.CadaNumero, 180f, 680.5f, 7f, 32f);
        h.Texto(t.CadaUnidad, 216f, 680.5f, 7f, 58f);
        h.Caja(t.IndicacionEspecial, 278f, 673.2f, 78f, 9.5f, 6f, 4f);
        h.Texto(t.DuracionNumero, 360f, 680.5f, 7f, 35f);
        h.Texto(t.DuracionPeriodo, 400f, 680.5f, 7f, 35f);
        h.Texto(t.CantidadTotal, 439f, 680.5f, 7f, 30f);

        h.Caja(t.IndicacionesAdicionales, 84f, 692.5f, 320f, 18f, 7f);
        h.Imagen(d.Firma, 425f, 684f, 100f, 17f);
    }

    private static void Procedimiento(Hoja p1, Hoja p2, DatosFormularioContingencia d, TecnologiaFormulario t)
    {
        // VII (página 1): CUPS 1 de la combinación
        p1.Digitos(t.Codigo, 306f, 721.8f, 729.3f, 6);

        // VIII (página 2)
        p2.Digitos(t.Codigo, 193.2f, 94.3f, 101.8f, 6);
        p2.Caja(t.Nombre, 127f, 110.4f, 156f, 8f, 7f, 4.5f);
        p2.Caja(t.Justificacion, 84f, 126.8f, 217f, 14.5f, 6.5f);
        p2.Texto(t.CantidadTotal, 309f, 124.6f, 7f, 40f);
        p2.Texto(t.DuracionNumero, 443f, 133.2f, 7f, 30f);
        p2.Texto(t.DuracionPeriodo, 478f, 133.2f, 7f, 50f);
        p2.Caja(Unir(t.IndicacionEspecial, t.IndicacionesAdicionales), 84f, 151f, 320f, 18f, 7f);
        p2.Imagen(d.Firma, 425f, 145f, 100f, 13f);
    }

    private static void Dispositivo(Hoja p2, DatosFormularioContingencia d, TecnologiaFormulario t)
    {
        p2.Texto(Unir(t.Codigo, t.Nombre, " - "), 84f, 223.6f, 7f, 198f);
        p2.Caja(t.Justificacion, 84f, 233.5f, 197f, 28f, 6.5f);
        p2.Texto(t.CantidadTotal, 309f, 231.6f, 7f, 40f);
        p2.Texto(t.DuracionNumero, 443f, 240.2f, 7f, 30f);
        p2.Texto(t.DuracionPeriodo, 478f, 240.2f, 7f, 50f);
        p2.Imagen(d.Firma, 425f, 243f, 100f, 13f);
    }

    private static void ProductoNutricional(Hoja p2, DatosFormularioContingencia d, TecnologiaFormulario t)
    {
        p2.Texto(Unir(t.Codigo, t.Nombre, " - "), 319f, 499f, 6.5f, 210f);
        p2.Texto(t.DuracionNumero, 337f, 546.9f, 7f, 35f);
        p2.Texto(t.DuracionPeriodo, 377f, 546.9f, 7f, 40f);
        p2.Texto(t.CantidadTotal, 439f, 546.9f, 7f, 30f);
        p2.Caja(Unir(t.IndicacionEspecial, t.IndicacionesAdicionales), 84f, 559f, 320f, 22f, 7f);
        p2.Imagen(d.Firma, 425f, 557f, 100f, 15f);
    }

    private static void ServicioComplementario(Hoja p2, DatosFormularioContingencia d, TecnologiaFormulario t)
    {
        p2.Caja(t.Justificacion, 141f, 640.4f, 389f, 9.6f, 6.5f);
        p2.Texto(Unir(t.Codigo, t.Nombre, " - "), 207f, 697f, 7f, 322f);
        p2.Texto(t.CantidadTotal, 94f, 720.8f, 7f, 40f);
        p2.Texto(t.DuracionNumero, 228f, 727f, 7f, 40f);
        p2.Texto(t.DuracionPeriodo, 280f, 727f, 7f, 40f);
        p2.Caja(Unir(t.IndicacionEspecial, t.IndicacionesAdicionales), 331f, 716f, 200f, 28f, 6.5f);
        p2.Imagen(d.Firma, 425f, 733f, 100f, 13f);
    }

    // ---- utilidades --------------------------------------------------------------------------
    private static string? SoloDigitos(string? valor) =>
        valor is null ? null : new string(valor.Where(char.IsDigit).ToArray());

    private static string? Unir(string? a, string? b, string separador = " — ") =>
        string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : $"{a}{separador}{b}";

    // Primitivas de dibujo con coordenadas medidas desde ARRIBA-izquierda de la página.
    private sealed class Hoja(PdfCanvas lienzo, PdfFont fuente)
    {
        private const float AltoPagina = 1008f;
        private static float Y(float yAbajo) => AltoPagina - yAbajo;

        private static string Limpiar(string? texto) =>
            (texto ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();

        public void Blanquear(float x, float yTop, float ancho, float alto)
        {
            lienzo.SaveState().SetFillColor(ColorConstants.WHITE).Rectangle(x, Y(yTop + alto), ancho, alto).Fill().RestoreState();
        }

        // Una sola línea; reduce el tamaño de letra hasta que quepa en `anchoMax` (mínimo 4.5 pt).
        public void Texto(string? texto, float x, float yBase, float tam, float anchoMax)
        {
            texto = Limpiar(texto);
            if (texto.Length == 0) return;
            while (tam > 4.5f && fuente.GetWidth(texto, tam) > anchoMax) tam -= 0.25f;
            lienzo.SaveState().BeginText().SetFillColor(ColorConstants.BLACK).SetFontAndSize(fuente, tam)
                .MoveText(x, Y(yBase)).ShowText(texto).EndText().RestoreState();
        }

        // Varias líneas dentro de una caja; reduce la letra hasta que el texto quepa.
        public void Caja(string? texto, float x, float yTop, float ancho, float alto, float tamMax, float tamMin = 4.5f)
        {
            texto = Limpiar(texto);
            if (texto.Length == 0) return;

            for (var tam = tamMax; ; tam -= 0.25f)
            {
                var lineas = Envolver(texto, tam, ancho);
                var interlineado = tam * 1.12f;
                if (lineas.Count * interlineado <= alto + 0.01f || tam <= tamMin)
                {
                    var maximo = Math.Max(1, (int)Math.Floor(alto / interlineado));
                    for (var i = 0; i < lineas.Count && i < maximo; i++)
                    {
                        var linea = lineas[i];
                        if (i == maximo - 1 && lineas.Count > maximo)
                        {
                            // No cabe todo ni con la letra mínima: se recorta la última línea visible.
                            linea += "…";
                            while (linea.Length > 2 && fuente.GetWidth(linea, tam) > ancho) linea = linea[..^2] + "…";
                        }

                        lienzo.SaveState().BeginText().SetFillColor(ColorConstants.BLACK).SetFontAndSize(fuente, tam)
                            .MoveText(x, Y(yTop + tam * 0.86f + i * interlineado)).ShowText(linea).EndText().RestoreState();
                    }

                    return;
                }
            }
        }

        private List<string> Envolver(string texto, float tam, float ancho)
        {
            var lineas = new List<string>();
            var actual = "";
            foreach (var palabra in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (fuente.GetWidth(prueba, tam) <= ancho || actual.Length == 0)
                {
                    actual = prueba;
                }
                else
                {
                    lineas.Add(actual);
                    actual = palabra;
                }
            }

            if (actual.Length > 0) lineas.Add(actual);
            return lineas;
        }

        // X dentro de una casilla de 8.5 x 6.1 pt.
        public void Marca(float x, float yTop, float ancho = 8.5f, float alto = 6.1f)
        {
            const float margen = 1.1f;
            lienzo.SaveState().SetStrokeColor(ColorConstants.BLACK).SetLineWidth(0.9f)
                .MoveTo(x + margen, Y(yTop + margen)).LineTo(x + ancho - margen, Y(yTop + alto - margen))
                .MoveTo(x + margen, Y(yTop + alto - margen)).LineTo(x + ancho - margen, Y(yTop + margen))
                .Stroke().RestoreState();
        }

        // Un carácter por celda, de izquierda a derecha (el resto de celdas queda vacío).
        public void Digitos(string? valor, float x0, float yTop, float yBottom, int celdas)
        {
            valor = Limpiar(valor).ToUpperInvariant();
            if (valor.Length == 0) return;
            const float tam = 7.5f;
            for (var i = 0; i < celdas && i < valor.Length; i++)
            {
                var xCelda = x0 + i * PasoCelda;
                Blanquear(xCelda + 0.9f, yTop, PasoCelda - 1.2f, yBottom - yTop);
                var c = valor[i].ToString();
                var x = xCelda + (PasoCelda - fuente.GetWidth(c, tam)) / 2f;
                lienzo.SaveState().BeginText().SetFillColor(ColorConstants.BLACK).SetFontAndSize(fuente, tam)
                    .MoveText(x, Y(yBottom - 1.2f)).ShowText(c).EndText().RestoreState();
            }
        }

        // Imagen (firma) ajustada dentro del rectángulo, conservando la proporción.
        public void Imagen(byte[]? bytes, float x, float yTop, float ancho, float alto)
        {
            if (bytes is null || bytes.Length == 0) return;
            try
            {
                var imagen = ImageDataFactory.Create(bytes);
                lienzo.AddImageFittedIntoRectangle(imagen, new Rectangle(x, Y(yTop + alto), ancho, alto), false);
            }
            catch (Exception ex) when (ex is iText.IO.Exceptions.IOException or iText.Kernel.Exceptions.PdfException or ArgumentException)
            {
                // Firma en un formato que iText no decodifica: el formulario sale sin ella.
            }
        }
    }
}
