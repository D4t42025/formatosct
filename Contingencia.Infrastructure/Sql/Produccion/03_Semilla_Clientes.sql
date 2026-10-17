-- 03_Semilla_Clientes.sql - cliente(s) de la base de produccion nueva (multi-CIA): DQS_REC_CLIENTES.
-- Generado el 2026-10-14. Idempotente (no duplica CIA+TIPO). La app toma de aqui el nombre y el logo del cliente.
-- Orden: despues de 01_Tablas.sql y 02_ProcedimientosAlmacenados.sql.

SET NOCOUNT ON;
GO

-- 890330031 - EPS SANITAS (copiado de DQUALITYREC)
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_CLIENTES WHERE CIA = N'890330031' AND TIPO = N'CTC')
    INSERT INTO dbo.DQS_REC_CLIENTES ([CIA], [CLIENTE], [NIT], [TIPO], [PREFIJO], [CONSECUTIVO], [COD_CLIENTE], [DIRECCION], [LOGO], [TEXTOCONTESTACION], [TEXTONOTIFICACION], [RUTAFORMATO], [FIRMA], [TELEFONO], [EMAIL], [TOKEN], [FECHALICENCIA], [CiudadCliente], [CiudadPrestacion], [NombreRepresentante], [Idrepresentante], [RegistroHab1], [RegistroHab2], [Mail2], [Nombrecontacto], [TelefonoContacto], [MailContacto], [EstadoContrato], [TipoContrato], [MarcaP])
    VALUES (N'890330031', N'EPS SANITAS', N'890330031', N'CTC', N'', 27, N'890330031', N'0', N'https://imperium.net.co/Img/EPS045/890330031/EPS-SANITAS.jpg', N'', N'', N'', NULL, N'0', N'1@.com', N'EPS', '2026-12-31', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'0');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_CLIENTES WHERE CIA = N'890330031' AND TIPO = N'TUTELA')
    INSERT INTO dbo.DQS_REC_CLIENTES ([CIA], [CLIENTE], [NIT], [TIPO], [PREFIJO], [CONSECUTIVO], [COD_CLIENTE], [DIRECCION], [LOGO], [TEXTOCONTESTACION], [TEXTONOTIFICACION], [RUTAFORMATO], [FIRMA], [TELEFONO], [EMAIL], [TOKEN], [FECHALICENCIA], [CiudadCliente], [CiudadPrestacion], [NombreRepresentante], [Idrepresentante], [RegistroHab1], [RegistroHab2], [Mail2], [Nombrecontacto], [TelefonoContacto], [MailContacto], [EstadoContrato], [TipoContrato], [MarcaP])
    VALUES (N'890330031', N'EPS SANITAS', N'890330031', N'TUTELA', N'', 23, N'890330031', N'0', N'https://imperium.net.co/Img/EPS045/890330031/EPS-SANITAS.jpg', N'', N'', N'', NULL, N'0', N'1@.com', N'EPS', '2026-12-31', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'0');
GO

-- 800006850 - HOSPITAL MARIO GAITAN YANGUAS (IPS; datos entregados por el usuario el 2026-10-14; licencia hasta 2026-12-01)
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_CLIENTES WHERE CIA = N'800006850' AND TIPO = N'CTC')
    INSERT INTO dbo.DQS_REC_CLIENTES ([CIA], [CLIENTE], [NIT], [TIPO], [PREFIJO], [CONSECUTIVO], [COD_CLIENTE], [DIRECCION], [LOGO], [TEXTOCONTESTACION], [TEXTONOTIFICACION], [RUTAFORMATO], [FIRMA], [TELEFONO], [EMAIL], [TOKEN], [FECHALICENCIA], [CiudadCliente], [CiudadPrestacion], [NombreRepresentante], [Idrepresentante], [RegistroHab1], [RegistroHab2], [Mail2], [Nombrecontacto], [TelefonoContacto], [MailContacto], [EstadoContrato], [TipoContrato], [MarcaP])
    VALUES (N'800006850', N'HOSPITAL MARIO GAITAN YANGUAS', N'800006850', N'CTC', N'', 1, N'800006850', N'0', N'https://imperium.net.co/Img/DQS/800006850/Logo_HMGY.jpg', N'', N'', N'', N'2026-10-13', N'0', N'0', N'IPS', '2026-12-01', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_CLIENTES WHERE CIA = N'800006850' AND TIPO = N'TUTELA')
    INSERT INTO dbo.DQS_REC_CLIENTES ([CIA], [CLIENTE], [NIT], [TIPO], [PREFIJO], [CONSECUTIVO], [COD_CLIENTE], [DIRECCION], [LOGO], [TEXTOCONTESTACION], [TEXTONOTIFICACION], [RUTAFORMATO], [FIRMA], [TELEFONO], [EMAIL], [TOKEN], [FECHALICENCIA], [CiudadCliente], [CiudadPrestacion], [NombreRepresentante], [Idrepresentante], [RegistroHab1], [RegistroHab2], [Mail2], [Nombrecontacto], [TelefonoContacto], [MailContacto], [EstadoContrato], [TipoContrato], [MarcaP])
    VALUES (N'800006850', N'HOSPITAL MARIO GAITAN YANGUAS', N'800006850', N'TUTELA', N'', 1, N'800006850', N'0', N'https://imperium.net.co/Img/DQS/800006850/Logo_HMGY.jpg', N'', N'', N'', N'2026-10-13', N'0', N'0', N'IPS', '2026-12-01', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'2');
GO

