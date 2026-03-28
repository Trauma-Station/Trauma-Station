# 1. Define UTF8 WITHOUT BOM
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$files = Get-ChildItem -Recurse -Filter "*.cs"

foreach ($file in $files) {
    try {
        # 2. READ using .NET to prevent character corruption
        $content = [System.IO.File]::ReadAllLines($file.FullName, $Utf8NoBom)
        
        $hasTarget = $false
        foreach ($line in $content) {
            if ($line -match "SPDX-FileCopyrightText:" -or $line -match "SPDX-License-Identifier:") {
                $hasTarget = $true
                break
            }
        }

        if (-not $hasTarget) { continue }

        $newContent = New-Object System.Collections.Generic.List[string]
        
        foreach ($line in $content) {
            $trimmed = $line.Trim()
            if ($trimmed.StartsWith("// <Trauma>")) {
                $null = $newContent.Add($line)
                continue
            }
            if ($trimmed.StartsWith("// SPDX-FileCopyrightText:") -or 
                $trimmed.StartsWith("// SPDX-License-Identifier:") -or
                $trimmed -eq "//") {
                continue
            }
            $null = $newContent.Add($line)
        }

        while ($newContent.Count -gt 0 -and [string]::IsNullOrWhiteSpace($newContent[0])) {
            $newContent.RemoveAt(0)
        }

        # 3. WRITE using .NET
        [System.IO.File]::WriteAllLines($file.FullName, $newContent, $Utf8NoBom)
        
        Write-Host "Cleaned: $($file.Name)" -ForegroundColor Green
    }
    catch {
        Write-Host "FAILED: $($file.Name)" -ForegroundColor Red
    }
}