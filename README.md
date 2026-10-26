# XPatherizerNPP x64

XPath query analyzer for **64-bit Notepad++**, maintained on the [x64 branch](https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64).

Version **2.10.2.0** adds faster XPath result construction and expandable results to the source-built x64 startup fixes. The plugin uses .NET Framework 4.8.

## Install

1. Download the [x64 ZIP](https://github.com/debadattanaik/XPatherizerNPP-x64/raw/refs/heads/x64/dist/x64/XPatherizerNPP-x64-2.10.2.zip) or [DLL](https://github.com/debadattanaik/XPatherizerNPP-x64/raw/refs/heads/x64/dist/x64/XPatherizerNPP/XPatherizerNPP.dll).
2. Close all Notepad++ windows.
3. Place `XPatherizerNPP.dll` inside `plugins\XPatherizerNPP\` under your **64-bit** Notepad++ installation. For a standard installation the complete path is `C:\Program Files\Notepad++\plugins\XPatherizerNPP\XPatherizerNPP.dll`.
4. Reopen Notepad++ and use its **Plugins → XPatherizerNPP** menu. The About dialog should show **2.10.2.0**.

See the [installation guide](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/INSTALL.md) for troubleshooting.

## Features

- Evaluate multiple XPath queries and navigate from results back to XML.
- Query selected text, enable automatic searching, and retain queries per tab.
- Beautify and verify XML.
- Export selected results or remove selected XML nodes.
- Save XML and XPath queries together in XPML files.

Search initially builds the matched nodes only; expand a result to load its children. **Show All** still loads and expands the complete result tree. Auto Search waits for a 350 ms pause in typing. See the [performance notes and reproducible benchmark](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/PERFORMANCE.md).

## Build and test

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-x64.ps1` from this repository. Build prerequisites and verification commands are in the [build guide](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/BUILD.md).

The source build was tested in Notepad++ **8.9.8.1 x64**: startup, docked panels, two expected XPath matches, new tabs, and per-tab query restoration. Direct tests cover high memory addresses, a 259-character Unicode filename, and saving before first buffer initialization. This does not establish compatibility with every Notepad++ version or test every legacy command.

## Documentation

- [XPath examples](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/XPATH.md)
- [Keyboard shortcuts](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/KEYBOARD.md)
- [Changes](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/CHANGELOG.md)
- [Attribution and license information](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/NOTICE.md)

Project and download links target this repository's `x64` branch. The XPath Help (W3 Schools) menu opens the W3Schools XPath introduction. Original authorship notices remain preserved.
