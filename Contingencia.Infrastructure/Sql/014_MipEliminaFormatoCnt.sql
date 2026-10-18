-- "Eliminar" en Consultar formato de contingencia (ConsultarFormato.razor) hasta ahora solo
-- borraba el archivo adjunto (disco o Azure Blob Storage según la CIA, ver sección 26 de
-- MIGRACION-AZURE-JUNTAS.md) — el usuario pidió que también borre el registro de
-- DQS_MIP_FORMATOS. No existía ningún SP de borrado para esta tabla.
--
-- CIA + RADICADO es la clave natural (sin PK formal en la tabla), mismo criterio que usa el
-- WHERE de sp_MIP_ActualizaFormatoCnt.
--
-- El botón que llama esto queda restringido en la capa de aplicación (no acá) a la cuenta
-- interna de validación "Imperium"/CIA 890330031 — ver
-- ClaimsPrincipalExtensions.PuedeEliminarComoImperium.
--
-- Aplicado el 2026-10-18 en:
--   - FormatosCT (172.25.2.13) — server 13.
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
-- NO aplicado en producción (45.169.253.168): a pedido explícito del usuario, no se usa más
-- el servidor 168 para este trabajo.

SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MIP_EliminaFormatoCnt
    @CIA        VARCHAR(50),
    @Radicado   VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@CIA, '') = '' OR ISNULL(@Radicado, '') = ''
    BEGIN
        SELECT 0 AS RESULTADO, 'CIA y Radicado son obligatorios.' AS MENSAJE;
        RETURN;
    END

    DELETE FROM dbo.DQS_MIP_FORMATOS WHERE CIA = @CIA AND RADICADO = @Radicado;

    IF @@ROWCOUNT > 0
        SELECT 1 AS RESULTADO, 'Formato eliminado.' AS MENSAJE;
    ELSE
        SELECT 0 AS RESULTADO, 'No se encontró el formato para eliminar.' AS MENSAJE;
END
GO
