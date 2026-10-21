<#
.SYNOPSIS
  Agrega al sitio de IIS las variables de entorno de Azure Blob Storage que necesita Contingencia.Web desde el commit 5edfeb7.

.DESCRIPTION
  Juntas guarda y lee sus archivos SIEMPRE en Azure Blob Storage; Formatos y Nueva Contingencia lo hacen para las CIA de
  Almacenamiento:Azure:CiasFormatosAzure / CiasContingenciasAzure (por defecto solo 890330031). Sin la cadena de conexion,
  esas pantallas fallan al abrir o guardar archivos. Las demas CIA (p. ej. el hospital 800006850) siguen en disco.

  Solo toca las variables de Azure: no hace falta volver a escribir la clave de SQL ni las de Turnstile. La cadena de conexion
  NO queda escrita en ningun archivo: se pasa por parametro. Ejecutar en el servidor IIS, como administrador.
  Con -SoloMostrar no cambia nada (el secreto sale enmascarado).

.EXAMPLE
  .\Configurar-AzureIIS.ps1 -Sitio 'Formatosct' -AzureConnectionString '<cadena de conexion de la cuenta de almacenamiento>'
#>
param(
    [Parameter(Mandatory)] [string] $Sitio,
    [Parameter(Mandatory)] [string] $AzureConnectionString,
    [string] $ContenedorJuntas = 'juntas',
    [string] $ContenedorFormatos = 'formatosct',
    [switch] $SoloMostrar
)
$ErrorActionPreference = 'Stop'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
if (-not $SoloMostrar -and -not (Test-Path $appcmd)) { throw 'No se encontro appcmd.exe: ejecute este script en el servidor IIS.' }
foreach ($v in @($AzureConnectionString, $ContenedorJuntas, $ContenedorFormatos)) {
    if ($v -and $v -match "['""]") { throw 'Ningun valor puede contener comillas simples ni dobles.' }
}
if ($AzureConnectionString -notmatch 'AccountName=[^;]+;.*AccountKey=') { throw 'La cadena de conexion no parece valida (debe incluir AccountName= y AccountKey=).' }

# nombre -> (valor, secreto?)
$vars = [ordered]@{
    'Almacenamiento__Azure__ConnectionString'    = @($AzureConnectionString, $true)
    'Almacenamiento__Azure__Contenedor'          = @($ContenedorJuntas, $false)
    'Almacenamiento__Azure__ContenedorFormatos'  = @($ContenedorFormatos, $false)
}
$seccion = 'system.webServer/aspNetCore'
foreach ($nombre in $vars.Keys) {
    $valor, $secreto = $vars[$nombre]
    $mostrar = if ($secreto) { '********' } else { $valor }
    if ($SoloMostrar) { "[simulacion] $nombre = $mostrar"; continue }
    & $appcmd set config $Sitio "-section:$seccion" "/-environmentVariables.[name='$nombre']" /commit:apphost 2>&1 | Out-Null   # quita la anterior (si no existe, no importa)
    & $appcmd set config $Sitio "-section:$seccion" "/+environmentVariables.[name='$nombre',value='$valor']" /commit:apphost | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudo definir $nombre" }
    "ok  $nombre = $mostrar"
}
if ($SoloMostrar) { '(No se cambio nada.)' } else { 'Listo. Recicle el grupo de aplicaciones (Restart-WebAppPool <grupo>) para que las tome.' }
