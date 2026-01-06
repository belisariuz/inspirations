# InspirationTab Build Script
# This script compiles the C# source into the Assemblies/InspirationTab.dll

$modRoot = Get-Item ".." | Select-Object -ExpandProperty FullName
$managedPath = Join-Path (Get-Item "..\..\.." | Select-Object -ExpandProperty FullName) "RimWorldWin64_Data\Managed"
$outPath = Join-Path $modRoot "Assemblies\InspirationTab.dll"

Write-Host "--- InspirationTab Build System ---"
Write-Host "Mod Root: $modRoot"
Write-Host "Managed Folder: $managedPath"
Write-Host "Output DLL: $outPath"

# References needed for RimWorld 1.6
$refs = @(
    "netstandard.dll",
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.AudioModule.dll"
)

# Build the reference string
$refStrings = @()
foreach ($r in $refs) {
    $fullPath = Join-Path $managedPath $r
    if (Test-Path $fullPath) {
        $refStrings += "/reference:`"$fullPath`""
    }
    else {
        Write-Warning "Missing reference: $r"
    }
}

# Find CSC
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "csc"
}

Write-Host "Compiling..."
$sources = Get-ChildItem -Filter *.cs | Select-Object -ExpandProperty FullName
& $csc /target:library /out:$outPath /nologo /warn:0 $refStrings $sources

if ($LASTEXITCODE -eq 0) {
    Write-Host "--- BUILD SUCCESSFUL ---" -ForegroundColor Green
}
else {
    Write-Error "--- BUILD FAILED ---"
}
