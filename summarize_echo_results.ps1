param(
    [Parameter(Mandatory = $true)][string]$CsvPath,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$inv = [System.Globalization.CultureInfo]::InvariantCulture

if (-not (Test-Path $CsvPath)) {
    Write-Host "Не найден файл: $CsvPath"
    exit 1
}

if (-not $OutDir) {
    $OutDir = Split-Path -Parent $CsvPath
}

$rows = @(Import-Csv -Path $CsvPath -Encoding UTF8)

if ($rows.Count -eq 0) {
    Write-Host "В $CsvPath нет данных."
    exit 0
}

$hasTextId = $rows[0].PSObject.Properties.Name -contains 'text_id'

function ConvertTo-Number($value) {
    $result = 0.0
    $ok = [double]::TryParse([string]$value, [System.Globalization.NumberStyles]::Float, $inv, [ref]$result)

    if ($ok -and -not [double]::IsNaN($result) -and -not [double]::IsInfinity($result)) {
        return $result
    }

    return $null
}

function Get-Average($values) {
    $numbers = @($values | ForEach-Object { ConvertTo-Number $_ } | Where-Object { $null -ne $_ })

    if ($numbers.Count -eq 0) {
        return $null
    }

    return ($numbers | Measure-Object -Average).Average
}

function Format-Value($value, [string]$format) {
    if ($null -eq $value) {
        return ''
    }

    return $value.ToString($format, $inv)
}

function New-Summary($group, [string[]]$keys, [switch]$WithQuality) {
    $first = $group.Group[0]
    $item = [ordered]@{}

    foreach ($key in $keys) {
        $item[$key] = $first.$key
    }

    $matchAvg = Get-Average ($group.Group | ForEach-Object { $_.match })

    $item['runs'] = $group.Count
    $item['match_pct'] = Format-Value $(if ($null -ne $matchAvg) { $matchAvg * 100 } else { $null }) 'F1'
    $item['ber'] = Format-Value (Get-Average ($group.Group | ForEach-Object { $_.ber })) 'F6'
    $item['ber_raw'] = Format-Value (Get-Average ($group.Group | ForEach-Object { $_.ber_raw })) 'F6'

    if ($WithQuality) {
        $item['snr_db'] = Format-Value (Get-Average ($group.Group | ForEach-Object { $_.snr_db })) 'F2'
        $item['lsd_db'] = Format-Value (Get-Average ($group.Group | ForEach-Object { $_.lsd_db })) 'F3'
    }

    return [pscustomobject]$item
}

function Write-Section([string]$title, $items, [string]$fileName) {
    Write-Host ''
    Write-Host "  $title"
    ($items | Format-Table -AutoSize | Out-String -Width 300).TrimEnd() | Write-Host

    $path = Join-Path $OutDir $fileName
    $items | Export-Csv -Path $path -NoTypeInformation -Encoding UTF8
    Write-Host "  -> $path"
}

$embedRows = @($rows | Where-Object { $_.test -eq 'embed' })
$postRows = @($rows | Where-Object { $_.test -ne 'embed' })

$textCount = if ($hasTextId) { @($rows | ForEach-Object { $_.text_id } | Select-Object -Unique).Count } else { 1 }
$sourceCount = @($rows | ForEach-Object { $_.source } | Select-Object -Unique).Count

Write-Host ''
Write-Host '================================================================================'
Write-Host "  СРЕДНИЕ ЗНАЧЕНИЯ: строк $textCount, источников $sourceCount"
Write-Host '  match_pct - доля успешных извлечений, %; остальное - среднее арифметическое.'
Write-Host '================================================================================'

if ($embedRows.Count -gt 0) {
    $byMethod = @($embedRows | Group-Object method | ForEach-Object { New-Summary $_ @('method') -WithQuality })
    Write-Section 'Качество встраивания и базовое извлечение (по всем строкам и источникам):' $byMethod 'summary_embed.csv'

    if ($sourceCount -gt 1) {
        $bySource = @($embedRows | Group-Object source, method | ForEach-Object { New-Summary $_ @('source', 'method') -WithQuality })
        Write-Section 'Качество встраивания по источникам (среднее по строкам):' $bySource 'summary_embed_by_source.csv'
    }
}

if ($postRows.Count -gt 0) {
    $byTest = @($postRows | Group-Object method, test | ForEach-Object { New-Summary $_ @('method', 'test') })
    Write-Section 'Устойчивость к постобработке (по всем строкам и источникам):' $byTest 'summary_postproc.csv'
}

if ($hasTextId -and $textCount -gt 1) {
    $texts = @{}
    $textsPath = Join-Path (Split-Path -Parent $CsvPath) 'texts.csv'

    if (Test-Path $textsPath) {
        Import-Csv -Path $textsPath -Encoding UTF8 | ForEach-Object { $texts[$_.text_id] = $_.text }
    }

    $byText = @($rows | Group-Object text_id | ForEach-Object {
        $summary = New-Summary $_ @('text_id')
        $summary | Add-Member -NotePropertyName text -NotePropertyValue $texts[$_.Name]
        $summary
    })
    Write-Section 'По строкам (все методы и обработки вместе):' $byText 'summary_by_text.csv'
}
