-- Parámetros por CIA (multi-CIA) — primer uso: de dónde salen los datos del paciente en el paso 4
-- "Paciente" del wizard "Nueva Contingencia".
--
--   AFILIADOS_ORIGEN  = 'WS'     -> se consulta el web service de afiliados (hoy solo el de EPS Sanitas).
--                       'MANUAL' -> el usuario digita los datos del paciente en un formulario.
--                       Sin fila para la CIA -> 'MANUAL' (una CIA sin web service nunca llama al de otra).
--   AFILIADOS_ENTIDAD = nombre de la EPS que se muestra en el panel del paciente (solo modo WS).
--
--   PRESTADORES_NIT   = NIT al que se restringe la lista de IPS del paso 2 "IPS" del wizard (solo los
--                       prestadores de DQS_MIP_COD_HAB con ese Extra_II_NroIDPrestador, es decir, las sedes de la
--                       propia IPS). Sin fila -> sin restricción (EPS: cualquier prestador del catálogo).
--
--   CODIGOS_CATALOGO  = valor de DQS_MIP_CODIGOS.CIA del que se leen los códigos MIPRES del paso 7 del wizard. Sin fila -> 'NEPS'
--                       (único catálogo cargado hoy).
--   AFILIADOS_WS_URL  = dirección del web service de afiliados de la CIA (solo modo WS). Sin fila -> la de EPS Sanitas.
--
-- Tabla genérica (CIA + CLAVE + VALOR) para poder parametrizar otras opciones por CIA más
-- adelante sin cambiar el esquema. Se administra por SQL (no hay pantalla):
--   UPDATE dbo.DQS_MIP_CIA_PARAMETROS SET VALOR = 'MANUAL' WHERE CIA = '...' AND CLAVE = 'AFILIADOS_ORIGEN';
--
-- Semilla: 890330031 (EPS Sanitas) = WS, sin restricción de prestadores; 800006850 (HOSPITAL MARIO GAITAN
-- YANGUAS, IPS sin web service propio) = MANUAL y PRESTADORES_NIT = 800006850 (sus 31 sedes).
--
-- Aplicado el 2026-10-14 (PRESTADORES_NIT agregado el mismo día re-ejecutando este script, que es idempotente) en:
--   - DQUALITYREC (172.25.2.15,1435) — base de pruebas.
--   - DQUALITY (45.169.253.168).

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.DQS_MIP_CIA_PARAMETROS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DQS_MIP_CIA_PARAMETROS
    (
        CIA                 NVARCHAR(50)  NOT NULL,
        CLAVE               NVARCHAR(50)  NOT NULL,
        VALOR               NVARCHAR(500) NOT NULL,
        DESCRIPCION         NVARCHAR(500) NULL,
        ACTIVO              BIT           NOT NULL CONSTRAINT DF_MIPCIAPARAM_ACTIVO DEFAULT (1),
        FECHAACTUALIZACION  DATETIME      NOT NULL CONSTRAINT DF_MIPCIAPARAM_FECHA DEFAULT (GETDATE()),
        CONSTRAINT PK_MIPCIAPARAM PRIMARY KEY (CIA, CLAVE)
    );
END
GO

-- Semilla idempotente (no pisa un valor que ya se haya cambiado a mano).
INSERT INTO dbo.DQS_MIP_CIA_PARAMETROS (CIA, CLAVE, VALOR, DESCRIPCION)
SELECT v.CIA, v.CLAVE, v.VALOR, v.DESCRIPCION
FROM (VALUES
    ('890330031', 'AFILIADOS_ORIGEN',  'WS',          'Datos del paciente: WS = web service de afiliados; MANUAL = formulario'),
    ('890330031', 'AFILIADOS_ENTIDAD', 'EPS Sanitas', 'EPS que se muestra en el panel del paciente (modo WS)'),
    ('800006850', 'AFILIADOS_ORIGEN',  'MANUAL',      'Datos del paciente: WS = web service de afiliados; MANUAL = formulario'),
    ('800006850', 'PRESTADORES_NIT',   '800006850',   'Paso 2 del wizard: solo prestadores (sedes) con este NIT')
) AS v (CIA, CLAVE, VALOR, DESCRIPCION)
WHERE NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CIA_PARAMETROS p WHERE p.CIA = v.CIA AND p.CLAVE = v.CLAVE);
GO

-- Parámetros activos de una CIA.
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ConsultaCiaParametros
    @Cia NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CLAVE, VALOR
    FROM dbo.DQS_MIP_CIA_PARAMETROS
    WHERE CIA = @Cia AND ACTIVO = 1
    ORDER BY CLAVE;
END
GO
