-- Migración aditiva del módulo MIP_PROFESIONALES y de la columna Firma en el acta.
-- Fuente de verdad: 172.25.2.13/DQUALITY (servidor real donde vive el legado
-- MIP_PROFESIONALES.aspx hoy, agregado ahí el 2026-09-25 — ver BASE_DE_DATOS.md,
-- sección "Página 10"). Este script reconstruye lo mismo, sacado de OBJECT_DEFINITION
-- real de ese servidor.
--
-- Aplicado el 2026-09-29 en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168) — a pedido explícito del usuario, aunque no es el
--     servidor donde corre hoy el legado con esta funcionalidad (ver nota en
--     BASE_DE_DATOS.md sobre qué servidor es cuál).
-- En ambos casos DQS_MIP_PROFESIONALES tenía 0 filas antes de aplicar este script, así
-- que el ALTER de columnas y el IDENTITY nuevo fueron seguros (sin datos que migrar).
-- Si se vuelve a aplicar sobre un servidor con filas existentes, revisar antes que el
-- ALTER COLUMN a NVARCHAR(1000)/2000 y el ADD de IDPROFESIONAL como IDENTITY no
-- choquen con datos o límites ya existentes.

SET NOCOUNT ON;
GO

-- 1) Ensanchar columnas existentes de varchar(50)/varchar(100) a nvarchar, igual que en producción.
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN CIA NVARCHAR(1000) NOT NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN TIPODOC NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN NoIDENTIFICACION NVARCHAR(1000) NOT NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN [APELLIDOS Y NOMBRES] NVARCHAR(1000) NOT NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN PROFESION NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN SEDE NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN SERVICIO NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN CORREOELECTRONICO NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN CELULAR NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN FActivación NVARCHAR(1000) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ALTER COLUMN Firma NVARCHAR(1000) NOT NULL;
GO

-- 2) Columnas nuevas del CRUD (idénticas a producción).
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD IDPROFESIONAL INT IDENTITY(1,1) NOT NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD ESTADO BIT NOT NULL CONSTRAINT DF_MIPPROF_ESTADO DEFAULT (1);
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD FECHACREACION DATETIME NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD USUARIOCREACION NVARCHAR(200) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD FECHAACTUALIZACION DATETIME NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD USUARIOACTUALIZACION NVARCHAR(200) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD FECHAINACTIVACION DATETIME NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD USUARIOINACTIVACION NVARCHAR(200) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD APELLIDOS NVARCHAR(500) NULL;
ALTER TABLE dbo.DQS_MIP_PROFESIONALES ADD NOMBRES NVARCHAR(500) NULL;
GO

-- 3) Procedimiento del acta: agregar la columna Firma (idéntico a producción).
DROP PROCEDURE IF EXISTS dbo.sp_DQS_ConsultaProfesionalAñadido;
GO
CREATE PROCEDURE dbo.sp_DQS_ConsultaProfesionalAñadido
    @CIA  VARCHAR(50),
    @ACTA VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  t1.[ACTA],
            t1.[NoIDENTIFICACION],
            t2.[APELLIDOS Y NOMBRES],
            t2.[PROFESION],
            t2.[Firma]
      FROM  [DQS_MIP_PROFESIONALESACTA] AS t1
      INNER JOIN [DQS_MIP_PROFESIONALES] AS t2
              ON t2.[CIA] = t1.[CIA]
             AND t2.[NoIDENTIFICACION] = t1.[NoIDENTIFICACION]
     WHERE  t1.[CIA] = @CIA
       AND  t1.[ACTA] = @ACTA;
END
GO

-- 4) Los 9 procedimientos nuevos del módulo MIP_PROFESIONALES (idénticos a producción).
DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesConsultar;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesConsultar
    @CIA        NVARCHAR(500),
    @Busqueda   NVARCHAR(500) = NULL,
    @Profesion  NVARCHAR(500) = NULL,
    @Sede       NVARCHAR(500) = NULL,
    @Estado     NVARCHAR(10)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Busqueda  = NULLIF(LTRIM(RTRIM(ISNULL(@Busqueda,  ''))), '');
    SET @Profesion = NULLIF(LTRIM(RTRIM(ISNULL(@Profesion, ''))), '');
    SET @Sede      = NULLIF(LTRIM(RTRIM(ISNULL(@Sede,      ''))), '');
    SET @Estado    = NULLIF(LTRIM(RTRIM(ISNULL(@Estado,    ''))), '');

    SELECT  T1.[IDPROFESIONAL],
            T1.[CIA],
            ISNULL(T1.[TIPODOC], '')                        AS [TIPODOC],
            T1.[NoIDENTIFICACION],
            T1.[APELLIDOS Y NOMBRES]                        AS [NOMBRECOMPLETO],
            ISNULL(T1.[APELLIDOS], '')                      AS [APELLIDOS],
            ISNULL(T1.[NOMBRES], '')                        AS [NOMBRES],
            ISNULL(T1.[PROFESION], '')                      AS [PROFESION],
            ISNULL(T1.[SEDE], '')                           AS [SEDE],
            ISNULL(T1.[SERVICIO], '')                       AS [SERVICIO],
            ISNULL(T1.[CORREOELECTRONICO], '')              AS [CORREOELECTRONICO],
            ISNULL(T1.[CELULAR], '')                        AS [CELULAR],
            ISNULL(T1.[FActivación], '')                    AS [FACTIVACION],
            ISNULL(T1.[Firma], '')                          AS [FIRMA],
            T1.[ESTADO],
            CASE WHEN T1.[ESTADO] = 1 THEN 'Activo' ELSE 'Inactivo' END AS [ESTADODESC],
            T1.[FECHACREACION],
            ISNULL(T1.[USUARIOCREACION], '')                AS [USUARIOCREACION],
            T1.[FECHAACTUALIZACION],
            ISNULL(T1.[USUARIOACTUALIZACION], '')           AS [USUARIOACTUALIZACION],
            T1.[FECHAINACTIVACION],
            ISNULL(T1.[USUARIOINACTIVACION], '')            AS [USUARIOINACTIVACION],
            ISNULL((SELECT TOP 1 T2.[USUARIO]
                      FROM [dbo].[DQS_REC_USUARIOS] AS T2
                     WHERE T2.[CEDULA] = T1.[NoIDENTIFICACION] AND T2.[CIA] = T1.[CIA]
                     ORDER BY T2.[USUARIO]), '')            AS [USUARIOAPP]
      FROM  [dbo].[DQS_MIP_PROFESIONALES] AS T1
     WHERE  T1.[CIA] = @CIA
       AND (@Estado    IS NULL OR T1.[ESTADO] = CONVERT(BIT, @Estado))
       AND (@Profesion IS NULL OR T1.[PROFESION] = @Profesion)
       AND (@Sede      IS NULL OR T1.[SEDE] = @Sede)
       AND (@Busqueda  IS NULL
            OR T1.[NoIDENTIFICACION]             LIKE '%' + @Busqueda + '%'
            OR T1.[APELLIDOS Y NOMBRES]          LIKE '%' + @Busqueda + '%'
            OR ISNULL(T1.[CORREOELECTRONICO],'') LIKE '%' + @Busqueda + '%'
            OR ISNULL(T1.[CELULAR],'')           LIKE '%' + @Busqueda + '%')
     ORDER BY T1.[ESTADO] DESC, T1.[APELLIDOS Y NOMBRES] ASC;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesObtener;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesObtener
    @CIA            NVARCHAR(500),
    @IdProfesional  INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  T1.[IDPROFESIONAL],
            T1.[CIA],
            ISNULL(T1.[TIPODOC], '')               AS [TIPODOC],
            T1.[NoIDENTIFICACION],
            T1.[APELLIDOS Y NOMBRES]               AS [NOMBRECOMPLETO],
            ISNULL(T1.[APELLIDOS], '')             AS [APELLIDOS],
            ISNULL(T1.[NOMBRES], '')               AS [NOMBRES],
            ISNULL(T1.[PROFESION], '')             AS [PROFESION],
            ISNULL(T1.[SEDE], '')                  AS [SEDE],
            ISNULL(T1.[SERVICIO], '')              AS [SERVICIO],
            ISNULL(T1.[CORREOELECTRONICO], '')     AS [CORREOELECTRONICO],
            ISNULL(T1.[CELULAR], '')               AS [CELULAR],
            ISNULL(T1.[FActivación], '')           AS [FACTIVACION],
            ISNULL(T1.[Firma], '')                 AS [FIRMA],
            T1.[ESTADO],
            CASE WHEN T1.[ESTADO] = 1 THEN 'Activo' ELSE 'Inactivo' END AS [ESTADODESC],
            T1.[FECHACREACION],
            ISNULL(T1.[USUARIOCREACION], '')       AS [USUARIOCREACION],
            T1.[FECHAACTUALIZACION],
            ISNULL(T1.[USUARIOACTUALIZACION], '')  AS [USUARIOACTUALIZACION],
            ISNULL((SELECT TOP 1 T2.[USUARIO]
                      FROM [dbo].[DQS_REC_USUARIOS] AS T2
                     WHERE T2.[CEDULA] = T1.[NoIDENTIFICACION] AND T2.[CIA] = T1.[CIA]
                     ORDER BY T2.[USUARIO]), '')   AS [USUARIOAPP]
      FROM  [dbo].[DQS_MIP_PROFESIONALES] AS T1
     WHERE  T1.[CIA] = @CIA
       AND  T1.[IDPROFESIONAL] = @IdProfesional;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesResolverUsuario;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesResolverUsuario
    @CIA        NVARCHAR(500),
    @Cedula     NVARCHAR(500),
    @Usuario    VARCHAR(150) = NULL OUTPUT,
    @YaExiste   BIT          = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Cedula = LTRIM(RTRIM(ISNULL(@Cedula, '')));
    SET @Usuario = NULL;
    SET @YaExiste = 0;

    IF @Cedula = '' RETURN;

    SELECT TOP 1 @Usuario = [USUARIO]
      FROM [dbo].[DQS_REC_USUARIOS]
     WHERE [CEDULA] = @Cedula AND [CIA] = @CIA
     ORDER BY [USUARIO];

    IF @Usuario IS NOT NULL
    BEGIN
        SET @YaExiste = 1;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[DQS_REC_USUARIOS] WHERE [USUARIO] = @Cedula)
    BEGIN
        SET @Usuario = CONVERT(VARCHAR(150), @Cedula);
        RETURN;
    END

    DECLARE @Candidato VARCHAR(150) = LEFT(CONVERT(VARCHAR(150), @Cedula) + '_' + CONVERT(VARCHAR(150), @CIA), 150);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[DQS_REC_USUARIOS] WHERE [USUARIO] = @Candidato)
    BEGIN
        SET @Usuario = @Candidato;
        RETURN;
    END

    DECLARE @i INT = 2;
    WHILE @i < 1000
    BEGIN
        SET @Candidato = LEFT(CONVERT(VARCHAR(150), @Cedula) + '-' + CONVERT(VARCHAR(10), @i), 150);

        IF NOT EXISTS (SELECT 1 FROM [dbo].[DQS_REC_USUARIOS] WHERE [USUARIO] = @Candidato)
        BEGIN
            SET @Usuario = @Candidato;
            RETURN;
        END

        SET @i = @i + 1;
    END
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesSincronizarUsuario;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesSincronizarUsuario
    @CIA            NVARCHAR(500),
    @Cedula         NVARCHAR(500),
    @Apellidos      NVARCHAR(250) = NULL,
    @Nombres        NVARCHAR(250) = NULL,
    @Email          NVARCHAR(500) = NULL,
    @Estado         BIT,
    @Imagen         NVARCHAR(500) = NULL,
    @UsuarioSesion  NVARCHAR(150) = NULL,
    @IdEmpresa      NVARCHAR(50)  = NULL,
    @Clave          NVARCHAR(500) = NULL,
    @Usuario        VARCHAR(150)  = NULL OUTPUT,
    @Creado         BIT           = NULL OUTPUT,
    @Mensaje        NVARCHAR(500) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Cedula  = LTRIM(RTRIM(ISNULL(@Cedula, '')));
    SET @Usuario = NULL;
    SET @Creado  = 0;
    SET @Mensaje = NULL;

    IF @Cedula = ''
    BEGIN
        SET @Mensaje = 'No se sincronizó el usuario: el profesional no tiene documento.';
        RETURN;
    END

    DECLARE @EstadoTexto VARCHAR(10) = CASE WHEN @Estado = 1 THEN 'Activo' ELSE 'Inactivo' END;

    DECLARE @Rol VARCHAR(10), @Token VARCHAR(200), @Cargo VARCHAR(50),
            @PwdTemp VARCHAR(50), @FechaPwd VARCHAR(50), @IdEmpresaBase VARCHAR(50);

    SELECT TOP 1 @Rol           = [ROL],
                 @Token         = [TOKEN],
                 @Cargo         = [Cargo],
                 @PwdTemp       = [PWDTEMP],
                 @FechaPwd      = [FechaPwd],
                 @IdEmpresaBase = [ID_EMPRESA]
      FROM [dbo].[DQS_REC_USUARIOS]
     WHERE [USUARIO] = @UsuarioSesion;

    IF @IdEmpresa IS NOT NULL AND LTRIM(RTRIM(@IdEmpresa)) <> ''
        SET @IdEmpresaBase = CONVERT(VARCHAR(50), @IdEmpresa);

    DECLARE @YaExiste BIT;
    EXEC [dbo].[sp_DQS_MIP_ProfesionalesResolverUsuario] @CIA, @Cedula, @Usuario OUTPUT, @YaExiste OUTPUT;

    IF @Usuario IS NULL
    BEGIN
        SET @Mensaje = 'No se pudo determinar un nombre de usuario disponible.';
        RETURN;
    END

    IF @YaExiste = 1
    BEGIN
        UPDATE [dbo].[DQS_REC_USUARIOS]
           SET [NOMBRES]   = ISNULL(@Nombres, ''),
               [APELLIDOS] = ISNULL(@Apellidos, ''),
               [EMAIL]     = @Email,
               [ESTADO]    = @EstadoTexto,
               [IMAGEN]    = CASE WHEN LTRIM(RTRIM(ISNULL(@Imagen, ''))) = '' THEN [IMAGEN] ELSE @Imagen END
         WHERE [USUARIO] = @Usuario;

        SET @Mensaje = CASE WHEN @Estado = 1
                            THEN 'Usuario ' + @Usuario + ' actualizado.'
                            ELSE 'Usuario ' + @Usuario + ' inactivado.' END;
        RETURN;
    END

    IF @Rol IS NULL
    BEGIN
        SET @Usuario = NULL;
        SET @Mensaje = 'No se creó el usuario: no se pudo leer el rol del usuario en sesión.';
        RETURN;
    END

    INSERT INTO [dbo].[DQS_REC_USUARIOS]
          ([ID_EMPRESA], [CEDULA], [NOMBRES], [APELLIDOS], [USUARIO], [EMAIL], [ESTADO],
           [CIA], [ROL], [CLAVE], [IMAGEN], [FECHA_ACT_PWD], [MARCA], [FECHA_SESION],
           [TOKEN], [Cargo], [PWDTEMP], [FechaPwd])
    VALUES (@IdEmpresaBase, @Cedula, ISNULL(@Nombres, ''), ISNULL(@Apellidos, ''), @Usuario,
            @Email, @EstadoTexto, @CIA, @Rol, @Clave, NULLIF(LTRIM(RTRIM(ISNULL(@Imagen, ''))), ''),
            GETDATE(), 0, GETDATE(), @Token, @Cargo, @PwdTemp, @FechaPwd);

    SET @Creado = 1;
    SET @Mensaje = 'Usuario ' + @Usuario + ' creado.';
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesInsertar;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesInsertar
    @CIA                NVARCHAR(500),
    @TipoDoc            NVARCHAR(500) = NULL,
    @NoIdentificacion   NVARCHAR(500),
    @Apellidos          NVARCHAR(250) = NULL,
    @NombresProf        NVARCHAR(250) = NULL,
    @Profesion          NVARCHAR(500) = NULL,
    @Sede               NVARCHAR(500) = NULL,
    @Servicio           NVARCHAR(500) = NULL,
    @Correo             NVARCHAR(500) = NULL,
    @Celular            NVARCHAR(500) = NULL,
    @FActivacion        NVARCHAR(500) = NULL,
    @Firma              NVARCHAR(500) = NULL,
    @Usuario            NVARCHAR(100) = NULL,
    @IdEmpresa          NVARCHAR(50)  = NULL,
    @Clave              NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NoIdentificacion = LTRIM(RTRIM(ISNULL(@NoIdentificacion, '')));
    SET @Apellidos        = LTRIM(RTRIM(ISNULL(@Apellidos, '')));
    SET @NombresProf      = LTRIM(RTRIM(ISNULL(@NombresProf, '')));

    DECLARE @NombreCompleto NVARCHAR(500) = LTRIM(RTRIM(@Apellidos + ' ' + @NombresProf));

    IF @NoIdentificacion = '' OR @NombreCompleto = ''
    BEGIN
        SELECT 0 AS RESULTADO, 0 AS IDPROFESIONAL,
               'El documento y el nombre del profesional son obligatorios.' AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [dbo].[DQS_MIP_PROFESIONALES]
                WHERE [CIA] = @CIA AND [NoIDENTIFICACION] = @NoIdentificacion)
    BEGIN
        SELECT 0 AS RESULTADO, 0 AS IDPROFESIONAL,
               'Ya existe un profesional registrado con el documento ' + @NoIdentificacion + '.' AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
        RETURN;
    END

    DECLARE @IdNuevo INT, @UsuarioApp VARCHAR(150), @UsuarioCreado BIT, @MensajeUsuario NVARCHAR(500);

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[DQS_MIP_PROFESIONALES]
              ([CIA], [TIPODOC], [NoIDENTIFICACION], [APELLIDOS Y NOMBRES], [APELLIDOS], [NOMBRES],
               [PROFESION], [SEDE], [SERVICIO], [CORREOELECTRONICO], [CELULAR], [FActivación], [Firma],
               [ESTADO], [FECHACREACION], [USUARIOCREACION])
        VALUES (@CIA, @TipoDoc, @NoIdentificacion, @NombreCompleto, @Apellidos, @NombresProf,
                @Profesion, @Sede, @Servicio, @Correo, @Celular, @FActivacion, ISNULL(@Firma, ''),
                1, GETDATE(), @Usuario);

        SET @IdNuevo = CONVERT(INT, SCOPE_IDENTITY());

        EXEC [dbo].[sp_DQS_MIP_ProfesionalesSincronizarUsuario]
             @CIA           = @CIA,
             @Cedula        = @NoIdentificacion,
             @Apellidos     = @Apellidos,
             @Nombres       = @NombresProf,
             @Email         = @Correo,
             @Estado        = 1,
             @Imagen        = @Firma,
             @UsuarioSesion = @Usuario,
             @IdEmpresa     = @IdEmpresa,
             @Clave         = @Clave,
             @Usuario       = @UsuarioApp OUTPUT,
             @Creado        = @UsuarioCreado OUTPUT,
             @Mensaje       = @MensajeUsuario OUTPUT;

        COMMIT TRANSACTION;

        SELECT 1 AS RESULTADO,
               @IdNuevo AS IDPROFESIONAL,
               'Profesional creado correctamente. ' + ISNULL(@MensajeUsuario, '') AS MENSAJE,
               ISNULL(@UsuarioApp, '') AS USUARIOAPP,
               CASE WHEN ISNULL(@UsuarioCreado, 0) = 1 THEN ISNULL(@Clave, '') ELSE '' END AS CLAVEAPP,
               ISNULL(@UsuarioCreado, 0) AS USUARIOCREADO;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, 0 AS IDPROFESIONAL, ERROR_MESSAGE() AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
    END CATCH
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesActualizar;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesActualizar
    @CIA                NVARCHAR(500),
    @IdProfesional      INT,
    @TipoDoc            NVARCHAR(500) = NULL,
    @NoIdentificacion   NVARCHAR(500),
    @Apellidos          NVARCHAR(250) = NULL,
    @NombresProf        NVARCHAR(250) = NULL,
    @Profesion          NVARCHAR(500) = NULL,
    @Sede               NVARCHAR(500) = NULL,
    @Servicio           NVARCHAR(500) = NULL,
    @Correo             NVARCHAR(500) = NULL,
    @Celular            NVARCHAR(500) = NULL,
    @FActivacion        NVARCHAR(500) = NULL,
    @Firma              NVARCHAR(500) = NULL,
    @Usuario            NVARCHAR(100) = NULL,
    @IdEmpresa          NVARCHAR(50)  = NULL,
    @Clave              NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @NoIdentificacion = LTRIM(RTRIM(ISNULL(@NoIdentificacion, '')));
    SET @Apellidos        = LTRIM(RTRIM(ISNULL(@Apellidos, '')));
    SET @NombresProf      = LTRIM(RTRIM(ISNULL(@NombresProf, '')));

    DECLARE @NombreCompleto NVARCHAR(500) = LTRIM(RTRIM(@Apellidos + ' ' + @NombresProf));
    DECLARE @EstadoActual BIT;

    SELECT @EstadoActual = [ESTADO]
      FROM [dbo].[DQS_MIP_PROFESIONALES]
     WHERE [CIA] = @CIA AND [IDPROFESIONAL] = @IdProfesional;

    IF @EstadoActual IS NULL
    BEGIN
        SELECT 0 AS RESULTADO, @IdProfesional AS IDPROFESIONAL,
               'El profesional que intenta modificar ya no existe.' AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
        RETURN;
    END

    IF @NoIdentificacion = '' OR @NombreCompleto = ''
    BEGIN
        SELECT 0 AS RESULTADO, @IdProfesional AS IDPROFESIONAL,
               'El documento y el nombre del profesional son obligatorios.' AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [dbo].[DQS_MIP_PROFESIONALES]
                WHERE [CIA] = @CIA
                  AND [NoIDENTIFICACION] = @NoIdentificacion
                  AND [IDPROFESIONAL] <> @IdProfesional)
    BEGIN
        SELECT 0 AS RESULTADO, @IdProfesional AS IDPROFESIONAL,
               'Ya existe otro profesional con el documento ' + @NoIdentificacion + '.' AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
        RETURN;
    END

    DECLARE @UsuarioApp VARCHAR(150), @UsuarioCreado BIT, @MensajeUsuario NVARCHAR(500);

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[DQS_MIP_PROFESIONALES]
           SET [TIPODOC]              = @TipoDoc,
               [NoIDENTIFICACION]     = @NoIdentificacion,
               [APELLIDOS Y NOMBRES]  = @NombreCompleto,
               [APELLIDOS]            = @Apellidos,
               [NOMBRES]              = @NombresProf,
               [PROFESION]            = @Profesion,
               [SEDE]                 = @Sede,
               [SERVICIO]             = @Servicio,
               [CORREOELECTRONICO]    = @Correo,
               [CELULAR]              = @Celular,
               [FActivación]          = @FActivacion,
               [Firma]                = ISNULL(@Firma, ''),
               [FECHAACTUALIZACION]   = GETDATE(),
               [USUARIOACTUALIZACION] = @Usuario
         WHERE [CIA] = @CIA
           AND [IDPROFESIONAL] = @IdProfesional;

        EXEC [dbo].[sp_DQS_MIP_ProfesionalesSincronizarUsuario]
             @CIA           = @CIA,
             @Cedula        = @NoIdentificacion,
             @Apellidos     = @Apellidos,
             @Nombres       = @NombresProf,
             @Email         = @Correo,
             @Estado        = @EstadoActual,
             @Imagen        = @Firma,
             @UsuarioSesion = @Usuario,
             @IdEmpresa     = @IdEmpresa,
             @Clave         = @Clave,
             @Usuario       = @UsuarioApp OUTPUT,
             @Creado        = @UsuarioCreado OUTPUT,
             @Mensaje       = @MensajeUsuario OUTPUT;

        COMMIT TRANSACTION;

        SELECT 1 AS RESULTADO, @IdProfesional AS IDPROFESIONAL,
               'Profesional actualizado correctamente. ' + ISNULL(@MensajeUsuario, '') AS MENSAJE,
               ISNULL(@UsuarioApp, '') AS USUARIOAPP,
               CASE WHEN ISNULL(@UsuarioCreado, 0) = 1 THEN ISNULL(@Clave, '') ELSE '' END AS CLAVEAPP,
               ISNULL(@UsuarioCreado, 0) AS USUARIOCREADO;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, @IdProfesional AS IDPROFESIONAL, ERROR_MESSAGE() AS MENSAJE,
               '' AS USUARIOAPP, '' AS CLAVEAPP, 0 AS USUARIOCREADO;
    END CATCH
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesCambiarEstado;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesCambiarEstado
    @CIA            NVARCHAR(500),
    @IdProfesional  INT,
    @Estado         BIT,
    @Usuario        NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cedula NVARCHAR(500);

    SELECT @Cedula = [NoIDENTIFICACION]
      FROM [dbo].[DQS_MIP_PROFESIONALES]
     WHERE [CIA] = @CIA AND [IDPROFESIONAL] = @IdProfesional;

    IF @Cedula IS NULL
    BEGIN
        SELECT 0 AS RESULTADO, 'El profesional ya no existe.' AS MENSAJE;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[DQS_MIP_PROFESIONALES]
           SET [ESTADO]               = @Estado,
               [FECHAINACTIVACION]    = CASE WHEN @Estado = 0 THEN GETDATE() ELSE NULL END,
               [USUARIOINACTIVACION]  = CASE WHEN @Estado = 0 THEN @Usuario ELSE NULL END,
               [FECHAACTUALIZACION]   = GETDATE(),
               [USUARIOACTUALIZACION] = @Usuario
         WHERE [CIA] = @CIA
           AND [IDPROFESIONAL] = @IdProfesional;

        UPDATE [dbo].[DQS_REC_USUARIOS]
           SET [ESTADO] = CASE WHEN @Estado = 1 THEN 'Activo' ELSE 'Inactivo' END
         WHERE [CIA] = @CIA
           AND [CEDULA] = @Cedula;

        COMMIT TRANSACTION;

        SELECT 1 AS RESULTADO,
               CASE WHEN @Estado = 0
                    THEN 'Profesional y usuario inactivados correctamente.'
                    ELSE 'Profesional y usuario reactivados correctamente.' END AS MENSAJE;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, ERROR_MESSAGE() AS MENSAJE;
    END CATCH
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesResumen;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesResumen
    @CIA NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  COUNT(1)                                                    AS TOTAL,
            SUM(CASE WHEN [ESTADO] = 1 THEN 1 ELSE 0 END)               AS ACTIVOS,
            SUM(CASE WHEN [ESTADO] = 0 THEN 1 ELSE 0 END)               AS INACTIVOS,
            COUNT(DISTINCT NULLIF(LTRIM(RTRIM(ISNULL([SEDE],''))), '')) AS SEDES
      FROM  [dbo].[DQS_MIP_PROFESIONALES]
     WHERE  [CIA] = @CIA;
END
GO

DROP PROCEDURE IF EXISTS dbo.sp_DQS_MIP_ProfesionalesListas;
GO
CREATE PROCEDURE dbo.sp_DQS_MIP_ProfesionalesListas
    @CIA NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT 'PROFESION' AS TIPO, LTRIM(RTRIM([PROFESION])) AS VALOR
      FROM [dbo].[DQS_MIP_PROFESIONALES]
     WHERE [CIA] = @CIA AND NULLIF(LTRIM(RTRIM(ISNULL([PROFESION], ''))), '') IS NOT NULL
    UNION
    SELECT DISTINCT 'SEDE' AS TIPO, LTRIM(RTRIM([SEDE])) AS VALOR
      FROM [dbo].[DQS_MIP_PROFESIONALES]
     WHERE [CIA] = @CIA AND NULLIF(LTRIM(RTRIM(ISNULL([SEDE], ''))), '') IS NOT NULL
    ORDER BY TIPO, VALOR;
END
GO

PRINT 'Migración MIP_PROFESIONALES aplicada correctamente a DQUALITYREC.';
GO
