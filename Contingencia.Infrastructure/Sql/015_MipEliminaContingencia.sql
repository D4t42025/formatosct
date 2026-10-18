-- "Eliminar" en Consultar nueva contingencia (ConsultarNuevaContingencia.razor) no existía
-- — el usuario pidió el mismo botón "Eliminar" que ya tiene Consultar formato (borra BD +
-- archivos). No existía ningún SP de borrado para DQS_MIP_CONTINGENCIAS.
--
-- DQS_MIP_CONTINGENCIAS (padre, clave natural CIA+RADICADO) tiene una tabla hija
-- DQS_MIP_CONTINGENCIAS_TEC (una fila por tecnología, CIA+RADICADO+ORDEN, ver
-- 010_MipContingenciasMultiplesTecnologias.sql) — se borra primero la hija y luego el padre
-- dentro de una transacción (orden inverso al de sp_MIP_InsertaContingencia). Las rutas de
-- soportes/firma (SOPORTES_RUTAS_JSON/FIRMA_RUTA) ya vienen en la fila padre que la UI
-- consulta antes de llamar a este SP, así que el borrado físico de esos archivos lo hace la
-- capa de aplicación (IContingenciaArchivoService.Eliminar) ANTES de llamar este SP, no acá.
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

CREATE OR ALTER PROCEDURE dbo.sp_MIP_EliminaContingencia
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

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS WHERE CIA = @CIA AND RADICADO = @Radicado)
    BEGIN
        SELECT 0 AS RESULTADO, 'No se encontró la contingencia para eliminar.' AS MENSAJE;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.DQS_MIP_CONTINGENCIAS_TEC WHERE CIA = @CIA AND RADICADO = @Radicado;
        DELETE FROM dbo.DQS_MIP_CONTINGENCIAS WHERE CIA = @CIA AND RADICADO = @Radicado;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, 'No fue posible eliminar la contingencia. Intente nuevamente.' AS MENSAJE;
        RETURN;
    END CATCH

    SELECT 1 AS RESULTADO, 'Contingencia eliminada.' AS MENSAJE;
END
GO
