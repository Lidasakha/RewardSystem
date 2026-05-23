$replacements = [ordered]@{
    "ðŸ‘‹" = "👋"
    "ðŸ‘¤" = "👤"
    "ðŸ”’" = "🔒"
    "â†’" = "→"
    "ðŸ“¤" = "📦"
    "â€“" = "–"
    "ðŸ”…" = "📅"
    "ðŸ“…" = "📅"
    "Â©" = "©"
    "â€”" = "—"
}

$files = Get-ChildItem -Path d:\workspace\payanname-sale4\RewardSystem -Recurse -Include *.cshtml
foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $content = [System.Text.Encoding]::UTF8.GetString($bytes)
    $originalContent = $content
    
    foreach ($key in $replacements.Keys) {
        $content = $content.Replace($key, $replacements[$key])
    }
    
    if ($content -cne $originalContent) {
        $newBytes = [System.Text.Encoding]::UTF8.GetBytes($content)
        [System.IO.File]::WriteAllBytes($file.FullName, $newBytes)
        Write-Host "Fixed emojis/symbols in $($file.FullName)"
    }
}
