-- 02b_ProcedimientoFiltrosDelgado.sql - sp_DQS_ConsultaFiltros "delgado" para la base de produccion nueva (Formatos + Juntas).
--
-- En las bases actuales sp_DQS_ConsultaFiltros es un envoltorio (registra cada llamada en DQS_LOG_CONSULTAFILTROS) de
-- sp_DQS_ConsultaFiltros_Core, un "multiplexor" de mas de 13.000 lineas con un IF @INTVALIDA = N por cada pagina de TODO el
-- sistema (Recobros, cuentas medicas, historia clinica, transporte...) que depende de ~110 tablas ajenas a esta app. Formatos
-- y Juntas solo usan DOS ramas:
--
--   @INTVALIDA = 136  NIT y token del web service SISPRO/MIPRES de la CIA    (DQS_MIP_TOKEN, por CIA + Regimen + Detalle)
--                     -> MipresPrescripcionValidator y JuntaProfesionalApiClient
--   @INTVALIDA = 138  catalogo de cruce CODIGO/DESCRIPCION/CAMPO              (DQS_MIP_CRUZAR, sin filtro de CIA)
--                     -> JuntasService (acta PDF) y NuevaContingencia (ambito, forma, via, dosis, frecuencia)
--
-- Esta version trae esas dos consultas copiadas tal cual de sp_DQS_ConsultaFiltros_Core y conserva el nombre y la firma de 13
-- parametros (el codigo de la app no cambia). No registra llamadas (la tabla DQS_LOG_CONSULTAFILTROS no esta en este paquete).
-- Cualquier otro @INTVALIDA falla con un error explicito en vez de devolver vacio, para detectar de inmediato si una pagina
-- nueva necesita otra rama.
--
-- NO ejecutar en DQUALITYREC / DQUALITY (reemplazaria el multiplexor que usa el legado): es solo para la base nueva.

SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DQS_ConsultaFiltros
    @STRCIA          VARCHAR(100),
    @INTVALIDA       INT,
    @STRUSUARIO      VARCHAR(100),
    @STRTIPO         VARCHAR(MAX),
    @STRPAQUETE      VARCHAR(100),
    @STRFACTURA      VARCHAR(100),
    @STRSERVICIO     VARCHAR(100),
    @STROPCION       VARCHAR(500),
    @STRESTADO       VARCHAR(100),
    @STREGIMEN       VARCHAR(100),
    @STRSUCURSAL     VARCHAR(100),
    @STRTIPOSERVICIO VARCHAR(100),
    @STRENTE         VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- 136: credenciales del web service MIPRES de la CIA
    IF @INTVALIDA = 136
    BEGIN
        SELECT NIT, TOKEN
        FROM dbo.DQS_MIP_TOKEN
        WHERE Cia = @STRCIA AND Regimen = @STREGIMEN AND DETALLE = @STROPCION
        ORDER BY NIT ASC;
        RETURN;
    END

    -- 138: tabla de cruce para el acta de Junta y para los listados del wizard "Nueva contingencia"
    IF @INTVALIDA = 138
    BEGIN
        SELECT T1.CODIGO, T1.DESCRIPCION, T1.CAMPO
        FROM dbo.DQS_MIP_CRUZAR AS T1
        ORDER BY T1.CAMPO, T1.CODIGO;
        RETURN;
    END;

    THROW 50001, 'sp_DQS_ConsultaFiltros (version delgada): solo soporta @INTVALIDA 136 (token MIPRES) y 138 (cruce).', 1;
END
GO
