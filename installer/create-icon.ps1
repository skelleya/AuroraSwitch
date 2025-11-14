# Create a simple icon for AuroraSwitch installer
# This script creates a basic ICO file

$iconPath = Join-Path $PSScriptRoot "icon.ico"

Write-Host "Creating AuroraSwitch icon..." -ForegroundColor Cyan

try {
    Add-Type -AssemblyName System.Drawing
    
    # Create a 32x32 bitmap
    $bitmap = New-Object System.Drawing.Bitmap(32, 32)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Draw gradient background (Aurora colors: blue to purple)
    $rect = New-Object System.Drawing.Rectangle(0, 0, 32, 32)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(100, 150, 255),
        [System.Drawing.Color]::FromArgb(150, 100, 255),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $graphics.FillRectangle($brush, $rect)
    
    # Draw "A" for AuroraSwitch
    $font = New-Object System.Drawing.Font("Arial", 20, [System.Drawing.FontStyle]::Bold)
    $graphics.DrawString("A", $font, [System.Drawing.Brushes]::White, 6, 3)
    
    # Save as PNG first
    $pngPath = Join-Path $PSScriptRoot "icon-temp.png"
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Convert PNG to ICO using a simple method
    # For a proper ICO, we'd need more complex code, but this creates a basic one
    $pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
    
    # Create minimal ICO file (this is a simplified version)
    # A proper ICO file has a specific structure, but Inno Setup can handle PNG-based ICOs
    $icoStream = New-Object System.IO.FileStream($iconPath, [System.IO.FileMode]::Create)
    
    # ICO header: 6 bytes
    $icoStream.WriteByte(0)  # Reserved
    $icoStream.WriteByte(0)
    $icoStream.WriteByte(1)  # Type (1 = ICO)
    $icoStream.WriteByte(0)
    $icoStream.WriteByte(1)  # Number of images
    $icoStream.WriteByte(0)
    
    # Image directory entry: 16 bytes
    $icoStream.WriteByte(32)  # Width (0 = 256, so 32 = 32)
    $icoStream.WriteByte(32)  # Height
    $icoStream.WriteByte(0)   # Color palette
    $icoStream.WriteByte(0)   # Reserved
    $icoStream.WriteByte(1)   # Color planes
    $icoStream.WriteByte(0)
    $icoStream.WriteByte(32)  # Bits per pixel
    $icoStream.WriteByte(0)
    $imageSize = $pngBytes.Length
    $icoStream.Write([BitConverter]::GetBytes($imageSize), 0, 4)  # Image size
    $offset = 22  # Header (6) + Directory (16) = 22
    $icoStream.Write([BitConverter]::GetBytes($offset), 0, 4)  # Offset to image data
    
    # Write PNG data
    $icoStream.Write($pngBytes, 0, $pngBytes.Length)
    $icoStream.Close()
    
    # Clean up
    Remove-Item $pngPath -ErrorAction SilentlyContinue
    $graphics.Dispose()
    $bitmap.Dispose()
    $brush.Dispose()
    $font.Dispose()
    
    Write-Host "Icon created successfully at: $iconPath" -ForegroundColor Green
    Write-Host "You can now uncomment the SetupIconFile line in auroraswitch-setup.iss"
    
} catch {
    Write-Host "Error creating icon: $_" -ForegroundColor Red
    Write-Host "You can manually create an icon.ico file or use an online icon generator."
}
