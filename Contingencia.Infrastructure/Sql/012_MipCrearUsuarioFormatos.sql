-- Alta de usuarios de Formatos (rol 601) en una CIA, para el despliegue multi-CIA. Procedimiento de
-- administración: lo ejecuta una persona con acceso a la base (no lo llama la app), una vez por usuario.
-- Sirve igual para un usuario compartido (p. ej. cwhospital) que para uno por sede o por persona.
--
-- Valida que la CIA exista como cliente (DQS_REC_CLIENTES, TIPO='CTC': sin esa fila el login no devuelve
-- nada) y que tenga el rol 601 sembrado (DQS_REC_ROLES). USUARIO es la clave primaria de
-- DQS_REC_USUARIOS, así que el nombre de usuario es único entre TODAS las CIA.
--
-- Valores que deja (mismos que los usuarios 601 existentes, salvo los dos marcados):
--   ROL='601', ESTADO='Activo', MARCA=0, PLANTA='NACIONAL', TOKEN='', FECHA_ACT_PWD=GETDATE().
--   PWDTEMP=NULL  -> la clave NO es temporal: si fuera igual a PWDTEMP el login mandaría al usuario a
--                    ActContraseña.aspx, página del legado que no está migrada.
--   FechaPwd='2099-12-31' -> sin fecha de cambio forzado de clave (por la misma razón; los usuarios antiguos
--                    traen '2026-12-31').
-- La clave se guarda en texto plano en DQS_REC_USUARIOS.CLAVE y el login la compara tal cual (así es el diseño
-- heredado); máximo 50 caracteres porque sp_DQS_ConsultaUsuario recibe @STRPWD VARCHAR(50).
--
-- Ejemplo (no dejar la clave escrita en archivos del repositorio):
--   EXEC dbo.sp_MIP_CrearUsuarioFormatos @Cia='800006850', @Usuario='cwhospital', @Cedula='800006850',
--        @Nombres='HOSPITAL MARIO GAITAN YANGUAS', @Apellidos='', @Clave='<clave>';
--
-- Aplicado el 2026-10-14 en DQUALITYREC (172.25.2.15,1435) y DQUALITY (45.169.253.168) — solo el
-- procedimiento; no se creó ningún usuario.

SET NOCOUNT ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MIP_CrearUsuarioFormatos
    @Cia        VARCHAR(150),
    @Usuario    VARCHAR(150),
    @Cedula     VARCHAR(50),
    @Nombres    VARCHAR(150),
    @Apellidos  VARCHAR(150) = '',
    @Email      VARCHAR(150) = '',
    @Clave      VARCHAR(50),
    @IdEmpresa  VARCHAR(50)  = NULL,   -- por defecto la propia CIA
    @Planta     VARCHAR(200) = 'NACIONAL'
AS
BEGIN
    SET NOCOUNT ON;

    SET @Cia     = LTRIM(RTRIM(ISNULL(@Cia, '')));
    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));
    SET @Cedula  = LTRIM(RTRIM(ISNULL(@Cedula, '')));
    SET @Nombres = LTRIM(RTRIM(ISNULL(@Nombres, '')));

    IF @Cia = '' OR @Usuario = '' OR @Cedula = '' OR @Nombres = ''
        BEGIN SELECT 0 AS RESULTADO, 'Cia, usuario, cedula y nombres son obligatorios.' AS MENSAJE; RETURN; END

    IF LEN(ISNULL(@Clave, '')) < 8
        BEGIN SELECT 0 AS RESULTADO, 'La clave debe tener al menos 8 caracteres.' AS MENSAJE; RETURN; END

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_CLIENTES WHERE CIA = @Cia AND TIPO = 'CTC')
        BEGIN SELECT 0 AS RESULTADO, 'La CIA no existe en DQS_REC_CLIENTES (TIPO CTC); sin esa fila el login no funciona.' AS MENSAJE; RETURN; END

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_ROLES WHERE CIA = @Cia AND ID_ROL = '601')
        BEGIN SELECT 0 AS RESULTADO, 'La CIA no tiene el rol 601 (Formatos) en DQS_REC_ROLES.' AS MENSAJE; RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = @Usuario)
        BEGIN SELECT 0 AS RESULTADO, 'El usuario ya existe (el nombre de usuario es unico entre todas las CIA).' AS MENSAJE; RETURN; END

    INSERT INTO dbo.DQS_REC_USUARIOS
        (ID_EMPRESA, CEDULA, NOMBRES, APELLIDOS, USUARIO, EMAIL, ESTADO, CIA, ROL, CLAVE,
         FECHA_ACT_PWD, MARCA, FECHA_SESION, TOKEN, Planta, PWDTEMP, FechaPwd)
    VALUES
        (ISNULL(NULLIF(LTRIM(RTRIM(@IdEmpresa)), ''), @Cia), @Cedula, @Nombres, ISNULL(@Apellidos, ''), @Usuario,
         ISNULL(@Email, ''), 'Activo', @Cia, '601', @Clave,
         GETDATE(), 0, GETDATE(), '', @Planta, NULL, '2099-12-31');

    SELECT 1 AS RESULTADO, 'Usuario creado con rol 601 (Formatos).' AS MENSAJE;
END
GO
