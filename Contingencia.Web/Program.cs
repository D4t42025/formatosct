using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Contingencia.Application.Auth;
using Contingencia.Application.Login;
using Contingencia.Application.Menu;
using Contingencia.Infrastructure.CiaParametros;
using Contingencia.Application.CiaParametros;
using Contingencia.Infrastructure.Captcha;
using Contingencia.Infrastructure.Correo;
using Contingencia.Infrastructure.Data;
using Contingencia.Infrastructure.Filtros;
using Contingencia.Infrastructure.Formatos;
using Contingencia.Infrastructure.Juntas;
using Contingencia.Infrastructure.Login;
using Contingencia.Infrastructure.Mipres;
using Contingencia.Infrastructure.SesionPuente;
using Contingencia.Application.Juntas;
using Contingencia.Application.Sugerencias;
using Contingencia.Application.Formatos;
using Contingencia.Application.Profesionales;
using Contingencia.Application.Contingencias;
using Contingencia.Application.Cie10;
using Contingencia.Application.Parametricas;
using Contingencia.Application.Afiliados;
using Contingencia.Application.Codigos;
using Contingencia.Application.Dci;
using Contingencia.Infrastructure.Profesionales;
using Contingencia.Infrastructure.Contingencias;
using Contingencia.Infrastructure.Contingencias.FormularioPdf;
using Contingencia.Infrastructure.Cie10;
using Contingencia.Infrastructure.Parametricas;
using Contingencia.Infrastructure.Afiliados;
using Contingencia.Infrastructure.Codigos;
using Contingencia.Infrastructure.Dci;
using Contingencia.Web.Auth;
using Contingencia.Web.Components;
using Contingencia.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        // Autenticado pero sin el módulo/rol que exige la página (p. ej. Formatos entrando a /juntas por URL).
        options.AccessDeniedPath = "/inicio";
        options.Cookie.Name = "ContingenciaWebAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
// Llaves de DataProtection (cifran la cookie de sesión y el estado antiforgery): sin esto IIS las deja en memoria o en un perfil que el
// grupo de aplicaciones puede no cargar, y cada reinicio/reciclaje cierra todas las sesiones. Con "DataProtection:RutaLlaves"
// (carpeta fuera de la de publicación, con permiso de escritura para el grupo de aplicaciones) las llaves persisten; en Windows se
// cifran además con DPAPI a nivel de equipo.
var rutaLlaves = builder.Configuration["DataProtection:RutaLlaves"];
if (!string.IsNullOrWhiteSpace(rutaLlaves))
{
    var dp = builder.Services.AddDataProtection()
        .SetApplicationName("Contingencia.Web")
        .PersistKeysToFileSystem(new DirectoryInfo(rutaLlaves.Trim()));
    if (OperatingSystem.IsWindows()) dp.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ImperiumPolicies.ModuloJuntas, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(contexto => contexto.User.TieneModuloExacto(ImperiumPolicies.NombreModuloJuntas)));
    options.AddPolicy(ImperiumPolicies.ModuloProfesionales, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(contexto => contexto.User.TieneModuloExacto(ImperiumPolicies.NombreModuloProfesionales)
                                      || contexto.User.TieneModuloExacto(ImperiumPolicies.NombreModuloJuntas)));
});

// Perfil de tarjetas de Opciones (IPS, INCAPACIDADES, PLUS, SANITASMIPRES, TUTELAS, INFORMES_GF); vacío = IPS.
builder.Services.AddScoped<IMenuOpcionesService>(_ => new MenuOpcionesService(builder.Configuration["Menu:Perfil"]));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IStoredProcedureExecutor, StoredProcedureExecutor>();
builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<ISesionPuenteRepository, SesionPuenteRepository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHttpClient<ITurnstileValidator, TurnstileValidator>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ICambioClaveService, CambioClaveService>();
builder.Services.AddScoped<ILegacyUrlBuilder, LegacyUrlBuilder>();
builder.Services.AddScoped<IFormatosRepository, FormatosRepository>();
builder.Services.AddScoped<IFiltrosRepository, FiltrosRepository>();
builder.Services.AddHttpClient<IMipresPrescripcionValidator, MipresPrescripcionValidator>();
builder.Services.AddScoped<IFormatosService, FormatosService>();
builder.Services.AddScoped<ISugerenciasService, SugerenciasService>();
builder.Services.AddScoped<IFormatosArchivoService, FormatosArchivoService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IJuntasRepository, JuntasRepository>();
builder.Services.AddScoped<IActaPdfBuilder, ActaPdfBuilder>();
builder.Services.AddHttpClient<IJuntaProfesionalApiClient, JuntaProfesionalApiClient>();
builder.Services.AddHttpClient<IFirmaImagenService, FirmaImagenService>();
builder.Services.AddScoped<IJuntasService, JuntasService>();
builder.Services.AddScoped<IJuntasFirmaService, JuntasFirmaService>();
builder.Services.AddScoped<IJuntasArchivoService, JuntasArchivoService>();
builder.Services.AddScoped<IProfesionalesRepository, ProfesionalesRepository>();
builder.Services.AddScoped<IProfesionalesService, ProfesionalesService>();
builder.Services.AddScoped<IProfesionalFirmaArchivoService, ProfesionalFirmaArchivoService>();
builder.Services.AddScoped<ICie10Repository, Cie10Repository>();
builder.Services.AddScoped<ICie10Service, Cie10Service>();
builder.Services.AddScoped<ICodigosRepository, CodigosRepository>();
builder.Services.AddScoped<ICodigosService, CodigosService>();
builder.Services.AddScoped<IDciRepository, DciRepository>();
builder.Services.AddScoped<IDciService, DciService>();
builder.Services.AddScoped<IContingenciasRepository, ContingenciasRepository>();
builder.Services.AddScoped<IContingenciasService, ContingenciasService>();
builder.Services.AddSingleton<IFormularioContingenciaPdfBuilder, FormularioContingenciaPdfBuilder>();
builder.Services.AddScoped<IContingenciaFormularioService, ContingenciaFormularioService>();
builder.Services.AddScoped<IContingenciaArchivoService, ContingenciaArchivoService>();
builder.Services.AddScoped<IParametricasRepository, ParametricasRepository>();
builder.Services.AddScoped<IParametricasService, ParametricasService>();
builder.Services.AddScoped<ICiaParametrosRepository, CiaParametrosRepository>();
builder.Services.AddScoped<ICiaParametrosService, CiaParametrosService>();
builder.Services.AddScoped<IAfiliadoTokenRepository, AfiliadoTokenRepository>();
builder.Services.AddHttpClient<IAfiliadoApiClient, AfiliadoApiClient>();
builder.Services.AddScoped<IAfiliadoService, AfiliadoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<PuenteSesionMiddleware>();

// Página de inicio: por defecto la raíz "/" abre el login (decisión del usuario para producción); la tarjeta de
// opciones (Opciones.razor) sigue disponible en "/opciones". "Inicio:Pagina" = "opciones" devuelve el comportamiento
// anterior (la raíz muestra las opciones). Quien ya tiene sesión va a su pantalla en vez de ver el login otra vez.
if (!string.Equals(app.Configuration["Inicio:Pagina"], "opciones", StringComparison.OrdinalIgnoreCase))
{
    app.Use(async (contexto, siguiente) =>
    {
        if (HttpMethods.IsGet(contexto.Request.Method) && contexto.Request.Path == "/")
        {
            var usuario = contexto.User;
            var destino = "/login";
            if (usuario.Identity?.IsAuthenticated == true)
            {
                var modulos = usuario.FindAll(ImperiumClaimTypes.Modulo).Select(m => m.Value).ToList();
                var soloJuntas = modulos.Contains(ImperiumPolicies.NombreModuloJuntas) && !modulos.Contains("Contingencia");
                destino = soloJuntas ? "/juntas" : "/inicio";
            }
            contexto.Response.Redirect(destino);
            return;
        }
        await siguiente();
    });
}

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Prueba de humo del despliegue: responde 200 si la aplicación llega a la base de datos y 503 si no (sin detalles del error).
app.MapGet("/salud", async (IConfiguration configuracion) =>
{
    try
    {
        await using var conexion = new SqlConnection(configuracion.GetConnectionString("DQUALITY"));
        await conexion.OpenAsync();
        await using var comando = new SqlCommand("SELECT 1", conexion);
        await comando.ExecuteScalarAsync();
        return Results.Ok(new { estado = "ok" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapPost("/cerrar-sesion", async (HttpContext context, ILoginRepository loginRepository) =>
{
    // Réplica de lnkcerrar_ServerClick en SiteMipresN.Master.vb.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated == true)
    {
        await loginRepository.ActualizarMarcaUsuarioAsync(usuario.Cia(), usuario.UsuApp(), intValida: 0);
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapGet("/api/formatos/archivo", async (HttpContext context, string radicado, string extension,
    IFormatosService formatosService, IFormatosArchivoService archivoService) =>
{
    // A diferencia del legado (archivos servidos sin autenticación bajo ~/MediaUploader), acá
    // se exige sesión activa y se confirma que el radicado pertenezca al usuario autenticado
    // reutilizando el mismo filtro que ya aplica sp_MIP_ConsultaFormatosCNT para rol 601
    // (USUARIO LIKE @USUARIO) — evita que un usuario descargue el formato de otro adivinando
    // el radicado.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var cia = usuario.Cia();
    var usuApp = usuario.UsuApp();
    var rad = Path.GetFileName(radicado);
    var ext = Path.GetFileName(extension);

    var resultado = await formatosService.ConsultarAsync(cia, usuApp, "Ninguno", "", "", rad, "", "", "Todo");
    if (!resultado.Exito || !resultado.Formatos.Any(f => f.Radicado == rad))
    {
        return Results.NotFound();
    }

    var ruta = archivoService.RutaFisica(cia, rad, ext);
    if (!archivoService.Existe(ruta)) return Results.NotFound();

    // Sin content-type explícito, Results.File cae a application/octet-stream y el
    // navegador lo descarga como blob genérico sin extensión ("archivo") en vez de abrirlo
    // inline en la pestaña — confirmado con una captura real del usuario. Tampoco se pasa
    // fileDownloadName: eso forzaría "Content-Disposition: attachment" y perdería el
    // comportamiento de "Ver" (abrir el PDF en la pestaña, no descargarlo).
    var contentType = ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".tiff" or ".tif" => "image/tiff",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream",
    };

    return Results.File(ruta, contentType);
}).RequireAuthorization();

app.MapGet("/api/contingencias/formulario", async (HttpContext context, string radicado,
    IContingenciasService contingenciasService, IContingenciaFormularioService formularioService,
    IContingenciaArchivoService archivoService) =>
{
    // Formulario oficial MIPRES NoPBS pre-llenado con los datos de la contingencia. Se genera al
    // vuelo en cada solicitud (no se guarda en disco). Mismo criterio de seguridad que
    // /api/contingencias/archivo: exige sesión y la CIA sale siempre de la sesión autenticada,
    // nunca de un parámetro del cliente.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var cia = usuario.Cia();
    var rad = Path.GetFileName(radicado);

    var resultado = await contingenciasService.ConsultarAsync(cia, null, new FiltroConsultaContingencias(Radicado: rad));
    var contingencia = resultado.Contingencias.FirstOrDefault(c => c.Radicado == rad);
    if (contingencia is null) return Results.NotFound();

    byte[]? firma = null;
    if (!string.IsNullOrWhiteSpace(contingencia.FirmaRuta) && archivoService.Existe(contingencia.FirmaRuta))
    {
        firma = await File.ReadAllBytesAsync(archivoService.RutaFisica(contingencia.FirmaRuta));
    }

    var pdf = await formularioService.GenerarAsync(cia, contingencia, firma);
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization();

app.MapGet("/api/contingencias/archivo", async (HttpContext context, string radicado, string ruta,
    IContingenciasService contingenciasService, IContingenciaArchivoService archivoService) =>
{
    // Mismo criterio de seguridad que /api/formatos/archivo: exige sesión activa y confirma
    // que el radicado pertenece a la CIA del usuario autenticado antes de servir cualquier
    // archivo, y solo permite rutas relativas que ya estén registradas en SOPORTES_RUTAS_JSON
    // o FIRMA_RUTA de esa contingencia — nunca una ruta arbitraria que el cliente arme.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var cia = usuario.Cia();
    var rad = Path.GetFileName(radicado);

    var resultado = await contingenciasService.ConsultarAsync(cia, null, new FiltroConsultaContingencias(Radicado: rad));
    var contingencia = resultado.Contingencias.FirstOrDefault(c => c.Radicado == rad);
    if (contingencia is null) return Results.NotFound();

    var rutasPermitidas = new List<string>();
    if (!string.IsNullOrWhiteSpace(contingencia.FirmaRuta)) rutasPermitidas.Add(contingencia.FirmaRuta);
    if (!string.IsNullOrWhiteSpace(contingencia.SoportesRutasJson))
    {
        rutasPermitidas.AddRange(
            System.Text.Json.JsonSerializer.Deserialize<string[]>(contingencia.SoportesRutasJson!) ?? []);
    }

    if (!rutasPermitidas.Contains(ruta)) return Results.NotFound();

    var rutaFisica = archivoService.RutaFisica(ruta);
    if (!archivoService.Existe(ruta)) return Results.NotFound();

    var contentType = Path.GetExtension(rutaFisica).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".tiff" or ".tif" => "image/tiff",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream",
    };

    return Results.File(rutaFisica, contentType);
}).RequireAuthorization();

app.MapGet("/api/juntas/acta", async (HttpContext context, string prescripcion, IJuntasService juntasService) =>
{
    // Réplica del botón "PDF" de gvconsulta_OnRowCommand en JuntaProfesional.aspx.vb: el
    // acta se genera al vuelo contra la API de SISPRO en cada solicitud (el legado tampoco
    // la guarda en disco), así que este endpoint no necesita más que el número de
    // prescripción — la cía y el tipo de token salen de la sesión autenticada, nunca de un
    // parámetro que el cliente pudiera manipular para pedir el acta de otra compañía.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var cia = usuario.Cia();
    var tokenIps = usuario.TokenMipres() == "IPS";
    var pdf = await juntasService.GenerarActaPdfAsync(cia, tokenIps, prescripcion);
    return Results.File(pdf, "application/pdf");
}).RequireAuthorization(ImperiumPolicies.ModuloJuntas);

app.MapGet("/api/juntas/cargue", (HttpContext context, string prescripcion, string archivo, IJuntasArchivoService archivoService) =>
{
    // Equivalente a los enlaces "Ver" de JuntasCargueDoc.aspx (que servían directo desde
    // ~/MediaUploader/ sin autenticación) — acá se exige sesión activa y la CIA sale
    // siempre de la sesión autenticada, nunca de un parámetro del cliente, para que un
    // usuario no pueda ver archivos de otra compañía cambiando la URL.
    var usuario = context.User;
    if (usuario.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var ruta = archivoService.RutaFisica(usuario.Cia(), Path.GetFileName(prescripcion), Path.GetFileName(archivo));
    if (ruta is null) return Results.NotFound();

    var contentType = Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".tiff" or ".tif" => "image/tiff",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".txt" => "text/plain",
        _ => "application/octet-stream",
    };

    return Results.File(ruta, contentType);
}).RequireAuthorization(ImperiumPolicies.ModuloJuntas);

app.Run();
