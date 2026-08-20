param(
    [string]$GeneratedRoot = 'C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d',
    [string]$ProjectRoot = 'D:\sts-2-mod'
)

Add-Type -AssemblyName System.Drawing

# One independently generated source per card. Never collapse multiple card
# identities onto a shared theme image again.
$map = [ordered]@{
    ice_lightning_spear      = 'exec-a14899d2-aefd-4c83-958f-8fea650c552a.png'
    cursed_claw_combo        = 'exec-5294eae8-b779-40d1-88b7-b5b4425179d1.png'
    halo                     = 'exec-cb4a2f8f-4a50-4c94-a74e-1bf430d94ca4.png'
    emergency_restore        = 'exec-40ef6ac9-ab30-4d73-aec8-04b03fa17d01.png'
    precise_lightning_strike = 'exec-9530b6b8-2368-4aa6-8a70-902138870a7f.png'
    threefold_halo           = 'exec-d4857930-d922-4135-9093-ddbe7f2bb784.png'
    ancient_dragon_lightning = 'exec-4566fee4-a224-44f0-9453-4ae9c57d9041.png'
    lansseax_blade           = 'exec-ff06718e-15b4-48a3-bf0b-341f3f349108.png'
    lightning_strike         = 'exec-aa626a44-c062-45ec-8c0d-faa61540e8ce.png'
    ancient_dragon_spear     = 'exec-da2b329e-5004-4e7a-a70f-33ae0162fb23.png'
    recover                  = 'exec-c06af0c7-f5ac-4cc2-b6ae-b51755ce99e5.png'
    flannsax_lightning_spear = 'exec-10b371f3-7d83-4d60-ae01-776f42d7c247.png'
    beast_claw               = 'exec-0466b3aa-deb2-40a1-a8b0-cb0064d19a0f.png'
    death_lightning          = 'exec-a6891a28-4991-4e05-8648-2290a4ba46ca.png'
    space_rending_frenzy     = 'exec-4ac4c197-149a-4408-843f-e3ea9c8db7e1.png'
    white_shadow_lure        = 'exec-2aa3b42b-c7e5-47e1-90d2-6046731bcf70.png'
    soulguard                = 'exec-a4d578cf-f96c-4caf-9937-4d06f1591087.png'
    lightning_spear          = 'exec-e67ec4dc-8c9e-44e6-a2ac-f6152f7258d2.png'
    spirit_form              = 'exec-8439a57c-f2ad-4868-9cb8-33c2949f818a.png'
    unbearable_frenzy        = 'exec-1807762f-a42d-4520-9c12-94d971ff8d0c.png'
    beaststone               = 'exec-f34b0e18-365e-4df6-b816-da33606e496b.png'
    radagon_halo             = 'exec-a0ca750f-dc28-40c7-8e87-012c4a12502c.png'
    soul_summon              = 'exec-f9c1293f-a97c-48f7-ab52-2dd35d45dcd5.png'
    grave_rob                = 'exec-d96bdbeb-6b29-40dd-ae7b-21b3c88e2aa3.png'
    greater_recover          = 'exec-229271aa-9182-4ecd-9269-9eed3011e381.png'
    ancient_dragon_faith     = 'exec-0710fae2-e8ec-4e66-a886-b039e61d910e.png'
    beast_claw_mark          = 'exec-468d153b-acb1-4639-8d23-eab22f0ae5bd.png'
    golden_order             = 'exec-ac15b7bc-73a4-41c9-ba56-769876bc89dc.png'
    spirit_link              = 'exec-f3e6f36b-a730-4f56-a697-47cdeae677a8.png'
    blessing_of_grace        = 'exec-8a51c8f3-b8ff-4a56-ab4c-e64bc0cf5a7a.png'
    gurranq_beast_claw       = 'exec-b32c8499-55ac-430b-bb01-663d4817bd06.png'
}

$outDir = Join-Path $ProjectRoot 'revenant_assets\cards'
$previewDir = Join-Path $ProjectRoot 'design\卡图预览\复仇者重制'
New-Item -ItemType Directory -Force -Path $outDir,$previewDir | Out-Null

function Save-Landscape([string]$source, [string]$destination) {
    $img = [System.Drawing.Image]::FromFile($source)
    try {
        $targetW = 1000; $targetH = 760
        $srcRatio = $img.Width / [double]$img.Height
        $dstRatio = $targetW / [double]$targetH
        if ($srcRatio -gt $dstRatio) {
            $cropH = $img.Height; $cropW = [int]($cropH * $dstRatio)
            $cropX = [int](($img.Width - $cropW) / 2); $cropY = 0
        } else {
            $cropW = $img.Width; $cropH = [int]($cropW / $dstRatio)
            $cropX = 0; $cropY = [int](($img.Height - $cropH) / 2)
        }
        $bmp = New-Object System.Drawing.Bitmap($targetW,$targetH)
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.DrawImage($img, [System.Drawing.Rectangle]::new(0,0,$targetW,$targetH), $cropX,$cropY,$cropW,$cropH, [System.Drawing.GraphicsUnit]::Pixel)
            } finally { $g.Dispose() }
            $bmp.Save($destination,[System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $bmp.Dispose() }
    } finally { $img.Dispose() }
}

foreach ($entry in $map.GetEnumerator()) {
    $source = Join-Path $GeneratedRoot $entry.Value
    if (-not (Test-Path -LiteralPath $source)) { throw "Missing generated source: $source" }
    $dest = Join-Path $outDir ($entry.Key + '.png')
    $preview = Join-Path $previewDir ($entry.Key + '_preview.png')
    Save-Landscape $source $dest
    Copy-Item -LiteralPath $dest -Destination $preview -Force
}

Write-Output "Prepared $($map.Count) independently generated Revenant card artworks at 1000x760."
