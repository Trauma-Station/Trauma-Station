# 1. Setup Encoding (UTF8 Without the hidden BOM)
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$requiredLicense = "// SPDX-License-Identifier: AGPL-3.0-or-later"

$files = Get-ChildItem -Recurse -Filter "*.cs"

foreach ($file in $files) {
    try {
        # 2. READ using .NET to prevent character corruption (Mojibake)
        $content = [System.IO.File]::ReadAllLines($file.FullName, $Utf8NoBom)
        
        $hasOldTarget = $false
        foreach ($line in $content) {
            if ($line -match "SPDX-FileCopyrightText:" -or ($line -match "SPDX-License-Identifier:" -and $line -notmatch "AGPL-3.0-or-later")) {
                $hasOldTarget = $true
                break
            }
        }

        # If it's already clean and has the license, don't touch it
        if (-not $hasOldTarget -and ($content.Count -gt 0 -and $content[0] -eq $requiredLicense)) {
            continue 
        }

        $newContent = New-Object System.Collections.Generic.List[string]
        
        # 3. Always put the required license at the absolute top
        $null = $newContent.Add($requiredLicense)
        $null = $newContent.Add("")

        foreach ($line in $content) {
            $trimmed = $line.Trim()

            # Preserve <Trauma> lines
            if ($trimmed.StartsWith("// <Trauma>")) {
                $null = $newContent.Add($line)
                continue
            }

            # Strip old SPDX lines and generic empty comments
            if ($trimmed.StartsWith("// SPDX-FileCopyrightText:") -or 
                $trimmed.StartsWith("// SPDX-License-Identifier:") -or
                $trimmed -eq "//") {
                continue
            }

            $null = $newContent.Add($line)
        }

        # 4. Cleanup spacing: Ensure only ONE blank line after our injected header
        while ($newContent.Count -gt 2 -and [string]::IsNullOrWhiteSpace($newContent[2])) {
            $newContent.RemoveAt(2)
        }

        # 5. WRITE using .NET (Safe for special characters and No BOM)
        [System.IO.File]::WriteAllLines($file.FullName, $newContent, $Utf8NoBom)
        
        Write-Host "Fixed & Licensed: $($file.Name)" -ForegroundColor Cyan
    }
    catch {
        Write-Host "FAILED: $($file.Name). Check if file is locked." -ForegroundColor Red
    }
}