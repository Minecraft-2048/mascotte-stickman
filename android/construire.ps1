# Fabrique MascotteStickman.apk : le fond d'ecran anime pour Android.
# Usage :   powershell -ExecutionPolicy Bypass -File android\construire.ps1  [-Sdk C:\chemin\du\sdk]  [-Jdk C:\chemin\du\jdk]
#
# Il faut le SDK Android (une plateforme et des build-tools : sdkmanager "platforms;android-34" "build-tools;34.0.0")
# et un JDK. Pas de Gradle : les outils du SDK sont appeles l'un apres l'autre (aapt2, javac, d8, zipalign, apksigner).
#
# La cle de signature est creee au premier lancement, hors du depot, dans %LOCALAPPDATA%\MascotteStickman\android.
# Il faut la garder : Android refuse de mettre a jour une appli signee avec une autre cle (il faudrait la desinstaller).
param([string]$Sdk = '', [string]$Jdk = '')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not $Sdk) { $Sdk = @($env:ANDROID_SDK_ROOT, $env:ANDROID_HOME, "$env:USERPROFILE\AndroidSdk", "$env:LOCALAPPDATA\Android\Sdk") | Where-Object { $_ -and (Test-Path "$_\build-tools") } | Select-Object -First 1 }
if (-not $Sdk) { throw "SDK Android introuvable : passer -Sdk, ou definir ANDROID_SDK_ROOT." }
if (-not $Jdk) { $Jdk = @('C:\Program Files\Java\jdk-21', $env:JAVA_HOME) | Where-Object { $_ -and (Test-Path "$_\bin\javac.exe") } | Select-Object -First 1 }
if (-not $Jdk) { throw "JDK introuvable : passer -Jdk, ou definir JAVA_HOME." }
$Jdk = $Jdk.TrimEnd('\')
$env:JAVA_HOME = $Jdk
$env:Path = "$Jdk\bin;$env:Path"
$outils = (Get-ChildItem "$Sdk\build-tools" -Directory | Sort-Object Name | Select-Object -Last 1).FullName
$android = (Get-ChildItem "$Sdk\platforms" -Directory | Sort-Object { [int]($_.Name -replace '\D', '') } | Select-Object -Last 1).FullName + '\android.jar'
if (-not (Test-Path $android)) { throw "android.jar introuvable dans $Sdk\platforms." }

function Outil {
    $programme = $args[0]
    $reste = @($args | Select-Object -Skip 1)
    & $programme @reste
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path $programme -Leaf) a echoue (code $LASTEXITCODE)." }
}

# le numero de version est celui de la version Windows (src\Stickman.cs) : 1.11.0 -> code 11100
$version = [regex]::Match((Get-Content ..\src\Stickman.cs -Raw), 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)').Groups
$nom = "$($version[1].Value).$($version[2].Value).$($version[3].Value)"
$code = [int]$version[1].Value * 10000 + [int]$version[2].Value * 100 + [int]$version[3].Value

# les animations : exportees par la version Windows quand elle est compilee a cote
if (Test-Path ..\MascotteStickman.exe) { Start-Process ..\MascotteStickman.exe -ArgumentList '--android', "`"$PSScriptRoot\assets\animations.bin`"" -Wait }
if (-not (Test-Path assets\animations.bin)) { throw "assets\animations.bin manque : compiler d'abord la version Windows (construire.ps1 a la racine)." }

if (Test-Path obj) { Remove-Item obj -Recurse -Force }
New-Item -ItemType Directory -Force obj\classes, obj\gen, obj\dex | Out-Null

Outil "$outils\aapt2.exe" compile --dir res -o obj\res.zip
Outil "$outils\aapt2.exe" link -o obj\base.apk -I $android --manifest AndroidManifest.xml -A assets --java obj\gen `
    --min-sdk-version 24 --target-sdk-version 34 --version-code $code --version-name $nom obj\res.zip

$sources = @(Get-ChildItem src, obj\gen -Recurse -Filter *.java | ForEach-Object FullName)
# -parameters : depuis Java 21, javac note les parametres caches des classes internes sans leur donner de nom,
# et le d8 des build-tools 34 plante dessus (NullPointerException) ; avec leurs noms, il les accepte.
Outil "$Jdk\bin\javac.exe" -source 8 -target 8 -parameters -bootclasspath $android -encoding UTF-8 '-Xlint:-options' -d obj\classes @sources
Outil "$Jdk\bin\jar.exe" cf obj\classes.jar -C obj\classes .
Outil "$outils\d8.bat" --lib $android --min-api 24 --output obj\dex obj\classes.jar

# le code rejoint les ressources dans l'archive ; jar garde tels quels les fichiers deja presents (resources.arsc doit rester non compresse)
Copy-Item obj\base.apk obj\non-aligne.apk
Outil "$Jdk\bin\jar.exe" uf obj\non-aligne.apk -C obj\dex classes.dex
Outil "$outils\zipalign.exe" -f -p 4 obj\non-aligne.apk obj\aligne.apk

$cle = "$env:LOCALAPPDATA\MascotteStickman\android"
if (-not (Test-Path "$cle\signature.jks")) {
    New-Item -ItemType Directory -Force $cle | Out-Null
    $octets = New-Object byte[] 24
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($octets)
    [IO.File]::WriteAllText("$cle\signature.txt", (($octets | ForEach-Object { $_.ToString('x2') }) -join ''), (New-Object Text.ASCIIEncoding))
    Outil "$Jdk\bin\keytool.exe" -genkeypair -keystore "$cle\signature.jks" -storetype PKCS12 '-storepass:file' "$cle\signature.txt" -alias stickman `
        -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=Mascotte Stickman'
    Write-Host "Cle de signature creee dans $cle : a garder precieusement."
}
Outil "$outils\apksigner.bat" sign --ks "$cle\signature.jks" --ks-pass "file:$cle\signature.txt" --ks-key-alias stickman --out MascotteStickman.apk obj\aligne.apk
Outil "$outils\apksigner.bat" verify MascotteStickman.apk
Remove-Item MascotteStickman.apk.idsig -ErrorAction SilentlyContinue
Write-Host "OK : android\MascotteStickman.apk (version $nom, code $code)"
