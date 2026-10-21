-- 02_ProcedimientosAlmacenados.sql - 36 SP que usa la app (sp_DQS_ConsultaFiltros va aparte, version delgada, en 02b_ProcedimientoFiltrosDelgado.sql)
-- Generado el 2026-10-14 desde DQUALITYREC (172.25.2.15,1435) por catalogo del sistema.
-- Idempotente: se puede ejecutar varias veces. Ejecutar con la base destino seleccionada (USE <base>).
-- 2026-10-21: se agregaron sp_MIP_EliminaFormatoCnt y sp_MIP_EliminaContingencia (scripts 014 y 015, ya aplicados en FormatosCT el 2026-10-18).

SET NOCOUNT ON;
GO

-- ---------------------------------------------------------------- sp_DQS_ConsultaUsuario
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_ConsultaUsuario]
	-- Add the parameters for the stored procedure here
	@STRUSU VARCHAR(50),
	@STRPWD VARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	-- [produccion] se elimino el UPDATE del legado que reiniciaba la clave de 'cwjuntas' a un valor fijo en cada login

	--PROCESO PARA DESBLOQUEAR USUARIO
	DECLARE @FECHAINGRESO DATETIME
	DECLARE @FECHAINGRESO2 DATETIME
	declare @cia varchar(50)
	DECLARE @INTMINUTOS INT
	DECLARE @INTMINUTO2 INT
	SET @FECHAINGRESO= (SELECT T1.FECHA_SESION FROM DQS_REC_USUARIOS AS T1 WHERE T1.USUARIO = @STRUSU AND T1.CLAVE=@STRPWD)

	set @cia =(select t1.CIA from DQS_REC_USUARIOS as t1 WHERE T1.USUARIO = @STRUSU AND T1.CLAVE=@STRPWD )
	SET @FECHAINGRESO2= (SELECT TOP 1 T1.FECHA_ACT_PWD FROM DQS_REC_USUARIOS AS T1 WHERE T1.CIA = @cia ORDER BY T1.FECHA_SESION DESC)
	
	SET @INTMINUTOS = (DATEDIFF(minute,@FECHAINGRESO,getdate()))
	SET @INTMINUTO2 = (DATEDIFF(minute,@FECHAINGRESO2,getdate()))
	
	IF @INTMINUTOS >=1
	BEGIN
	UPDATE DQS_REC_USUARIOS
	SET MARCA = 0,
	FECHA_SESION = GETDATE()
	WHERE USUARIO = @STRUSU
	END

	UPDATE DQS_REC_USUARIOS
	SET MARCA = 0,
	FECHA_SESION = GETDATE()
	WHERE rol = '35'

	--update dqs_rec_usuarios
	--set MARCA=0
	

	---------------------------------------------------
	DECLARE @AÑO VARCHAR (4)=YEAR(GETDATE())
	DECLARE @MES VARCHAR(2)=MONTH(GETDATE())
	DECLARE @DIA VARCHAR(2)=DAY(GETDATE())

	IF LEN(@MES)=1 BEGIN SET @MES = '0' + @MES END
	IF LEN(@DIA)=1 BEGIN SET @DIA = '0' + @DIA END
	DECLARE @FECHA2 VARCHAR(50) = @AÑO + '-' + @MES + '-' + @DIA
	-----------------------------------------------------
	--BEGIN TRY
	
	--SET @CIA= (SELECT T1.CIA FROM DQS_REC_USUARIOS AS T1 WHERE T1.USUARIO=@STRUSU)
	
	--DECLARE @FECHAACT VARCHAR(50)= (SELECT TOP 1 FIRMA FROM DQS_REC_CLIENTES WHERE CIA=@CIA)
	
	--DECLARE @INTVALIDAFECHA INT
	--IF @FECHAACT =  @AÑO + '-' + @MES + '-' + @DIA
	--BEGIN
	--SET @INTVALIDAFECHA=0
	--END
	--ELSE
	--BEGIN
	--SET @INTVALIDAFECHA=1
	--END
	
	
	--IF (@cia ='EPSC34' OR @cia='890330031' OR @CIA='90066593' or @cia ='818000140' or @cia =	'EPSS2405' or @cia =	'EPSS2408' or @cia =	'EPSS2413' or @cia =	'EPSS2415' or @cia =	'ESS024' or @cia =	'EPSS2420' or @cia =	'EPSS2423' or @cia ='EPSS2447' or @cia =	'EPSS2454' or @cia =	'EPSS2468' or @cia =	'EPSS2470' or @cia =	'EPSS2476' or @cia =	'EPSS2494') AND @INTVALIDAFECHA = 1
	--BEGIN
	
	--Declare @Object as Int;
	--Declare @ResponseText as Varchar(8000);
	--DECLARE @NIT VARCHAR(50)= (SELECT T1.NIT FROM DQS_MIP_TOKEN AS T1 WHERE T1.Cia=@CIA AND T1.Regimen ='Contributivo' and T1.Detalle ='APIPRESCRIPCION')
	--DECLARE @TOKENP VARCHAR(50)=(SELECT T1.Token FROM DQS_MIP_TOKEN AS T1 WHERE T1.Cia=@CIA AND T1.Regimen ='Contributivo' and T1.Detalle ='APIPRESCRIPCION')
	--DECLARE @NITS VARCHAR(50)= (SELECT T1.NIT FROM DQS_MIP_TOKEN AS T1 WHERE T1.Cia=@CIA AND T1.Regimen ='Subsidiado' and T1.Detalle ='APIPRESCRIPCION')
	--DECLARE @TOKENPS VARCHAR(50)=(SELECT T1.Token FROM DQS_MIP_TOKEN AS T1 WHERE T1.Cia=@CIA AND T1.Regimen ='Subsidiado' and T1.Detalle ='APIPRESCRIPCION')
	

	--DECLARE @CADENA VARCHAR(2000)='https://wsmipres.sispro.gov.co/WSSUMMIPRESNOPBS/api/GenerarToken/'
	--SET @CADENA =@CADENA + @NIT + '/' + @TOKENP

	--Exec sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;
	--Exec sp_OAMethod @Object, 'open', NULL, 'get',
	--				 @CADENA, --Your Web Service Url (invoked)
	--				 'false'
	--Exec sp_OAMethod @Object, 'send'
	--Exec sp_OAMethod @Object, 'responseText', @ResponseText OUTPUT
	--Exec sp_OADestroy @Object

	--UPDATE DQS_MIP_TOKEN
	--SET Token=REPLACE(@ResponseText,'"','')
	--WHERE NIT=@NIT AND  DETALLE='APIDIREPS' AND REGIMEN ='Contributivo'

	--	print @ResponseText
	

	--DECLARE @CADENAS VARCHAR(2000)='https://wsmipres.sispro.gov.co/WSSUMMIPRESNOPBS/api/GenerarToken/'
	--SET @CADENAS =@CADENAS + @NITS + '/' + @TOKENPS

	--Exec sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;
	--Exec sp_OAMethod @Object, 'open', NULL, 'get',
	--				 @CADENAS, --Your Web Service Url (invoked)
	--				 'false'
	--Exec sp_OAMethod @Object, 'send'
	--Exec sp_OAMethod @Object, 'responseText', @ResponseText OUTPUT
	--Exec sp_OADestroy @Object
	
	--UPDATE DQS_MIP_TOKEN
	--SET Token=REPLACE(@ResponseText,'"','')
	--WHERE NIT=@NIT AND  DETALLE='APIDIREPS' AND REGIMEN ='Subsidiado'



	--UPDATE DQS_REC_CLIENTES
	--SET Firma= @AÑO + '-' + @MES + '-' + @DIA
	--WHERE CIA=@CIA


	--END	
	--END TRY
	--BEGIN CATCH
	--PRINT 'E'
	--END CATCH
	--------------------------------------------------------
	update DQS_REC_USUARIOS
    set TOKEN=''
	WHERE TOKEN IS NULL


	SELECT T1.CIA,T2.ROL,T1.NOMBRES + ' ' + T1.APELLIDOS,T1.ID_EMPRESA,T2.ID_ROL,T1.USUARIO  ,
	T1.IMAGEN, T3.COD_CLIENTE,T1.FECHA_ACT_PWD AS Fecha,t3.logo,T3.TEXTOCONTESTACION,t3.TEXTONOTIFICACION,
	t3.cliente,t3.rutaformato,T3.FIRMA,t3.nit,t3.direccion,t3.telefono,T3.TOKEN,T3.EMAIL,T4.[Código del prestador de servicios de salud]
	,t1.CEDULA,T3.MarcaP,t1.PWDTEMP,t1.FechaPwd,t1.Planta,T1.EMAIL as Emailusu,T1.TOKEN as GA
	FROM DQS_REC_USUARIOS AS T1 
	INNER JOIN DQS_REC_ROLES AS T2 ON T1.CIA = T2.CIA AND T1.ROL = T2.ID_ROL
	INNER JOIN DQS_REC_CLIENTES AS T3 ON T3.CIA = T1.CIA AND T3.TIPO = 'CTC'
	left OUTER JOIN DQS_REC_IPS AS T4 ON T4.NIT = T1.CIA
	
	WHERE USUARIO =@STRUSU AND CLAVE = @STRPWD AND ESTADO='Activo' AND (MARCA=0 or marca=1)
	AND CAST(T3.FECHALICENCIA AS date) >= CAST(@FECHA2 AS date)

	

	IF @INTMINUTO2 >=5
	BEGIN
	BEGIN TRY
	print 'OK'
	UPDATE DQS_REC_USUARIOS
	SET FECHA_ACT_PWD = GETDATE()
	WHERE CIA=@cia
	--EXEC DQUALITY.dbo.Limpiar2
	END TRY
	BEGIN CATCH
	print 'ERROR'
	END CATCH
	END



END
GO

-- ---------------------------------------------------------------- sp_DQS_ConsultaModxRol
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_ConsultaModxRol]
	-- Add the parameters for the stored procedure here
	@STRCIA VARCHAR(50),
	@STRROL VARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
		
	SELECT T3.MODULO  FROM DQS_REC_ROLMODULO AS T1 INNER JOIN DQS_REC_ROLES AS T2
	ON T2.CIA = T1.CIA AND T1.IDROL = T2.ID_ROL
	INNER JOIN DQS_REC_MODULOS AS T3 ON T3.CIA = T1.CIA AND T3.ID_MODULO = T1.IDMODULO
	WHERE T1.IDROL = @STRROL AND T1.CIA = @STRCIA

END
GO

-- ---------------------------------------------------------------- sp_DQS_ActualizaMarcaUsuario
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_ActualizaMarcaUsuario]
	-- Add the parameters for the stored procedure here
	@STRCIA VARCHAR(100),
	@STRUSUARIO VARCHAR(100),
	@INTMARCA INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;


	UPDATE DQS_REC_USUARIOS
	SET MARCA=@INTMARCA,
	FECHA_SESION = GETDATE()
	WHERE  USUARIO = @STRUSUARIO
   
END
GO

-- ---------------------------------------------------------------- sp_opt_ingresalogueo
CREATE OR ALTER PROCEDURE [dbo].[sp_OPT_IngresaLogueo]
	-- Add the parameters for the stored procedure here
	@CIA NVARCHAR(100),
	@USUAPP NVARCHAR(100),
    @IP NVARCHAR(100)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	INSERT INTO [DQS_OPT_LOGUEO]
		([CIA]
		,[USUAPP]
		,[IP]
		,[FECHAACT])
	VALUES
		(@CIA,
		@USUAPP,
		@IP,
		GETDATE());
END
GO

-- ---------------------------------------------------------------- sp_DQS_ActualizaPwd
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_ActualizaPwd]
	-- Add the parameters for the stored procedure here
	@STREMAIL VARCHAR(200),
	@STRPWD VARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	--IDENTIFICA EL USUARIO Y LA COMPAÑIA
	DECLARE @CIA VARCHAR (50)= (SELECT TOP 1 CIA FROM  DQS_REC_USUARIOS WHERE EMAIL = @STREMAIL)
	DECLARE @USU VARCHAR (50)= (SELECT TOP 1 USUARIO FROM  DQS_REC_USUARIOS WHERE EMAIL = @STREMAIL)

	---------------------------------------------------------------------------------------------------
	INSERT INTO [DQS_REC_NOTIFICACIONES]
           ([CIA]
           ,[NOTIFICACION]
           ,[USUARIO]
           ,[FECHA])
     VALUES
           (@CIA
           ,'Contraseña actualizada'
           ,@USU
           ,getdate())
	------------------------------------------------------------------------------------------------
	UPDATE DQS_REC_USUARIOS
	SET CLAVE = @STRPWD,
	FECHA_ACT_PWD =GETDATE(),PWDTEMP=@STRPWD
	WHERE EMAIL = @STREMAIL
	and USUARIO not in (
	'CIFormatosC', 
    'CMFormatosC', 
    'cwformatos'
	)





END
GO

-- ---------------------------------------------------------------- sp_MIP_InsertaFormatoCnt
CREATE OR ALTER PROCEDURE [dbo].[sp_MIP_InsertaFormatoCnt]
	-- Add the parameters for the stored procedure here
           @CIA varchar(50),
           @TIPOID varchar(50),
           @NUMID varchar(50),
           @NIT varchar(5000),
           @OBJ varchar(5000),
		   @ext varchar(100),
		   @email varchar(5000),
		   @fechaformato varchar(5000),
		   @usuario varchar(5000)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

  DECLARE @AÑO VARCHAR(10)=YEAR(GETDATE())
  DECLARE @MES VARCHAR(10)=MONTH(GETDATE())
  DECLARE @DIA VARCHAR(10)=DAY(GETDATE())
  DECLARE @CUENTA VARCHAR(100)=(SELECT COUNT(*) FROM [DQS_MIP_FORMATOS])
  
  IF LEN(@MES)=1   BEGIN   SET @MES='0' + @MES END
  IF LEN(@DIA)=1   BEGIN   SET @DIA='0' + @DIA END

  IF LEN(@CUENTA)=1   BEGIN   SET @CUENTA = '0000000000000' + @CUENTA   END
  IF LEN(@CUENTA)=2   BEGIN   SET @CUENTA = '000000000000' + @CUENTA   END
  IF LEN(@CUENTA)=3   BEGIN   SET @CUENTA = '00000000000' + @CUENTA   END
  IF LEN(@CUENTA)=4   BEGIN   SET @CUENTA = '0000000000' + @CUENTA   END
  IF LEN(@CUENTA)=5   BEGIN   SET @CUENTA = '000000000' + @CUENTA   END
  IF LEN(@CUENTA)=6   BEGIN   SET @CUENTA = '00000000' + @CUENTA   END
  IF LEN(@CUENTA)=7   BEGIN   SET @CUENTA = '0000000' + @CUENTA   END
  IF LEN(@CUENTA)=8   BEGIN   SET @CUENTA = '000000' + @CUENTA   END
  IF LEN(@CUENTA)=9   BEGIN   SET @CUENTA = '00000' + @CUENTA   END
  IF LEN(@CUENTA)=10   BEGIN   SET @CUENTA = '0000' + @CUENTA   END
  IF LEN(@CUENTA)=11   BEGIN   SET @CUENTA = '000' + @CUENTA   END
  IF LEN(@CUENTA)=12   BEGIN   SET @CUENTA = '00' + @CUENTA   END
  IF LEN(@CUENTA)=13   BEGIN   SET @CUENTA = '0' + @CUENTA   END
  IF LEN(@CUENTA)=14   BEGIN   SET @CUENTA = '' + @CUENTA   END
  
  SET @CUENTA=@AÑO+@MES+@DIA+@CUENTA

  INSERT INTO [dbo].[DQS_MIP_FORMATOS]
           ([CIA]
           ,[TIPOID]
           ,[NUMID]
           ,[NIT]
           ,[OBJ]
           ,[RADICADO]
           ,[RUTA]
           ,[FECHA],EMAIL,FECHAFORMATO,USUARIO,Estado)
     VALUES
           (@CIA
           ,@TIPOID
           ,@NUMID
           ,@NIT
           ,@OBJ
           ,@CUENTA
           ,'~/MediaUploader/Formatos/' + @CIA + '/' + @CUENTA + @ext
           ,GETDATE(),@email,@fechaformato,@usuario,'Cargado')

		   select @CUENTA

END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaFormatosCNT
CREATE OR ALTER PROCEDURE [dbo].[sp_MIP_ConsultaFormatosCNT] 
	-- Add the parameters for the stored procedure here
	@CIA VARCHAR(100),
	@TIPO VARCHAR(100),
	@ID VARCHAR(100),
	@NIT VARCHAR(1000),
	@RADICADO VARCHAR(1000),
	@FECHA VARCHAR(100),
	@USUARIO VARCHAR(100)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	 

	declare @rol varchar(50)=(select ROL from DQS_REC_USUARIOS where USUARIO=@USUARIO)

	if @rol='601'
	begin
	select [CIA]
      ,[TIPOID]
      ,[NUMID]
      ,[NIT]
      ,[OBJ]
      ,[RADICADO]
      ,replace([RUTA],'~/','') as RUTA
      ,[FECHA],FECHAFORMATO,'' as Gestion,NOPRESCRIPCION,OBSERVACIONES,ESTADO
	  from [DQS_MIP_FORMATOS]
	WHERE CIA=@CIA AND TIPOID LIKE @TIPO AND NUMID LIKE @ID AND NIT LIKE @NIT
	AND RADICADO LIKE @RADICADO AND LEFT(CAST(FECHA AS DATE),10) LIKE @FECHA
	AND USUARIO LIKE @USUARIO
	end
	else
	begin
	select [CIA]
      ,[TIPOID]
      ,[NUMID]
      ,[NIT]
      ,[OBJ]
      ,[RADICADO]
      ,replace([RUTA],'~/','') as RUTA
      ,[FECHA],FECHAFORMATO,NOPRESCRIPCION,OBSERVACIONES,ESTADO
	  ,'<a onclick="ventana2(''' + RADICADO  +''',''' + case when(NOPRESCRIPCION)is null then '' else NOPRESCRIPCION  end  +''',''' + case when(observaciones)is null then '' else observaciones  end  +''',''' + case when(estado)is null then '' else estado  end  +''',''' + case when(email)is null then '' else email  end  +''')"><img src="img/lapizgrisblanco.png" style="width:20px" /></a>' AS Gestion
	  from [DQS_MIP_FORMATOS]
	WHERE CIA=@CIA AND TIPOID LIKE @TIPO AND NUMID LIKE @ID AND NIT LIKE @NIT
	AND RADICADO LIKE @RADICADO AND LEFT(CAST(FECHA AS DATE),10) LIKE @FECHA
--	AND USUARIO LIKE @USUARIO
	end
    -- Insert statements for procedure here
	
END
GO

-- ---------------------------------------------------------------- sp_MIP_ActualizaFormatoCnt
CREATE OR ALTER PROCEDURE [dbo].[sp_MIP_ActualizaFormatoCnt]
	-- Add the parameters for the stored procedure here
           @CIA varchar(50),
           @mipres varchar(100),
		   @obj varchar(5000),
		   @estado varchar(50),
		   @usuario varchar(100),
		   @radicado varchar(100)
as
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;


	update DQS_MIP_FORMATOS
	set [NOPRESCRIPCION]=@mipres,estado=@estado,[OBSERVACIONES]=@obj
	where cia=@cia	and RADICADO=@radicado


END
GO

-- ---------------------------------------------------------------- sp_DQS_ConsultaProfesionalAñadido
CREATE OR ALTER PROCEDURE dbo.sp_DQS_ConsultaProfesionalAñadido
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

-- ---------------------------------------------------------------- sp_DQS_ConsultaProfesional
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_ConsultaProfesional]
	-- Add the parameters for the stored procedure here
	@cia varchar(50),
	@doc varchar(50),
	@nom varchar(100),
	@prof varchar(100),
	@sede varchar(100),
	@serv varchar(100)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

IF @doc ='' BEGIN SET @doc='%' END
IF @nom ='' BEGIN SET @nom='%' END
IF @prof ='' BEGIN SET @prof='%' END
IF @sede ='' BEGIN SET @sede='%' END
IF @serv ='' BEGIN SET @serv='%' END

    -- Insert statements for procedure here

SELECT [Firma],
       [TIPODOC]
      ,[NoIDENTIFICACION]
      ,[APELLIDOS Y NOMBRES]
      ,[PROFESION]
      ,[SEDE]
      ,[SERVICIO]
      ,[CORREOELECTRONICO]
      ,[CELULAR]
      ,[FActivación]
   
  FROM [dbo].[DQS_MIP_PROFESIONALES]
  WHERE CIA=@cia 
  AND [NoIDENTIFICACION] LIKE '%' + @doc + '%'
  AND [APELLIDOS Y NOMBRES] LIKE '%' + @nom + '%'
  AND [PROFESION] LIKE '%' + @prof + '%'
  AND [SEDE] LIKE '%' + @sede + '%'
  AND [SERVICIO] LIKE '%' + @serv + '%'

END
GO

-- ---------------------------------------------------------------- sp_DQS_AsignaProfesional
CREATE OR ALTER PROCEDURE [dbo].[sp_DQS_AsignaProfesional]
	-- Add the parameters for the stored procedure here
	@CIA VARCHAR(50),
	@ACTA VARCHAR(50),
	@DOC VARCHAR(50)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	INSERT INTO [dbo].[DQS_MIP_PROFESIONALESACTA]
           ([CIA]
           ,[ACTA]
           ,[NoIDENTIFICACION],ESTADO)
     VALUES
           (@CIA
           ,@ACTA
           ,@DOC,'APROBADA Y FIRMADA')

END
GO

-- ---------------------------------------------------------------- sp_DQS_EliminaProfesional
CREATE OR ALTER PROCEDURE sp_DQS_EliminaProfesional
	-- Add the parameters for the stored procedure here
	@CIA VARCHAR(50),
	@ACTA VARCHAR(50),
	@DOC VARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

DELETE FROM DQS_MIP_PROFESIONALESACTA
WHERE CIA=@CIA AND ACTA=@ACTA AND NoIDENTIFICACION=@DOC


END
GO

-- ---------------------------------------------------------------- sp_DQS_ActualizaProfesional
CREATE OR ALTER PROCEDURE [dbo].sp_DQS_ActualizaProfesional
	-- Add the parameters for the stored procedure here
	@CIA VARCHAR(50),
	@ACTA VARCHAR(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	UPDATE DQS_MIP_PROFESIONALESACTA
	SET ESTADO='ENVIADA A EPS'
	WHERE CIA=@CIA AND ACTA=@ACTA

END
GO

-- ---------------------------------------------------------------- sp_DQS_ActualizaJunta
CREATE OR ALTER PROCEDURE [dbo].sp_DQS_ActualizaJunta
	-- Add the parameters for the stored procedure here
	@Cia varchar(50),
	@MIPRES varchar(500),
    @TIPOT varchar(50),
	@CONT varchar(50)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

DELETE FROM DQS_MIP_JUNTASC
WHERE CIA=@Cia AND MIPRES=@MIPRES AND TIPOT=@TIPOT AND CONT=@CONT

INSERT INTO DQS_MIP_JUNTASC
SELECT @Cia,@MIPRES,@TIPOT,@CONT,GETDATE()

END
GO

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesResolverUsuario
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesResolverUsuario
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesSincronizarUsuario
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesSincronizarUsuario
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesConsultar
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesConsultar
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesObtener
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesObtener
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesInsertar
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesInsertar
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesActualizar
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesActualizar
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesCambiarEstado
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesCambiarEstado
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesResumen
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesResumen
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

-- ---------------------------------------------------------------- sp_DQS_MIP_ProfesionalesListas
CREATE OR ALTER PROCEDURE dbo.sp_DQS_MIP_ProfesionalesListas
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

-- ---------------------------------------------------------------- sp_MIP_InsertaContingencia
CREATE OR ALTER PROCEDURE dbo.sp_MIP_InsertaContingencia
    @CIA                             NVARCHAR(50),
    @Usuario                         NVARCHAR(150),
    @Causal                          NVARCHAR(500),
    @CausalNombre                    NVARCHAR(500)  = NULL,
    @CausalDescripcion               NVARCHAR(500)  = NULL,
    @IpsCodigo                       NVARCHAR(100)  = NULL,
    @IpsNombre                       NVARCHAR(500)  = NULL,
    @ProfesionalDocumento            NVARCHAR(100),
    @ProfesionalNombre               NVARCHAR(500)  = NULL,
    @ProfesionalProfesion            NVARCHAR(500)  = NULL,
    @PacienteTipoDoc                 NVARCHAR(10),
    @PacienteNumDoc                  NVARCHAR(100),
    @PacienteNombres                 NVARCHAR(250),
    @PacienteApellidos               NVARCHAR(250),
    @PacienteSexo                    NVARCHAR(20)   = NULL,
    @PacienteFechaNacimiento         DATE           = NULL,
    @PacienteMunicipio               NVARCHAR(200)  = NULL,
    @PacienteTelefono                NVARCHAR(50)   = NULL,
    @PacienteRegimen                 NVARCHAR(100)  = NULL,
    @PacienteFechaAtencion           DATE           = NULL,
    @PacienteHoraAtencion            NVARCHAR(10)   = NULL,
    @PacienteAmbitoAtencionCodigo    NVARCHAR(20)   = NULL,
    @PacienteAmbitoAtencionNombre    NVARCHAR(200)  = NULL,
    @DiagnosticoCodigo               NVARCHAR(20),
    @DiagnosticoDescripcion          NVARCHAR(500)  = NULL,
    @DiagnosticoRel1Codigo           NVARCHAR(20)   = NULL,
    @DiagnosticoRel1Descripcion      NVARCHAR(500)  = NULL,
    @DiagnosticoRel2Codigo           NVARCHAR(20)   = NULL,
    @DiagnosticoRel2Descripcion      NVARCHAR(500)  = NULL,
    @TecnologiasJson                 NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Anio VARCHAR(10) = CONVERT(VARCHAR(10), YEAR(GETDATE()));
    DECLARE @Mes  VARCHAR(10) = CONVERT(VARCHAR(10), MONTH(GETDATE()));
    DECLARE @Dia  VARCHAR(10) = CONVERT(VARCHAR(10), DAY(GETDATE()));
    DECLARE @Cuenta VARCHAR(100) = CONVERT(VARCHAR(100), (SELECT COUNT(*) FROM dbo.DQS_MIP_CONTINGENCIAS));

    IF LEN(@Mes) = 1 SET @Mes = '0' + @Mes;
    IF LEN(@Dia) = 1 SET @Dia = '0' + @Dia;

    SET @Cuenta = RIGHT('00000000000000' + @Cuenta, 14);

    DECLARE @Radicado VARCHAR(50) = @Anio + @Mes + @Dia + @Cuenta;

    BEGIN TRANSACTION;

    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS
        (CIA, RADICADO, USUARIO, CAUSAL, CAUSAL_NOMBRE, CAUSAL_DESCRIPCION, IPS_CODIGO, IPS_NOMBRE,
         PROFESIONAL_DOCUMENTO, PROFESIONAL_NOMBRE, PROFESIONAL_PROFESION,
         PACIENTE_TIPODOC, PACIENTE_NUMDOC, PACIENTE_NOMBRES, PACIENTE_APELLIDOS,
         PACIENTE_SEXO, PACIENTE_FECHANACIMIENTO, PACIENTE_MUNICIPIO, PACIENTE_TELEFONO, PACIENTE_REGIMEN,
         PACIENTE_FECHAATENCION, PACIENTE_HORAATENCION, PACIENTE_AMBITOATENCION_CODIGO, PACIENTE_AMBITOATENCION_NOMBRE,
         DIAGNOSTICO_CODIGO, DIAGNOSTICO_DESCRIPCION,
         DIAGNOSTICO_REL1_CODIGO, DIAGNOSTICO_REL1_DESCRIPCION, DIAGNOSTICO_REL2_CODIGO, DIAGNOSTICO_REL2_DESCRIPCION)
    VALUES
        (@CIA, @Radicado, @Usuario, @Causal, @CausalNombre, @CausalDescripcion, @IpsCodigo, @IpsNombre,
         @ProfesionalDocumento, @ProfesionalNombre, @ProfesionalProfesion,
         @PacienteTipoDoc, @PacienteNumDoc, @PacienteNombres, @PacienteApellidos,
         @PacienteSexo, @PacienteFechaNacimiento, @PacienteMunicipio, @PacienteTelefono, @PacienteRegimen,
         @PacienteFechaAtencion, @PacienteHoraAtencion, @PacienteAmbitoAtencionCodigo, @PacienteAmbitoAtencionNombre,
         @DiagnosticoCodigo, @DiagnosticoDescripcion,
         @DiagnosticoRel1Codigo, @DiagnosticoRel1Descripcion, @DiagnosticoRel2Codigo, @DiagnosticoRel2Descripcion);

    -- Una fila por elemento del arreglo; ORDEN = posición en el arreglo (1..n).
    INSERT INTO dbo.DQS_MIP_CONTINGENCIAS_TEC
        (CIA, RADICADO, ORDEN, TIPOTECNOLOGIA, TEC_NOMBRE, TEC_CODIGO, TEC_PRINCIPIOACTIVO, TEC_FORMAFARMACEUTICA,
         TEC_VIAADMINISTRACION, TEC_DOSIS, POS_TIPOPRESTACION, POS_CANTIDADFORMULADA, POS_CANTIDADFORMULADAUNIDAD,
         POS_CADAFRECUENCIA, POS_FRECUENCIA, POS_DURACIONTRATAMIENTO, POS_DURACIONUNIDAD, POS_INDICACIONESPECIAL,
         POS_CANTIDADTOTAL, POS_INDICADORRECURRENCIA, POS_INDICACIONESADICIONALES, JUSTIFICACION_TECNICA)
    SELECT @CIA, @Radicado, CONVERT(INT, j.[key]) + 1, t.TipoTecnologia, t.TecNombre, t.TecCodigo, t.TecPrincipioActivo,
           t.TecFormaFarmaceutica, t.TecViaAdministracion, t.TecDosis, t.PosTipoPrestacion, t.PosCantidadFormulada,
           t.PosCantidadFormuladaUnidad, t.PosCadaFrecuencia, t.PosFrecuencia, t.PosDuracionTratamiento, t.PosDuracionUnidad,
           t.PosIndicacionEspecial, t.PosCantidadTotal, t.PosIndicadorRecurrencia, t.PosIndicacionesAdicionales,
           t.JustificacionTecnica
      FROM OPENJSON(@TecnologiasJson) j
     CROSS APPLY OPENJSON(j.[value]) WITH
        (
            TipoTecnologia              NVARCHAR(50)   '$.TipoTecnologia',
            TecNombre                   NVARCHAR(500)  '$.TecNombre',
            TecCodigo                   NVARCHAR(100)  '$.TecCodigo',
            TecPrincipioActivo          NVARCHAR(1000) '$.TecPrincipioActivo',
            TecFormaFarmaceutica        NVARCHAR(200)  '$.TecFormaFarmaceutica',
            TecViaAdministracion        NVARCHAR(200)  '$.TecViaAdministracion',
            TecDosis                    NVARCHAR(200)  '$.TecDosis',
            PosTipoPrestacion           NVARCHAR(20)   '$.PosTipoPrestacion',
            PosCantidadFormulada        NVARCHAR(100)  '$.PosCantidadFormulada',
            PosCantidadFormuladaUnidad  NVARCHAR(50)   '$.PosCantidadFormuladaUnidad',
            PosCadaFrecuencia           NVARCHAR(20)   '$.PosCadaFrecuencia',
            PosFrecuencia               NVARCHAR(20)   '$.PosFrecuencia',
            PosDuracionTratamiento      NVARCHAR(20)   '$.PosDuracionTratamiento',
            PosDuracionUnidad           NVARCHAR(20)   '$.PosDuracionUnidad',
            PosIndicacionEspecial       NVARCHAR(50)   '$.PosIndicacionEspecial',
            PosCantidadTotal            NVARCHAR(100)  '$.PosCantidadTotal',
            PosIndicadorRecurrencia     NVARCHAR(5)    '$.PosIndicadorRecurrencia',
            PosIndicacionesAdicionales  NVARCHAR(500)  '$.PosIndicacionesAdicionales',
            JustificacionTecnica        NVARCHAR(MAX)  '$.JustificacionTecnica'
        ) t
     ORDER BY CONVERT(INT, j.[key]);

    COMMIT TRANSACTION;

    SELECT @Radicado AS RADICADO;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaTokenAfiliados
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ConsultaTokenAfiliados
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) TOKEN
      FROM dbo.DQS_MIP_AFILIADOS_TOKEN
     ORDER BY FECHACREACION DESC, ID DESC;
END
GO

-- ---------------------------------------------------------------- sp_MIP_InsertaTokenAfiliados
CREATE OR ALTER PROCEDURE dbo.sp_MIP_InsertaTokenAfiliados
    @Token   NVARCHAR(MAX),
    @Usuario NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.DQS_MIP_AFILIADOS_TOKEN (TOKEN, USUARIO) VALUES (@Token, @Usuario);
    SELECT SCOPE_IDENTITY() AS ID;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ActualizaContingenciaArchivos
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ActualizaContingenciaArchivos
    @CIA                NVARCHAR(50),
    @Radicado            NVARCHAR(50),
    @FirmaRuta           NVARCHAR(500)  = NULL,
    @SoportesRutasJson   NVARCHAR(MAX)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DQS_MIP_CONTINGENCIAS
       SET FIRMA_RUTA          = @FirmaRuta,
           SOPORTES_RUTAS_JSON = @SoportesRutasJson
     WHERE CIA = @CIA
       AND RADICADO = @Radicado;

    SELECT @@ROWCOUNT AS FILASACTUALIZADAS;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaContingencias
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ConsultaContingencias
    @CIA                    NVARCHAR(50),
    @Usuario                NVARCHAR(150) = NULL,
    @Radicado               NVARCHAR(50)  = NULL,
    @Causal                 NVARCHAR(500) = NULL,
    @IpsCodigo              NVARCHAR(100) = NULL,
    @ProfesionalDocumento   NVARCHAR(100) = NULL,
    @PacienteNumDoc         NVARCHAR(100) = NULL,
    @TipoTecnologia         NVARCHAR(50)  = NULL,
    @FechaInicio            DATE          = NULL,
    @FechaFin               DATE          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario              = NULLIF(LTRIM(RTRIM(ISNULL(@Usuario, ''))), '');
    SET @Radicado             = NULLIF(LTRIM(RTRIM(ISNULL(@Radicado, ''))), '');
    SET @Causal               = NULLIF(LTRIM(RTRIM(ISNULL(@Causal, ''))), '');
    SET @IpsCodigo            = NULLIF(LTRIM(RTRIM(ISNULL(@IpsCodigo, ''))), '');
    SET @ProfesionalDocumento = NULLIF(LTRIM(RTRIM(ISNULL(@ProfesionalDocumento, ''))), '');
    SET @PacienteNumDoc       = NULLIF(LTRIM(RTRIM(ISNULL(@PacienteNumDoc, ''))), '');
    SET @TipoTecnologia       = NULLIF(LTRIM(RTRIM(ISNULL(@TipoTecnologia, ''))), '');

    SELECT  c.ID, c.CIA, c.RADICADO, c.ESTADO, c.USUARIO, c.FECHACREACION,
            c.CAUSAL, c.CAUSAL_NOMBRE, c.CAUSAL_DESCRIPCION,
            c.IPS_CODIGO, c.IPS_NOMBRE,
            c.PROFESIONAL_DOCUMENTO, c.PROFESIONAL_NOMBRE, c.PROFESIONAL_PROFESION,
            c.PACIENTE_TIPODOC, c.PACIENTE_NUMDOC, c.PACIENTE_NOMBRES, c.PACIENTE_APELLIDOS,
            c.PACIENTE_SEXO, c.PACIENTE_FECHANACIMIENTO, c.PACIENTE_MUNICIPIO, c.PACIENTE_TELEFONO, c.PACIENTE_REGIMEN,
            c.PACIENTE_FECHAATENCION, c.PACIENTE_HORAATENCION, c.PACIENTE_AMBITOATENCION_CODIGO, c.PACIENTE_AMBITOATENCION_NOMBRE,
            c.DIAGNOSTICO_CODIGO, c.DIAGNOSTICO_DESCRIPCION,
            c.DIAGNOSTICO_REL1_CODIGO, c.DIAGNOSTICO_REL1_DESCRIPCION, c.DIAGNOSTICO_REL2_CODIGO, c.DIAGNOSTICO_REL2_DESCRIPCION,
            c.SOPORTES_RUTAS_JSON, c.FIRMA_RUTA, c.FIRMAPENDIENTEFEA
      FROM  dbo.DQS_MIP_CONTINGENCIAS c
     WHERE  c.CIA = @CIA
       AND (@Usuario              IS NULL OR c.USUARIO = @Usuario)
       AND (@Radicado             IS NULL OR c.RADICADO = @Radicado)
       AND (@Causal               IS NULL OR c.CAUSAL = @Causal)
       AND (@IpsCodigo            IS NULL OR c.IPS_CODIGO = @IpsCodigo)
       AND (@ProfesionalDocumento IS NULL OR c.PROFESIONAL_DOCUMENTO = @ProfesionalDocumento)
       AND (@PacienteNumDoc       IS NULL OR c.PACIENTE_NUMDOC = @PacienteNumDoc)
       AND (@TipoTecnologia       IS NULL OR EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS_TEC t
                                                      WHERE t.CIA = c.CIA AND t.RADICADO = c.RADICADO
                                                        AND t.TIPOTECNOLOGIA = @TipoTecnologia))
       AND (@FechaInicio          IS NULL OR c.FECHACREACION >= @FechaInicio)
       AND (@FechaFin             IS NULL OR c.FECHACREACION < DATEADD(DAY, 1, @FechaFin))
     ORDER BY c.FECHACREACION DESC;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaContingenciaTecnologias
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ConsultaContingenciaTecnologias
    @CIA            NVARCHAR(50),
    @RadicadosJson  NVARCHAR(MAX)   -- arreglo JSON de radicados, ej. ["2026100200000000000000"]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  t.RADICADO, t.ORDEN, t.TIPOTECNOLOGIA,
            t.TEC_NOMBRE, t.TEC_CODIGO, t.TEC_PRINCIPIOACTIVO, t.TEC_FORMAFARMACEUTICA, t.TEC_VIAADMINISTRACION, t.TEC_DOSIS,
            t.POS_TIPOPRESTACION, t.POS_CANTIDADFORMULADA, t.POS_CANTIDADFORMULADAUNIDAD, t.POS_CADAFRECUENCIA, t.POS_FRECUENCIA,
            t.POS_DURACIONTRATAMIENTO, t.POS_DURACIONUNIDAD, t.POS_INDICACIONESPECIAL, t.POS_CANTIDADTOTAL, t.POS_INDICADORRECURRENCIA,
            t.POS_INDICACIONESADICIONALES, t.JUSTIFICACION_TECNICA
      FROM  dbo.DQS_MIP_CONTINGENCIAS_TEC t
     WHERE  t.CIA = @CIA
       AND  t.RADICADO IN (SELECT [value] FROM OPENJSON(@RadicadosJson))
     ORDER BY t.RADICADO, t.ORDEN;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaParametricas
CREATE OR ALTER PROCEDURE dbo.sp_MIP_ConsultaParametricas
    @Grupo NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  GRUPO, CODIGO, VALOR, ORDEN
      FROM  dbo.DQS_MIP_CONT_PARAMETRICAS
     WHERE  ACTIVO = 1
       AND (@Grupo IS NULL OR GRUPO = @Grupo)
     ORDER BY GRUPO, ORDEN;
END
GO

-- ---------------------------------------------------------------- sp_MIP_InsertaParametrica
CREATE OR ALTER PROCEDURE dbo.sp_MIP_InsertaParametrica
    @Grupo  NVARCHAR(50),
    @Codigo NVARCHAR(50),
    @Valor  NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONT_PARAMETRICAS WHERE GRUPO = @Grupo AND CODIGO = @Codigo)
    BEGIN
        SELECT 0 AS RESULTADO, 'Ya existe una causal con ese nombre.' AS MENSAJE;
        RETURN;
    END

    DECLARE @Orden INT = ISNULL((SELECT MAX(ORDEN) FROM dbo.DQS_MIP_CONT_PARAMETRICAS WHERE GRUPO = @Grupo), 0) + 1;

    INSERT INTO dbo.DQS_MIP_CONT_PARAMETRICAS (GRUPO, CODIGO, VALOR, ORDEN, ACTIVO)
    VALUES (@Grupo, @Codigo, @Valor, @Orden, 1);

    SELECT 1 AS RESULTADO, 'Valor creado correctamente.' AS MENSAJE;
END
GO

-- ---------------------------------------------------------------- sp_MIP_ConsultaCiaParametros
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

-- ---------------------------------------------------------------- sp_MIP_CrearUsuarioFormatos
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

-- ---------------------------------------------------------------- sp_MIP_CambiarClaveUsuario
CREATE OR ALTER PROCEDURE dbo.sp_MIP_CambiarClaveUsuario
    @Usuario      VARCHAR(150),
    @ClaveActual  VARCHAR(50),
    @ClaveNueva   VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Usuario = LTRIM(RTRIM(ISNULL(@Usuario, '')));

    IF @Usuario = '' OR ISNULL(@ClaveActual, '') = '' OR ISNULL(@ClaveNueva, '') = ''
        BEGIN SELECT 0 AS RESULTADO, 'Usuario, clave actual y clave nueva son obligatorios.' AS MENSAJE; RETURN; END

    IF @Usuario IN ('CIFormatosC', 'CMFormatosC', 'cwformatos')
        BEGIN SELECT 0 AS RESULTADO, 'Este usuario es compartido; su clave la administra soporte.' AS MENSAJE; RETURN; END

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = @Usuario AND CLAVE = @ClaveActual AND ESTADO = 'Activo')
        BEGIN SELECT 0 AS RESULTADO, 'Contraseña actual o temporal incorrecta.' AS MENSAJE; RETURN; END

    DECLARE @Cia VARCHAR(150) = (SELECT TOP 1 CIA FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = @Usuario);

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.DQS_REC_NOTIFICACIONES (CIA, NOTIFICACION, USUARIO, FECHA)
        VALUES (@Cia, 'Contraseña actualizada', @Usuario, GETDATE());

        UPDATE dbo.DQS_REC_USUARIOS
        SET CLAVE = @ClaveNueva,
            FECHA_ACT_PWD = GETDATE(),
            PWDTEMP = '',
            FechaPwd = CASE
                WHEN TRY_CONVERT(date, FechaPwd) IS NULL THEN FechaPwd
                ELSE CONVERT(varchar(10),
                        DATEADD(day, 90, CASE WHEN TRY_CONVERT(date, FechaPwd) > CAST(GETDATE() AS date)
                                              THEN TRY_CONVERT(date, FechaPwd) ELSE CAST(GETDATE() AS date) END), 23)
            END
        WHERE USUARIO = @Usuario;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, 'No fue posible actualizar la contraseña. Intente nuevamente.' AS MENSAJE;
        RETURN;
    END CATCH

    SELECT 1 AS RESULTADO, 'Contraseña actualizada.' AS MENSAJE;
END
GO

-- ---------------------------------------------------------------- sp_MIP_EliminaFormatoCnt
CREATE OR ALTER PROCEDURE dbo.sp_MIP_EliminaFormatoCnt
    @CIA        VARCHAR(50),
    @Radicado   VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@CIA, '') = '' OR ISNULL(@Radicado, '') = ''
    BEGIN
        SELECT 0 AS RESULTADO, 'CIA y Radicado son obligatorios.' AS MENSAJE;
        RETURN;
    END

    DELETE FROM dbo.DQS_MIP_FORMATOS WHERE CIA = @CIA AND RADICADO = @Radicado;

    IF @@ROWCOUNT > 0
        SELECT 1 AS RESULTADO, 'Formato eliminado.' AS MENSAJE;
    ELSE
        SELECT 0 AS RESULTADO, 'No se encontró el formato para eliminar.' AS MENSAJE;
END
GO

-- ---------------------------------------------------------------- sp_MIP_EliminaContingencia
CREATE OR ALTER PROCEDURE dbo.sp_MIP_EliminaContingencia
    @CIA        VARCHAR(50),
    @Radicado   VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@CIA, '') = '' OR ISNULL(@Radicado, '') = ''
    BEGIN
        SELECT 0 AS RESULTADO, 'CIA y Radicado son obligatorios.' AS MENSAJE;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.DQS_MIP_CONTINGENCIAS WHERE CIA = @CIA AND RADICADO = @Radicado)
    BEGIN
        SELECT 0 AS RESULTADO, 'No se encontró la contingencia para eliminar.' AS MENSAJE;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.DQS_MIP_CONTINGENCIAS_TEC WHERE CIA = @CIA AND RADICADO = @Radicado;
        DELETE FROM dbo.DQS_MIP_CONTINGENCIAS WHERE CIA = @CIA AND RADICADO = @Radicado;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS RESULTADO, 'No fue posible eliminar la contingencia. Intente nuevamente.' AS MENSAJE;
        RETURN;
    END CATCH

    SELECT 1 AS RESULTADO, 'Contingencia eliminada.' AS MENSAJE;
END
GO
