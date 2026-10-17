-- Tabla paramétrica genérica nueva para el wizard "Nueva Contingencia" (primer uso: el
-- desplegable de "Causal", paso 1). Diseñada para servir también otros listados cerrados
-- que se necesiten más adelante en el mismo wizard (un GRUPO nuevo por listado, sin tocar
-- esquema otra vez) — mismo espíritu que DQS_MIP_CRUZAR (CODIGO/DESCRIPCION/CAMPO), pero
-- como tabla propia de este wizard, no compartida con Junta Profesional.
--
-- También separa, en DQS_MIP_CONTINGENCIAS, "Causal" (código del catálogo, antes texto
-- libre) de una "Descripción de la contingencia" nueva (texto libre) — el prototipo externo
-- las muestra como dos campos distintos en el paso 1. La tabla seguía con 0 filas reales al
-- momento de este script, así que estos cambios (incluido el cambio de significado de la
-- columna CAUSAL) son seguros.
--
-- Aplicado el 2026-09-29 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, para nivelarlo.

SET NOCOUNT ON;
GO

-- 1) Tabla paramétrica nueva.
IF OBJECT_ID('dbo.DQS_MIP_CONT_PARAMETRICAS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DQS_MIP_CONT_PARAMETRICAS
    (
        ID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MIPCONTPARAM PRIMARY KEY,
        GRUPO   NVARCHAR(50)  NOT NULL,
        CODIGO  NVARCHAR(50)  NOT NULL,
        VALOR   NVARCHAR(500) NOT NULL,
        ORDEN   INT           NOT NULL CONSTRAINT DF_MIPCONTPARAM_ORDEN DEFAULT (0),
        ACTIVO  BIT           NOT NULL CONSTRAINT DF_MIPCONTPARAM_ACTIVO DEFAULT (1)
    );

    CREATE INDEX IX_MIPCONTPARAM_GRUPO ON dbo.DQS_MIP_CONT_PARAMETRICAS (GRUPO, ORDEN);
END
GO

-- 2) Semilla inicial — grupo CAUSAL (6 valores confirmados por el usuario contra el
--    prototipo externo). Idempotente: no duplica si ya se sembró antes.
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONT_PARAMETRICAS WHERE GRUPO = 'CAUSAL')
BEGIN
    INSERT INTO dbo.DQS_MIP_CONT_PARAMETRICAS (GRUPO, CODIGO, VALOR, ORDEN) VALUES
        ('CAUSAL', 'DIFICULTAD_TECNICA',           N'Dificultad técnica',                1),
        ('CAUSAL', 'SIN_SERVICIO_ELECTRICO',       N'Sin servicio eléctrico',            2),
        ('CAUSAL', 'SIN_CONECTIVIDAD',             N'Sin conectividad',                  3),
        ('CAUSAL', 'INCONSISTENCIA_AFILIACION',    N'Inconsistencia de afiliación',      4),
        ('CAUSAL', 'INCONSISTENCIA_IDENTIFICACION',N'Inconsistencia de identificación',  5),
        ('CAUSAL', 'URGENCIA_MEDICA',              N'Urgencia médica',                   6);
END
GO

-- 3) sp_MIP_ConsultaParametricas — trae TODO el catálogo (o un grupo puntual si se pasa
--    @Grupo), mismo criterio de "traer todo y agrupar en memoria" ya usado para
--    DQS_MIP_CRUZAR vía sp_DQS_ConsultaFiltros/JuntasService.CargarCruzarAsync.
DROP PROCEDURE IF EXISTS dbo.sp_MIP_ConsultaParametricas;
GO
CREATE PROCEDURE dbo.sp_MIP_ConsultaParametricas
    @Grupo NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  GRUPO, CODIGO, VALOR, ORDEN
      FROM  dbo.DQS_MIP_CONT_PARAMETRICAS
     WHERE  ACTIVO = 1
       AND (@Grupo IS NULL OR GRUPO = @Grupo)
     ORDER BY GRUPO, ORDEN;
END
GO

-- 4) Columnas nuevas en DQS_MIP_CONTINGENCIAS (aditivo).
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'CAUSAL_NOMBRE') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD CAUSAL_NOMBRE NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'CAUSAL_DESCRIPCION') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD CAUSAL_DESCRIPCION NVARCHAR(500) NULL;
GO

-- 5) sp_MIP_InsertaContingencia — recreado con @CausalNombre/@CausalDescripcion justo
--    después de @Causal (que ahora guarda el CODIGO del catálogo, no texto libre).
DROP PROCEDURE IF EXISTS dbo.sp_MIP_InsertaContingencia;
GO
CREATE PROCEDURE dbo.sp_MIP_InsertaContingencia
    @CIA                        NVARCHAR(50),
    @Usuario                    NVARCHAR(150),
    @Causal                     NVARCHAR(500),
    @CausalNombre               NVARCHAR(500)  = NULL,
    @CausalDescripcion          NVARCHAR(500)  = NULL,
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
        (CIA, RADICADO, USUARIO, CAUSAL, CAUSAL_NOMBRE, CAUSAL_DESCRIPCION, IPS_CODIGO, IPS_NOMBRE,
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
        (@CIA, @Radicado, @Usuario, @Causal, @CausalNombre, @CausalDescripcion, @IpsCodigo, @IpsNombre,
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

-- 6) sp_MIP_ConsultaContingencias — recreado agregando CAUSAL_NOMBRE/CAUSAL_DESCRIPCION al SELECT.
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
            CAUSAL, CAUSAL_NOMBRE, CAUSAL_DESCRIPCION,
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
       AND (@Causal               IS NULL OR CAUSAL = @Causal)
       AND (@IpsCodigo            IS NULL OR IPS_CODIGO = @IpsCodigo)
       AND (@ProfesionalDocumento IS NULL OR PROFESIONAL_DOCUMENTO = @ProfesionalDocumento)
       AND (@PacienteNumDoc       IS NULL OR PACIENTE_NUMDOC = @PacienteNumDoc)
       AND (@TipoTecnologia       IS NULL OR TIPOTECNOLOGIA = @TipoTecnologia)
       AND (@FechaInicio          IS NULL OR FECHACREACION >= @FechaInicio)
       AND (@FechaFin             IS NULL OR FECHACREACION < DATEADD(DAY, 1, @FechaFin))
     ORDER BY FECHACREACION DESC;
END
GO

PRINT 'Migración MIP_CONTINGENCIAS_PARAMETRICAS aplicada correctamente.';
GO
