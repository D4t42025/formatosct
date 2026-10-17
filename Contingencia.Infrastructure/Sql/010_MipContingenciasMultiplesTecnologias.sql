-- El usuario pidió que una contingencia pueda registrar MÁS DE UNA tecnología (ej. un
-- medicamento y un servicio): los pasos 6 (tipo), 7 (detalle), 8 (posología) y 9
-- (justificación técnica) se repiten por cada tecnología; causal, IPS, profesional, paciente,
-- diagnósticos, soportes y firma se capturan una sola vez.
--
-- Cambios:
--   1) Tabla hija nueva DQS_MIP_CONTINGENCIAS_TEC (una fila por tecnología, ORDEN 1..n).
--   2) Se copian a esa tabla los datos de tecnología/posología/justificación que ya existían
--      en DQS_MIP_CONTINGENCIAS (ORDEN=1) y SOLO si la copia quedó completa se eliminan esas
--      columnas de la tabla padre (para no tener dos fuentes de verdad).
--   3) sp_MIP_InsertaContingencia se recrea: recibe @TecnologiasJson (arreglo JSON) y
--      inserta cabecera + todas las tecnologías en una sola transacción (OPENJSON).
--   4) sp_MIP_ConsultaContingencias se recrea sin las columnas de tecnología (el filtro
--      @TipoTecnologia pasa a buscar en la tabla hija).
--   5) sp_MIP_ConsultaContingenciaTecnologias (nuevo): devuelve las tecnologías de una lista
--      de radicados (JSON), para armar el detalle/grilla.
--
-- Aplicado el 2026-10-02 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, para nivelarlo.

SET NOCOUNT ON;
GO

-- 1) Tabla hija -------------------------------------------------------------------------
IF OBJECT_ID('dbo.DQS_MIP_CONTINGENCIAS_TEC', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DQS_MIP_CONTINGENCIAS_TEC
    (
        ID                          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MIPCONTTEC PRIMARY KEY,
        CIA                         NVARCHAR(50)   NOT NULL,
        RADICADO                    NVARCHAR(50)   NOT NULL,
        ORDEN                       INT            NOT NULL,
        TIPOTECNOLOGIA              NVARCHAR(50)   NOT NULL,
        TEC_NOMBRE                  NVARCHAR(500)  NULL,
        TEC_CODIGO                  NVARCHAR(100)  NULL,
        TEC_PRINCIPIOACTIVO         NVARCHAR(1000) NULL,
        TEC_FORMAFARMACEUTICA       NVARCHAR(200)  NULL,
        TEC_VIAADMINISTRACION       NVARCHAR(200)  NULL,
        TEC_DOSIS                   NVARCHAR(200)  NULL,
        POS_TIPOPRESTACION          NVARCHAR(20)   NULL,
        POS_CANTIDADFORMULADA       NVARCHAR(100)  NULL,
        POS_CANTIDADFORMULADAUNIDAD NVARCHAR(50)   NULL,
        POS_CADAFRECUENCIA          NVARCHAR(20)   NULL,
        POS_FRECUENCIA              NVARCHAR(20)   NULL,
        POS_DURACIONTRATAMIENTO     NVARCHAR(20)   NULL,
        POS_DURACIONUNIDAD          NVARCHAR(20)   NULL,
        POS_INDICACIONESPECIAL      NVARCHAR(50)   NULL,
        POS_CANTIDADTOTAL           NVARCHAR(100)  NULL,
        POS_INDICADORRECURRENCIA    NVARCHAR(5)    NULL,
        POS_INDICACIONESADICIONALES NVARCHAR(500)  NULL,
        JUSTIFICACION_TECNICA       NVARCHAR(MAX)  NOT NULL
    );
    CREATE INDEX IX_MIPCONTTEC_CIA_RADICADO ON dbo.DQS_MIP_CONTINGENCIAS_TEC (CIA, RADICADO, ORDEN);
END
GO

-- 2) Copia de lo existente y retiro de las columnas del padre ---------------------------
IF COL_LENGTH('dbo.DQS_MIP_CONTINGENCIAS', 'TIPOTECNOLOGIA') IS NOT NULL
BEGIN
    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS_TEC
        (CIA, RADICADO, ORDEN, TIPOTECNOLOGIA, TEC_NOMBRE, TEC_CODIGO, TEC_PRINCIPIOACTIVO, TEC_FORMAFARMACEUTICA,
         TEC_VIAADMINISTRACION, TEC_DOSIS, POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CANTIDADFORMULADAUNIDAD,
         POS_CADAFRECUENCIA, POS_FRECUENCIA, POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL,
         POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA, POS_INDICACIONESADICIONALES, JUSTIFICACION_TECNICA)
    SELECT p.CIA, p.RADICADO, 1, p.TIPOTECNOLOGIA, p.TEC_NOMBRE, p.TEC_CODIGO, p.TEC_PRINCIPIOACTIVO, p.TEC_FORMAFARMACEUTICA,
           p.TEC_VIAADMINISTRACION, p.TEC_DOSIS, p.POS_TIPOPRESTACION, p.POS_CANTIDADFORMULADA, p.POS_CANTIDADFORMULADAUNIDAD,
           p.POS_CADAFRECUENCIA, p.POS_FRECUENCIA, p.POS_DURACIONTRATAMIENTO, p.POS_DURACIONUNIDAD, p.POS_INDICACIONESPECIAL,
           p.POS_CANTIDADTOTAL, p.POS_INDICADORRECURRENCIA, p.POS_INDICACIONESADICIONALES, p.JUSTIFICACION_TECNICA
      FROM dbo.DQS_MIP_CONTINGENCIAS p
     WHERE NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS_TEC t WHERE t.CIA = p.CIA AND t.RADICADO = p.RADICADO);

    IF (SELECT COUNT(*) FROM dbo.DQS_MIP_CONTINGENCIAS p
         WHERE NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS_TEC t WHERE t.CIA = p.CIA AND t.RADICADO = p.RADICADO)) = 0
    BEGIN
        ALTER TABLE dbo.DQS_MIP_CONTINGENCIAS DROP COLUMN
            TIPOTECNOLOGIA, TEC_NOMBRE, TEC_CODIGO, TEC_PRINCIPIOACTIVO, TEC_FORMAFARMACEUTICA, TEC_VIAADMINISTRACION, TEC_DOSIS,
            POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CANTIDADFORMULADAUNIDAD, POS_CADAFRECUENCIA, POS_FRECUENCIA,
            POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL, POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA,
            POS_INDICACIONESADICIONALES, JUSTIFICACION_TECNICA;
    END
    ELSE
        RAISERROR('La copia a DQS_MIP_CONTINGENCIAS_TEC quedó incompleta; no se eliminaron columnas.', 16, 1);
END
GO

-- 3) sp_MIP_InsertaContingencia ---------------------------------------------------------
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
    @DiagnosticoRel1Codigo           NVARCHAR(20)   = NULL,
    @DiagnosticoRel1Descripcion      NVARCHAR(500)  = NULL,
    @DiagnosticoRel2Codigo           NVARCHAR(20)   = NULL,
    @DiagnosticoRel2Descripcion      NVARCHAR(500)  = NULL,
    @TecnologiasJson                 NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Anio VARCHAR(10) = CONVERT(VARCHAR(10), YEAR(GETDATE()));
    DECLARE @Mes  VARCHAR(10) = CONVERT(VARCHAR(10), MONTH(GETDATE()));
    DECLARE @Dia  VARCHAR(10) = CONVERT(VARCHAR(10), DAY(GETDATE()));
    DECLARE @Cuenta VARCHAR(100) = CONVERT(VARCHAR(100), (SELECT COUNT(*) FROM dbo.DQS_MIP_CONTINGENCIAS));

    IF LEN(@Mes) = 1 SET @Mes = '0' + @Mes;
    IF LEN(@Dia) = 1 SET @Dia = '0' + @Dia;

    SET @Cuenta = RIGHT('00000000000000' + @Cuenta, 14);

    DECLARE @Radicado VARCHAR(50) = @Anio + @Mes + @Dia + @Cuenta;

    BEGIN TRANSACTION;

    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS
        (CIA, RADICADO, USUARIO, CAUSAL, CAUSAL_NOMBRE, CAUSAL_DESCRIPCION, IPS_CODIGO, IPS_NOMBRE,
         PROFESIONAL_DOCUMENTO, PROFESIONAL_NOMBRE, PROFESIONAL_PROFESION,
         PACIENTE_TIPODOC, PACIENTE_NUMDOC, PACIENTE_NOMBRES, PACIENTE_APELLIDOS,
         PACIENTE_SEXO, PACIENTE_FECHANACIMIENTO, PACIENTE_MUNICIPIO, PACIENTE_TELEFONO, PACIENTE_REGIMEN,
         PACIENTE_FECHAATENCION, PACIENTE_HORAATENCION, PACIENTE_AMBITOATENCION_CODIGO, PACIENTE_AMBITOATENCION_NOMBRE,
         DIAGNOSTICO_CODIGO, DIAGNOSTICO_DESCRIPCION,
         DIAGNOSTICO_REL1_CODIGO, DIAGNOSTICO_REL1_DESCRIPCION, DIAGNOSTICO_REL2_CODIGO, DIAGNOSTICO_REL2_DESCRIPCION)
    VALUES
        (@CIA, @Radicado, @Usuario, @Causal, @CausalNombre, @CausalDescripcion, @IpsCodigo, @IpsNombre,
         @ProfesionalDocumento, @ProfesionalNombre, @ProfesionalProfesion,
         @PacienteTipoDoc, @PacienteNumDoc, @PacienteNombres, @PacienteApellidos,
         @PacienteSexo, @PacienteFechaNacimiento, @PacienteMunicipio, @PacienteTelefono, @PacienteRegimen,
         @PacienteFechaAtencion, @PacienteHoraAtencion, @PacienteAmbitoAtencionCodigo, @PacienteAmbitoAtencionNombre,
         @DiagnosticoCodigo, @DiagnosticoDescripcion,
         @DiagnosticoRel1Codigo, @DiagnosticoRel1Descripcion, @DiagnosticoRel2Codigo, @DiagnosticoRel2Descripcion);

    -- Una fila por elemento del arreglo; ORDEN = posición en el arreglo (1..n).
    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS_TEC
        (CIA, RADICADO, ORDEN, TIPOTECNOLOGIA, TEC_NOMBRE, TEC_CODIGO, TEC_PRINCIPIOACTIVO, TEC_FORMAFARMACEUTICA,
         TEC_VIAADMINISTRACION, TEC_DOSIS, POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CANTIDADFORMULADAUNIDAD,
         POS_CADAFRECUENCIA, POS_FRECUENCIA, POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL,
         POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA, POS_INDICACIONESADICIONALES, JUSTIFICACION_TECNICA)
    SELECT @CIA, @Radicado, CONVERT(INT, j.[key]) + 1, t.TipoTecnologia, t.TecNombre, t.TecCodigo, t.TecPrincipioActivo,
           t.TecFormaFarmaceutica, t.TecViaAdministracion, t.TecDosis, t.PosTipoPrestacion, t.PosCantidadFormulada,
           t.PosCantidadFormuladaUnidad, t.PosCadaFrecuencia, t.PosFrecuencia, t.PosDuracionTratamiento, t.PosDuracionUnidad,
           t.PosIndicacionEspecial, t.PosCantidadTotal, t.PosIndicadorRecurrencia, t.PosIndicacionesAdicionales,
           t.JustificacionTecnica
      FROM OPENJSON(@TecnologiasJson) j
     CROSS APPLY OPENJSON(j.[value]) WITH
        (
            TipoTecnologia              NVARCHAR(50)   '$.TipoTecnologia',
            TecNombre                   NVARCHAR(500)  '$.TecNombre',
            TecCodigo                   NVARCHAR(100)  '$.TecCodigo',
            TecPrincipioActivo          NVARCHAR(1000) '$.TecPrincipioActivo',
            TecFormaFarmaceutica        NVARCHAR(200)  '$.TecFormaFarmaceutica',
            TecViaAdministracion        NVARCHAR(200)  '$.TecViaAdministracion',
            TecDosis                    NVARCHAR(200)  '$.TecDosis',
            PosTipoPrestacion           NVARCHAR(20)   '$.PosTipoPrestacion',
            PosCantidadFormulada        NVARCHAR(100)  '$.PosCantidadFormulada',
            PosCantidadFormuladaUnidad  NVARCHAR(50)   '$.PosCantidadFormuladaUnidad',
            PosCadaFrecuencia           NVARCHAR(20)   '$.PosCadaFrecuencia',
            PosFrecuencia               NVARCHAR(20)   '$.PosFrecuencia',
            PosDuracionTratamiento      NVARCHAR(20)   '$.PosDuracionTratamiento',
            PosDuracionUnidad           NVARCHAR(20)   '$.PosDuracionUnidad',
            PosIndicacionEspecial       NVARCHAR(50)   '$.PosIndicacionEspecial',
            PosCantidadTotal            NVARCHAR(100)  '$.PosCantidadTotal',
            PosIndicadorRecurrencia     NVARCHAR(5)    '$.PosIndicadorRecurrencia',
            PosIndicacionesAdicionales  NVARCHAR(500)  '$.PosIndicacionesAdicionales',
            JustificacionTecnica        NVARCHAR(MAX)  '$.JustificacionTecnica'
        ) t
     ORDER BY CONVERT(INT, j.[key]);

    COMMIT TRANSACTION;

    SELECT @Radicado AS RADICADO;
END
GO

-- 4) sp_MIP_ConsultaContingencias (solo cabecera) ---------------------------------------
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

    SELECT  c.ID, c.CIA, c.RADICADO, c.ESTADO, c.USUARIO, c.FECHACREACION,
            c.CAUSAL, c.CAUSAL_NOMBRE, c.CAUSAL_DESCRIPCION,
            c.IPS_CODIGO, c.IPS_NOMBRE,
            c.PROFESIONAL_DOCUMENTO, c.PROFESIONAL_NOMBRE, c.PROFESIONAL_PROFESION,
            c.PACIENTE_TIPODOC, c.PACIENTE_NUMDOC, c.PACIENTE_NOMBRES, c.PACIENTE_APELLIDOS,
            c.PACIENTE_SEXO, c.PACIENTE_FECHANACIMIENTO, c.PACIENTE_MUNICIPIO, c.PACIENTE_TELEFONO, c.PACIENTE_REGIMEN,
            c.PACIENTE_FECHAATENCION, c.PACIENTE_HORAATENCION, c.PACIENTE_AMBITOATENCION_CODIGO, c.PACIENTE_AMBITOATENCION_NOMBRE,
            c.DIAGNOSTICO_CODIGO, c.DIAGNOSTICO_DESCRIPCION,
            c.DIAGNOSTICO_REL1_CODIGO, c.DIAGNOSTICO_REL1_DESCRIPCION, c.DIAGNOSTICO_REL2_CODIGO, c.DIAGNOSTICO_REL2_DESCRIPCION,
            c.SOPORTES_RUTAS_JSON, c.FIRMA_RUTA, c.FIRMAPENDIENTEFEA
      FROM  dbo.DQS_MIP_CONTINGENCIAS c
     WHERE  c.CIA = @CIA
       AND (@Usuario              IS NULL OR c.USUARIO = @Usuario)
       AND (@Radicado             IS NULL OR c.RADICADO = @Radicado)
       AND (@Causal               IS NULL OR c.CAUSAL = @Causal)
       AND (@IpsCodigo            IS NULL OR c.IPS_CODIGO = @IpsCodigo)
       AND (@ProfesionalDocumento IS NULL OR c.PROFESIONAL_DOCUMENTO = @ProfesionalDocumento)
       AND (@PacienteNumDoc       IS NULL OR c.PACIENTE_NUMDOC = @PacienteNumDoc)
       AND (@TipoTecnologia       IS NULL OR EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS_TEC t
                                                      WHERE t.CIA = c.CIA AND t.RADICADO = c.RADICADO
                                                        AND t.TIPOTECNOLOGIA = @TipoTecnologia))
       AND (@FechaInicio          IS NULL OR c.FECHACREACION >= @FechaInicio)
       AND (@FechaFin             IS NULL OR c.FECHACREACION < DATEADD(DAY, 1, @FechaFin))
     ORDER BY c.FECHACREACION DESC;
END
GO

-- 5) sp_MIP_ConsultaContingenciaTecnologias ---------------------------------------------
DROP PROCEDURE IF EXISTS dbo.sp_MIP_ConsultaContingenciaTecnologias;
GO
CREATE PROCEDURE dbo.sp_MIP_ConsultaContingenciaTecnologias
    @CIA            NVARCHAR(50),
    @RadicadosJson  NVARCHAR(MAX)   -- arreglo JSON de radicados, ej. ["2026100200000000000000"]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  t.RADICADO, t.ORDEN, t.TIPOTECNOLOGIA,
            t.TEC_NOMBRE, t.TEC_CODIGO, t.TEC_PRINCIPIOACTIVO, t.TEC_FORMAFARMACEUTICA, t.TEC_VIAADMINISTRACION, t.TEC_DOSIS,
            t.POS_TIPOPRESTACION, t.POS_CANTIDADFORMULADA, t.POS_CANTIDADFORMULADAUNIDAD, t.POS_CADAFRECUENCIA, t.POS_FRECUENCIA,
            t.POS_DURACIONTRATAMIENTO, t.POS_DURACIONUNIDAD, t.POS_INDICACIONESPECIAL, t.POS_CANTIDADTOTAL, t.POS_INDICADORRECURRENCIA,
            t.POS_INDICACIONESADICIONALES, t.JUSTIFICACION_TECNICA
      FROM  dbo.DQS_MIP_CONTINGENCIAS_TEC t
     WHERE  t.CIA = @CIA
       AND  t.RADICADO IN (SELECT [value] FROM OPENJSON(@RadicadosJson))
     ORDER BY t.RADICADO, t.ORDEN;
END
GO

PRINT 'Migración MIP_CONTINGENCIAS_MULTIPLES_TECNOLOGIAS aplicada correctamente.';
GO
