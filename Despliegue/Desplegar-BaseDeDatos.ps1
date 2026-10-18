<#
.SYNOPSIS
    Crea y carga la base de datos de produccion de Formatos + Juntas (multi-CIA) en UN solo paso.

.DESCRIPTION
    Ejecuta en orden: crear la base (con la intercalacion Modern_Spanish_CI_AS), esquema (tablas y procedimientos), semillas
    (clientes, roles y modulos, parametros por CIA, tokens SISPRO de 800006850), el usuario cwhospital, la copia de datos desde la
    base de origen (catalogos, tokens de Sanitas y los usuarios cwformatos y cwjuntas), los ajustes posteriores y una verificacion.
    Se detiene en el primer error. Es repetible: lo que ya existe se omite.

    Requisitos en el equipo donde se ejecuta: sqlcmd (SQL Server command line utilities) y, para la copia, el programa
    CopiarCatalogos (ver -ExeCopia) o el SDK de .NET para ejecutarlo desde el codigo fuente.

.EXAMPLE
    # SQL Server con usuario y clave; la clave de SQL va en la variable de entorno SQLCMDPASSWORD para no dejarla en la linea de comandos
    $env:SQLCMDPASSWORD = '<clave de sa>'
    .\Desplegar-BaseDeDatos.ps1 -Servidor 'SRV-DB01' -Usuario 'sa' `
        -OrigenCadena 'Server=45.169.253.168;Database=DQUALITY;User Id=...;Password=...;TrustServerCertificate=True' `
        -ClaveCwhospital '<clave de cwhospital, min. 8>' -NuevaClaveCwjuntas '<12-20 caracteres con mayuscula, minuscula, numero y simbolo>'

.EXAMPLE
    # Autenticacion de Windows y solo verificar lo ya desplegado
    .\Desplegar-BaseDeDatos.ps1 -Servidor 'SRV-DB01' -AutenticacionWindows -OrigenCadena $env:CONTINGENCIA_ORIGEN -SoloVerificar
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Servidor,
    [string] $Base = 'DQS_MIPRES',
    [string] $Usuario,
    [switch] $AutenticacionWindows,
    [string] $OrigenCadena = $env:CONTINGENCIA_ORIGEN,
    [string] $ClaveCwhospital,
    [string] $NuevaClaveCwjuntas,
    [string] $ExeCopia,
    [switch] $SaltarCopia,
    [switch] $SoloVerificar
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$sql = Join-Path $raiz 'Contingencia.Infrastructure\Sql\Produccion'
$herramienta = Join-Path $raiz 'Herramientas\CopiarCatalogos'

function Paso($texto) { Write-Host ''; Write-Host "== $texto" -ForegroundColor Cyan }
function Fallar($texto) { Write-Host "ERROR: $texto" -ForegroundColor Red; exit 1 }

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { Fallar 'No se encontro sqlcmd. Instalar "Microsoft ODBC Driver + sqlcmd utilities" o usar un equipo que lo tenga.' }
if (-not $AutenticacionWindows -and [string]::IsNullOrWhiteSpace($Usuario)) { Fallar 'Indique -Usuario (y SQLCMDPASSWORD) o -AutenticacionWindows.' }
if (-not $AutenticacionWindows -and [string]::IsNullOrWhiteSpace($env:SQLCMDPASSWORD)) { Fallar 'Falta la clave de SQL: defina $env:SQLCMDPASSWORD (asi no queda en la linea de comandos).' }
if ($Base -notmatch '^[A-Za-z0-9_]+$') { Fallar 'El nombre de la base solo puede tener letras, numeros y guion bajo.' }

function InvocarSql([string[]] $argumentos) {
    $auth = @(if ($AutenticacionWindows) { '-E' } else { '-U'; $Usuario })
    & sqlcmd -S $Servidor @auth -C -f65001 -b @argumentos
    if ($LASTEXITCODE -ne 0) { Fallar "sqlcmd termino con codigo $LASTEXITCODE (ver el mensaje de arriba)." }
}
function Escalar([string] $consulta) {
    $auth = @(if ($AutenticacionWindows) { '-E' } else { '-U'; $Usuario })
    $r = & sqlcmd -S $Servidor -d $Base @auth -C -f65001 -b -h -1 -W -Q "SET NOCOUNT ON; $consulta"
    if ($LASTEXITCODE -ne 0) { Fallar "No se pudo consultar: $consulta" }
    return (($r | Out-String).Trim())
}
function Archivo([string] $nombre, [string[]] $extra = @()) {
    $ruta = Join-Path $sql $nombre
    if (-not (Test-Path $ruta)) { Fallar "No existe $ruta" }
    Write-Host "   $nombre"
    InvocarSql (@('-d', $Base, '-i', $ruta) + $extra)
}

# --- validaciones de los datos de entrada -------------------------------------------------------------------------
if (-not $SoloVerificar) {
    if ([string]::IsNullOrWhiteSpace($ClaveCwhospital) -or $ClaveCwhospital.Length -lt 8) { Fallar '-ClaveCwhospital es obligatoria (minimo 8 caracteres).' }
    if ($NuevaClaveCwjuntas) {
        $c = $NuevaClaveCwjuntas
        if ($c.Length -lt 12 -or $c.Length -gt 20 -or $c -notmatch '[a-z]' -or $c -notmatch '[A-Z]' -or $c -notmatch '\d' -or $c -notmatch '[\W_]' -or $c -match '\s' -or $c.Contains('_TEMPORAL')) {
            Fallar '-NuevaClaveCwjuntas debe tener 12 a 20 caracteres, mayuscula, minuscula, numero y simbolo, sin espacios.'
        }
    }
}
if (-not $SaltarCopia -and [string]::IsNullOrWhiteSpace($OrigenCadena)) { Fallar 'Falta -OrigenCadena (cadena de conexion de DQUALITY) o la variable CONTINGENCIA_ORIGEN. Con -SaltarCopia se omite la copia.' }

# --- la herramienta de copia ---------------------------------------------------------------------------------------
function Copia([string[]] $argumentos) {
    $env:CONTINGENCIA_ORIGEN = $OrigenCadena
    $autenticacion = if ($AutenticacionWindows) { 'Integrated Security=True' } else { "User Id=$Usuario;Password=$($env:SQLCMDPASSWORD)" }
    $env:CONTINGENCIA_DESTINO = "Server=$Servidor;Database=$Base;$autenticacion;TrustServerCertificate=True;Connect Timeout=30"
    $comun = @('--tokens', '890330031', '--usuarios', '890330031', '--solo-usuarios', 'cwformatos,cwjuntas')
    if ($ExeCopia) { & $ExeCopia @comun @argumentos } else { & dotnet run -c Release --project $herramienta -- @comun @argumentos }
    if ($LASTEXITCODE -ne 0) { Fallar 'La herramienta de copia termino con errores (ver arriba).' }
}

Write-Host "Servidor: $Servidor | Base: $Base" -ForegroundColor White

if ($SoloVerificar) {
    Paso 'Verificando conteos origen/destino'
    Copia @('--verificar')
    Write-Host ''; Write-Host 'VERIFICACION TERMINADA' -ForegroundColor Green
    exit 0
}

Paso '1/8 Crear la base de datos (intercalacion Modern_Spanish_CI_AS)'
Write-Host '   00_CrearBaseDeDatos.sql'
InvocarSql @('-d', 'master', '-i', (Join-Path $sql '00_CrearBaseDeDatos.sql'), '-v', "NOMBRE_BD=$Base")
$intercalacion = Escalar 'SELECT CONVERT(varchar(128), DATABASEPROPERTYEX(DB_NAME(), ''Collation''))'
if ($intercalacion -ne 'Modern_Spanish_CI_AS') { Fallar "La base existe pero con intercalacion '$intercalacion'; debe ser Modern_Spanish_CI_AS. Eliminela y vuelva a ejecutar." }

Paso '2/8 Esquema: tablas y procedimientos'
Archivo '01_Tablas.sql'
Archivo '02_ProcedimientosAlmacenados.sql'
Archivo '02b_ProcedimientoFiltrosDelgado.sql'

Paso '3/8 Semillas: clientes, roles y modulos, parametros por CIA'
Archivo '03_Semilla_Clientes.sql'
Archivo '04_Semilla_Seguridad.sql'
Archivo '04b_Semilla_ParametrosCia.sql'

Paso '4/8 Tokens SISPRO de 800006850 (contiene secretos)'
Archivo '05_Semilla_Tokens_SISPRO_800006850.sql'

Paso '5/8 Usuario cwhospital'
Archivo '06_Usuario_cwhospital.sql' @('-v', "CLAVE_CWHOSPITAL=$ClaveCwhospital")

if (-not $SaltarCopia) {
    Paso '6/8 Copia de datos desde el origen (catalogos, tokens de Sanitas, cwformatos y cwjuntas)'
    Copia @()
    Paso '7/8 Ajustes de los usuarios copiados'
    Archivo '07_Ajustes_UsuariosCopiados.sql'
}
else {
    Write-Host ''; Write-Host '6/8 y 7/8 omitidos (-SaltarCopia)' -ForegroundColor Yellow
}

if ($NuevaClaveCwjuntas -and -not $SaltarCopia) {
    Paso 'Cambiando la clave fija de cwjuntas'
    $actual = Escalar "SELECT CLAVE FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = 'cwjuntas'"
    if ([string]::IsNullOrWhiteSpace($actual)) { Fallar 'cwjuntas no esta en la base (¿la copia se omitio o fallo?).' }
    $temporal = Join-Path $env:TEMP ("cwjuntas_" + [guid]::NewGuid().ToString('N') + '.sql')
    try {
        $actualSql = $actual.Replace("'", "''"); $nuevaSql = $NuevaClaveCwjuntas.Replace("'", "''")
        Set-Content -Path $temporal -Encoding UTF8 -Value "EXEC dbo.sp_MIP_CambiarClaveUsuario @Usuario = 'cwjuntas', @ClaveActual = '$actualSql', @ClaveNueva = '$nuevaSql';"
        $salida = & sqlcmd -S $Servidor -d $Base @(if ($AutenticacionWindows) { '-E' } else { '-U'; $Usuario }) -C -f65001 -b -h -1 -W -i $temporal
        if ($LASTEXITCODE -ne 0 -or ($salida | Out-String) -notmatch '^\s*1\s') { Fallar "No se pudo cambiar la clave de cwjuntas: $($salida | Out-String)" }
        Write-Host '   clave de cwjuntas actualizada'
    }
    finally { if (Test-Path $temporal) { Remove-Item $temporal -Force } }
}

Paso '8/8 Verificacion final'
if (-not $SaltarCopia) { Copia @('--verificar') }
$resumen = Escalar "SELECT 'tablas=' + CAST((SELECT COUNT(*) FROM sys.tables) AS varchar) + ' | procedimientos=' + CAST((SELECT COUNT(*) FROM sys.procedures) AS varchar) + ' | usuarios=' + CAST((SELECT COUNT(*) FROM dbo.DQS_REC_USUARIOS) AS varchar) + ' | clientes=' + CAST((SELECT COUNT(DISTINCT CIA) FROM dbo.DQS_REC_CLIENTES) AS varchar)"
Write-Host "   $resumen"
$esperado = Escalar "SELECT CASE WHEN (SELECT COUNT(*) FROM sys.tables) = 24 AND (SELECT COUNT(*) FROM dbo.DQS_REC_USUARIOS) = 3 THEN 'ok' ELSE 'revisar' END"
if ($esperado -ne 'ok') { Write-Host 'AVISO: se esperaban 24 tablas y 3 usuarios (cwformatos, cwjuntas, cwhospital); revise el resumen.' -ForegroundColor Yellow }

$sigueFija = Escalar "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.DQS_REC_USUARIOS WHERE USUARIO = 'cwjuntas' AND CLAVE = 'juntas2023**') THEN 'si' ELSE 'no' END"
if ($sigueFija -eq 'si') {
    Write-Host 'AVISO: cwjuntas conserva la clave fija del sistema anterior (conocida, esta escrita en el codigo de un procedimiento del legado). Cambiela: use -NuevaClaveCwjuntas o EXEC dbo.sp_MIP_CambiarClaveUsuario.' -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'BASE DE DATOS DESPLEGADA' -ForegroundColor Green
Write-Host "Siguiente: apuntar la aplicacion a esta base (ConnectionStrings__DQUALITY) y abrir https://<subdominio>/salud (debe responder 200)."
