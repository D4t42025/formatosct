-- Permite crear valores nuevos en el catálogo paramétrico (dbo.DQS_MIP_CONT_PARAMETRICAS)
-- desde la propia aplicación — botón "Crear" al lado del desplegable de Causal en el paso 1
-- del wizard "Nueva Contingencia" (el usuario puede agregar una causal que no esté en la
-- semilla inicial, sin necesitar un script nuevo cada vez). El código se genera en C#
-- (ParametricasService.GenerarCodigo) a partir del texto que escribe el usuario.
--
-- Aplicado el 2026-09-30 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, para nivelarlo.

SET NOCOUNT ON;
GO

DROP PROCEDURE IF EXISTS dbo.sp_MIP_InsertaParametrica;
GO
CREATE PROCEDURE dbo.sp_MIP_InsertaParametrica
    @Grupo  NVARCHAR(50),
    @Codigo NVARCHAR(50),
    @Valor  NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONT_PARAMETRICAS WHERE GRUPO = @Grupo AND CODIGO = @Codigo)
    BEGIN
        SELECT 0 AS RESULTADO, 'Ya existe una causal con ese nombre.' AS MENSAJE;
        RETURN;
    END

    DECLARE @Orden INT = ISNULL((SELECT MAX(ORDEN) FROM dbo.DQS_MIP_CONT_PARAMETRICAS WHERE GRUPO = @Grupo), 0) + 1;

    INSERT INTO dbo.DQS_MIP_CONT_PARAMETRICAS (GRUPO, CODIGO, VALOR, ORDEN, ACTIVO)
    VALUES (@Grupo, @Codigo, @Valor, @Orden, 1);

    SELECT 1 AS RESULTADO, 'Valor creado correctamente.' AS MENSAJE;
END
GO

PRINT 'Migración MIP_PARAMETRICA_INSERTAR aplicada correctamente.';
GO
