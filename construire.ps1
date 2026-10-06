# Compile MascotteStickman.exe avec le compilateur C# livre avec Windows (.NET Framework 4).
# Usage : clic droit > Executer avec PowerShell, ou   powershell -ExecutionPolicy Bypass -File construire.ps1
# Le stickman en cours d'execution verrouille son exe : le quitter avant de recompiler.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$net = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
& "$net\csc.exe" /nologo /nowarn:618 /target:winexe /optimize+ /codepage:65001 /out:MascotteStickman.exe /win32icon:assets\stickman.ico `
    /r:"$net\WPF\PresentationFramework.dll" /r:"$net\WPF\PresentationCore.dll" /r:"$net\WPF\WindowsBase.dll" /r:System.Xaml.dll `
    /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
    /r:"$net\WPF\UIAutomationClient.dll" /r:"$net\WPF\UIAutomationTypes.dll" `
    src\Stickman.cs src\StickmanAnimations.cs src\StickmanOreille.cs src\StickmanCorrecteur.cs src\StickmanLangue.cs
if ($LASTEXITCODE -ne 0) { throw "La compilation de MascotteStickman a echoue." }
Write-Host "OK : MascotteStickman.exe"
