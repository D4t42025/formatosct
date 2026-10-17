-- 01_Tablas.sql - esquema de las 24 tablas que usa Formatos + Juntas (Contingencia.Web)
-- Generado el 2026-10-14 desde DQUALITYREC (172.25.2.15,1435) por catalogo del sistema.
-- Idempotente: se puede ejecutar varias veces. Ejecutar con la base destino seleccionada (USE <base>).

SET NOCOUNT ON;
GO

-- ---------------------------------------------------------------- DQS_REC_USUARIOS
IF OBJECT_ID(N'dbo.DQS_REC_USUARIOS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_USUARIOS]
    (
    [ID_EMPRESA] varchar(50) NOT NULL,
    [CEDULA] varchar(50) NOT NULL,
    [NOMBRES] varchar(150) NULL,
    [APELLIDOS] varchar(150) NULL,
    [USUARIO] varchar(150) NOT NULL,
    [EMAIL] varchar(150) NULL,
    [ESTADO] varchar(10) NULL,
    [CIA] varchar(150) NOT NULL,
    [ROL] varchar(10) NOT NULL,
    [CLAVE] varchar(500) NULL,
    [IMAGEN] varchar(500) NULL,
    [FECHA_ACT_PWD] datetime NULL,
    [MARCA] int NULL,
    [FECHA_SESION] datetime NULL,
    [TOKEN] varchar(200) NULL,
    [Dirección] varchar(200) NULL,
    [Movil1] varchar(200) NULL,
    [Movil2] varchar(200) NULL,
    [Registro] varchar(200) NULL,
    [Planta] varchar(200) NULL,
    [Cargo] varchar(500) NULL,
    [PWDTEMP] varchar(50) NULL,
    [FechaPwd] varchar(50) NULL,
    CONSTRAINT [PK_DQS_REC_USUARIOS] PRIMARY KEY CLUSTERED ([USUARIO])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IDX_REC_USUARIOS' AND object_id = OBJECT_ID(N'dbo.DQS_REC_USUARIOS'))
    CREATE NONCLUSTERED INDEX [IDX_REC_USUARIOS] ON dbo.[DQS_REC_USUARIOS] ([CIA], [USUARIO], [ROL]);
GO

-- ---------------------------------------------------------------- DQS_REC_ROLES
IF OBJECT_ID(N'dbo.DQS_REC_ROLES', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_ROLES]
    (
    [ID_ROL] varchar(50) NOT NULL,
    [ROL] varchar(50) NULL,
    [DESCRIPCION] varchar(500) NULL,
    [CIA] varchar(50) NOT NULL,
    [APLICACION] varchar(50) NULL,
    CONSTRAINT [PK_DQS_REC_ROLES] PRIMARY KEY CLUSTERED ([ID_ROL], [CIA])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_ROLMODULO
IF OBJECT_ID(N'dbo.DQS_REC_ROLMODULO', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_ROLMODULO]
    (
    [IDROL] varchar(50) NOT NULL,
    [IDMODULO] varchar(50) NOT NULL,
    [CIA] varchar(50) NOT NULL,
    CONSTRAINT [PK_DQS_REC_ROLMODULO] PRIMARY KEY CLUSTERED ([IDROL], [IDMODULO], [CIA])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_MODULOS
IF OBJECT_ID(N'dbo.DQS_REC_MODULOS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_MODULOS]
    (
    [ID_MODULO] varchar(50) NOT NULL,
    [MODULO] varchar(50) NULL,
    [CIA] varchar(50) NOT NULL,
    CONSTRAINT [PK_DQS_REC_MODULOS] PRIMARY KEY CLUSTERED ([ID_MODULO], [CIA])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_CLIENTES
IF OBJECT_ID(N'dbo.DQS_REC_CLIENTES', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_CLIENTES]
    (
    [CIA] varchar(100) NOT NULL,
    [CLIENTE] varchar(100) NULL,
    [NIT] varchar(100) NOT NULL,
    [TIPO] varchar(100) NOT NULL,
    [PREFIJO] varchar(100) NULL,
    [CONSECUTIVO] int NULL,
    [COD_CLIENTE] varchar(100) NOT NULL,
    [DIRECCION] varchar(100) NOT NULL,
    [LOGO] varchar(500) NULL,
    [TEXTOCONTESTACION] varchar(5000) NULL,
    [TEXTONOTIFICACION] varchar(5000) NULL,
    [RUTAFORMATO] varchar(5000) NULL,
    [FIRMA] varchar(max) NULL,
    [TELEFONO] varchar(500) NULL,
    [EMAIL] varchar(100) NULL,
    [TOKEN] varchar(max) NULL,
    [FECHALICENCIA] date NULL,
    [CiudadCliente] varchar(100) NULL,
    [CiudadPrestacion] varchar(100) NULL,
    [NombreRepresentante] varchar(100) NULL,
    [Idrepresentante] varchar(100) NULL,
    [RegistroHab1] varchar(100) NULL,
    [RegistroHab2] varchar(100) NULL,
    [Mail2] varchar(100) NULL,
    [Nombrecontacto] varchar(100) NULL,
    [TelefonoContacto] varchar(100) NULL,
    [MailContacto] varchar(100) NULL,
    [EstadoContrato] varchar(100) NULL,
    [TipoContrato] varchar(100) NULL,
    [MarcaP] varchar(50) NULL,
    CONSTRAINT [PK_DQS_REC_CLIENTES_1] PRIMARY KEY CLUSTERED ([CIA], [TIPO])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_IPS
IF OBJECT_ID(N'dbo.DQS_REC_IPS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_IPS]
    (
    [TIPO DE identificacion ips] varchar(300) NULL,
    [NIT] varchar(100) NOT NULL,
    [Direccion ips] varchar(300) NULL,
    [Telefono] varchar(300) NULL,
    [NOMBRE_IPS] varchar(300) NULL,
    [COD_DANE] varchar(300) NULL,
    [CIUDAD] varchar(300) NOT NULL,
    [Código del prestador de servicios de salud] varchar(300) NULL,
    [telefono2] varchar(300) NULL,
    [estado_ips] varchar(300) NULL,
    [Registro] varchar(300) NULL,
    [Direccionamiento] varchar(300) NULL,
    [TIPO IPS] varchar(300) NULL,
    [Recaudo : Define si la entidad realiza cobro de copagos y/o Cuotas moderadoras] varchar(300) NULL,
    [ips_adscrito : Define si la IPS tiene contrato o no lo tiene] varchar(300) NULL,
    [TIPO SOCIEDAD] varchar(300) NULL,
    [CONTRIBUYENTE] varchar(300) NULL,
    [AUTORETENEDOR] varchar(300) NULL,
    [MAIL NOTIFICACIONES] varchar(300) NULL,
    [Contacto de la IPS: Define el funcionario a quien se le envia la correspondencia en la entidad] varchar(300) NULL,
    [Cargo Funcionario de la ips] varchar(300) NULL,
    [TIPO DE CONTRATO : Define el tipo de Facturacion que debe realizar la ips] varchar(300) NULL,
    [ESTADO CONTRATO ] varchar(300) NULL,
    [Cod_Hab] varchar(50) NULL,
    [Dv] varchar(50) NULL,
    [Logo] image NULL,
    CONSTRAINT [PK_DQS_REC_IPS2] PRIMARY KEY CLUSTERED ([NIT], [CIUDAD])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_NOTIFICACIONES
IF OBJECT_ID(N'dbo.DQS_REC_NOTIFICACIONES', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_NOTIFICACIONES]
    (
    [CIA] varchar(50) NULL,
    [NOTIFICACION] varchar(200) NULL,
    [USUARIO] varchar(50) NULL,
    [FECHA] datetime NULL
    );
END;
GO

-- ---------------------------------------------------------------- DQS_OPT_LOGUEO
IF OBJECT_ID(N'dbo.DQS_OPT_LOGUEO', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_OPT_LOGUEO]
    (
    [ID] bigint IDENTITY(1,1) NOT NULL,
    [CIA] nvarchar(100) NULL,
    [USUAPP] nvarchar(100) NULL,
    [IP] nvarchar(100) NULL,
    [FechaAct] nvarchar(200) NULL
    );
END;
GO

-- ---------------------------------------------------------------- ImperiumSesionPuente
IF OBJECT_ID(N'dbo.ImperiumSesionPuente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ImperiumSesionPuente]
    (
    [Token] uniqueidentifier NOT NULL,
    [Cia] nvarchar(50) NOT NULL,
    [Rol] nvarchar(50) NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [IdEmp] nvarchar(50) NOT NULL,
    [UsuApp] nvarchar(50) NOT NULL,
    [Logo] nvarchar(500) NULL,
    [CodigoEps] nvarchar(50) NULL,
    [Fecha] nvarchar(50) NULL,
    [LogoEmpresa] nvarchar(500) NULL,
    [TextoCarta] nvarchar(max) NULL,
    [TextoNotificacion] nvarchar(max) NULL,
    [NombreCliente] nvarchar(200) NULL,
    [RutaPdf] nvarchar(500) NULL,
    [Firma] nvarchar(500) NULL,
    [NitCliente] nvarchar(50) NULL,
    [DireccionCliente] nvarchar(200) NULL,
    [TelefonoCliente] nvarchar(50) NULL,
    [PasswordInicial] nvarchar(200) NULL,
    [ModActIni] nvarchar(5) NULL,
    [ModulosJson] nvarchar(max) NULL,
    [Log] nvarchar(100) NULL,
    [CreadoEn] datetime2(7) NOT NULL,
    [ExpiraEn] datetime2(7) NOT NULL,
    [IdRol] nvarchar(50) NULL,
    [TokenMipres] nvarchar(50) NULL,
    CONSTRAINT [PK__Imperium__1EB4F816C93D5C0E] PRIMARY KEY CLUSTERED ([Token])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ImperiumSesionPuente_ExpiraEn' AND object_id = OBJECT_ID(N'dbo.ImperiumSesionPuente'))
    CREATE NONCLUSTERED INDEX [IX_ImperiumSesionPuente_ExpiraEn] ON dbo.[ImperiumSesionPuente] ([ExpiraEn]);
GO

-- ---------------------------------------------------------------- DQS_MIP_FORMATOS
IF OBJECT_ID(N'dbo.DQS_MIP_FORMATOS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_FORMATOS]
    (
    [CIA] varchar(50) NULL,
    [TIPOID] varchar(50) NULL,
    [NUMID] varchar(50) NULL,
    [NIT] varchar(5000) NULL,
    [OBJ] varchar(5000) NULL,
    [RADICADO] varchar(50) NULL,
    [RUTA] varchar(5000) NULL,
    [FECHA] varchar(50) NULL,
    [EMAIL] varchar(5000) NULL,
    [FECHAFORMATO] varchar(5000) NULL,
    [USUARIO] varchar(5000) NULL,
    [NOPRESCRIPCION] varchar(5000) NULL,
    [OBSERVACIONES] varchar(5000) NULL,
    [ESTADO] varchar(5000) NULL
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_COD_HAB
IF OBJECT_ID(N'dbo.DQS_MIP_COD_HAB', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_COD_HAB]
    (
    [Tabla] varchar(800) NULL,
    [Codigo] varchar(800) NOT NULL,
    [Nombre] varchar(800) NULL,
    [Descripcion] varchar(800) NULL,
    [Habilitado] varchar(800) NULL,
    [Aplicacion] varchar(800) NULL,
    [IsStandardGEL] varchar(800) NULL,
    [IsStandardMSPS] varchar(800) NULL,
    [Extra_I_TipoIDPrestador] varchar(800) NULL,
    [Extra_II_NroIDPrestador] varchar(800) NULL,
    [Extra_III_CodigoPrestador] varchar(800) NULL,
    [Extra_IV_CodMpioSede] varchar(800) NULL,
    [Extra_V_NombreMpioSede] varchar(800) NULL,
    [Extra_VI_NombreDptoSede] varchar(800) NULL,
    [Extra_VII] varchar(800) NULL,
    [Extra_VIII] varchar(800) NULL,
    [Extra_IX] varchar(800) NULL,
    [Extra_X] varchar(800) NULL,
    [ValorRegistro] varchar(800) NULL,
    [UsuarioResponsable] varchar(800) NULL,
    [Fecha_Actualizacion] varchar(800) NULL,
    [IsPublicPrivate] varchar(800) NULL,
    CONSTRAINT [PK_DQS_MIP_COD_HAB] PRIMARY KEY CLUSTERED ([Codigo])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_TOKEN
IF OBJECT_ID(N'dbo.DQS_MIP_TOKEN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_TOKEN]
    (
    [Cia] varchar(50) NOT NULL,
    [NIT] varchar(50) NOT NULL,
    [Token] varchar(500) NOT NULL,
    [Detalle] varchar(100) NOT NULL,
    [Regimen] varchar(50) NOT NULL,
    CONSTRAINT [PK_DQS_MIP_TOKEN] PRIMARY KEY CLUSTERED ([Cia], [Token], [Detalle], [Regimen])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DQS_MIP_TOKEN_ConsultaTokens' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_TOKEN'))
    CREATE NONCLUSTERED INDEX [IX_DQS_MIP_TOKEN_ConsultaTokens] ON dbo.[DQS_MIP_TOKEN] ([Cia], [Regimen], [Detalle], [NIT]) INCLUDE ([Token]);
GO

-- ---------------------------------------------------------------- DQS_MIP_PROFESIONALES
IF OBJECT_ID(N'dbo.DQS_MIP_PROFESIONALES', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_PROFESIONALES]
    (
    [CIA] nvarchar(1000) NOT NULL,
    [TIPODOC] nvarchar(1000) NULL,
    [NoIDENTIFICACION] nvarchar(1000) NOT NULL,
    [APELLIDOS Y NOMBRES] nvarchar(1000) NOT NULL,
    [PROFESION] nvarchar(1000) NULL,
    [SEDE] nvarchar(1000) NULL,
    [SERVICIO] nvarchar(1000) NULL,
    [CORREOELECTRONICO] nvarchar(1000) NULL,
    [CELULAR] nvarchar(1000) NULL,
    [FActivación] nvarchar(1000) NULL,
    [Firma] nvarchar(1000) NOT NULL,
    [IDPROFESIONAL] int IDENTITY(1,1) NOT NULL,
    [ESTADO] bit NOT NULL CONSTRAINT [DF_MIPPROF_ESTADO] DEFAULT ((1)),
    [FECHACREACION] datetime NULL,
    [USUARIOCREACION] nvarchar(200) NULL,
    [FECHAACTUALIZACION] datetime NULL,
    [USUARIOACTUALIZACION] nvarchar(200) NULL,
    [FECHAINACTIVACION] datetime NULL,
    [USUARIOINACTIVACION] nvarchar(200) NULL,
    [APELLIDOS] nvarchar(500) NULL,
    [NOMBRES] nvarchar(500) NULL
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_PROFESIONALESACTA
IF OBJECT_ID(N'dbo.DQS_MIP_PROFESIONALESACTA', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_PROFESIONALESACTA]
    (
    [CIA] varchar(50) NOT NULL,
    [ACTA] varchar(50) NOT NULL,
    [NoIDENTIFICACION] varchar(50) NOT NULL,
    [ESTADO] varchar(50) NOT NULL,
    CONSTRAINT [PK_DQS_MIP_PROFESIONALESACTA] PRIMARY KEY CLUSTERED ([CIA], [ACTA], [NoIDENTIFICACION])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_JUNTASC
IF OBJECT_ID(N'dbo.DQS_MIP_JUNTASC', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_JUNTASC]
    (
    [CIA] varchar(50) NOT NULL,
    [MIPRES] varchar(50) NOT NULL,
    [TIPOT] varchar(50) NOT NULL,
    [CONT] varchar(50) NOT NULL,
    [FECHA] varchar(50) NULL,
    CONSTRAINT [PK_DQS_MIP_JUNTASC] PRIMARY KEY CLUSTERED ([CIA], [MIPRES], [TIPOT], [CONT])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_CRUZAR
IF OBJECT_ID(N'dbo.DQS_MIP_CRUZAR', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CRUZAR]
    (
    [CODIGO] varchar(500) NOT NULL,
    [DESCRIPCION] varchar(5000) NULL,
    [CAMPO] varchar(100) NOT NULL,
    [DAVIPOLA] varchar(100) NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ClusteredIndex-20230216-125826' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_CRUZAR'))
    CREATE CLUSTERED INDEX [ClusteredIndex-20230216-125826] ON dbo.[DQS_MIP_CRUZAR] ([CODIGO], [CAMPO]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_DQS_MIP_CRUZAR_CODIGO_CAMPO' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_CRUZAR'))
    CREATE NONCLUSTERED INDEX [idx_DQS_MIP_CRUZAR_CODIGO_CAMPO] ON dbo.[DQS_MIP_CRUZAR] ([CODIGO], [CAMPO]);
GO

-- ---------------------------------------------------------------- DQS_MIP_CONTINGENCIAS
IF OBJECT_ID(N'dbo.DQS_MIP_CONTINGENCIAS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CONTINGENCIAS]
    (
    [ID] int IDENTITY(1,1) NOT NULL,
    [CIA] nvarchar(50) NOT NULL,
    [RADICADO] nvarchar(50) NOT NULL,
    [ESTADO] nvarchar(50) NOT NULL CONSTRAINT [DF_MIPCONT_ESTADO] DEFAULT ('Registrado'),
    [USUARIO] nvarchar(150) NOT NULL,
    [FECHACREACION] datetime NOT NULL CONSTRAINT [DF_MIPCONT_FECHA] DEFAULT (getdate()),
    [CAUSAL] nvarchar(500) NOT NULL,
    [IPS_CODIGO] nvarchar(100) NULL,
    [IPS_NOMBRE] nvarchar(500) NULL,
    [PROFESIONAL_DOCUMENTO] nvarchar(100) NOT NULL,
    [PROFESIONAL_NOMBRE] nvarchar(500) NULL,
    [PROFESIONAL_PROFESION] nvarchar(500) NULL,
    [PACIENTE_TIPODOC] nvarchar(10) NOT NULL,
    [PACIENTE_NUMDOC] nvarchar(100) NOT NULL,
    [PACIENTE_NOMBRES] nvarchar(250) NOT NULL,
    [PACIENTE_APELLIDOS] nvarchar(250) NOT NULL,
    [PACIENTE_SEXO] nvarchar(20) NULL,
    [PACIENTE_FECHANACIMIENTO] date NULL,
    [PACIENTE_MUNICIPIO] nvarchar(200) NULL,
    [PACIENTE_TELEFONO] nvarchar(50) NULL,
    [PACIENTE_REGIMEN] nvarchar(100) NULL,
    [DIAGNOSTICO_CODIGO] nvarchar(20) NOT NULL,
    [DIAGNOSTICO_DESCRIPCION] nvarchar(500) NULL,
    [SOPORTES_RUTAS_JSON] nvarchar(max) NULL,
    [FIRMA_RUTA] nvarchar(500) NULL,
    [FIRMAPENDIENTEFEA] bit NOT NULL CONSTRAINT [DF_MIPCONT_FEAPEND] DEFAULT ((1)),
    [CAUSAL_NOMBRE] nvarchar(500) NULL,
    [CAUSAL_DESCRIPCION] nvarchar(500) NULL,
    [PACIENTE_FECHAATENCION] date NULL,
    [PACIENTE_HORAATENCION] nvarchar(10) NULL,
    [PACIENTE_AMBITOATENCION_CODIGO] nvarchar(20) NULL,
    [PACIENTE_AMBITOATENCION_NOMBRE] nvarchar(200) NULL,
    [DIAGNOSTICO_REL1_CODIGO] nvarchar(20) NULL,
    [DIAGNOSTICO_REL1_DESCRIPCION] nvarchar(500) NULL,
    [DIAGNOSTICO_REL2_CODIGO] nvarchar(20) NULL,
    [DIAGNOSTICO_REL2_DESCRIPCION] nvarchar(500) NULL,
    CONSTRAINT [PK_MIPCONT] PRIMARY KEY CLUSTERED ([ID])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MIPCONT_CIA_RADICADO' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_CONTINGENCIAS'))
    CREATE NONCLUSTERED INDEX [IX_MIPCONT_CIA_RADICADO] ON dbo.[DQS_MIP_CONTINGENCIAS] ([CIA], [RADICADO]);
GO

-- ---------------------------------------------------------------- DQS_MIP_CONTINGENCIAS_TEC
IF OBJECT_ID(N'dbo.DQS_MIP_CONTINGENCIAS_TEC', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CONTINGENCIAS_TEC]
    (
    [ID] int IDENTITY(1,1) NOT NULL,
    [CIA] nvarchar(50) NOT NULL,
    [RADICADO] nvarchar(50) NOT NULL,
    [ORDEN] int NOT NULL,
    [TIPOTECNOLOGIA] nvarchar(50) NOT NULL,
    [TEC_NOMBRE] nvarchar(500) NULL,
    [TEC_CODIGO] nvarchar(100) NULL,
    [TEC_PRINCIPIOACTIVO] nvarchar(1000) NULL,
    [TEC_FORMAFARMACEUTICA] nvarchar(200) NULL,
    [TEC_VIAADMINISTRACION] nvarchar(200) NULL,
    [TEC_DOSIS] nvarchar(200) NULL,
    [POS_TIPOPRESTACION] nvarchar(20) NULL,
    [POS_CANTIDADFORMULADA] nvarchar(100) NULL,
    [POS_CANTIDADFORMULADAUNIDAD] nvarchar(50) NULL,
    [POS_CADAFRECUENCIA] nvarchar(20) NULL,
    [POS_FRECUENCIA] nvarchar(20) NULL,
    [POS_DURACIONTRATAMIENTO] nvarchar(20) NULL,
    [POS_DURACIONUNIDAD] nvarchar(20) NULL,
    [POS_INDICACIONESPECIAL] nvarchar(50) NULL,
    [POS_CANTIDADTOTAL] nvarchar(100) NULL,
    [POS_INDICADORRECURRENCIA] nvarchar(5) NULL,
    [POS_INDICACIONESADICIONALES] nvarchar(500) NULL,
    [JUSTIFICACION_TECNICA] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_MIPCONTTEC] PRIMARY KEY CLUSTERED ([ID])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MIPCONTTEC_CIA_RADICADO' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_CONTINGENCIAS_TEC'))
    CREATE NONCLUSTERED INDEX [IX_MIPCONTTEC_CIA_RADICADO] ON dbo.[DQS_MIP_CONTINGENCIAS_TEC] ([CIA], [RADICADO], [ORDEN]);
GO

-- ---------------------------------------------------------------- DQS_MIP_CONT_PARAMETRICAS
IF OBJECT_ID(N'dbo.DQS_MIP_CONT_PARAMETRICAS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CONT_PARAMETRICAS]
    (
    [ID] int IDENTITY(1,1) NOT NULL,
    [GRUPO] nvarchar(50) NOT NULL,
    [CODIGO] nvarchar(50) NOT NULL,
    [VALOR] nvarchar(500) NOT NULL,
    [ORDEN] int NOT NULL CONSTRAINT [DF_MIPCONTPARAM_ORDEN] DEFAULT ((0)),
    [ACTIVO] bit NOT NULL CONSTRAINT [DF_MIPCONTPARAM_ACTIVO] DEFAULT ((1)),
    CONSTRAINT [PK_MIPCONTPARAM] PRIMARY KEY CLUSTERED ([ID])
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MIPCONTPARAM_GRUPO' AND object_id = OBJECT_ID(N'dbo.DQS_MIP_CONT_PARAMETRICAS'))
    CREATE NONCLUSTERED INDEX [IX_MIPCONTPARAM_GRUPO] ON dbo.[DQS_MIP_CONT_PARAMETRICAS] ([GRUPO], [ORDEN]);
GO

-- ---------------------------------------------------------------- DQS_MIP_AFILIADOS_TOKEN
IF OBJECT_ID(N'dbo.DQS_MIP_AFILIADOS_TOKEN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_AFILIADOS_TOKEN]
    (
    [ID] int IDENTITY(1,1) NOT NULL,
    [TOKEN] nvarchar(max) NOT NULL,
    [FECHACREACION] datetime NOT NULL CONSTRAINT [DF_MIPAFITOKEN_FECHA] DEFAULT (getdate()),
    [USUARIO] nvarchar(150) NULL,
    CONSTRAINT [PK_MIPAFITOKEN] PRIMARY KEY CLUSTERED ([ID])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_CIA_PARAMETROS
IF OBJECT_ID(N'dbo.DQS_MIP_CIA_PARAMETROS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CIA_PARAMETROS]
    (
    [CIA] nvarchar(50) NOT NULL,
    [CLAVE] nvarchar(50) NOT NULL,
    [VALOR] nvarchar(500) NOT NULL,
    [DESCRIPCION] nvarchar(500) NULL,
    [ACTIVO] bit NOT NULL CONSTRAINT [DF_MIPCIAPARAM_ACTIVO] DEFAULT ((1)),
    [FECHAACTUALIZACION] datetime NOT NULL CONSTRAINT [DF_MIPCIAPARAM_FECHA] DEFAULT (getdate()),
    CONSTRAINT [PK_MIPCIAPARAM] PRIMARY KEY CLUSTERED ([CIA], [CLAVE])
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_DCI
IF OBJECT_ID(N'dbo.DQS_MIP_DCI', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_DCI]
    (
    [ID DCI Concentracion] varchar(50) NULL,
    [Descripcion DCI Concentracion] varchar(2000) NULL,
    [Tipo Medicamento] varchar(50) NULL,
    [Habilitado MIPRES] varchar(50) NULL,
    [VERSIÓN MIPRES] varchar(50) NULL,
    [FECHA] varchar(50) NULL
    );
END;
GO

-- ---------------------------------------------------------------- DQS_MIP_CODIGOS
IF OBJECT_ID(N'dbo.DQS_MIP_CODIGOS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_MIP_CODIGOS]
    (
    [CIA] varchar(50) NOT NULL,
    [Tipo] varchar(50) NULL,
    [TipoServicio] varchar(50) NOT NULL,
    [Codigo] varchar(50) NOT NULL,
    [Descripción] varchar(1000) NULL,
    [FechaIni] varchar(100) NULL,
    [FechaFin] varchar(100) NULL,
    [CodigoLargo] varchar(50) NOT NULL,
    [Grupo] varchar(50) NULL,
    [Fomra] varchar(50) NULL,
    [Presentacion] varchar(50) NULL,
    [Unidades] varchar(50) NULL
    );
END;
GO

-- ---------------------------------------------------------------- DQS_REC_CARGUECIE10
IF OBJECT_ID(N'dbo.DQS_REC_CARGUECIE10', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DQS_REC_CARGUECIE10]
    (
    [Código] nvarchar(255) NULL,
    [Descripción] nvarchar(1000) NULL,
    [MARCA] varchar(100) NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_DQS_REC_CARGUECIE10_Codigo' AND object_id = OBJECT_ID(N'dbo.DQS_REC_CARGUECIE10'))
    CREATE NONCLUSTERED INDEX [idx_DQS_REC_CARGUECIE10_Codigo] ON dbo.[DQS_REC_CARGUECIE10] ([Código]);
GO

