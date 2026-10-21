<#
.SYNOPSIS
  Migra los DATOS de Formatos (tabla dbo.DQS_MIP_FORMATOS) del servidor legado (168, base DQUALITY) a produccion
  (13, base FormatosCT). Pensado para correr a diario mientras el legado siga activo. Los archivos ya los migra
  Migrar-FormatosAzure.ps1; este script solo mueve filas de la base de datos.

  Puede correr en cualquier equipo que alcance los DOS servidores (hoy: el equipo de desarrollo, porque el 168 no llega
  al 13). Se conecta con SqlBulkCopy, sin servidores vinculados ni MSDTC.

.DESCRIPTION
  La base de destino es multi-CIA: solo se leen del origen y solo se tocan en el destino las CIA de -Cias (por defecto
  890330031). Las filas de cualquier otra CIA del destino (p. ej. el hospital 800006850) no se tocan nunca.
  La clave de comparacion es CIA + RADICADO.

  Cada corrida (la primera lleva todo; las siguientes, solo lo que falta) hace, en UNA transaccion en el destino:
    1) copia las filas de esas CIA del origen a una tabla temporal;
    2) CONFLICTOS: mismo CIA+RADICADO pero con otros datos (TIPOID, NUMID, NIT, USUARIO, FECHA). El radicado se genera como
       fecha + total de filas de la tabla, asi que el legado y la app nueva pueden generar el mismo numero el mismo dia.
       NO se sobrescribe nada: se registra y el script termina con codigo 2;
    3) CAMBIOS: misma fila en ambos lados pero distinto ESTADO, OBSERVACIONES o NOPRESCRIPCION (p. ej. un formato que en el
       legado paso de Cargado a Gestionado). Se APLICAN por defecto (el origen manda mientras el legado siga activo);
       con -SoloNuevas solo se informan;
    4) NUEVAS: inserta las que no existen en el destino (nunca borra ni actualiza otras).
  Un candado de aplicacion (sp_getapplock) y un mutex impiden dos corridas a la vez (la tabla no tiene clave unica).

  Codigos de salida: 0 = bien; 1 = error; 2 = termino bien pero hay CONFLICTOS que revisar; 3 = ya hay otra corrida en curso.
  El Programador de tareas y el Agente de SQL Server muestran como fallida toda corrida con codigo distinto de 0.

.PARAMETER Cias
  CIA a migrar. Por defecto 890330031.
.PARAMETER OrigenCadena
  Cadena del origen (168, base DQUALITY; ideal: un usuario de solo lectura, ver Crear-UsuarioLecturaMigracion.sql).
  Si no se pasa: variable de entorno FORMATOS_ORIGEN_CS o el archivo cifrado creado con -GuardarOrigen.
.PARAMETER DestinoCadena
  Cadena del destino (13, base FormatosCT; usar el usuario de la aplicacion, nunca admin/sa). Si no se pasa: variable de
  entorno FORMATOS_DESTINO_CS o el archivo cifrado creado con -GuardarDestino.
.PARAMETER GuardarOrigen
.PARAMETER GuardarDestino
  Piden la cadena (sin mostrarla), la prueban y la guardan cifradas con DPAPI en -ArchivoOrigen / -ArchivoDestino. Con el
  ambito CurrentUser (por defecto) solo las puede leer el mismo usuario de Windows en este equipo: la tarea programada debe
  correr con ese usuario. Nunca se escriben en el registro ni en el repositorio.
.PARAMETER Simular
  Hace todo y REVIERTE la transaccion: no escribe nada en el destino.
.PARAMETER SoloNuevas
  Por defecto, ademas de insertar lo nuevo, se copian ESTADO, OBSERVACIONES y NOPRESCRIPCION del origen a las filas
  existentes cuya identidad coincide (nunca a las que estan en conflicto). Con -SoloNuevas solo se insertan las nuevas
  y los cambios solo se informan.

.EXAMPLE
  .\Migrar-FormatosBD.ps1 -GuardarOrigen             # una sola vez, con el usuario que correra la tarea
  .\Migrar-FormatosBD.ps1 -GuardarDestino            # una sola vez, igual
  .\Migrar-FormatosBD.ps1 -Simular                   # ver que haria, sin escribir
  .\Migrar-FormatosBD.ps1                            # corrida real (la primera lleva todo; aplica cambios de estado)
  .\Migrar-FormatosBD.ps1 -SoloNuevas               # solo inserta lo nuevo, los cambios solo se informan
#>
[CmdletBinding()]
param(
    [string[]] $Cias = @('890330031'),
    [string] $OrigenCadena,
    [string] $DestinoCadena,
    [switch] $GuardarOrigen,
    [switch] $GuardarDestino,
    [ValidateSet('CurrentUser', 'LocalMachine')] [string] $Ambito = 'CurrentUser',
    [switch] $Simular,
    [switch] $SoloNuevas,
    [string] $CarpetaLogs,
    [string] $ArchivoOrigen = (Join-Path $env:LOCALAPPDATA 'MigracionFormatosBD\origen.dat'),
    [string] $ArchivoDestino = (Join-Path $env:LOCALAPPDATA 'MigracionFormatosBD\destino.dat')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$Tabla = 'DQS_MIP_FORMATOS'
$Identidad = @('TIPOID', 'NUMID', 'NIT', 'USUARIO', 'FECHA')          # no cambian despues de creada la fila
$Mutables = @('ESTADO', 'OBSERVACIONES', 'NOPRESCRIPCION')           # los cambia la gestion en el legado

# OJO: $PSScriptRoot llega VACIO dentro de un valor por defecto de param() cuando se ejecuta con powershell.exe -File y
# [CmdletBinding()] (como lo hace el Programador de tareas), por eso la carpeta se resuelve aqui, en el cuerpo.
if (-not $CarpetaLogs) {
    $raizScript = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $CarpetaLogs = Join-Path $raizScript 'logs-migracion-formatos-bd'
}
New-Item -ItemType Directory -Force -Path $CarpetaLogs | Out-Null
$archivoLog = Join-Path $CarpetaLogs ("migracion-{0}.log" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))
function Log([string]$m) {
    $l = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $m
    Write-Host $l
    Add-Content -Path $archivoLog -Value $l -Encoding UTF8
}
function Descripcion([string]$cs) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cs)
    "$($b.DataSource) / $($b.InitialCatalog)"      # nunca se registra el usuario ni la clave
}
function Abrir([string]$cs) {
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cs)
    $b['Connect Timeout'] = 30
    $c = New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString)
    $c.Open(); $c
}

# ---------------------------------------------------------------- guardar una cadena cifrada (una sola vez)
function Cifrar([string]$texto) { [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($texto), $null, [Security.Cryptography.DataProtectionScope]::$Ambito) }
function Descifrar([string]$archivo) { [Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($archivo), $null, [Security.Cryptography.DataProtectionScope]::$Ambito)) }
if ($GuardarOrigen -or $GuardarDestino) {
    $que = if ($GuardarOrigen) { 'ORIGEN' } else { 'DESTINO' }
    $archivo = if ($GuardarOrigen) { $ArchivoOrigen } else { $ArchivoDestino }
    $cs = if ($GuardarOrigen -and $OrigenCadena) { $OrigenCadena } elseif ($GuardarDestino -and $DestinoCadena) { $DestinoCadena } else { $null }
    if (-not $cs) {
        $seg = Read-Host "Pegue la cadena de conexion del $que (no se muestra)" -AsSecureString
        $cs = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($seg))
    }
    if ([string]::IsNullOrWhiteSpace($cs)) { throw 'La cadena esta vacia.' }
    $c = Abrir $cs; $c.Close()
    New-Item -ItemType Directory -Force -Path (Split-Path $archivo) | Out-Null
    [IO.File]::WriteAllBytes($archivo, (Cifrar $cs))
    Log "Cadena del $que probada ($(Descripcion $cs)) y guardada cifrada ($Ambito) en $archivo"
    exit 0
}

# ---------------------------------------------------------------- una sola corrida a la vez en esta maquina
$mutex = New-Object System.Threading.Mutex($false, 'Global\MigracionFormatosBD')
if (-not $mutex.WaitOne(0)) { Log 'Ya hay otra corrida en curso en este equipo. No se hace nada.'; exit 3 }

$codigo = 0
$src = $null; $dst = $null; $tx = $null
try {
    foreach ($c in $Cias) { if ($c -notmatch '^\d{3,20}$') { throw "CIA invalida: '$c' (solo digitos)." } }
    if (-not $OrigenCadena) { $OrigenCadena = $env:FORMATOS_ORIGEN_CS }
    if (-not $OrigenCadena -and (Test-Path $ArchivoOrigen)) { $OrigenCadena = Descifrar $ArchivoOrigen }
    if (-not $OrigenCadena) { throw 'Falta la cadena del ORIGEN: use -GuardarOrigen una vez, o la variable FORMATOS_ORIGEN_CS, o -OrigenCadena.' }
    if (-not $DestinoCadena) { $DestinoCadena = $env:FORMATOS_DESTINO_CS }
    if (-not $DestinoCadena -and (Test-Path $ArchivoDestino)) { $DestinoCadena = Descifrar $ArchivoDestino }
    if (-not $DestinoCadena) { throw 'Falta la cadena del DESTINO: use -GuardarDestino una vez, o la variable FORMATOS_DESTINO_CS, o -DestinoCadena.' }

    Log "=== Migracion de datos de Formatos ($(if ($Simular) { 'SIMULACION: no se escribe nada' } else { 'corrida real' })) ==="
    Log "Origen : $(Descripcion $OrigenCadena)"
    Log "Destino: $(Descripcion $DestinoCadena)"
    Log "CIA    : $($Cias -join ', ')    Aplicar cambios de estado: $(-not $SoloNuevas)"

    $src = Abrir $OrigenCadena
    $dst = Abrir $DestinoCadena
    if ((Descripcion $OrigenCadena) -eq (Descripcion $DestinoCadena)) { throw 'El origen y el destino son la misma base.' }

    function Estructura($cn) {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS largo FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '$Tabla' ORDER BY ORDINAL_POSITION"
        $dt = New-Object System.Data.DataTable; $dt.Load($cmd.ExecuteReader()); , $dt
    }
    $eo = Estructura $src; $ed = Estructura $dst
    if ($eo.Rows.Count -eq 0) { throw "El origen no tiene dbo.$Tabla." }
    if ($ed.Rows.Count -eq 0) { throw "El destino no tiene dbo.$Tabla." }
    $so = ($eo.Rows | ForEach-Object { "$($_.COLUMN_NAME)|$($_.DATA_TYPE)|$($_.largo)" }) -join ';'
    $sd = ($ed.Rows | ForEach-Object { "$($_.COLUMN_NAME)|$($_.DATA_TYPE)|$($_.largo)" }) -join ';'
    if ($so -cne $sd) { throw "La estructura de dbo.$Tabla es distinta entre origen y destino. Origen: $so | Destino: $sd" }
    $columnas = @($eo.Rows | ForEach-Object { $_.COLUMN_NAME })
    foreach ($req in ($Identidad + $Mutables + @('CIA', 'RADICADO'))) { if ($columnas -notcontains $req) { throw "Falta la columna $req en dbo.$Tabla." } }
    $lista = ($columnas | ForEach-Object { "[$_]" }) -join ', '

    # ---- parametros de CIA
    $inCias = ($Cias | ForEach-Object { "'$_'" }) -join ', '
    function Escalar($cn, $sql, $t = $null) { $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql; if ($t) { $cmd.Transaction = $t }; $cmd.CommandTimeout = 300; $cmd.ExecuteScalar() }
    function Filas($cn, $sql, $t = $null) { $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql; if ($t) { $cmd.Transaction = $t }; $cmd.CommandTimeout = 300; $dt = New-Object System.Data.DataTable; $dt.Load($cmd.ExecuteReader()); , $dt }
    function NoQuery($cn, $sql, $t) { $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql; $cmd.Transaction = $t; $cmd.CommandTimeout = 600; $cmd.ExecuteNonQuery() }

    $condOrigen = "CIA IN ($inCias) AND RADICADO IS NOT NULL AND LTRIM(RTRIM(RADICADO)) <> ''"
    $totalOrigen = [int](Escalar $src "SELECT COUNT(*) FROM dbo.$Tabla WHERE $condOrigen")
    $sinRadicado = [int](Escalar $src "SELECT COUNT(*) FROM dbo.$Tabla WHERE CIA IN ($inCias) AND (RADICADO IS NULL OR LTRIM(RTRIM(RADICADO)) = '')")
    $otras = Filas $src "SELECT CIA, COUNT(*) AS filas FROM dbo.$Tabla WHERE CIA NOT IN ($inCias) OR CIA IS NULL GROUP BY CIA"
    $antes = [int](Escalar $dst "SELECT COUNT(*) FROM dbo.$Tabla WHERE CIA IN ($inCias)")
    $totalDestinoTodas = [int](Escalar $dst "SELECT COUNT(*) FROM dbo.$Tabla")
    Log "Origen : $totalOrigen filas de esas CIA con radicado$(if ($sinRadicado) { " ($sinRadicado sin radicado: NO se migran)" })"
    if ($otras.Rows.Count) { Log ("Origen : CIA que NO se migran (no estan en -Cias): " + (($otras.Rows | ForEach-Object { "$($_.CIA)=$($_.filas)" }) -join ', ')) }
    Log "Destino: $antes filas de esas CIA ($totalDestinoTodas en total, de todas las CIA que contiene)"
    if ($antes -eq 0) { Log 'Es la PRIMERA corrida para estas CIA: se llevara todo lo que tiene el origen.' }

    # ---- todo en una transaccion del destino
    $tx = $dst.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    $lock = $dst.CreateCommand(); $lock.Transaction = $tx
    $lock.CommandText = "DECLARE @r int; EXEC @r = sp_getapplock @Resource = N'MigracionFormatosBD', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 0; SELECT @r"
    if ([int]$lock.ExecuteScalar() -lt 0) { Log 'Otra corrida (en otro equipo) tiene el candado del destino. No se hace nada.'; $tx.Rollback(); $tx = $null; $codigo = 3; throw [System.OperationCanceledException]::new('candado') }

    $defCols = ($eo.Rows | ForEach-Object {
        if ($_.DATA_TYPE -ne 'varchar') { throw "Tipo no soportado en $($_.COLUMN_NAME): $($_.DATA_TYPE)" }
        $len = if ([int]$_.largo -le 0) { 'max' } else { [string]$_.largo }
        "[$($_.COLUMN_NAME)] varchar($len) COLLATE DATABASE_DEFAULT NULL"
    }) -join ', '
    [void](NoQuery $dst "CREATE TABLE #stg ($defCols)" $tx)

    $cmdLeer = $src.CreateCommand()
    $cmdLeer.CommandText = "SELECT $lista FROM dbo.$Tabla WHERE $condOrigen"
    $cmdLeer.CommandTimeout = 300
    $rd = $cmdLeer.ExecuteReader()
    $bc = New-Object System.Data.SqlClient.SqlBulkCopy($dst, [System.Data.SqlClient.SqlBulkCopyOptions]::Default, $tx)
    $bc.DestinationTableName = '#stg'; $bc.BatchSize = 2000; $bc.BulkCopyTimeout = 600
    foreach ($c in $columnas) { [void]$bc.ColumnMappings.Add($c, $c) }
    $bc.WriteToServer($rd); $rd.Close(); $bc.Close()
    $cargadas = [int](Escalar $dst 'SELECT COUNT(*) FROM #stg' $tx)

    $dup = NoQuery $dst ";WITH d AS (SELECT ROW_NUMBER() OVER (PARTITION BY CIA, RADICADO ORDER BY (SELECT NULL)) rn FROM #stg) DELETE FROM d WHERE rn > 1" $tx
    if ($dup -gt 0) { Log "AVISO: $dup filas repetidas (misma CIA+RADICADO) en el ORIGEN; se toma una sola de cada una." }

    $idS = ($Identidad | ForEach-Object { "s.[$_]" }) -join ', '; $idD = ($Identidad | ForEach-Object { "d.[$_]" }) -join ', '
    $muS = ($Mutables | ForEach-Object { "s.[$_]" }) -join ', '; $muD = ($Mutables | ForEach-Object { "d.[$_]" }) -join ', '
    $mismaIdentidad = "EXISTS (SELECT $idS INTERSECT SELECT $idD)"          # comparacion que trata NULL = NULL
    $join = 'FROM #stg s JOIN dbo.' + $Tabla + ' d ON d.CIA = s.CIA AND d.RADICADO = s.RADICADO'

    $conflictos = Filas $dst "SELECT DISTINCT s.CIA, s.RADICADO, d.NUMID AS NUMID_destino, s.NUMID AS NUMID_origen, d.USUARIO AS USUARIO_destino, s.USUARIO AS USUARIO_origen $join WHERE NOT $mismaIdentidad ORDER BY s.RADICADO" $tx
    $cambios = Filas $dst "SELECT DISTINCT s.CIA, s.RADICADO, d.ESTADO AS ESTADO_destino, s.ESTADO AS ESTADO_origen $join WHERE $mismaIdentidad AND NOT EXISTS (SELECT $muS INTERSECT SELECT $muD) ORDER BY s.RADICADO" $tx
    $existentes = [int](Escalar $dst "SELECT COUNT(DISTINCT s.CIA + '|' + s.RADICADO) $join" $tx)

    $insertadas = NoQuery $dst "INSERT INTO dbo.$Tabla ($lista) SELECT $(($columnas | ForEach-Object { "s.[$_]" }) -join ', ') FROM #stg s WHERE NOT EXISTS (SELECT 1 FROM dbo.$Tabla d WITH (UPDLOCK, HOLDLOCK) WHERE d.CIA = s.CIA AND d.RADICADO = s.RADICADO)" $tx

    $actualizadas = 0
    if (-not $SoloNuevas -and $cambios.Rows.Count -gt 0) {
        $asigna = ($Mutables | ForEach-Object { "d.[$_] = s.[$_]" }) -join ', '
        $actualizadas = NoQuery $dst "UPDATE d SET $asigna $join WHERE $mismaIdentidad AND NOT EXISTS (SELECT $muS INTERSECT SELECT $muD)" $tx
    }

    $despues = [int](Escalar $dst "SELECT COUNT(*) FROM dbo.$Tabla WHERE CIA IN ($inCias)" $tx)
    $otrasCiaAntes = $totalDestinoTodas - $antes
    $otrasCiaDespues = [int](Escalar $dst "SELECT COUNT(*) FROM dbo.$Tabla WHERE CIA NOT IN ($inCias)" $tx)
    if ($otrasCiaDespues -ne $otrasCiaAntes) { throw "Las filas de otras CIA cambiaron ($otrasCiaAntes -> $otrasCiaDespues): se revierte todo." }

    if ($Simular) { $tx.Rollback(); Log 'SIMULACION: se revirtio la transaccion, el destino no cambio.' } else { $tx.Commit() }
    $tx = $null

    Log '--- Resultado ---'
    Log "Filas leidas del origen (sin repetidas): $($cargadas - $dup)"
    Log "Ya existian en el destino con los mismos datos de identidad: $($existentes - $conflictos.Rows.Count)"
    Log "NUEVAS $(if ($Simular) { '(que se insertarian)' } else { 'insertadas' }): $insertadas"
    Log "CONFLICTOS (mismo CIA+RADICADO, distintos datos; NO se tocan): $($conflictos.Rows.Count)"
    foreach ($f in ($conflictos.Rows | Select-Object -First 20)) { Log "   conflicto: CIA $($f.CIA) radicado $($f.RADICADO) | NUMID destino=$($f.NUMID_destino) origen=$($f.NUMID_origen) | USUARIO destino=$($f.USUARIO_destino) origen=$($f.USUARIO_origen)" }
    Log "CON CAMBIOS en el origen (ESTADO/OBSERVACIONES/NOPRESCRIPCION): $($cambios.Rows.Count)$(if (-not $SoloNuevas) { " -> actualizadas: $actualizadas" } else { ' (no se aplican: corrida con -SoloNuevas)' })"
    foreach ($f in ($cambios.Rows | Select-Object -First 20)) { Log "   cambio: radicado $($f.RADICADO) | estado destino=$($f.ESTADO_destino) origen=$($f.ESTADO_origen)" }
    Log "Destino, filas de estas CIA: $antes -> $despues$(if ($Simular) { ' (en la simulacion; el valor real sigue en ' + $antes + ')' })"
    if (-not $Simular) {
        $final = [int](Escalar $dst "SELECT COUNT(*) FROM dbo.$Tabla WHERE CIA IN ($inCias)")
        Log "Verificacion despues del commit: $final filas de estas CIA en el destino (origen: $totalOrigen, conflictos: $($conflictos.Rows.Count))"
        if ($final -lt ($totalOrigen - $conflictos.Rows.Count - $dup)) { throw "El destino tiene menos filas de las esperadas ($final)." }
    }
    if ($conflictos.Rows.Count -gt 0) { $codigo = 2; Log 'TERMINO CON CONFLICTOS: revisar a mano (codigo de salida 2).' } else { Log 'Termino bien.' }
}
catch [System.OperationCanceledException] { if ($codigo -eq 0) { $codigo = 3 } }
catch {
    $codigo = 1
    Log "ERROR: $($_.Exception.Message)"
    if ($tx) { try { $tx.Rollback(); Log 'Se revirtio la transaccion: el destino no cambio.' } catch { } }
}
finally {
    if ($src) { $src.Close() }
    if ($dst) { $dst.Close() }
    $mutex.ReleaseMutex()
    # conserva 90 dias de registros
    Get-ChildItem $CarpetaLogs -Filter 'migracion-*.log' -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-90) } | ForEach-Object { [IO.File]::Delete($_.FullName) }
}
exit $codigo
