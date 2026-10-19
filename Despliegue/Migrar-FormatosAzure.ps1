<#
.SYNOPSIS
  Sincroniza los archivos de Formatos del servidor legado
  (imperiumsanitas.net.co) hacia Azure Blob Storage (cuenta "gitsanitas",
  contenedor "formatosct"), con la misma estructura que ya usa la app nueva:
  {CIA}/Formatos/{radicado}{extensión}.

.DESCRIPTION
  Mismo mecanismo que Migrar-JuntasAzure.ps1: usa AzCopy (herramienta oficial
  de Microsoft) en modo "sync" — compara origen y destino por fecha/tamaño de
  archivo y solo copia lo nuevo o modificado. Seguro de ejecutar todos los
  días: la primera corrida hace la migración completa ("corte de hoy") y las
  siguientes solo suben lo que se agregó o cambió desde la corrida anterior.

  Por defecto NO borra nada en Azure (si un archivo desaparece del origen, en
  Azure se queda). Usar -Borrar solo si de verdad se quiere replicar borrados
  del servidor legado hacia Azure.

.PARAMETER Borrar
  Si se pasa, además de copiar lo nuevo, borra en Azure lo que ya no exista
  en el origen.

.NOTES
  Requiere la variable de entorno de USUARIO (no de sesión, para que la vea
  el Programador de tareas) AZCOPY_FORMATOS_SAS con la "Blob SAS URL" del
  contenedor "formatosct" (permisos Lectura+Escritura+Listado+Creación),
  generada desde el Portal de Azure (misma cuenta "gitsanitas" que Juntas,
  pero SAS distinto porque es otro contenedor). Nunca se escribe ni se pide
  la clave de la cuenta de almacenamiento en este script — solo ese SAS.

  Requiere azcopy.exe instalado y en el PATH del servidor, o junto a este
  script (https://aka.ms/downloadazcopy-v10-windows).

.EXAMPLE
  .\Migrar-FormatosAzure.ps1
  Corrida normal: sube lo nuevo desde la última vez (o todo, la primera vez).

.EXAMPLE
  .\Migrar-FormatosAzure.ps1 -Borrar
  Igual, pero además borra en Azure lo que ya no esté en el servidor legado.
#>

param(
    [switch]$Borrar
)

$ErrorActionPreference = 'Stop'

# Carpeta real en el servidor legado, confirmada por el usuario (única CIA hoy: 890330031).
# OJO: en Formatos el orden es MediaUploader\Formatos\{CIA} (al revés que Juntas, que es
# MediaUploader\{CIA}\JuntasP) — confirmado explícitamente por el usuario.
$cia = '890330031'
$origen = "C:\Inetpub\vhosts\imperiumsanitas.net.co\httpdocs\MediaUploader\Formatos\$cia"

$sasUrlBase = $env:AZCOPY_FORMATOS_SAS
if (-not $sasUrlBase) {
    throw "Falta la variable de entorno de usuario AZCOPY_FORMATOS_SAS (Blob SAS URL del contenedor 'formatosct'). " +
          "Configúrala en Propiedades del sistema > Variables de entorno > Variables de usuario, y abre una " +
          "terminal/tarea NUEVA para que la tome (no sirve en la misma sesión donde se creó)."
}
if (-not (Test-Path $origen)) {
    throw "No existe la carpeta de origen: $origen"
}

$azcopyLocal = Join-Path $PSScriptRoot 'azcopy.exe'
$azcopyCmd = Get-Command azcopy -ErrorAction SilentlyContinue
if ($azcopyCmd) {
    $azcopyExe = $azcopyCmd.Source
} elseif (Test-Path $azcopyLocal) {
    $azcopyExe = $azcopyLocal
} else {
    throw "No se encontró azcopy.exe (ni en el PATH ni junto a este script). Descárgalo de " +
          "https://aka.ms/downloadazcopy-v10-windows y colócalo en: $PSScriptRoot"
}

# El SAS URL del contenedor trae "...?sv=...&sig=..."; insertamos el subpath
# CIA/Formatos antes del "?" para que AzCopy sincronice justo esa subcarpeta,
# reflejando la misma ruta relativa que usa FormatosArchivoServiceAzure.cs.
$signoPregunta = $sasUrlBase.IndexOf('?')
if ($signoPregunta -lt 0) {
    throw "AZCOPY_FORMATOS_SAS no parece una Blob SAS URL válida (falta el '?' con el token SAS)."
}
$rutaSegura = $sasUrlBase.Substring(0, $signoPregunta)
Write-Host "Ruta base detectada (sin token): $rutaSegura"
if ($rutaSegura -notlike '*/formatosct') {
    throw "AZCOPY_FORMATOS_SAS no apunta al contenedor 'formatosct' (falta '/formatosct' antes del '?'). " +
          "Regenera el SAS DESDE DENTRO del contenedor 'formatosct', no desde la cuenta completa, " +
          "y actualiza la variable de entorno con la nueva URL."
}
$destino = $sasUrlBase.Insert($signoPregunta, "/$cia/Formatos")

$logFolder = Join-Path $PSScriptRoot 'logs-migracion-formatos'
New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
$logFile = Join-Path $logFolder ("sync-{0}.log" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))

$azcopyArgs = @('sync', $origen, $destino, '--recursive=true')
if ($Borrar) {
    $azcopyArgs += '--delete-destination=true'
}

Write-Host "Sincronizando '$origen' -> Azure Blob (contenedor formatosct/$cia/Formatos)..."
& $azcopyExe @azcopyArgs 2>&1 | Tee-Object -FilePath $logFile

if ($LASTEXITCODE -ne 0) {
    throw "azcopy terminó con código $LASTEXITCODE. Revisa el log: $logFile"
}

Write-Host "Listo. Log: $logFile"
