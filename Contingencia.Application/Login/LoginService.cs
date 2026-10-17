using System.Data;
using System.Text;
using Contingencia.Domain.Login;
using Contingencia.Domain.SesionPuente;
using Contingencia.Infrastructure.Correo;
using Contingencia.Infrastructure.Captcha;
using Contingencia.Infrastructure.Login;
using Contingencia.Infrastructure.SesionPuente;
using Microsoft.Extensions.Caching.Memory;

namespace Contingencia.Application.Login;

// Equivalente a btnlogin2_ServerClick en Login3.aspx.vb, con dos adiciones pedidas
// explícitamente por el usuario (2026-09-02) que van MÁS ALLÁ de lo que hace hoy Login3.aspx:
//
// 1. Captcha: Login3.aspx.vb trae el control GoogleReCaptcha comentado (deshabilitado). Se
//    agrega Cloudflare Turnstile (más moderno que el reCAPTCHA v2 que usa Recobros) en vez
//    de simplemente reactivar el mismo mecanismo viejo — ver TurnstileValidator.
// 2. Recuperar clave: el flujo real (crearcorreo() + GenerarCadena(8)) SÍ existe en
//    Login3.aspx.vb, pero solo es alcanzable hoy vía un query string "txtemail" que ningún
//    control visible dispara (el enlace en pantalla apunta a WhatsApp). Se implementa como
//    un formulario real y accesible, igual que ya lo tiene Recobros.
//
// Además, mismo contador de intentos fallidos por caché de servidor (20 min) en vez de
// Session("suma") que ya usa Imperium.Application.Login.LoginService (Recobros) —
// Session("suma") se reinicia con solo refrescar la página, así que en la práctica no
// protegía casi nada contra fuerza bruta.
public class LoginService(
    ILoginRepository loginRepository,
    ISesionPuenteRepository sesionPuenteRepository,
    ITurnstileValidator turnstileValidator,
    IEmailSender emailSender,
    IMemoryCache cache) : ILoginService
{
    private const string MensajeCredencialesInvalidas =
        "Señor usuario contacte a Soporte Imperium para validar si no puede ingresar al sistema por: " +
        "1.Olvido de contraseña 2.Bloqueo del sistema 3.Usuario Inactivo o no Registrado 4.Licencia de Plataforma Caducada";

    private const string MensajeBloqueado =
        "Usuario bloqueado, ha llegado al límite de intentos(3). Contacte al administrador del sistema para generar el desbloqueo.";

    public async Task<ResultadoIntentoLogin> IniciarSesionAsync(string usuario, string clave, string captchaResponse, string ip)
    {
        if (!await turnstileValidator.EsValidoAsync(captchaResponse))
        {
            return new ResultadoIntentoLogin
            {
                Estado = EstadoIntentoLogin.CaptchaInvalido,
                Mensaje = "Captcha inválido",
            };
        }

        var dt = await loginRepository.ConsultarUsuarioAsync(usuario, clave);
        if (dt.Rows.Count == 0)
        {
            var claveIntentos = ClaveIntentos(usuario);
            var intentos = cache.TryGetValue(claveIntentos, out int previos) ? previos + 1 : 1;
            cache.Set(claveIntentos, intentos, TimeSpan.FromMinutes(20));

            if (intentos >= 3)
            {
                // Réplica fiel: el legado tampoco resuelve la Cía real aquí (nunca se
                // asignó objent.strcia antes de este punto en la rama de fallo).
                await loginRepository.ActualizarMarcaUsuarioAsync(cia: "", usuario, intValida: 2);
                return new ResultadoIntentoLogin
                {
                    Estado = EstadoIntentoLogin.Bloqueado,
                    Mensaje = MensajeBloqueado,
                };
            }

            return new ResultadoIntentoLogin
            {
                Estado = EstadoIntentoLogin.CredencialesInvalidas,
                Mensaje = MensajeCredencialesInvalidas,
            };
        }

        cache.Remove(ClaveIntentos(usuario));

        var resultadoUsuario = ResultadoUsuario.DesdeFila(dt.Rows[0]);

        var modulosDt = await loginRepository.ConsultarModulosPorRolAsync(resultadoUsuario.Cia, resultadoUsuario.RolParaModulos);
        await loginRepository.ActualizarMarcaUsuarioAsync(resultadoUsuario.Cia, resultadoUsuario.UsuarioApp, intValida: 1);

        var modActIni = modulosDt.Rows.Cast<DataRow>()
            .Any(fila => Convert.ToString(fila["MODULO"])?.Contains("Inicio") == true) ? "1" : "";

        var passwordInicial = resultadoUsuario.ClaveTemporalReferencia is not null && resultadoUsuario.ClaveTemporalReferencia == clave
            ? clave
            : null;

        var log = $"{ip}_{Random.Shared.Next(10000, 109999)}";

        var datosPuente = new DatosSesionPuente
        {
            Cia = resultadoUsuario.Cia,
            Rol = resultadoUsuario.Rol,
            Nombre = resultadoUsuario.Nombre,
            IdEmp = resultadoUsuario.IdEmp,
            UsuApp = resultadoUsuario.UsuarioApp,
            Logo = resultadoUsuario.Logo,
            CodigoEps = resultadoUsuario.CodigoEps,
            Fecha = resultadoUsuario.Fecha,
            LogoEmpresa = resultadoUsuario.LogoEmpresa,
            TextoCarta = resultadoUsuario.TextoCarta,
            TextoNotificacion = resultadoUsuario.TextoNotificacion,
            NombreCliente = resultadoUsuario.NombreCliente,
            RutaPdf = resultadoUsuario.RutaPdf,
            Firma = resultadoUsuario.Firma,
            NitCliente = resultadoUsuario.NitCliente,
            DireccionCliente = resultadoUsuario.DireccionCliente,
            TelefonoCliente = resultadoUsuario.TelefonoCliente,
            PasswordInicial = passwordInicial,
            ModActIni = modActIni,
            Log = log,
            IdRol = resultadoUsuario.RolParaModulos,
            TokenMipres = resultadoUsuario.Token,
        };

        var token = await sesionPuenteRepository.CrearAsync(datosPuente, modulosDt, TimeSpan.FromMinutes(10));

        // Réplica fiel: el legado envuelve OPT_IngresaLogueo en Try/Catch y no hace nada
        // con el error — a diferencia de Login.aspx.vb (Recobros), que no lo protege.
        try
        {
            await loginRepository.RegistrarLogueoAsync(resultadoUsuario.Cia, resultadoUsuario.UsuarioApp, log);
        }
        catch
        {
            // Intencional, igual que el legado.
        }

        return new ResultadoIntentoLogin
        {
            Estado = EstadoIntentoLogin.Exito,
            TokenPuente = token,
            UrlDestino = DeterminarDestino(resultadoUsuario, clave),
        };
    }

    private static string ClaveIntentos(string usuario) => $"login-intentos:{usuario.Trim().ToLowerInvariant()}";

    public async Task<bool> EnviarClaveTemporalAsync(string email)
    {
        var claveTemporal = GenerarClaveAleatoria(8);

        // Réplica fiel de GenerarCadena(8) en Login3.aspx.vb: el correo se envía aunque la
        // actualización en BD falle (el resultado de ActualizarPasswordAsync no se valida).
        await loginRepository.ActualizarPasswordAsync(email, claveTemporal);

        var cuerpo = PlantillaRecuperacionClave.Generar(claveTemporal);
        await emailSender.EnviarAsync(email, "Reestablecer contraseña", cuerpo);
        return true;
    }

    // Réplica fiel de GenerarCadena(8) en Login3.aspx.vb, incluyendo sus dos rarezas (idénticas
    // a las de Login.aspx.vb / Recobros, mismo código copiado/pegado en el legado): el bucle
    // "0 To numeroCaracteres" es inclusivo (genera 9 caracteres, no 8), y el último elemento
    // del arreglo de letras (índice 51) nunca se usa.
    private static string GenerarClaveAleatoria(int numeroCaracteres)
    {
        var letras = new char[52];
        var n = 0;
        for (var item = 65; item <= 90; item++)
        {
            letras[n] = (char)item;
            letras[n + 1] = char.ToLowerInvariant((char)item);
            n += 2;
        }

        var resultado = new StringBuilder();
        for (var i = 0; i <= numeroCaracteres; i++)
        {
            resultado.Append(letras[Random.Shared.Next(0, 51)]);
        }

        return resultado.ToString();
    }

    // Réplica fiel de la secuencia de "If"s (no ElseIf) en Login3.aspx.vb: cada condición
    // que aplica sobreescribe el destino anterior, así que el ORDEN importa. El chequeo de
    // vencimiento de clave vive en un segundo bloque Try separado que corre después y por
    // lo tanto tiene la última palabra, incluso sobre el rol 601.
    // ActContraseña.aspx del legado ya está migrada (/actualizar-contrasena); una ruta que empieza con "/" es propia de esta app.
    private static string DestinoActualizarClave(string mensaje) => "/actualizar-contrasena?msj=" + Uri.EscapeDataString(mensaje);

    private static string DeterminarDestino(ResultadoUsuario usuario, string claveIngresada)
    {
        var destino = "InicioMIPRES.aspx";

        if (usuario.ClaveTemporalReferencia is not null && usuario.ClaveTemporalReferencia == claveIngresada)
        {
            destino = DestinoActualizarClave("Debe actualizar la contraseña temporal");
        }

        if (usuario.RolParaModulos == "35")
        {
            destino = "ConsultaMipresWEB.aspx";
        }

        if (usuario.RolParaModulos == "60")
        {
            // Antes apuntaba al legado (JuntaProfesional.aspx) — migrado (2026-09-02, ver
            // Contingencia.Web/Components/Pages/Juntas.razor). Confirmado con el usuario de
            // pruebas real "cwjuntas" (rol 60, CIA 890330031): este es el único destino/
            // módulo que ve ese rol.
            destino = "/juntas";
        }

        if (usuario.RolParaModulos == "601")
        {
            destino = "/inicio";
        }

        // Enhancement explícito del usuario (2026-09-02, no existe en Login3.aspx.vb — ahí
        // estos roles caían al default "InicioMIPRES.aspx", otra app distinta): los roles
        // 27/29/30 son los que en el legado ven el ícono de "Gestion" en
        // JuntasFormatoCntCO.aspx (la rama del SP para "cualquier rol distinto de 601"). Se
        // les manda directo a Consultar formato (no a /inicio con las 2 tarjetas, porque
        // estos roles gestionan formatos ajenos, no cargan los suyos).
        if (usuario.RolParaModulos is "27" or "29" or "30")
        {
            destino = "/formatos/consultar";
        }

        var fechaHoy = DateTime.Now.ToString("yyyy-MM-dd");
        if (fechaHoy == usuario.FechaVencimientoClave)
        {
            destino = DestinoActualizarClave("Debe actualizar su contraseña por motivos de seguridad");
        }

        return destino;
    }
}
