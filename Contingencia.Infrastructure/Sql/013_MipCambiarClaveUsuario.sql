-- Cambio de contraseña del propio usuario (pantalla "Actualizar contraseña", migración de ActContraseña.aspx).
--
-- El legado (ActContraseña.aspx.vb) comparaba la clave actual contra Session("pwdini") y llamaba
-- sp_DQS_ActualizaPwd2(usuario, clave nueva) sin verificar nada en base de datos. Este procedimiento hace la
-- verificación en el servidor (clave actual correcta y usuario activo) y aplica los mismos efectos:
--   CLAVE = nueva, FECHA_ACT_PWD = GETDATE(), PWDTEMP = '' (la clave deja de ser temporal),
--   FechaPwd = vencimiento + 90 días y un registro 'Contraseña actualizada' en DQS_REC_NOTIFICACIONES.
-- Diferencia con sp_DQS_ActualizaPwd2: el legado suma 90 días a la FechaPwd anterior, así que si ya estaba vencida
-- (o era hoy) la nueva fecha podía seguir vencida y el usuario quedaba atrapado; aquí se suma a MAX(FechaPwd, hoy).
-- Una FechaPwd lejana (p. ej. '2099-12-31' de los usuarios creados con sp_MIP_CrearUsuarioFormatos) se conserva + 90.
--
-- Como el legado, no permite cambiar la clave de los usuarios compartidos CIFormatosC, CMFormatosC y cwformatos.
-- La política de la clave (12-20 caracteres, mayúscula, minúscula, número, símbolo, sin espacios, sin '_TEMPORAL',
-- distinta de la actual) la valida la aplicación; este procedimiento solo exige que no esté vacía y quepa en 50
-- caracteres (límite de @STRPWD VARCHAR(50) en sp_DQS_ConsultaUsuario). La clave sigue guardándose en texto plano
-- (decisión del usuario: no se pasa a hash por ahora).
--
-- Aplicado el 2026-10-14 en DQUALITYREC (172.25.2.15,1435) y DQUALITY (45.169.253.168).

SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MIP_CambiarClaveUsuario
    @Usuario      VARCHAR(150),
    @ClaveActual  VARCHAR(50),
    @ClaveNueva   VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));

    IF @Usuario = '' OR ISNULL(@ClaveActual, '') = '' OR ISNULL(@ClaveNueva, '') = ''
        BEGIN SELECT 0 AS RESULTADO, 'Usuario, clave actual y clave nueva son obligatorios.' AS MENSAJE; RETURN; END

    IF @Usuario IN ('CIFormatosC', 'CMFormatosC', 'cwformatos')
        BEGIN SELECT 0 AS RESULTADO, 'Este usuario es compartido; su clave la administra soporte.' AS MENSAJE; RETURN; END

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = @Usuario AND CLAVE = @ClaveActual AND ESTADO = 'Activo')
        BEGIN SELECT 0 AS RESULTADO, 'Contraseña actual o temporal incorrecta.' AS MENSAJE; RETURN; END

    DECLARE @Cia VARCHAR(150) = (SELECT TOP 1 CIA FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = @Usuario);

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.DQS_REC_NOTIFICACIONES (CIA, NOTIFICACION, USUARIO, FECHA)
        VALUES (@Cia, 'Contraseña actualizada', @Usuario, GETDATE());

        UPDATE dbo.DQS_REC_USUARIOS
        SET CLAVE = @ClaveNueva,
            FECHA_ACT_PWD = GETDATE(),
            PWDTEMP = '',
            FechaPwd = CASE
                WHEN TRY_CONVERT(date, FechaPwd) IS NULL THEN FechaPwd
                ELSE CONVERT(varchar(10),
                        DATEADD(day, 90, CASE WHEN TRY_CONVERT(date, FechaPwd) > CAST(GETDATE() AS date)
                                              THEN TRY_CONVERT(date, FechaPwd) ELSE CAST(GETDATE() AS date) END), 23)
            END
        WHERE USUARIO = @Usuario;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, 'No fue posible actualizar la contraseña. Intente nuevamente.' AS MENSAJE;
        RETURN;
    END CATCH

    SELECT 1 AS RESULTADO, 'Contraseña actualizada.' AS MENSAJE;
END
GO
