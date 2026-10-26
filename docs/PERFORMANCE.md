# XPath search performance

Version 2.10.2 initially builds the matching result nodes and defers their descendants until expansion. Element labels no longer serialize whole subtrees. Sibling indices are cached; each query reuses its evaluated iterator; result groups update together on the UI thread. Documents without default namespaces avoid an unnecessary serialization and second parse.

Auto Search waits for a 350 ms pause in typing. Manual Search runs immediately. Expanding a node loads its immediate children; Show All deliberately materializes and expands everything, so it can still take longer on very large results. No search matches are capped or discarded.

## Synthetic comparison

The benchmark uses a generated **893,280-byte, 24,023-line XML document**, ten `line` matches, and this query:

```xpath
/server/responses/Session.getDocumentRs/session/data/policy/line
```

Each matched line contains 600 item elements. Separate x64 .NET Framework processes load the baseline and updated DLLs. After warm-up, each reports the median of three runs. On the development machine, initial XML preparation, namespace discovery, XPath evaluation, and result-tree construction measured approximately **794 ms for 2.10.1** and **13 ms for 2.10.2** (about 60 times faster for this workload).

This is a synthetic result-construction benchmark, not a measurement of the user's private XML or full editor latency. It excludes reading the editor buffer, painting the TreeView, and later expansion of deferred descendants. Structure, query, hardware, and match count affect timings.

## Reproduce

Run the following in Windows PowerShell, once per DLL in a separate process. Supply an old 2.10.1 DLL for the legacy baseline; the previous ZIP is retained in this [x64 branch](https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64/dist/x64).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -STA -File .\tests\benchmark-search.ps1 -PluginDll C:\path\to\2.10.1\XPatherizerNPP.dll -Legacy
powershell -NoProfile -ExecutionPolicy Bypass -STA -File .\tests\benchmark-search.ps1 -PluginDll .\dist\x64\XPatherizerNPP\XPatherizerNPP.dll
```

The updated run asserts ten matches, deferred descendants, expansion, a high sibling-index navigation path, selected-node export content, node removal, and full result expansion/path resolution against unnamespaced, default-namespace, and prefixed-namespace XML.

The optional in-process [smoke test](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/docs/BUILD.md) additionally exercises actual Notepad++ docking, result-to-editor navigation, tab-state persistence, scalar/empty/invalid queries, and cancellation/restart.
