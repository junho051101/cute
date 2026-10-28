# Splits the title painting into animatable layers for Assets/Resources/Title.
# Usage (from the project root): powershell -File tools/title_splitter/split.ps1
param(
    [string]$Src = "art_reference/concepts/loading/title_main.png",
    [string]$Out = "Assets/Resources/Title"
)
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Join-Path $PSScriptRoot "TitleSplitter.cs") -ReferencedAssemblies System.Drawing
[TitleSplitter]::Run((Resolve-Path (Join-Path $root $Src)).Path, (Join-Path $root $Out))
