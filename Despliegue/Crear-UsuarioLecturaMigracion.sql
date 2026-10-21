-- Crear-UsuarioLecturaMigracion.sql - usuario de SOLO LECTURA para que Migrar-FormatosBD.ps1 lea del servidor legado (168).
--
-- Da un unico permiso: SELECT sobre dbo.DQS_MIP_FORMATOS. No puede escribir, ni ver otras tablas, ni administrar nada.
-- Asi la cadena guardada en el equipo que corre la tarea NO es la de sa.
--
-- Se ejecuta UNA vez en el servidor 168 con una cuenta administradora (la clave no queda escrita en este archivo):
--   sqlcmd -S 45.169.253.168 -U <admin> -C -f65001 -b -v NOMBRE_BD="DQUALITY" USUARIO_LECTURA="migracion_formatos" CLAVE_LECTURA="<clave>" -i Crear-UsuarioLecturaMigracion.sql
-- Cadena para -GuardarOrigen:
--   Server=45.169.253.168;Database=DQUALITY;User Id=migracion_formatos;Password=<clave>;TrustServerCertificate=True;Encrypt=False
-- Idempotente: si el inicio de sesion o el usuario ya existen no los recrea (ni cambia la clave).
-- Generar la clave solo con letras, numeros y # ! @ * + - (un = o una comilla rompe sqlcmd -v).
-- Quitar despues del corte final: DROP USER [migracion_formatos]; DROP LOGIN [migracion_formatos];

USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(USUARIO_LECTURA)')
    CREATE LOGIN [$(USUARIO_LECTURA)] WITH PASSWORD = N'$(CLAVE_LECTURA)', DEFAULT_DATABASE = [$(NOMBRE_BD)], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
GO
USE [$(NOMBRE_BD)];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(USUARIO_LECTURA)')
    CREATE USER [$(USUARIO_LECTURA)] FOR LOGIN [$(USUARIO_LECTURA)];
GO
GRANT SELECT ON OBJECT::dbo.DQS_MIP_FORMATOS TO [$(USUARIO_LECTURA)];
GO
SELECT dp.name AS usuario, p.permission_name, p.state_desc, OBJECT_NAME(p.major_id) AS objeto
FROM sys.database_principals dp JOIN sys.database_permissions p ON p.grantee_principal_id = dp.principal_id
WHERE dp.name = N'$(USUARIO_LECTURA)' AND p.class = 1;
GO
