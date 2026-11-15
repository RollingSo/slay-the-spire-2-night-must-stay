param([Parameter(Mandatory=$true)][string]$BackgroundPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$source = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $BackgroundPath).Path)
$bitmap = [Drawing.Bitmap]::new(256,256,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [Drawing.Graphics]::FromImage($bitmap)
try {
    $minX=$source.Width; $minY=$source.Height; $maxX=0; $maxY=0
    for($y=0;$y -lt $source.Height;$y+=2) { for($x=0;$x -lt $source.Width;$x+=2) {
        if($source.GetPixel($x,$y).A -gt 32) {
            $minX=[Math]::Min($minX,$x); $maxX=[Math]::Max($maxX,$x)
            $minY=[Math]::Min($minY,$y); $maxY=[Math]::Max($maxY,$y)
        }
    } }
    $bounds=[Drawing.Rectangle]::new($minX-3,$minY-3,$maxX-$minX+7,$maxY-$minY+7)
    $ratio=242.0/[Math]::Max($bounds.Width,$bounds.Height)
    $w=[single]($bounds.Width*$ratio); $h=[single]($bounds.Height*$ratio)
    $g.Clear([Drawing.Color]::Transparent)
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($source,[Drawing.RectangleF]::new((256-$w)/2,(256-$h)/2,$w,$h),$bounds,[Drawing.GraphicsUnit]::Pixel)
    # Background/frame ONLY. The original Layer2 supplies the water above it.
    $bitmap.Save((Join-Path $root 'wylder_assets/energy_counter/pursuit_orb.png'),[Drawing.Imaging.ImageFormat]::Png)
    foreach($p in @(@(0,0),@(255,0),@(0,255),@(255,255))) {
        if($bitmap.GetPixel($p[0],$p[1]).A -ne 0) { throw 'Nontransparent corner' }
    }
    # All five native PNGs retain the complete RGB painting but use different
    # alpha masks. Extract RGB water components before applying those masks.
    $original=[Drawing.Bitmap]::new((Join-Path $root 'wylder_assets/energy_counter/wylder_orb_layer_1.png'))
    $water=[Drawing.Bitmap]::new(256,256,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $seen=[bool[,]]::new(256,256); $mask=[bool[,]]::new(256,256)
        for($y=1;$y -lt 255;$y++) { for($x=1;$x -lt 255;$x++) {
            if($seen[$x,$y]) { continue }
            $c=$original.GetPixel($x,$y)
            if($c.R -lt 60 -or $c.R -lt $c.B*0.6) { continue }
            $queue=[Collections.Generic.Queue[Drawing.Point]]::new()
            $points=[Collections.Generic.List[Drawing.Point]]::new()
            $queue.Enqueue([Drawing.Point]::new($x,$y)); $seen[$x,$y]=$true
            $left=$x; $right=$x; $top=$y; $bottom=$y
            while($queue.Count -gt 0) {
                $p=$queue.Dequeue(); $points.Add($p)
                $left=[Math]::Min($left,$p.X);$right=[Math]::Max($right,$p.X)
                $top=[Math]::Min($top,$p.Y);$bottom=[Math]::Max($bottom,$p.Y)
                foreach($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) {
                    $nx=$p.X+$d[0];$ny=$p.Y+$d[1]
                    if($nx -lt 1 -or $ny -lt 1 -or $nx -gt 254 -or $ny -gt 254 -or $seen[$nx,$ny]) { continue }
                    $c=$original.GetPixel($nx,$ny)
                    if($c.R -ge 60 -and $c.R -ge $c.B*0.6) {
                        $seen[$nx,$ny]=$true;$queue.Enqueue([Drawing.Point]::new($nx,$ny))
                    }
                }
            }
            if($left -ge 50 -and $right -le 205 -and $top -ge 70 -and $bottom -le 180 -and $points.Count -gt 100) {
                foreach($p in $points) { for($dy=-1;$dy -le 1;$dy++){for($dx=-1;$dx -le 1;$dx++){$mask[($p.X+$dx),($p.Y+$dy)]=$true}} }
            }
        } }
        $count=0
        for($y=0;$y -lt 256;$y++){for($x=0;$x -lt 256;$x++){
            if($mask[$x,$y]) {
                $c=$original.GetPixel($x,$y)
                $water.SetPixel($x,$y,[Drawing.Color]::FromArgb(255,$c.R,$c.G,$c.B)); $count++
            }
        }}
        if($count -lt 7000) { throw 'Incomplete water extraction' }
        $water.Save((Join-Path $root 'wylder_assets/energy_counter/pursuit_water.png'),[Drawing.Imaging.ImageFormat]::Png)
        Write-Output "256x256 RGBA background/frame plus independent complete water layer ($count source RGB pixels, original coordinates)."
    } finally { $original.Dispose();$water.Dispose() }
} finally { $g.Dispose(); $bitmap.Dispose(); $source.Dispose() }
