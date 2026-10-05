param([string]$ManifestPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $ManifestPath) { $ManifestPath = Join-Path $root 'build/item_icon_paths.json' }
Add-Type -AssemblyName System.Drawing
$paths = [Collections.Generic.List[string]]::new()
$count = 0
foreach ($role in @('guardian','ironeye','revenant','duchess')) {
    foreach ($kind in @('relics','potions')) {
        $folder = Join-Path $root ($role+'_assets/'+$kind)
        foreach ($icon in Get-ChildItem -LiteralPath $folder -Filter '*.png' | Where-Object BaseName -NotLike '*_outline') {
            foreach ($file in @($icon.FullName,(Join-Path $folder ($icon.BaseName+'_outline.png')))) {
                if (-not (Test-Path -LiteralPath $file)) { throw "Missing icon or outline: $file" }
                $bitmap = [Drawing.Bitmap]::FromFile($file)
                try {
                    if ($bitmap.Width -ne 256 -or $bitmap.Height -ne 256) { throw "Expected 256x256: $file" }
                    foreach ($corner in @(@(0,0),@(255,0),@(0,255),@(255,255))) {
                        if ($bitmap.GetPixel($corner[0],$corner[1]).A -ne 0) { throw "Nontransparent corner: $file" }
                    }
                } finally { $bitmap.Dispose() }
                $relative = $file.Substring($root.Length+1).Replace('\','/')
                $paths.Add('res://'+$relative)
            }
            if ($kind -eq 'potions') {
                $relative = 'images/atlases/potion_atlas.sprites/'+$icon.BaseName+'.tres'
                $mapping = Get-Content -LiteralPath (Join-Path $root $relative) -Raw
                $expected = 'res://'+$role+'_assets/potions/'+$icon.BaseName+'.png'
                if (-not $mapping.Contains('path="'+$expected+'"') -or -not $mapping.Contains('region = Rect2(0, 0, 256, 256)')) {
                    throw "Incorrect potion icon mapping: $relative"
                }
                $paths.Add('res://'+$relative)
                $relative = 'images/atlases/potion_outline_atlas.sprites/'+$icon.BaseName+'.tres'
                $mapping = Get-Content -LiteralPath (Join-Path $root $relative) -Raw
                $expected = 'res://'+$role+'_assets/potions/'+$icon.BaseName+'_outline.png'
                if (-not $mapping.Contains('path="'+$expected+'"') -or -not $mapping.Contains('region = Rect2(0, 0, 256, 256)')) {
                    throw "Incorrect potion outline mapping: $relative"
                }
                $paths.Add('res://'+$relative)
            }
            $count++
        }
    }
}
if ($count -ne 49 -or $paths.Count -ne 124) { throw "Unexpected item icon inventory: $count icons / $($paths.Count) resources" }
$parent = Split-Path -Parent $ManifestPath
New-Item -ItemType Directory -Force -Path $parent | Out-Null
[IO.File]::WriteAllText($ManifestPath,(@{paths=@($paths)} | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
Write-Output 'Item icons validated: 49 icons, 49 outlines, 13 potion icon and 13 outline mappings.'
