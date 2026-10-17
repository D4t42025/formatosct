-- 04b_Semilla_ParametrosCia.sql - parametros por CIA (DQS_MIP_CIA_PARAMETROS), multi-CIA.
-- Generado el 2026-10-14 desde DQUALITYREC. Idempotente (no pisa un valor ya cambiado a mano). Despues de 04_Semilla_Seguridad.sql.
--   AFILIADOS_ORIGEN  : WS = web service de afiliados (solo existe el de EPS Sanitas); MANUAL = el usuario digita los datos del paciente.
--                       Una CIA sin fila usa MANUAL.
--   AFILIADOS_ENTIDAD : EPS que se muestra en el panel del paciente (solo modo WS).
--   PRESTADORES_NIT   : restringe el paso 2 "IPS" del wizard a los prestadores con ese NIT (las sedes de la propia IPS). Sin fila = sin restriccion.
--   CODIGOS_CATALOGO  : catalogo de DQS_MIP_CODIGOS (columna CIA) para el paso 7. Sin fila = 'NEPS'.
--   AFILIADOS_WS_URL  : URL del WS de afiliados de la CIA (modo WS). Sin fila = la de EPS Sanitas.
-- 890330031 (EPS Sanitas): WS, sin restriccion.  800006850 (HOSPITAL MARIO GAITAN YANGUAS, IPS): MANUAL y solo sus sedes.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CIA_PARAMETROS WHERE CIA = N'800006850' AND CLAVE = N'AFILIADOS_ORIGEN')
    INSERT INTO dbo.DQS_MIP_CIA_PARAMETROS (CIA, CLAVE, VALOR, DESCRIPCION) VALUES (N'800006850', N'AFILIADOS_ORIGEN', N'MANUAL', N'Datos del paciente: WS = web service de afiliados; MANUAL = formulario');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CIA_PARAMETROS WHERE CIA = N'800006850' AND CLAVE = N'PRESTADORES_NIT')
    INSERT INTO dbo.DQS_MIP_CIA_PARAMETROS (CIA, CLAVE, VALOR, DESCRIPCION) VALUES (N'800006850', N'PRESTADORES_NIT', N'800006850', N'Paso 2 del wizard: solo prestadores (sedes) con este NIT');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CIA_PARAMETROS WHERE CIA = N'890330031' AND CLAVE = N'AFILIADOS_ENTIDAD')
    INSERT INTO dbo.DQS_MIP_CIA_PARAMETROS (CIA, CLAVE, VALOR, DESCRIPCION) VALUES (N'890330031', N'AFILIADOS_ENTIDAD', N'EPS Sanitas', N'EPS que se muestra en el panel del paciente (modo WS)');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CIA_PARAMETROS WHERE CIA = N'890330031' AND CLAVE = N'AFILIADOS_ORIGEN')
    INSERT INTO dbo.DQS_MIP_CIA_PARAMETROS (CIA, CLAVE, VALOR, DESCRIPCION) VALUES (N'890330031', N'AFILIADOS_ORIGEN', N'WS', N'Datos del paciente: WS = web service de afiliados; MANUAL = formulario');
GO
