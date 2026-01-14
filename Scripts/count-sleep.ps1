param($file)
$total = 0
Get-Content $file | ForEach-Object {
    if ($_ -match 'Sleep,\s*(\d+)') {
        $total += [int]$matches[1]
    }
}
Write-Host "$file : $total ms ($([math]::Round($total/1000/60, 2)) minutes)"
