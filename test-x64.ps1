$ErrorActionPreference = 'Stop'
$dll = Join-Path $PSScriptRoot 'dist\x64\XPatherizerNPP\XPatherizerNPP.dll'
$assembly = [Reflection.Assembly]::LoadFile($dll)
$header = $assembly.GetType('NppPluginNET.Sci_NotifyHeader', $true)
if ([Runtime.InteropServices.Marshal]::SizeOf([Activator]::CreateInstance($header)) -ne 24) { throw 'Incorrect x64 header size' }
$arrayType = $assembly.GetType('NppPluginNET.ClikeStringArray', $true)
$array = [Activator]::CreateInstance($arrayType, @([int]2, [int]260))
try {
    $pointer = $array.NativePointer
    $first = [Runtime.InteropServices.Marshal]::ReadIntPtr($pointer)
    $text = ('x' * 258) + [char]0x03A9
    $bytes = [Text.Encoding]::Unicode.GetBytes($text + [char]0)
    [Runtime.InteropServices.Marshal]::Copy($bytes, 0, $first, $bytes.Length)
    $strings = $array.ManagedStringsUnicode
    if ($strings[0] -cne $text -or $strings[1] -ne '') { throw 'Unicode buffer round-trip failed' }
    Write-Output ('PASS: native pointer 0x{0:X}, 259-character Unicode filename, empty second buffer' -f $pointer.ToInt64())
} finally { $array.Dispose() }
$tempType = $assembly.GetType('XPatherizerNPP.XPathTemp', $true)
$temp = [Activator]::CreateInstance($tempType, @([IO.Path]::Combine([IO.Path]::GetTempPath(), [guid]::NewGuid().ToString() + '.tmp')))
$temp.Save('/before-first-buffer')
Write-Output 'PASS: saving before first buffer initialization does not throw'
Write-Output 'PASS: x64 notification header is 24 bytes'
