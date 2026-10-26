XPatherizerNPP 2.10.2.0 for 64-bit Notepad++

Manual installation
===================

1. Close every 64-bit Notepad++ window.
2. Create this folder under the 64-bit Notepad++ directory:
   plugins\XPatherizerNPP
3. Copy XPatherizerNPP\XPatherizerNPP.dll into that folder.
4. Start 64-bit Notepad++ and open the Plugins menu.

For a standard 64-bit installation the final path is normally:
C:\Program Files\Notepad++\plugins\XPatherizerNPP\XPatherizerNPP.dll

Do not install this DLL into 32-bit Notepad++. The existing 32-bit plugin and
this 64-bit build can be kept in their respective Notepad++ installations.

Validation performed
====================

- PE machine: AMD64 (0x8664)
- File version: 2.10.2.0
- Required exports: beNotified, getFuncsArray, getName, isUnicode,
  messageProc, setInfo
- Loaded successfully in isolated Notepad++ 8.9.8.1 x64 portable instance
  using the 64-bit .NET Framework runtime.
- Built directly from the corrected C# source using build-x64.ps1.
- Corrected pointer arithmetic, Unicode filename buffer sizes, docking handles,
  notification header reads, and per-tab XPath initialization/reentrancy.
- Verified docked panels, exactly two results for /catalog/book in the sample
  XML, opening another tab, and restoration of per-tab XPath query text.
- Per-tab XPath state is enabled again.
- Results load descendants on expansion. Show All materializes the full tree.
- Auto Search waits for a 350 ms typing pause. Duplicate query evaluation and
  needless document serialization/reparsing are removed; sibling indices are cached.

Build and functional test
=========================
Run build-x64.ps1 from the repository root to build the production x64 DLL. This uses the installed
.NET Framework 4.8 compiler, Windows SDK ILDasm and framework ILAsm to generate
the six native exports without the obsolete exporter DLLs.
Run build-x64.ps1 -SelfTest only for the isolated test installation. That build
adds an in-process smoke test and logs to %TEMP%\XPatherizerNPP-smoketest.log.
The distributed production DLL does not include the smoke test.
Unexpected callback exceptions are logged to %TEMP%\XPatherizerNPP-errors.log.

Project, source, and download references use this branch:
https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64
XPath Help (W3 Schools) opens https://www.w3schools.com/xml/xpath_intro.asp
