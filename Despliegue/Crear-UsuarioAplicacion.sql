-- Crear-UsuarioAplicacion.sql - inicio de sesion de SQL Server para la APLICACION (minimo privilegio), en lugar de una cuenta sysadmin.
--
-- Da solo lo que la aplicacion usa: leer y escribir las tablas, ejecutar los procedimientos y ver su definicion (el ejecutor de
-- procedimientos descubre los parametros de cada uno). No puede crear ni borrar objetos, ni administrar el servidor.
--
-- Se ejecuta UNA vez con una cuenta administradora y las variables de sqlcmd (la clave no queda escrita en este archivo):
--   sqlcmd -S <servidor> -U <admin> -C -f65001 -b -v NOMBRE_BD="FormatosCT" USUARIO_APP="contingencia_app" CLAVE_APP="<clave>" -i Crear-UsuarioAplicacion.sql
-- Luego la cadena de la aplicacion usa ese usuario: Server=<servidor>;Database=<base>;User Id=contingencia_app;Password=<clave>;
--   TrustServerCertificate=True;Connect Timeout=30;Max Pool Size=100
-- Idempotente: si el inicio de sesion o el usuario ya existen no los recrea (ni cambia la clave).

USE [master];
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(USUARIO_APP)')
    CREATE LOGIN [$(USUARIO_APP)] WITH PASSWORD = N'$(CLAVE_APP)', DEFAULT_DATABASE = [$(NOMBRE_BD)], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
GO

USE [$(NOMBRE_BD)];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(USUARIO_APP)')
    CREATE USER [$(USUARIO_APP)] FOR LOGIN [$(USUARIO_APP)];
GO
ALTER ROLE db_datareader ADD MEMBER [$(USUARIO_APP)];
ALTER ROLE db_datawriter ADD MEMBER [$(USUARIO_APP)];
GRANT EXECUTE ON SCHEMA::dbo TO [$(USUARIO_APP)];
GRANT VIEW DEFINITION ON SCHEMA::dbo TO [$(USUARIO_APP)];
GO

SELECT dp.name AS usuario, STUFF((SELECT ', ' + r.name FROM sys.database_role_members m JOIN sys.database_principals r ON r.principal_id = m.role_principal_id WHERE m.member_principal_id = dp.principal_id FOR XML PATH('')), 1, 2, '') AS roles
FROM sys.database_principals dp WHERE dp.name = N'$(USUARIO_APP)';
GO
