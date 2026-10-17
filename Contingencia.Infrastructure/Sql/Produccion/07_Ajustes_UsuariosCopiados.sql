-- 07_Ajustes_UsuariosCopiados.sql - ajustes a los usuarios de Sanitas traidos con CopiarCatalogos (--usuarios 890330031 --solo-usuarios cwformatos,cwjuntas).
-- Ejecutar despues de la copia. Idempotente.
--
-- cwformatos es un usuario COMPARTIDO: sp_MIP_CambiarClaveUsuario (como el sp_DQS_ActualizaPwd2 del legado) se niega a cambiar su clave. En la base de
-- origen trae FechaPwd = '2026-12-31': ese dia el login (el vencimiento del dia tiene la ultima palabra, incluso sobre el rol 601) lo mandaria a la
-- pantalla "Actualizar contrasena" y no podria completarla. Se le quita ese vencimiento (el mismo valor lejano que usa sp_MIP_CrearUsuarioFormatos).
--
-- cwjuntas NO se toca aqui: si puede cambiar su clave (su vencimiento del 2026-12-31 lo llevaria a la pantalla, que si funciona para el). Ojo: en las bases
-- de origen la clave de cwjuntas es la fija 'juntas2023**' que el sp_DQS_ConsultaUsuario del legado le reasigna en cada login; esa clave esta escrita en
-- el codigo de ese procedimiento, asi que despues de la copia hay que cambiarla (ver BASE_DE_DATOS.md, Paso 4):
--   EXEC dbo.sp_MIP_CambiarClaveUsuario @Usuario = 'cwjuntas', @ClaveActual = '<clave actual>', @ClaveNueva = '<clave nueva>';

SET NOCOUNT ON;
GO

UPDATE dbo.DQS_REC_USUARIOS
SET FechaPwd = '2099-12-31'
WHERE USUARIO = 'cwformatos' AND CIA = '890330031' AND (FechaPwd IS NULL OR FechaPwd < '2099-12-31');
GO

SELECT USUARIO, CIA, ROL, FechaPwd FROM dbo.DQS_REC_USUARIOS WHERE USUARIO IN ('cwformatos', 'cwjuntas', 'cwhospital') ORDER BY USUARIO;
GO
