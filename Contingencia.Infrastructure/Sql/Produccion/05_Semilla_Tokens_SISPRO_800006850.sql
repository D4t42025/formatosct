-- 05_Semilla_Tokens_SISPRO_800006850.sql - SECRETO: contiene los tokens reales de SISPRO MIPRES del HOSPITAL MARIO GAITAN YANGUAS.
-- No publicar ni compartir este archivo; ejecutarlo solo en la base de produccion y no dejar copias.
-- Generado el 2026-10-14 con los datos entregados por el usuario. Idempotente (CIA+Detalle+Regimen).
-- Columnas de DQS_MIP_TOKEN: Cia, NIT, Token, Detalle, Regimen. La app usa APIPRESCRIPCION (validar prescripcion al gestionar
-- un formato) y APIPRESCRIPCION2 (consulta de Juntas con token IPS); APIDIRIPS (direccionamiento) se guarda pero esta app no lo lee.
-- Los tokens de Sanitas (890330031) NO estan en el repositorio: se copian de la base actual al cargar los datos (paso 4).

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_TOKEN WHERE Cia = N'800006850' AND Detalle = N'APIPRESCRIPCION2' AND Regimen = N'Contributivo')
    INSERT INTO dbo.DQS_MIP_TOKEN (Cia, NIT, Token, Detalle, Regimen) VALUES (N'800006850', N'800006850', N'2809762E-DC14-4681-90BB-8418D9CD83B1', N'APIPRESCRIPCION2', N'Contributivo');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_TOKEN WHERE Cia = N'800006850' AND Detalle = N'APIPRESCRIPCION2' AND Regimen = N'Subsidiado')
    INSERT INTO dbo.DQS_MIP_TOKEN (Cia, NIT, Token, Detalle, Regimen) VALUES (N'800006850', N'800006850', N'2809762E-DC14-4681-90BB-8418D9CD83B1', N'APIPRESCRIPCION2', N'Subsidiado');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_TOKEN WHERE Cia = N'800006850' AND Detalle = N'APIPRESCRIPCION' AND Regimen = N'Contributivo')
    INSERT INTO dbo.DQS_MIP_TOKEN (Cia, NIT, Token, Detalle, Regimen) VALUES (N'800006850', N'800006850', N'82795B24-A8D9-44AE-8543-67054FFB4795', N'APIPRESCRIPCION', N'Contributivo');
GO
-- PROVISIONAL (decision del usuario, 2026-10-14): APIPRESCRIPCION Subsidiado usa por ahora el MISMO token que el Contributivo; el usuario aclaro
-- que el token real del Subsidiado es distinto y lo entregara despues. Cuando lo tenga: UPDATE dbo.DQS_MIP_TOKEN SET Token='<token>' WHERE Cia='800006850' AND Detalle='APIPRESCRIPCION' AND Regimen='Subsidiado'.
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_TOKEN WHERE Cia = N'800006850' AND Detalle = N'APIPRESCRIPCION' AND Regimen = N'Subsidiado')
    INSERT INTO dbo.DQS_MIP_TOKEN (Cia, NIT, Token, Detalle, Regimen) VALUES (N'800006850', N'800006850', N'82795B24-A8D9-44AE-8543-67054FFB4795', N'APIPRESCRIPCION', N'Subsidiado');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_TOKEN WHERE Cia = N'800006850' AND Detalle = N'APIDIRIPS' AND Regimen = N'Contributivo')
    INSERT INTO dbo.DQS_MIP_TOKEN (Cia, NIT, Token, Detalle, Regimen) VALUES (N'800006850', N'800006850', N'eIYQIAD-xY3vuCpsQEdoyIMYQYMqf9HJh7mMUvTPSk0=', N'APIDIRIPS', N'Contributivo');
GO
