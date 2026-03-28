# Define the required license line
$requiredLicense = "// SPDX-License-Identifier: AGPL-3.0-or-later"

# Get all .cs files recursively
$files = Get-ChildItem -Recurse -Filter "*.cs"

foreach ($file in $files) {
    # Read the file content
    $content = Get-Content $file.FullName
    $newContent = New-Object System.Collections.Generic.List[string]
    $hasLicense = $false

    foreach ($line in $content) {
        $trimmed = $line.Trim()

        # 1. Skip lines starting with CopyrightText (using the proper .Method())
        if ($trimmed.StartsWith("// SPDX-FileCopyrightText:")) {
            continue
        }

        # 2. Skip empty comment lines
        if ($trimmed -eq "//") {
            continue
        }

        # 3. Check if the license exists
        if ($trimmed -eq $requiredLicense) {
            $hasLicense = $true
        }

        $null = $newContent.Add($line)
    }

    # 4. Add the license to the top if missing
    if (-not $hasLicense) {
        $newContent.Insert(0, $requiredLicense)
        $newContent.Insert(1, "") # Optional: adds a newline after header
    }

    # Save with UTF8 to keep C# happy
    $newContent | Set-Content $file.FullName -Encoding UTF8
    Write-Host "Processed: $($file.Name)" -ForegroundColor Cyan
}