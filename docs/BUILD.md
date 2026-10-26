# Build and verification

Use the [x64 branch](https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64). Prerequisites are Windows x64, .NET Framework 4.8 with its framework compiler/MSBuild, and the Windows SDK .NET Framework tools containing x64 `ildasm.exe`.

The build script expects ILDasm at `C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\ildasm.exe`. Framework tools are found relative to `%WINDIR%`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-x64.ps1
```

The production DLL is written to `XPatherizerNPP\bin\x64\Release\XPatherizerNPP.dll`. The script compiles C#, disassembles the result, adds six native entry points with 64-bit vtable fixups, and assembles the final DLL. No bundled export-weaver binaries or NuGet restore are required. Building the project directly without this script deliberately fails to avoid producing a DLL with missing exports.

To test the distributed DLL:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test-x64.ps1
```

This checks the 24-byte notification header, native pointer arithmetic, a 259-character UTF-16 filename round trip, and saving before first buffer initialization.

For the in-process smoke test, build with `build-x64.ps1 -SelfTest`, install its output into an **isolated portable Notepad++ x64** copy, and launch that copy with `tests\catalog.xml` open. The test exercises startup, docking, an XPath query with exactly two expected book matches, and switching tabs with query restoration. Results are written to `%TEMP%\XPatherizerNPP-smoketest.log`. This test build opens tabs and changes query text; use it only in the isolated test copy. Rebuild without `-SelfTest` for normal use.

The checked-in production DLL contains no smoke-test code. The optional source is enabled only by the `PLUGIN_TEST` compilation symbol. The original x86 configurations remain in the solution, but this repository's build script and shipped binary target x64 only.
