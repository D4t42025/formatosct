-- Dos cosas nuevas para el paso 4 (Paciente) del wizard, a pedido del usuario tras comparar
-- con el prototipo externo:
--
-- 1) El paciente ya NO se diligencia a mano: se busca por tipo+número de documento contra un
--    web service real de afiliados de EPS Sanitas (https://imperiumgitsanitas.net:51044/
--    tutelas/tutelasmipres, POST, Authorization: Bearer <token>). El token es un JWT con
--    fecha de expiración — se guarda en una tabla nueva (no en appsettings.json) para poder
--    rotarlo sin redeploy, mismo criterio ya usado para el token del WS de SISPRO/MIPRES
--    (DQS_MIP_TOKEN, ver JuntaProfesionalApiClient) — la app relee el token vigente antes de
--    cada llamada, no lo cachea en memoria al iniciar.
--
-- 2) Tres campos nuevos que diligencia el propio usuario en ese mismo paso (no vienen del
--    web service de afiliados): Fecha de atención, Hora, y Ámbito de atención (MIPRES) — este
--    último es el mismo catálogo DQS_MIP_CRUZAR (CAMPO='CodAmbAte') que ya usa Junta
--    Profesional para el acta en PDF (códigos reales confirmados: 11/12/21/22/30).
--
-- Aplicado el 2026-09-30 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, para nivelarlo.

SET NOCOUNT ON;
GO

-- 1) Tabla nueva para el token del WS de afiliados. Nunca se borra el histórico: siempre se
--    hace INSERT y se lee el más reciente — permite rotar el token sin perder rastro del
--    anterior, mismo espíritu que la auditoría del resto del proyecto.
IF OBJECT_ID('dbo.DQS_MIP_AFILIADOS_TOKEN', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DQS_MIP_AFILIADOS_TOKEN
    (
        ID              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MIPAFITOKEN PRIMARY KEY,
        TOKEN           NVARCHAR(MAX)  NOT NULL,
        FECHACREACION   DATETIME       NOT NULL CONSTRAINT DF_MIPAFITOKEN_FECHA DEFAULT (GETDATE()),
        USUARIO         NVARCHAR(150)  NULL
    );
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_MIP_ConsultaTokenAfiliados;
GO
CREATE PROCEDURE dbo.sp_MIP_ConsultaTokenAfiliados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) TOKEN
      FROM dbo.DQS_MIP_AFILIADOS_TOKEN
     ORDER BY FECHACREACION DESC, ID DESC;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_MIP_InsertaTokenAfiliados;
GO
CREATE PROCEDURE dbo.sp_MIP_InsertaTokenAfiliados
    @Token   NVARCHAR(MAX),
    @Usuario NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.DQS_MIP_AFILIADOS_TOKEN (TOKEN, USUARIO) VALUES (@Token, @Usuario);
    SELECT SCOPE_IDENTITY() AS ID;
END
GO

-- 2) Columnas nuevas en DQS_MIP_CONTINGENCIAS (aditivo).
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'PACIENTE_FECHAATENCION') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD PACIENTE_FECHAATENCION DATE NULL;
GO
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'PACIENTE_HORAATENCION') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD PACIENTE_HORAATENCION NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'PACIENTE_AMBITOATENCION_CODIGO') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD PACIENTE_AMBITOATENCION_CODIGO NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'PACIENTE_AMBITOATENCION_NOMBRE') IS NULL
    ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS ADD PACIENTE_AMBITOATENCION_NOMBRE NVARCHAR(200) NULL;
GO

-- 3) sp_MIP_InsertaContingencia — recreado agregando los 4 campos nuevos justo después de
--    @PacienteRegimen (siguen siendo del "paso Paciente" aunque técnicamente describen la
--    atención, no al paciente en sí — mismo agrupamiento que usa el wizard).
DROP PROCEDURE IF EXISTS dbo.sp_MIP_InsertaContingencia;
GO
CREATE PROCEDURE dbo.sp_MIP_InsertaContingencia
    @CIA                             NVARCHAR(50),
    @Usuario                         NVARCHAR(150),
    @Causal                          NVARCHAR(500),
    @CausalNombre                    NVARCHAR(500)  = NULL,
    @CausalDescripcion               NVARCHAR(500)  = NULL,
    @IpsCodigo                       NVARCHAR(100)  = NULL,
    @IpsNombre                       NVARCHAR(500)  = NULL,
    @ProfesionalDocumento            NVARCHAR(100),
    @ProfesionalNombre               NVARCHAR(500)  = NULL,
    @ProfesionalProfesion            NVARCHAR(500)  = NULL,
    @PacienteTipoDoc                 NVARCHAR(10),
    @PacienteNumDoc                  NVARCHAR(100),
    @PacienteNombres                 NVARCHAR(250),
    @PacienteApellidos               NVARCHAR(250),
    @PacienteSexo                    NVARCHAR(20)   = NULL,
    @PacienteFechaNacimiento         DATE           = NULL,
    @PacienteMunicipio               NVARCHAR(200)  = NULL,
    @PacienteTelefono                NVARCHAR(50)   = NULL,
    @PacienteRegimen                 NVARCHAR(100)  = NULL,
    @PacienteFechaAtencion           DATE           = NULL,
    @PacienteHoraAtencion            NVARCHAR(10)   = NULL,
    @PacienteAmbitoAtencionCodigo    NVARCHAR(20)   = NULL,
    @PacienteAmbitoAtencionNombre    NVARCHAR(200)  = NULL,
    @DiagnosticoCodigo               NVARCHAR(20),
    @DiagnosticoDescripcion          NVARCHAR(500)  = NULL,
    @TipoTecnologia                  NVARCHAR(50),
    @TecNombre                       NVARCHAR(500)  = NULL,
    @TecCodigo                       NVARCHAR(100)  = NULL,
    @TecFormaFarmaceutica            NVARCHAR(200)  = NULL,
    @TecViaAdministracion            NVARCHAR(200)  = NULL,
    @TecDosis                        NVARCHAR(200)  = NULL,
    @PosTipoPrestacion               NVARCHAR(20)   = NULL,
    @PosCantidadFormulada            NVARCHAR(100)  = NULL,
    @PosCadaFrecuencia               NVARCHAR(20)   = NULL,
    @PosFrecuencia                   NVARCHAR(20)   = NULL,
    @PosDuracionTratamiento          NVARCHAR(20)   = NULL,
    @PosDuracionUnidad               NVARCHAR(20)   = NULL,
    @PosIndicacionEspecial           NVARCHAR(50)   = NULL,
    @PosCantidadTotal                NVARCHAR(100)  = NULL,
    @PosIndicadorRecurrencia         NVARCHAR(5)    = NULL,
    @JustificacionTecnica            NVARCHAR(MAX)
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
         PACIENTE_FECHAATENCION, PACIENTE_HORAATENCION, PACIENTE_AMBITOATENCION_CODIGO, PACIENTE_AMBITOATENCION_NOMBRE,
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
         @PacienteFechaAtencion, @PacienteHoraAtencion, @PacienteAmbitoAtencionCodigo, @PacienteAmbitoAtencionNombre,
         @DiagnosticoCodigo, @DiagnosticoDescripcion,
         @TipoTecnologia,
         @TecNombre, @TecCodigo, @TecFormaFarmaceutica, @TecViaAdministracion, @TecDosis,
         @PosTipoPrestacion, @PosCantidadFormulada, @PosCadaFrecuencia, @PosFrecuencia,
         @PosDuracionTratamiento, @PosDuracionUnidad, @PosIndicacionEspecial, @PosCantidadTotal, @PosIndicadorRecurrencia,
         @JustificacionTecnica);

    SELECT @Radicado AS RADICADO;
END
GO

-- 4) sp_MIP_ConsultaContingencias — recreado agregando los 4 campos nuevos al SELECT.
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
            PACIENTE_FECHAATENCION, PACIENTE_HORAATENCION, PACIENTE_AMBITOATENCION_CODIGO, PACIENTE_AMBITOATENCION_NOMBRE,
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

PRINT 'Migración MIP_AFILIADOS_Y_ATENCION aplicada correctamente.';
GO
