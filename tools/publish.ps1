[CmdletBinding()]
param(
    [ValidateSet('Unsigned', 'WebPreview', 'Signed', 'Portable', 'Msi', 'Bundle', 'XmlStats')]
    [string]$Target = 'Unsigned',

    [ValidateSet('x64', 'x86', 'arm64')]
    [string]$Architecture = 'x64'
)

$ErrorActionPreference = 'Stop'
$architecture = $Architecture.ToLowerInvariant()
$repository = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repository 'src'
$packaging = Join-Path $source 'Packaging'
$artifacts = Join-Path $repository 'artifacts'

function Invoke-Build([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Get-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $path = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe' |
            Select-Object -First 1
        if ($path) {
            return $path
        }
    }

    throw 'Visual Studio MSBuild is required to build the MSIX package.'
}

function Publish-XmlStats([string]$Output) {
    Invoke-Build dotnet @('publish', (Join-Path $source 'XmlStats\XmlStats.csproj'), '-c', 'Release',
        '-r', "win-$architecture", '--self-contained', '-p:PublishSingleFile=true', '-o', $Output, '-warnaserror')
}

$packageDirectories = @{ Unsigned = 'msix'; WebPreview = 'web-preview'; Signed = 'web-release' }
if ($packageDirectories.ContainsKey($Target)) {
    $output = Join-Path $artifacts $packageDirectories[$Target]
    $web = $Target -ne 'Unsigned'
    $arguments = @((Join-Path $packaging 'Msix\XmlNotepadPackage.wapproj'), '/restore', '/t:Build',
        '/p:Configuration=Release', '/p:Platform=x64', '/p:UapAppxPackageBuildMode=SideloadOnly',
        '/v:minimal', "/p:AppxPackageDir=$output/")
    if ($Target -ne 'Signed') {
        $arguments += '/p:AppxPackageSigningEnabled=false', "/p:GenerateAppInstallerFile=$web"
    }
    Invoke-Build (Get-MSBuild) $arguments
}
else {
    $output = Join-Path $artifacts "$($Target.ToLowerInvariant())\win-$architecture"
    switch ($Target) {
        Portable {
            Invoke-Build dotnet @('publish', (Join-Path $source 'Application\XmlNotepad.csproj'), '-c', 'Release',
                '-r', "win-$architecture", "-p:Platform=$architecture", '-p:PublishProfile=Portable', '-o', $output, '-warnaserror')
            Publish-XmlStats $output
        }
        XmlStats { Publish-XmlStats $output }
        default {
            $project = if ($Target -eq 'Msi') { 'Msi\XmlNotepadSetup.wixproj' } else { 'Bundle\XmlNotepadBundle.wixproj' }
            Invoke-Build dotnet @('build', (Join-Path $packaging $project), '-c', 'Release',
                "-p:Platform=$architecture", '-o', $output, '-warnaserror')
        }
    }
}

Write-Host "Artifacts: $output"
