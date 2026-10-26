# Installation

Download the [2.10.1 x64 ZIP](https://github.com/debadattanaik/XPatherizerNPP-x64/raw/refs/heads/x64/dist/x64/XPatherizerNPP-x64-2.10.1.zip). Extract `XPatherizerNPP.dll` and copy it into `plugins\XPatherizerNPP\` under the Notepad++ installation directory while all Notepad++ windows are closed. Administrator approval may be required when writing under Program Files.

The host must be **64-bit Notepad++**. This DLL cannot load in the 32-bit edition. .NET Framework 4.8 must be installed.

After reopening Notepad++, select Plugins → XPatherizerNPP → About. The full file version is **2.10.1.0**.

If an earlier DLL was installed, replace it; do not put two copies in different plugin folders. Unexpected managed callback failures are recorded in `%TEMP%\XPatherizerNPP-errors.log`. Report the Notepad++ version, plugin file version, and the relevant exception when describing a failure. Review diagnostic paths before sharing logs.

The source and documentation for this build are on the [x64 branch](https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64).
