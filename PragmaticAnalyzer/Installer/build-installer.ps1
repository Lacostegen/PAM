param(
    [ValidateSet("None", "One", "All")]
    [string]$ModelMode = "One",

    [string]$ModelFile = "",

    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$installerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Resolve-Path (Join-Path $installerDir "..")
$packageDir = Join-Path $installerDir "Package"
$outputDir = Join-Path $projectDir "bin\Installer"
$projectFile = Join-Path $projectDir "PragmaticAnalyzer.csproj"
$portableDir = Join-Path $projectDir "bin\Portable\PragmaticAnalyzerPortable"
$debugDir = Join-Path $projectDir "bin\Debug\net8.0-windows7.0"
$redistDir = Join-Path $installerDir "redist"
$vcRedist = Join-Path $redistDir "vc_redist.x64.exe"

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Child
    )

    $parentFull = [System.IO.Path]::GetFullPath($Parent)
    $childFull = [System.IO.Path]::GetFullPath($Child)
    if (-not $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe path outside workspace: $childFull"
    }
}

function Reset-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-ChildPath -Parent $projectDir -Child $Path
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Copy-DirectoryIfExists {
    param(
        [Parameter(Mandatory = $true)][string[]]$Candidates,
        [Parameter(Mandatory = $true)][string]$Destination,
        [string[]]$Exclude = @()
    )

    $source = $Candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $source) {
        Write-Host "Skipped: source not found for $Destination"
        return
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $copyArgs = @($source, $Destination, "/E", "/NFL", "/NDL", "/NJH", "/NJS", "/NP")
    foreach ($mask in $Exclude) {
        $copyArgs += @("/XF", $mask)
    }
    robocopy @copyArgs | Out-Null
    if ($LASTEXITCODE -gt 7) {
        throw "Robocopy failed for $source -> $Destination, exit code $LASTEXITCODE"
    }
}

function Remove-PathIfExists {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-ChildPath -Parent $projectDir -Child $Path
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

Reset-Directory -Path $outputDir

if (-not $SkipPublish) {
    Reset-Directory -Path $packageDir
    dotnet publish $projectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:PublishReadyToRun=false `
        -o $packageDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
elseif (-not (Test-Path -LiteralPath $packageDir)) {
    throw "Package folder does not exist: $packageDir"
}

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $debugDir "Database"),
        (Join-Path $portableDir "Database"),
        (Join-Path $projectDir "Database")
    ) `
    -Destination (Join-Path $packageDir "Database")

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $projectDir "Models"),
        (Join-Path $debugDir "Models"),
        (Join-Path $portableDir "Models")
    ) `
    -Destination (Join-Path $packageDir "Models")

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $debugDir "KnowledgeBase"),
        (Join-Path $portableDir "KnowledgeBase"),
        (Join-Path $projectDir "KnowledgeBase")
    ) `
    -Destination (Join-Path $packageDir "KnowledgeBase")

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $debugDir "ExploitText"),
        (Join-Path $portableDir "ExploitText"),
        (Join-Path $projectDir "ExploitText")
    ) `
    -Destination (Join-Path $packageDir "ExploitText")

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $projectDir "NativeLlama"),
        (Join-Path $debugDir "NativeLlama"),
        (Join-Path $portableDir "NativeLlama")
    ) `
    -Destination (Join-Path $packageDir "NativeLlama")

Copy-DirectoryIfExists `
    -Candidates @(
        (Join-Path $projectDir "SemanticSearchEngine"),
        (Join-Path $debugDir "SemanticSearchEngine"),
        (Join-Path $portableDir "SemanticSearchEngine")
    ) `
    -Destination (Join-Path $packageDir "SemanticSearchEngine") `
    -Exclude @("glove_model.npz")

$rootMatcher = Join-Path $projectDir "matcher.exe"
if (Test-Path -LiteralPath $rootMatcher) {
    Copy-Item -LiteralPath $rootMatcher -Destination (Join-Path $packageDir "matcher.exe") -Force
}

$packageTranslator = Join-Path $packageDir "Translator"
Reset-Directory -Path $packageTranslator
$sourceTranslator = @(
    (Join-Path $portableDir "Translator"),
    (Join-Path $debugDir "Translator"),
    (Join-Path $projectDir "Translator")
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if ($ModelMode -eq "All") {
    if (-not $sourceTranslator) {
        throw "Translator folder with GGUF models was not found."
    }

    $allModels = Get-ChildItem -LiteralPath $sourceTranslator -Filter "*.gguf"
    if (-not $allModels) {
        throw "Translator folder does not contain GGUF models."
    }

    foreach ($model in $allModels) {
        Copy-Item -LiteralPath $model.FullName -Destination $packageTranslator -Force
    }
}
elseif ($ModelMode -eq "One") {
    if (-not $sourceTranslator) {
        throw "Translator folder with GGUF models was not found."
    }

    if ([string]::IsNullOrWhiteSpace($ModelFile)) {
        $preferred = @(
            "YandexGPT-5-Lite-8B-instruct.Q3_K_M.gguf",
            "yandexgpt-5-lite-8b-instruct-q5_k_m.gguf",
            "gemma-2b-it-Q4_K_M.gguf"
        )

        foreach ($name in $preferred) {
            $candidate = Join-Path $sourceTranslator $name
            if (Test-Path -LiteralPath $candidate) {
                $ModelFile = $candidate
                break
            }
        }

        if ([string]::IsNullOrWhiteSpace($ModelFile)) {
            $ModelFile = Get-ChildItem -LiteralPath $sourceTranslator -Filter "*.gguf" |
                Sort-Object Length |
                Select-Object -First 1 -ExpandProperty FullName
        }
    }

    if (-not (Test-Path -LiteralPath $ModelFile)) {
        throw "GGUF model was not found: $ModelFile"
    }

    Copy-Item -LiteralPath $ModelFile -Destination $packageTranslator -Force
}

Get-ChildItem -LiteralPath $packageDir -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force

Remove-PathIfExists -Path (Join-Path $packageDir "SemanticSearchEngine\__pycache__")

foreach ($culture in @("cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "tr", "zh-Hans", "zh-Hant")) {
    Remove-PathIfExists -Path (Join-Path $packageDir $culture)
}

New-Item -ItemType Directory -Path $redistDir -Force | Out-Null
if (-not (Test-Path -LiteralPath $vcRedist)) {
    $knownRedist = @(
        "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Redist\MSVC\14.51.36231\vc_redist.x64.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Redist\MSVC\v145\vc_redist.x64.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

    if ($knownRedist) {
        Copy-Item -LiteralPath $knownRedist -Destination $vcRedist -Force
    }
}

$iss = Join-Path $installerDir "PragmaticAnalyzer.iss"
$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
    $isccCandidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    $isccPath = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($isccPath) {
        $iscc = Get-Item -LiteralPath $isccPath
    }
}

if (-not $iscc) {
    $innoCompilerProject = Join-Path $installerDir "InnoCompiler.csproj"
    $nugetPackageRoot = Join-Path $installerDir ".nuget"
    if (Test-Path -LiteralPath $innoCompilerProject) {
        dotnet restore $innoCompilerProject -p:RestorePackagesPath=$nugetPackageRoot
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore for Inno Setup compiler failed with exit code $LASTEXITCODE"
        }

        $packageIscc = Get-ChildItem -LiteralPath $nugetPackageRoot -Filter "ISCC.exe" -Recurse |
            Select-Object -First 1
        if ($packageIscc) {
            $iscc = $packageIscc
        }
    }
}

if ($iscc) {
    & $iscc.FullName $iss
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compiler failed with exit code $LASTEXITCODE"
    }
    Write-Host "Installer is ready: $(Join-Path $outputDir 'SPPR-ZI-Setup-1.0.exe')"
}
else {
    Write-Host "Package prepared: $packageDir"
    Write-Host "Inno Setup Compiler was not found. Install Inno Setup 6 and run:"
    Write-Host "  iscc Installer\PragmaticAnalyzer.iss"
}
