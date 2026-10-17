-- 00_CrearBaseDeDatos.sql - crea la base de datos nueva de produccion (Formatos + Juntas, multi-CIA). Ejecutar contra la base "master".
--
-- IMPORTANTE - INTERCALACION (collation): DQUALITYREC y DQUALITY usan Modern_Spanish_CI_AS en la base y en todas sus columnas de
-- texto. La base nueva DEBE crearse con la misma: los scripts 01..06 no declaran COLLATE por columna (heredan la de la base), y de
-- ella dependen las comparaciones y busquedas de la app (sin distinguir mayusculas, con acentos, p. ej. WHERE Regimen = 'Contributivo'
-- o LIKE sobre los catalogos). Con otra intercalacion la copia de datos con CopiarCatalogos se rechaza ("el id de configuracion
-- regional no coincide") y el comportamiento del login y de las busquedas puede cambiar.
--
-- El nombre de la base llega como variable de sqlcmd (la pasa Despliegue\Desplegar-BaseDeDatos.ps1; a mano:
--   sqlcmd -S <servidor> -d master -C -f65001 -b -v NOMBRE_BD="DQS_MIPRES" -i 00_CrearBaseDeDatos.sql ).
-- Si el servidor ya trae esa intercalacion por defecto, el COLLATE es inofensivo. Archivos de datos/log: los del servidor por defecto.

IF DB_ID(N'$(NOMBRE_BD)') IS NULL
BEGIN
    CREATE DATABASE [$(NOMBRE_BD)] COLLATE Modern_Spanish_CI_AS;
END
GO

SELECT name AS base, collation_name AS intercalacion FROM sys.databases WHERE name = N'$(NOMBRE_BD)';
GO
