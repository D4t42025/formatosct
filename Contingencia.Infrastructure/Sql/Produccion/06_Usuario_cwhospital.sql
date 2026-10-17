-- 06_Usuario_cwhospital.sql - primer usuario de la CIA 800006850 (HOSPITAL MARIO GAITAN YANGUAS): cwhospital, rol 601 (Formatos).
--
-- Usuario compartido, igual que cwformatos en Sanitas. Mas adelante se pueden pedir mas usuarios (uno por sede o por
-- persona) llamando de nuevo a dbo.sp_MIP_CrearUsuarioFormatos con otro @Usuario/@Cedula/@Nombres.
--
-- LA CLAVE NO SE GUARDA EN ESTE ARCHIVO: se pasa al ejecutarlo con la variable de sqlcmd CLAVE_CWHOSPITAL. Si no se pasa,
-- sqlcmd aborta ("scripting variable not defined") y no se crea nada:
--
--   sqlcmd -S <servidor> -d <base> -U <usuario> -P <clave_sql> -C -f65001 -b ^
--          -v CLAVE_CWHOSPITAL="<clave elegida, minimo 8 caracteres>" -i 06_Usuario_cwhospital.sql
--
-- Orden: despues de 01 a 05 (necesita el cliente, el rol 601 y los modulos de la CIA).
-- Si el usuario ya existe no hace nada y devuelve el mensaje del procedimiento (no pisa la clave).

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = 'cwhospital')
    EXEC dbo.sp_MIP_CrearUsuarioFormatos
        @Cia       = '800006850',
        @Usuario   = 'cwhospital',
        @Cedula    = '800006850',
        @Nombres   = 'HOSPITAL MARIO GAITAN YANGUAS',
        @Apellidos = '',
        @Email     = '',
        @Clave     = '$(CLAVE_CWHOSPITAL)',
        @IdEmpresa = '800006850';
ELSE
    SELECT 0 AS RESULTADO, 'cwhospital ya existe; no se modifico.' AS MENSAJE;
GO
