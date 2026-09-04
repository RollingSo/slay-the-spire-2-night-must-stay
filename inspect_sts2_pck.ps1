param(
    [Parameter(Mandatory = $true)]
    [string]$PckPath,
    [string]$Pattern = 'card_portraits'
)

$stream = [System.IO.File]::OpenRead($PckPath)
$reader = [System.IO.BinaryReader]::new($stream, [System.Text.Encoding]::UTF8, $true)
try {
    $magic = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    if ($magic -ne 'GDPC') { throw "Not a Godot PCK: $PckPath" }
    $packVersion = $reader.ReadUInt32()
    $engineMajor = $reader.ReadUInt32()
    $engineMinor = $reader.ReadUInt32()
    $enginePatch = $reader.ReadUInt32()
    $flags = $reader.ReadUInt32()
    $fileBase = $reader.ReadUInt64()
    $directoryOffset = $reader.ReadUInt64()
    $stream.Seek(64, [System.IO.SeekOrigin]::Current) | Out-Null
    $stream.Seek([int64]$directoryOffset, [System.IO.SeekOrigin]::Begin) | Out-Null
    $fileCount = $reader.ReadUInt32()
    for ($index = 0; $index -lt $fileCount; $index++) {
        $pathLength = $reader.ReadUInt32()
        $pathBytes = $reader.ReadBytes([int]$pathLength)
        $path = [System.Text.Encoding]::UTF8.GetString($pathBytes).TrimEnd([char]0)
        $offset = $reader.ReadUInt64()
        $size = $reader.ReadUInt64()
        $hash = [Convert]::ToHexString($reader.ReadBytes(16))
        $entryFlags = $reader.ReadUInt32()
        if ($path -match $Pattern) {
            [pscustomobject]@{
                Path = $path
                Offset = $offset
                Size = $size
                Flags = $entryFlags
                FileBase = $fileBase
                PackVersion = $packVersion
                Engine = "$engineMajor.$engineMinor.$enginePatch"
                PckFlags = $flags
                ShaOrMd5 = $hash
            }
        }
    }
} finally {
    $reader.Dispose()
    $stream.Dispose()
}
