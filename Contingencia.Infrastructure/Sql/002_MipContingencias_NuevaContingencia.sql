-- Tabla y procedimientos nuevos para el wizard "Nueva Contingencia MIPRES" (11 pasos),
-- que registra contingencias con datos estructurados en vez de solo cargar un PDF ya
-- diligenciado (eso es lo que hace DQS_MIP_FORMATOS/sp_MIP_InsertaFormatoCnt, que solo
-- soporta 9 parámetros fijos y no tiene columnas para causal/paciente/tecnología/posología).
-- No existe ningún equivalente legado de este flujo (el prototipo analizado es una app
-- externa nueva, no una migración de una pantalla WebForms existente) — por eso esta
-- tabla no replica ninguna estructura ya existente en producción, es 100% nueva.
--
-- El algoritmo de radicado replica exactamente el de sp_MIP_InsertaFormatoCnt (confirmado
-- vía OBJECT_DEFINITION en DQUALITYREC): AAAAMMDD + COUNT(*) de la tabla, el contador
-- rellenado con ceros a la izquierda hasta 14 caracteres (el radicado final queda de 22
-- caracteres). Es un contador no atómico (mismo patrón/limitación ya aceptada en el resto
-- del proyecto para DQS_MIP_FORMATOS).
--
-- Aplicado el 2026-09-29 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, para nivelarlo (mismo
--     criterio ya usado en 001_MipProfesionales_FirmaEnActa.sql).

SET NOCOUNT ON;
GO

-- 1) Tabla nueva.
IF OBJECT_ID('dbo.DQS_MIP_CONTINGENCIAS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DQS_MIP_CONTINGENCIAS
    (
        ID                          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MIPCONT PRIMARY KEY,
        CIA                         NVARCHAR(50)   NOT NULL,
        RADICADO                    NVARCHAR(50)   NOT NULL,
        ESTADO                      NVARCHAR(50)   NOT NULL CONSTRAINT DF_MIPCONT_ESTADO DEFAULT ('Registrado'),
        USUARIO                     NVARCHAR(150)  NOT NULL,
        FECHACREACION               DATETIME       NOT NULL CONSTRAINT DF_MIPCONT_FECHA DEFAULT (GETDATE()),

        CAUSAL                      NVARCHAR(500)  NOT NULL,

        IPS_CODIGO                  NVARCHAR(100)  NULL,
        IPS_NOMBRE                  NVARCHAR(500)  NULL,

        PROFESIONAL_DOCUMENTO       NVARCHAR(100)  NOT NULL,
        PROFESIONAL_NOMBRE          NVARCHAR(500)  NULL,
        PROFESIONAL_PROFESION       NVARCHAR(500)  NULL,

        PACIENTE_TIPODOC            NVARCHAR(10)   NOT NULL,
        PACIENTE_NUMDOC             NVARCHAR(100)  NOT NULL,
        PACIENTE_NOMBRES            NVARCHAR(250)  NOT NULL,
        PACIENTE_APELLIDOS          NVARCHAR(250)  NOT NULL,
        PACIENTE_SEXO               NVARCHAR(20)   NULL,
        PACIENTE_FECHANACIMIENTO    DATE           NULL,
        PACIENTE_MUNICIPIO          NVARCHAR(200)  NULL,
        PACIENTE_TELEFONO           NVARCHAR(50)   NULL,
        PACIENTE_REGIMEN            NVARCHAR(100)  NULL,

        DIAGNOSTICO_CODIGO          NVARCHAR(20)   NOT NULL,
        DIAGNOSTICO_DESCRIPCION     NVARCHAR(500)  NULL,

        TIPOTECNOLOGIA              NVARCHAR(50)   NOT NULL,

        TEC_NOMBRE                  NVARCHAR(500)  NULL,
        TEC_CODIGO                  NVARCHAR(100)  NULL,
        TEC_FORMAFARMACEUTICA       NVARCHAR(200)  NULL,
        TEC_VIAADMINISTRACION       NVARCHAR(200)  NULL,
        TEC_DOSIS                   NVARCHAR(200)  NULL,

        POS_TIPOPRESTACION          NVARCHAR(20)   NULL,
        POS_CANTIDADFORMULADA       NVARCHAR(100)  NULL,
        POS_CADAFRECUENCIA          NVARCHAR(20)   NULL,
        POS_FRECUENCIA              NVARCHAR(20)   NULL,
        POS_DURACIONTRATAMIENTO     NVARCHAR(20)   NULL,
        POS_DURACIONUNIDAD          NVARCHAR(20)   NULL,
        POS_INDICACIONESPECIAL      NVARCHAR(50)   NULL,
        POS_CANTIDADTOTAL           NVARCHAR(100)  NULL,
        POS_INDICADORRECURRENCIA    NVARCHAR(5)    NULL,

        JUSTIFICACION_TECNICA       NVARCHAR(MAX)  NOT NULL,

        SOPORTES_RUTAS_JSON         NVARCHAR(MAX)  NULL,
        FIRMA_RUTA                  NVARCHAR(500)  NULL,
        FIRMAPENDIENTEFEA           BIT            NOT NULL CONSTRAINT DF_MIPCONT_FEAPEND DEFAULT (1)
    );

    CREATE INDEX IX_MIPCONT_CIA_RADICADO ON dbo.DQS_MIP_CONTINGENCIAS (CIA, RADICADO);
END
GO

-- 2) sp_MIP_InsertaContingencia — inserta todo excepto los dos campos de archivos
--    (se completan con sp_MIP_ActualizaContingenciaArchivos una vez el radicado ya
--    existe y los archivos se guardaron en disco con ese radicado en la ruta).
DROP PROCEDURE IF EXISTS dbo.sp_MIP_InsertaContingencia;
GO
CREATE PROCEDURE dbo.sp_MIP_InsertaContingencia
    @CIA                        NVARCHAR(50),
    @Usuario                    NVARCHAR(150),
    @Causal                     NVARCHAR(500),
    @IpsCodigo                  NVARCHAR(100)  = NULL,
    @IpsNombre                  NVARCHAR(500)  = NULL,
    @ProfesionalDocumento       NVARCHAR(100),
    @ProfesionalNombre          NVARCHAR(500)  = NULL,
    @ProfesionalProfesion       NVARCHAR(500)  = NULL,
    @PacienteTipoDoc            NVARCHAR(10),
    @PacienteNumDoc             NVARCHAR(100),
    @PacienteNombres            NVARCHAR(250),
    @PacienteApellidos          NVARCHAR(250),
    @PacienteSexo               NVARCHAR(20)   = NULL,
    @PacienteFechaNacimiento    DATE           = NULL,
    @PacienteMunicipio          NVARCHAR(200)  = NULL,
    @PacienteTelefono           NVARCHAR(50)   = NULL,
    @PacienteRegimen            NVARCHAR(100)  = NULL,
    @DiagnosticoCodigo          NVARCHAR(20),
    @DiagnosticoDescripcion     NVARCHAR(500)  = NULL,
    @TipoTecnologia             NVARCHAR(50),
    @TecNombre                  NVARCHAR(500)  = NULL,
    @TecCodigo                  NVARCHAR(100)  = NULL,
    @TecFormaFarmaceutica       NVARCHAR(200)  = NULL,
    @TecViaAdministracion       NVARCHAR(200)  = NULL,
    @TecDosis                   NVARCHAR(200)  = NULL,
    @PosTipoPrestacion          NVARCHAR(20)   = NULL,
    @PosCantidadFormulada       NVARCHAR(100)  = NULL,
    @PosCadaFrecuencia          NVARCHAR(20)   = NULL,
    @PosFrecuencia              NVARCHAR(20)   = NULL,
    @PosDuracionTratamiento     NVARCHAR(20)   = NULL,
    @PosDuracionUnidad          NVARCHAR(20)   = NULL,
    @PosIndicacionEspecial      NVARCHAR(50)   = NULL,
    @PosCantidadTotal           NVARCHAR(100)  = NULL,
    @PosIndicadorRecurrencia    NVARCHAR(5)    = NULL,
    @JustificacionTecnica       NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Anio VARCHAR(10) = CONVERT(VARCHAR(10), YEAR(GETDATE()));
    DECLARE @Mes  VARCHAR(10) = CONVERT(VARCHAR(10), MONTH(GETDATE()));
    DECLARE @Dia  VARCHAR(10) = CONVERT(VARCHAR(10), DAY(GETDATE()));
    DECLARE @Cuenta VARCHAR(100) = CONVERT(VARCHAR(100), (SELECT COUNT(*) FROM dbo.DQS_MIP_CONTINGENCIAS));

    IF LEN(@Mes) = 1 SET @Mes = '0' + @Mes;
    IF LEN(@Dia) = 1 SET @Dia = '0' + @Dia;

    SET @Cuenta = RIGHT('00000000000000' + @Cuenta, 14);

    DECLARE @Radicado VARCHAR(50) = @Anio + @Mes + @Dia + @Cuenta;

    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS
        (CIA, RADICADO, USUARIO, CAUSAL, IPS_CODIGO, IPS_NOMBRE,
         PROFESIONAL_DOCUMENTO, PROFESIONAL_NOMBRE, PROFESIONAL_PROFESION,
         PACIENTE_TIPODOC, PACIENTE_NUMDOC, PACIENTE_NOMBRES, PACIENTE_APELLIDOS,
         PACIENTE_SEXO, PACIENTE_FECHANACIMIENTO, PACIENTE_MUNICIPIO, PACIENTE_TELEFONO, PACIENTE_REGIMEN,
         DIAGNOSTICO_CODIGO, DIAGNOSTICO_DESCRIPCION,
         TIPOTECNOLOGIA,
         TEC_NOMBRE, TEC_CODIGO, TEC_FORMAFARMACEUTICA, TEC_VIAADMINISTRACION, TEC_DOSIS,
         POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CADAFRECUENCIA, POS_FRECUENCIA,
         POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL, POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA,
         JUSTIFICACION_TECNICA)
    VALUES
        (@CIA, @Radicado, @Usuario, @Causal, @IpsCodigo, @IpsNombre,
         @ProfesionalDocumento, @ProfesionalNombre, @ProfesionalProfesion,
         @PacienteTipoDoc, @PacienteNumDoc, @PacienteNombres, @PacienteApellidos,
         @PacienteSexo, @PacienteFechaNacimiento, @PacienteMunicipio, @PacienteTelefono, @PacienteRegimen,
         @DiagnosticoCodigo, @DiagnosticoDescripcion,
         @TipoTecnologia,
         @TecNombre, @TecCodigo, @TecFormaFarmaceutica, @TecViaAdministracion, @TecDosis,
         @PosTipoPrestacion, @PosCantidadFormulada, @PosCadaFrecuencia, @PosFrecuencia,
         @PosDuracionTratamiento, @PosDuracionUnidad, @PosIndicacionEspecial, @PosCantidadTotal, @PosIndicadorRecurrencia,
         @JustificacionTecnica);

    SELECT @Radicado AS RADICADO;
END
GO

-- 3) sp_MIP_ActualizaContingenciaArchivos — completa firma/soportes una vez guardados en disco.
DROP PROCEDURE IF EXISTS dbo.sp_MIP_ActualizaContingenciaArchivos;
GO
CREATE PROCEDURE dbo.sp_MIP_ActualizaContingenciaArchivos
    @CIA                NVARCHAR(50),
    @Radicado            NVARCHAR(50),
    @FirmaRuta           NVARCHAR(500)  = NULL,
    @SoportesRutasJson   NVARCHAR(MAX)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DQS_MIP_CONTINGENCIAS
       SET FIRMA_RUTA          = @FirmaRuta,
           SOPORTES_RUTAS_JSON = @SoportesRutasJson
     WHERE CIA = @CIA
       AND RADICADO = @Radicado;

    SELECT @@ROWCOUNT AS FILASACTUALIZADAS;
END
GO

-- 4) sp_MIP_ConsultaContingencias — sin precedente legado, filtros opcionales estilo
--    NULLIF/IS NULL (igual que sp_DQS_MIP_ProfesionalesConsultar), no el estilo "%" de
--    sp_MIP_ConsultaFormatosCNT (ese es fiel a un legado que no aplica acá).
DROP PROCEDURE IF EXISTS dbo.sp_MIP_ConsultaContingencias;
GO
CREATE PROCEDURE dbo.sp_MIP_ConsultaContingencias
    @CIA                    NVARCHAR(50),
    @Usuario                NVARCHAR(150) = NULL,
    @Radicado               NVARCHAR(50)  = NULL,
    @Causal                 NVARCHAR(500) = NULL,
    @IpsCodigo              NVARCHAR(100) = NULL,
    @ProfesionalDocumento   NVARCHAR(100) = NULL,
    @PacienteNumDoc         NVARCHAR(100) = NULL,
    @TipoTecnologia         NVARCHAR(50)  = NULL,
    @FechaInicio            DATE          = NULL,
    @FechaFin               DATE          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario              = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @Radicado             = NULLIF(LTRIM(RTRIM(ISNULL(@Radicado, ''))), '');
    SET @Causal               = NULLIF(LTRIM(RTRIM(ISNULL(@Causal, ''))), '');
    SET @IpsCodigo            = NULLIF(LTRIM(RTRIM(ISNULL(@IpsCodigo, ''))), '');
    SET @ProfesionalDocumento = NULLIF(LTRIM(RTRIM(ISNULL(@ProfesionalDocumento, ''))), '');
    SET @PacienteNumDoc       = NULLIF(LTRIM(RTRIM(ISNULL(@PacienteNumDoc, ''))), '');
    SET @TipoTecnologia       = NULLIF(LTRIM(RTRIM(ISNULL(@TipoTecnologia, ''))), '');

    SELECT  ID, CIA, RADICADO, ESTADO, USUARIO, FECHACREACION,
            CAUSAL,
            IPS_CODIGO, IPS_NOMBRE,
            PROFESIONAL_DOCUMENTO, PROFESIONAL_NOMBRE, PROFESIONAL_PROFESION,
            PACIENTE_TIPODOC, PACIENTE_NUMDOC, PACIENTE_NOMBRES, PACIENTE_APELLIDOS,
            PACIENTE_SEXO, PACIENTE_FECHANACIMIENTO, PACIENTE_MUNICIPIO, PACIENTE_TELEFONO, PACIENTE_REGIMEN,
            DIAGNOSTICO_CODIGO, DIAGNOSTICO_DESCRIPCION,
            TIPOTECNOLOGIA,
            TEC_NOMBRE, TEC_CODIGO, TEC_FORMAFARMACEUTICA, TEC_VIAADMINISTRACION, TEC_DOSIS,
            POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CADAFRECUENCIA, POS_FRECUENCIA,
            POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL, POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA,
            JUSTIFICACION_TECNICA,
            SOPORTES_RUTAS_JSON, FIRMA_RUTA, FIRMAPENDIENTEFEA
      FROM  dbo.DQS_MIP_CONTINGENCIAS
     WHERE  CIA = @CIA
       AND (@Usuario              IS NULL OR USUARIO = @Usuario)
       AND (@Radicado             IS NULL OR RADICADO = @Radicado)
       AND (@Causal               IS NULL OR CAUSAL LIKE '%' + @Causal + '%')
       AND (@IpsCodigo            IS NULL OR IPS_CODIGO = @IpsCodigo)
       AND (@ProfesionalDocumento IS NULL OR PROFESIONAL_DOCUMENTO = @ProfesionalDocumento)
       AND (@PacienteNumDoc       IS NULL OR PACIENTE_NUMDOC = @PacienteNumDoc)
       AND (@TipoTecnologia       IS NULL OR TIPOTECNOLOGIA = @TipoTecnologia)
       AND (@FechaInicio          IS NULL OR FECHACREACION >= @FechaInicio)
       AND (@FechaFin             IS NULL OR FECHACREACION < DATEADD(DAY, 1, @FechaFin))
     ORDER BY FECHACREACION DESC;
END
GO

PRINT 'Migración MIP_CONTINGENCIAS aplicada correctamente.';
GO
