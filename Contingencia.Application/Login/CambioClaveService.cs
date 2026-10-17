using System.Text.RegularExpressions;
using Contingencia.Infrastructure.Login;

namespace Contingencia.Application.Login;

public class CambioClaveService(ILoginRepository loginRepository) : ICambioClaveService
{
    public async Task<ResultadoCambioClave> CambiarAsync(string usuario, string claveActual, string claveNueva, string confirmacion)
    {
        if (string.IsNullOrWhiteSpace(usuario))
            return new ResultadoCambioClave(false, "No se pudo identificar al usuario. Inicie sesión nuevamente.");
        if (string.IsNullOrEmpty(claveActual))
            return new ResultadoCambioClave(false, "Ingrese su contraseña actual o temporal.");
        if (string.IsNullOrEmpty(claveNueva))
            return new ResultadoCambioClave(false, "Ingrese la contraseña nueva.");
        if (claveNueva != confirmacion)
            return new ResultadoCambioClave(false, "Contraseña nueva y confirmación no coinciden");

        var error = ValidarPolitica(claveNueva, claveActual);
        if (error is not null) return new ResultadoCambioClave(false, error);

        var (exito, mensaje) = await loginRepository.CambiarClaveAsync(usuario.Trim(), claveActual, claveNueva);
        return new ResultadoCambioClave(exito, mensaje);
    }

    // Mismas reglas y mensajes, en el mismo orden, que btnactclave_ServerClick en ActContraseña.aspx.vb.
    public static string? ValidarPolitica(string nuevaClave, string claveActual)
    {
        if (nuevaClave == claveActual) return "La nueva contraseña no puede ser igual a la anterior.";
        if (nuevaClave.Length < 12 || nuevaClave.Length > 20) return "La contraseña debe tener entre 12 y 20 caracteres.";
        if (!Regex.IsMatch(nuevaClave, "[a-z]")) return "La contraseña debe contener al menos una letra minúscula.";
        if (!Regex.IsMatch(nuevaClave, "[A-Z]")) return "La contraseña debe contener al menos una letra mayúscula.";
        if (!Regex.IsMatch(nuevaClave, @"\d")) return "La contraseña debe contener al menos un número.";
        if (!Regex.IsMatch(nuevaClave, @"[\W_]")) return "La contraseña debe contener al menos un símbolo o carácter especial.";
        if (nuevaClave.Contains("_TEMPORAL")) return "La contraseña no puede contener \"_TEMPORAL\".";
        if (!Regex.IsMatch(nuevaClave, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_])[^\s]{12,20}$"))
            return "La contraseña no cumple con los requisitos de seguridad: 12-20 caracteres, mayúscula, minúscula, número y símbolo, sin espacios.";
        return null;
    }
}
