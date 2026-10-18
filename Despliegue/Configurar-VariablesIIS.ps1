<#
.SYNOPSIS
  Define en un sitio de IIS las variables de entorno de produccion de Contingencia.Web (Formatos + Juntas).

.DESCRIPTION
  Escribe en applicationHost.config (seccion system.webServer/aspNetCore del sitio) las variables de la "tabla C" de
  Contingencia.Infrastructure/BASE_DE_DATOS.md. Ningun valor secreto queda en este archivo: se pasan por parametro.
  Es repetible: cada variable se quita y se vuelve a poner. Ejecutar en el servidor IIS, como administrador.
  Con -SoloMostrar no cambia nada: imprime lo que haria, con los secretos enmascarados.

.EXAMPLE
  .\Configurar-VariablesIIS.ps1 -Sitio 'formatosct.imperium.net.co' -ClaveAppSql '<clave de contingencia_app>' `
      -TurnstileSiteKey '<site key>' -TurnstileSecretKey '<secret key>' `
      -SmtpHost '<host>' -SmtpUser '<usuario>' -SmtpPassword '<clave>'
#>
param(
    [Parameter(Mandatory)] [string] $Sitio,
    [Parameter(Mandatory)] [string] $ClaveAppSql,
    [Parameter(Mandatory)] [string] $TurnstileSiteKey,
    [Parameter(Mandatory)] [string] $TurnstileSecretKey,
    [string] $SmtpHost, [string] $SmtpUser, [string] $SmtpPassword,
    [string] $ServidorSql = '172.25.2.13',
    [string] $BaseDatos = 'FormatosCT',
    [string] $UsuarioApp = 'contingencia_app',
    [string] $CarpetaDatos = 'D:\DatosContingencia',
    [string] $UrlLogoActa,
    [switch] $SoloMostrar
)
$ErrorActionPreference = 'Stop'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
if (-not $SoloMostrar -and -not (Test-Path $appcmd)) { throw "No se encontro appcmd.exe: ejecute este script en el servidor IIS (con la caracteristica Herramientas de administracion de IIS)." }

# Los valores no pueden llevar comillas simples ni dobles (rompen la sintaxis de appcmd)
foreach ($v in @($ClaveAppSql, $TurnstileSiteKey, $TurnstileSecretKey, $SmtpHost, $SmtpUser, $SmtpPassword, $CarpetaDatos, $UrlLogoActa)) {
    if ($v -and $v -match "['""]") { throw 'Ningun valor puede contener comillas simples ni dobles.' }
}

$base = $CarpetaDatos.TrimEnd('\')   # sin Join-Path: la unidad puede no existir en la maquina donde se simula
$cadena = "Server=$ServidorSql;Database=$BaseDatos;User Id=$UsuarioApp;Password=$ClaveAppSql;TrustServerCertificate=True;Connect Timeout=30;Max Pool Size=100"
# ordenado: nombre -> (valor, secreto?)
$vars = [ordered]@{
    'ASPNETCORE_ENVIRONMENT'                = @('Production', $false)
    'ASPNETCORE_FORWARDEDHEADERS_ENABLED'   = @('true', $false)          # obligatoria detras del balanceador de AWS
    'ConnectionStrings__DQUALITY'           = @($cadena, $true)
    'DataProtection__RutaLlaves'            = @("$base\Llaves", $false)
    'Almacenamiento__RutaBase'              = @("$base\Archivos", $false)
    'Menu__Perfil'                          = @('FORMATOS_JUNTAS', $false)
    'Turnstile__SiteKey'                    = @($TurnstileSiteKey, $false)
    'Turnstile__SecretKey'                  = @($TurnstileSecretKey, $true)
}
if ($SmtpHost)     { $vars['Smtp__Host']     = @($SmtpHost, $false) }
if ($SmtpUser)     { $vars['Smtp__User']     = @($SmtpUser, $false) }
if ($SmtpPassword) { $vars['Smtp__Password'] = @($SmtpPassword, $true) }
if ($UrlLogoActa)  { $vars['Acta__UrlLogo']  = @($UrlLogoActa, $false) }

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

if (-not $SoloMostrar) {
    ''
    'Pendiente a mano: crear las carpetas y dar permiso Modificar a IIS AppPool\<grupo>:'
    "  $base\Llaves  y  $base\Archivos"
    'Luego reciclar el grupo de aplicaciones y abrir https://<subdominio>/salud'
}
else { '(No se cambio nada.)' }
