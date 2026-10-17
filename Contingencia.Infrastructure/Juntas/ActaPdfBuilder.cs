using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

// SkiaSharp (MIT) reemplaza a System.Drawing (Windows-only, ver FirmaActa.vb del legado) y a
// SixLabors.ImageSharp (la versión sin vulnerabilidades conocidas exige licencia comercial). Sus
// tipos llevan prefijo SK, así que no chocan con los de iText (Image, Rectangle).

namespace Contingencia.Infrastructure.Juntas;

// Réplica de la generación de PDF (iTextSharp) en gvconsulta_OnRowCommand
// (CommandName="PDF") de JuntaProfesional.aspx.vb, portada a iText7 (iTextSharp solo
// corre en .NET Framework). Misma estructura de tablas y mismo contenido — JuntasService
// ya resolvió todos los cruces contra base de datos (DQS_MIP_CRUZAR, DQS_MIP_PROFESIONALESACTA)
// antes de llamar acá, así que esta clase es pura maquetación.
//
// Exclusión deliberada (documentada en BASE_DE_DATOS.md): el legado tiene un bloque
// completo de firmas fijas hardcodeadas ("ANGELA MARIA CABAL PEREZ", etc.) que solo se
// activa cuando Session("CIA")="900971006" — una EPS totalmente distinta a la que usa esta
// app (Contingencia = EPS Sanitas, CIA 890330031). Nunca se ejecutaría para ningún usuario
// real de esta app, así que no se porta.
public class ActaPdfBuilder(IConfiguration configuration) : IActaPdfBuilder
{
    // Logo del encabezado del acta. Antes era una URL fija descargada en CADA acta (si el servidor no tenía salida a internet, o ese sitio
    // caía, el acta entera fallaba). Ahora: "Acta:UrlLogo" puede ser una URL o la ruta de un archivo local (útil sin internet); se descarga
    // una sola vez por proceso y, si no se puede obtener, el acta sale igual con el recuadro del logo vacío.
    private const string UrlLogoPorDefecto = "https://imperium.net.co/img/minsalud.jpg";

    private static readonly object CandadoLogo = new();
    private static string? _origenLogoEnCache;
    private static byte[]? _logoEnCache;

    // ---- Constantes de la celda de firma — réplica exacta de FirmaActa.vb (2026-09-25) ----

    // Ancho útil de la celda: son 3 columnas de 83.3pt, menos los márgenes.
    private const float FirmaAnchoMaximo = 235.0f;

    // Alto máximo. Por encima de esto el renglón de integrantes queda desproporcionado
    // frente al resto del acta.
    private const float FirmaAltoMaximo = 70.0f;

    // Aire que se deja alrededor del trazo al recortar, en píxeles.
    private const int FirmaMargenRecorte = 6;

    // Por debajo de esta opacidad se considera fondo y no trazo.
    private const byte FirmaAlfaMinimo = 16;

    // Un píxel con los tres canales por encima de esto se toma como papel en blanco. Sirve
    // para las firmas escaneadas o fotografiadas, que no traen transparencia.
    private const byte FirmaBlancoMinimo = 240;

    public byte[] Generar(DatosActaPdf datos)
    {
        using var memoria = new MemoryStream();
        using (var writer = new PdfWriter(memoria))
        using (var pdf = new PdfDocument(writer))
        {
            pdf.SetDefaultPageSize(PageSize.LETTER.Rotate());
            pdf.AddEventHandler(PdfDocumentEvent.START_PAGE, new PiePaginaEvento());

            using var document = new Document(pdf);
            document.SetMargins(5, 5, 5, 5);

            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var negrita = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            document.Add(TablaEncabezado(datos, normal, negrita, ObtenerLogo(configuration["Acta:UrlLogo"])));
            document.Add(TablaPrestador(datos, normal, negrita));
            document.Add(TablaPaciente(datos, normal));
            document.Add(TablaClinica(datos, normal));
            document.Add(TablaTecnologiasYJunta(datos, normal, negrita));
        }

        return memoria.ToArray();
    }

    private static byte[]? ObtenerLogo(string? configurado)
    {
        var origen = string.IsNullOrWhiteSpace(configurado) ? UrlLogoPorDefecto : configurado.Trim();
        lock (CandadoLogo)
        {
            if (_logoEnCache is not null && _origenLogoEnCache == origen) return _logoEnCache;
            try
            {
                if (File.Exists(origen))
                {
                    _logoEnCache = File.ReadAllBytes(origen);
                }
                else
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    _logoEnCache = http.GetByteArrayAsync(origen).GetAwaiter().GetResult();
                }

                _origenLogoEnCache = origen;
                return _logoEnCache;
            }
            catch
            {
                // Sin logo el acta se genera igual; se reintenta en la próxima (no se guarda el fallo).
                return null;
            }
        }
    }

    // ---- Tabla 1: encabezado (logo, título, acta) + DATOS DEL PRESTADOR (3 columnas) ----
    private static Table TablaEncabezado(DatosActaPdf d, PdfFont normal, PdfFont negrita, byte[]? logo)
    {
        var tabla = NuevaTabla(300, 300, 150);

        var celdaLogo = new Paragraph();
        if (logo is not null) celdaLogo.Add(new Image(ImageDataFactory.Create(logo)).ScaleToFit(150, 150));
        tabla.AddCell(Celda(celdaLogo, rowspan: 4));
        tabla.AddCell(Celda(Texto("ACTA DE JUNTAS DE PROFESIONALES DE LA SALUD\nMIPRES NO PBSUPC", negrita), rowspan: 4));
        tabla.AddCell(Celda(Texto($"Nro. Acta\n{d.NumeroActa}", negrita)));
        tabla.AddCell(Celda(Texto($"Fecha de acta (AAAA-MM-DD)\n{d.FechaActa}", normal)));
        tabla.AddCell(Celda(Texto($"Modalidad:{d.Modalidad}", negrita)));
        tabla.AddCell(Celda(Texto($"Fecha y hora Expedición(AAAA-MM-DD HH:mm)\n{d.FechaExpedicion}", normal)));

        tabla.AddCell(CeldaSeccion("DATOS DEL PRESTADOR", negrita, 3));
        // "Departamento" y "Municipio" muestran el mismo valor — ver DatosActaPdf.
        tabla.AddCell(Celda(Texto($"Departamento: \n{d.Municipio}", normal)));
        tabla.AddCell(Celda(Texto($"Municipio: \n{d.Municipio}", normal)));
        tabla.AddCell(Celda(Texto($"Código Habilitación: \n{d.CodigoHabilitacion}", normal)));

        return tabla;
    }

    // ---- Tabla 2: detalle del prestador + encabezado DATOS DEL PACIENTE (2 columnas) ----
    private static Table TablaPrestador(DatosActaPdf d, PdfFont normal, PdfFont negrita)
    {
        var tabla = NuevaTabla(375, 375);
        tabla.AddCell(Celda(Texto($"Documento de identificación: \n{d.DocumentoPaciente}", normal)));
        tabla.AddCell(Celda(Texto($"Nombre Prestador de Servicios de Salud: \n{d.NombrePrestador}", normal)));
        tabla.AddCell(Celda(Texto($"Dirección: \n{d.Direccion}", normal)));
        tabla.AddCell(Celda(Texto($"Teléfono: \n{d.Telefono}", normal)));
        // El legado marca esta celda con Colspan=3 pese a que la tabla solo tiene 2
        // columnas (copy/paste del bloque de 3 columnas de arriba) — corregido a 2 acá
        // porque iText7 sí valida los límites de columna (a diferencia de iTextSharp).
        tabla.AddCell(CeldaSeccion("DATOS DEL PACIENTE", negrita, 2));
        return tabla;
    }

    // ---- Tabla 3: datos del paciente (5 columnas) ----
    private static Table TablaPaciente(DatosActaPdf d, PdfFont normal)
    {
        var tabla = NuevaTabla(150, 150, 150, 150, 150);
        tabla.AddCell(Celda(Texto($"Documento de identificación: \n{d.DocumentoPaciente}", normal)));
        tabla.AddCell(Celda(Texto($"Primer Apellido: \n{d.PrimerApellido}", normal)));
        tabla.AddCell(Celda(Texto($"Segundo Apellido: \n{d.SegundoApellido}", normal)));
        tabla.AddCell(Celda(Texto($"Primer Nombre: \n{d.PrimerNombre}", normal)));
        tabla.AddCell(Celda(Texto($"Segundo nombre: \n{d.SegundoNombre}", normal)));
        return tabla;
    }

    // ---- Tabla 4: datos clínicos (4 columnas) ----
    private static Table TablaClinica(DatosActaPdf d, PdfFont normal)
    {
        var tabla = NuevaTabla(187.5f, 187.5f, 187.5f, 187.5f);
        tabla.AddCell(Celda(Texto($"Número Historia Clínica: \n{d.HistoriaClinica}", normal)));
        tabla.AddCell(Celda(Texto($"Diagnóstico Principal: \n{d.DiagnosticoPrincipal}", normal)));
        // "Usuario Régimen" siempre queda vacío — así estaba en el legado (nunca se le
        // asignó un valor real a esa celda).
        tabla.AddCell(Celda(Texto("Usuario Régimen: \n", normal)));
        tabla.AddCell(Celda(Texto($"Ámbito de atención: \n{d.AmbitoAtencion}", normal)));
        return tabla;
    }

    // ---- Tabla 5: tecnologías (medicamentos/procedimientos/dispositivos/productos
    // nutricionales/servicios complementarios) + justificación/decisión + junta de
    // profesionales — todo en UNA sola tabla de 9 columnas, tal como el legado (que nunca
    // hace un Add intermedio entre estas secciones). ----
    private static Table TablaTecnologiasYJunta(DatosActaPdf d, PdfFont normal, PdfFont negrita)
    {
        var tabla = NuevaTabla(83.3f, 83.3f, 83.3f, 83.3f, 83.3f, 83.3f, 83.3f, 83.3f, 83.4f);

        if (d.Medicamentos.Count > 0)
        {
            tabla.AddCell(CeldaSeccion("ANÁLISIS DE LAS TECONOLOGÍAS EN SALUD NO PBSUPC O SERVICIOS COMPLEMENTARIOS", negrita, 9));
            tabla.AddCell(CeldaSeccion("MEDICAMENTOS", negrita, 9));
            AgregarEncabezados(tabla, normal, "Tipo prestación", "NombreMedicamento /Forma Farmacéutica", "Dosis",
                "Vía Administración", "FrecuenciaAdministración", "Indicaciones especiales", "Duración tratamiento",
                "Recomendaciones", "Cantidades farmacéuticas");

            foreach (var m in d.Medicamentos)
            {
                tabla.AddCell(Celda(Texto(TipoPrestacion(m.TipoPrest), normal)));
                tabla.AddCell(Celda(Texto(m.DescMedPrinAct, normal)));
                tabla.AddCell(Celda(Texto(m.Dosis, normal)));
                tabla.AddCell(Celda(Texto(m.CodVA, normal)));
                tabla.AddCell(Celda(Texto($"{m.NoFAdmon} {FrecuenciaAdministracion(m.CodFreAdmon)}", normal)));
                tabla.AddCell(Celda(Texto(IndicacionEspecial(m.IndEsp), normal)));
                tabla.AddCell(Celda(Texto($"{m.CanTrat} {DuracionTratamiento(m.DurTrat)}", normal)));
                tabla.AddCell(Celda(Texto(m.IndRec, normal)));
                tabla.AddCell(Celda(Texto(m.CantTotalF, normal)));
            }
        }

        if (d.ProductosNutricionales.Count > 0)
        {
            tabla.AddCell(CeldaSeccion("PRODUCTOS DE SOPORTE NUTRICIONAL", negrita, 9));
            AgregarEncabezados(tabla, normal, "Tipo prestación", "Producto de SoporteNutricional / Forma", "Dosis",
                "Vía Administración", "FrecuenciaAdministración", "Indicaciones especiales", "Duración tratamiento",
                "Recomendaciones", "Cantidades farmacéuticas");

            foreach (var p in d.ProductosNutricionales)
            {
                tabla.AddCell(Celda(Texto(TipoPrestacion(p.TipoPrest), normal)));
                tabla.AddCell(Celda(Texto(p.DescProdNutr, normal)));
                tabla.AddCell(Celda(Texto(p.Dosis, normal)));
                tabla.AddCell(Celda(Texto(p.CodViaAdmon, normal)));
                tabla.AddCell(Celda(Texto($"{p.NoFAdmon} {FrecuenciaAdministracion(p.CodFreAdmon)}", normal)));
                tabla.AddCell(Celda(Texto(IndicacionEspecial(p.IndEsp), normal)));
                tabla.AddCell(Celda(Texto($"{p.CanTrat} {DuracionTratamiento(p.DurTrat)}", normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto(p.CantTotalF, normal)));
            }
        }

        if (d.Procedimientos.Count > 0)
        {
            tabla.AddCell(CeldaSeccion("PROCEDIMIENTOS", negrita, 9));
            AgregarEncabezados(tabla, normal, "Tipo prestación", "Procedimiento", "Indicaciones o Recomendaciones",
                "Cantidad", "Frecuencia Uso", "Indicaciones", "Duración tratamiento", "Recomendaciones", "Cantidad Total");

            foreach (var p in d.Procedimientos)
            {
                tabla.AddCell(Celda(Texto(TipoPrestacion(p.TipoPrest), normal)));
                tabla.AddCell(Celda(Texto(p.CodCUPS, normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto(p.CanForm, normal)));
                tabla.AddCell(Celda(Texto($"{p.CadaFreUso} {FrecuenciaUso(p.CodFreUso)}", normal)));
                // El legado lee aquí "dispositivos(i).IndRec" (otra lista, mismo índice) en
                // vez de "procedimientos(i).IndRec" — copy/paste que además puede tirar
                // IndexOutOfRange cuando hay más procedimientos que dispositivos. Corregido
                // para repetir su PROPIO IndRec, igual que hacen las secciones de
                // dispositivos y servicios complementarios (ambas repiten su propio IndRec
                // en esta misma columna "Indicaciones").
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto($"{p.Cant} {DuracionPeriodo(p.CodPerDurTrat)}", normal)));
                tabla.AddCell(Celda(Texto("", normal)));
                tabla.AddCell(Celda(Texto(p.CantTotal, normal)));
            }
        }

        if (d.Dispositivos.Count > 0)
        {
            tabla.AddCell(CeldaSeccion("DISPOSITIVOS MÉDICOS", negrita, 9));
            AgregarEncabezados(tabla, normal, "Tipo prestación", "Dispositivo Médico", "Indicaciones o Recomendaciones",
                "Cantidad", "Frecuencia Uso", "Indicaciones", "Duración", "Recomendaciones", "Cantidad Total");

            foreach (var p in d.Dispositivos)
            {
                tabla.AddCell(Celda(Texto(TipoPrestacion(p.TipoPrest), normal)));
                tabla.AddCell(Celda(Texto(p.CodDisp, normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto(p.CanForm, normal)));
                tabla.AddCell(Celda(Texto($"{p.CadaFreUso} {FrecuenciaUso(p.CodFreUso)}", normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto($"{p.Cant} {DuracionPeriodo(p.CodPerDurTrat)}", normal)));
                tabla.AddCell(Celda(Texto("", normal)));
                tabla.AddCell(Celda(Texto(p.CantTotal, normal)));
            }
        }

        if (d.ServiciosComplementarios.Count > 0)
        {
            tabla.AddCell(CeldaSeccion("SERVICIOS COMPLEMENTARIOS", negrita, 9));
            AgregarEncabezados(tabla, normal, "Tipo prestación", "Servicio", "Indicaciones o Recomendaciones",
                "Cantidad", "Frecuencia Uso", "Indicaciones", "Duración", "Recomendaciones", "Cantidad Total");

            foreach (var p in d.ServiciosComplementarios)
            {
                tabla.AddCell(Celda(Texto(TipoPrestacion(p.TipoPrest), normal)));
                tabla.AddCell(Celda(Texto(string.IsNullOrEmpty(p.DescSerComp) ? p.CodSerComp : $"{p.CodSerComp}-{p.DescSerComp}", normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto(p.CanForm, normal)));
                tabla.AddCell(Celda(Texto($"{p.CadaFreUso} {FrecuenciaUso(p.CodFreUso)}", normal)));
                tabla.AddCell(Celda(Texto(p.IndRec, normal)));
                tabla.AddCell(Celda(Texto($"{p.Cant} {DuracionPeriodo(p.CodPerDurTrat)}", normal)));
                tabla.AddCell(Celda(Texto("", normal)));
                tabla.AddCell(Celda(Texto(p.CantTotal, normal)));
            }
        }

        tabla.AddCell(Celda(Texto($"Justifiación médica,técnica y de pertinencia:\n{d.JustificacionTecnica}", negrita), colspan: 6));
        tabla.AddCell(Celda(Texto($"Decisión:\n{d.Decision}", negrita), colspan: 3));

        tabla.AddCell(CeldaSeccion("INTEGRANTES DE LA JUNTA PROFESIONAL DE LA SALUD", negrita, 9));
        foreach (var m in d.Profesionales)
        {
            tabla.AddCell(Celda(Texto($"Documento de identificación:\n{m.NoIdentificacion}", negrita), colspan: 2));
            tabla.AddCell(Celda(Texto($"Nombre:\n{m.ApellidosYNombres}", negrita), colspan: 2));
            tabla.AddCell(Celda(Texto($"Especialidad:\n{m.Profesion}", negrita), colspan: 2));
            tabla.AddCell(CeldaFirma(m.ImagenFirma, negrita));
        }

        return tabla;
    }

    // ---- Traducciones de código (mismos mapeos "If X = N Then ..." repetidos en el
    // legado en cada sección — consolidados acá una sola vez, mismo resultado). ----
    private static string TipoPrestacion(string codigo) => codigo switch
    {
        "1" => "Única",
        "2" => "Sucesiva",
        _ => "",
    };

    // CodFreAdmon (medicamentos/productos nutricionales): 1-7.
    private static string FrecuenciaAdministracion(string codigo) => codigo switch
    {
        "1" => "Minuto(s)",
        "2" => "Hora(s)",
        "3" => "Día(s)",
        "4" => "Semana(s)",
        "5" => "Mes(es)",
        "6" => "Año",
        "7" => "Según respuesta al",
        _ => "",
    };

    // CodFreUso (procedimientos/dispositivos/servicios): 1-6 y 8 (no existe el 7).
    private static string FrecuenciaUso(string codigo) => codigo switch
    {
        "1" => "Minuto(s)",
        "2" => "Hora(s)",
        "3" => "Día(s)",
        "4" => "Semana(s)",
        "5" => "Mes(es)",
        "6" => "Año",
        "8" => "Única",
        _ => "",
    };

    // DurTrat (medicamentos/productos nutricionales) y CodPerDurTrat (procedimientos/
    // dispositivos/servicios) usan el mismo mapeo 1-6 en el legado.
    private static string DuracionTratamiento(string codigo) => DuracionPeriodo(codigo);

    private static string DuracionPeriodo(string codigo) => codigo switch
    {
        "1" => "Minuto(s)",
        "2" => "Hora(s)",
        "3" => "Día(s)",
        "4" => "Semana(s)",
        "5" => "Mes(es)",
        "6" => "Año",
        _ => "",
    };

    private static string IndicacionEspecial(string codigo) => codigo switch
    {
        "1" => "Administración en dosis única",
        "2" => "Administración inmediata",
        "3" => "Administrar en Bolo",
        "4" => "Administrar en Goteo",
        "5" => "Infusión continua",
        "6" => "Infusión intermitente",
        "7" => "Infusión intermitente simultánea con perfusión deotra solución",
        "8" => "Microgoteo",
        "9" => "Perfusión",
        "10" => "Sin indicación especial",
        _ => "",
    };

    // ---- Celda "Firma:" de INTEGRANTES DE LA JUNTA — réplica de FirmaActa.vb, portada de
    // System.Drawing (Windows-only) a SkiaSharp (multiplataforma). ----

    // Si no hay imagen, o si falla el recorte/decodificado, la celda sale con el rótulo y
    // nada más: una firma que falta no puede impedir que se imprima el acta.
    private static Cell CeldaFirma(byte[]? imagenBruta, PdfFont negrita)
    {
        var contenido = new Paragraph("Firma:\n").SetFont(negrita).SetFontSize(9);

        var firma = ImagenDeFirma(imagenBruta);
        if (firma is not null)
        {
            contenido.Add(firma);
        }

        return Celda(contenido.SetTextAlignment(TextAlignment.CENTER), colspan: 3);
    }

    private static Image? ImagenDeFirma(byte[]? imagenBruta)
    {
        if (imagenBruta is null || imagenBruta.Length == 0) return null;

        try
        {
            var recortada = RecortadaAlTrazo(imagenBruta) ?? imagenBruta;
            var firma = new Image(ImageDataFactory.Create(recortada));
            firma.ScaleToFit(FirmaAnchoMaximo, FirmaAltoMaximo);
            return firma;
        }
        catch
        {
            return null;
        }
    }

    // El lienzo donde se dibuja la firma mide 756x200 y el trazo casi siempre ocupa una
    // parte pequeña. Escalando el lienzo completo la firma sale diminuta, así que primero se
    // busca el rectángulo que de verdad tiene tinta y se recorta a eso. Devuelve null si no
    // hay nada que recortar o si el archivo no es una imagen (p.ej. un PDF escaneado subido
    // como firma) — en ese caso se usa el archivo original tal cual.
    private static byte[]? RecortadaAlTrazo(byte[] imagenBruta)
    {
        try
        {
            // Se pide RGBA sin premultiplicar: el canal alfa y los colores se comparan tal cual
            // contra los umbrales, sin que la conversión altere los píxeles semitransparentes.
            var info = SKBitmap.DecodeBounds(imagenBruta);
            if (info.IsEmpty) return null;
            using var original = SKBitmap.Decode(imagenBruta, new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            if (original is null) return null;

            var limites = LimitesDelTrazo(original);

            if (limites.Width <= 0 || limites.Height <= 0) return null;
            if (limites.Width >= original.Width && limites.Height >= original.Height) return null;

            using var recorte = new SKBitmap();
            if (!original.ExtractSubset(recorte, limites)) return null;
            using var imagen = SKImage.FromBitmap(recorte);
            using var datos = imagen.Encode(SKEncodedImageFormat.Png, 100);
            return datos.ToArray();
        }
        catch
        {
            return null;
        }
    }

    // Rectángulo que encierra los píxeles con tinta, con un margen de aire. Se recorre el
    // búfer de bytes directamente (4 bytes RGBA por píxel), no pixel a pixel con GetPixel: el
    // campo también acepta imágenes cargadas a mano desde ActualizarUsuarioMIPRES, y con la foto
    // de un celular un acceso ingenuo se tarda segundos.
    private static SKRectI LimitesDelTrazo(SKBitmap mapa)
    {
        var minX = mapa.Width;
        var minY = mapa.Height;
        var maxX = -1;
        var maxY = -1;

        var bytes = mapa.GetPixelSpan();
        var anchoFila = mapa.RowBytes;

        for (var y = 0; y < mapa.Height; y++)
        {
            var fila = bytes.Slice(y * anchoFila, mapa.Width * 4);
            for (var x = 0; x < mapa.Width; x++)
            {
                var r = fila[x * 4];
                var g = fila[x * 4 + 1];
                var b = fila[x * 4 + 2];
                var a = fila[x * 4 + 3];

                // Hay tinta si el píxel no es transparente —el fondo del canvas— ni
                // prácticamente blanco —el papel de una firma escaneada—.
                var esTinta = a > FirmaAlfaMinimo &&
                    !(r > FirmaBlancoMinimo && g > FirmaBlancoMinimo && b > FirmaBlancoMinimo);

                if (!esTinta) continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        // Lienzo en blanco: no hay firma que recortar.
        if (maxX < 0 || maxY < 0) return SKRectI.Empty;

        minX = Math.Max(0, minX - FirmaMargenRecorte);
        minY = Math.Max(0, minY - FirmaMargenRecorte);
        maxX = Math.Min(mapa.Width - 1, maxX + FirmaMargenRecorte);
        maxY = Math.Min(mapa.Height - 1, maxY + FirmaMargenRecorte);

        return new SKRectI(minX, minY, maxX + 1, maxY + 1);
    }

    // ---- Helpers de maquetación ----
    private static Table NuevaTabla(params float[] anchosColumnas) => new(UnitValue.CreatePointArray(anchosColumnas));

    // La API de SISPRO deja varios campos en null (acta sin asignar, campos opcionales
    // sin diligenciar) — iText7 no acepta null como texto de un Paragraph, así que se
    // sanea acá, en el único punto por el que pasa todo el texto de las celdas.
    private static Paragraph Texto(string? texto, PdfFont fuente) => new Paragraph(texto ?? "").SetFont(fuente).SetFontSize(9);

    private static Cell Celda(IBlockElement contenido, int rowspan = 1, int colspan = 1)
    {
        var celda = new Cell(rowspan, colspan);
        celda.Add(contenido);
        celda.SetVerticalAlignment(VerticalAlignment.MIDDLE);
        return celda;
    }

    private static Cell CeldaSeccion(string texto, PdfFont fuente, int colspan) =>
        Celda(Texto(texto, fuente).SetTextAlignment(TextAlignment.CENTER), colspan: colspan)
            .SetBackgroundColor(ColorConstants.GRAY);

    private static void AgregarEncabezados(Table tabla, PdfFont fuente, params string[] titulos)
    {
        foreach (var titulo in titulos)
        {
            tabla.AddCell(Celda(Texto(titulo, fuente).SetTextAlignment(TextAlignment.CENTER)));
        }
    }

    // Réplica de itsEvents en ConsultaMIPRES.aspx.vb: numera páginas y estampa el mismo pie
    // de página ("Copia generada desde IMPERIUM.NET.CO...") en cada una.
    // iText 9: IEventHandler/Event pasaron a AbstractPdfDocumentEventHandler/AbstractPdfDocumentEvent
    // (namespace iText.Kernel.Pdf.Event); el tipo de evento se registra en AddEventHandler.
    private class PiePaginaEvento : AbstractPdfDocumentEventHandler
    {
        protected override void OnAcceptedEvent(AbstractPdfDocumentEvent currentEvent)
        {
            var docEvent = (PdfDocumentEvent)currentEvent;
            var pdfDoc = docEvent.GetDocument();
            var pagina = docEvent.GetPage();
            var fuente = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var numeroPagina = pdfDoc.GetPageNumber(pagina);

            using var canvas = new Canvas(pagina, pagina.GetPageSize());
            canvas.SetFont(fuente).SetFontSize(10);
            canvas.ShowTextAligned($"Página: {numeroPagina}", 680, 25, TextAlignment.RIGHT);
            canvas.ShowTextAligned(
                $"Copia generada desde IMPERIUM.NET.CO || MIPRES NO PBSUPC - fecha de impresión: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                20, 25, TextAlignment.LEFT);
        }
    }
}
