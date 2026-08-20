param(
    [string]$GeneratedRoot = 'C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d',
    [string]$ProjectRoot = 'D:\sts-2-mod'
)

Add-Type -AssemblyName System.Drawing

$map = [ordered]@{
    white_shadow_lure_power    = 'exec-cea1c67c-3d36-4692-8aa9-865da6622f53.png'
    soulguard_power            = 'exec-f3bf5b5f-d502-48b4-b962-63eacc5d970d.png'
    spirit_form_power          = 'exec-8da9622f-80d8-4ebe-8294-aa62d9de186e.png'
    ancient_dragon_faith_power = 'exec-62aecde7-ddb3-41f6-8d60-f2020baff95d.png'
    beast_claw_mark_power      = 'exec-3347440f-0923-427b-bbf9-cd7e7ebba38c.png'
    golden_order_power         = 'exec-cd427b1c-791e-40d6-beee-5c1846f943c8.png'
    blessing_of_grace_power    = 'exec-10bde32c-9eae-4102-b508-2c0dd1531508.png'
}

$destinations = @(
    (Join-Path $ProjectRoot 'revenant_assets\powers'),
    (Join-Path $ProjectRoot 'images\powers'),
    (Join-Path $ProjectRoot 'powers')
)
New-Item -ItemType Directory -Force -Path $destinations | Out-Null

function Save-Icon([string]$source, [string]$destination) {
    $img = [System.Drawing.Image]::FromFile($source)
    $temporary = $destination + '.tmp.png'
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    try {
        $bmp = [System.Drawing.Bitmap]::new(
            256,
            256,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                $g.Clear([System.Drawing.Color]::Transparent)
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.DrawImage($img, 0, 0, 256, 256)
            } finally { $g.Dispose() }

            for ($y = 0; $y -lt 256; $y++) {
                for ($x = 0; $x -lt 256; $x++) {
                    $pixel = $bmp.GetPixel($x, $y)
                    if ($pixel.A -lt 8) { $bmp.SetPixel($x, $y, [System.Drawing.Color]::Transparent) }
                }
            }
            $bmp.SetPixel(0,0,[System.Drawing.Color]::Transparent)
            $bmp.SetPixel(255,0,[System.Drawing.Color]::Transparent)
            $bmp.SetPixel(0,255,[System.Drawing.Color]::Transparent)
            $bmp.SetPixel(255,255,[System.Drawing.Color]::Transparent)
            $stream = [System.IO.File]::Open($temporary, [System.IO.FileMode]::Create)
            try {
                $bmp.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
            } finally { $stream.Dispose() }
        } finally { $bmp.Dispose() }
    } finally { $img.Dispose() }
    Move-Item -LiteralPath $temporary -Destination $destination -Force
}

foreach ($entry in $map.GetEnumerator()) {
    $source = Join-Path $GeneratedRoot $entry.Value
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing generated icon: $source" }
    foreach ($directory in $destinations) {
        Save-Icon $source (Join-Path $directory ($entry.Key + '.png'))
    }
}

Write-Output "Prepared $($map.Count) Revenant power icons with verified 256x256 true alpha."
