param([switch]$SelfTest)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'XPatherizerNPP\XPatherizerNPP.csproj'
$build = Join-Path $PSScriptRoot 'build-source-x64'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$ildasm = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\ildasm.exe'
New-Item -ItemType Directory -Force -Path $build | Out-Null
$testDefines = if ($SelfTest) { '/p:DefineConstants=PLUGIN_TEST' } else { '/p:DefineConstants=TRACE' }
& "$framework\MSBuild.exe" $project /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:SkipLegacyExports=true $testDefines /v:minimal
if ($LASTEXITCODE) { throw 'C# compilation failed' }
$compiled = Join-Path $PSScriptRoot 'XPatherizerNPP\bin\x64\Release\XPatherizerNPP.dll'
Push-Location $build
try {
    & $ildasm $compiled /out=XPatherizerNPP.il /utf8 /nobar
    if ($LASTEXITCODE) { throw 'IL disassembly failed' }
    $ilPath = Join-Path $build 'XPatherizerNPP.il'
    $il = [IO.File]::ReadAllText($ilPath)
    $il = $il -replace '\.corflags 0x[0-9A-Fa-f]+', '.corflags 0x00000000'
    $il += "`r`n.vtfixup [6] int64 fromunmanaged at EXPORT_TABLE`r`n.data EXPORT_TABLE = int64[6]`r`n"
    $names = @('isUnicode','setInfo','getFuncsArray','messageProc','getName','beNotified')
    for ($i = 0; $i -lt $names.Count; $i++) {
        $name = $names[$i]
        $pattern = '(?s)(\.method private hidebysig static\s+)(bool|void|native int|uint32)(\s+' + $name + '\(.*?cil managed\s*\{)'
        $match = [regex]::Match($il, $pattern)
        if (!$match.Success) { throw "Export method missing: $name" }
        $replacement = $match.Groups[1].Value + $match.Groups[2].Value +
            ' modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl)' +
            $match.Groups[3].Value + "`r`n    .vtentry 1 : $($i + 1)`r`n    .export [$($i + 1)] as $name"
        $il = $il.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
    }
    [IO.File]::WriteAllText($ilPath, $il, [Text.UTF8Encoding]::new($false))
    & "$framework\ilasm.exe" XPatherizerNPP.il /dll /x64 /output=XPatherizerNPP.dll /resource=XPatherizerNPP.res /quiet
    if ($LASTEXITCODE) { throw 'Native export assembly failed' }
    Copy-Item -LiteralPath (Join-Path $build 'XPatherizerNPP.dll') -Destination $compiled -Force
} finally { Pop-Location }
Write-Output "Built from source: $compiled"
