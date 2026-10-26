# XPath help

Open an XML document, then open XPatherizerNPP's search and results panels from the Plugins menu. Enter one XPath expression per line and run the search. Select a result to navigate back to the XML.

For the [sample catalog](https://github.com/debadattanaik/XPatherizerNPP-x64/blob/x64/tests/catalog.xml):

| Expression | Selects |
| --- | --- |
| `/catalog/book` | Both books |
| `/catalog/book[@id='1']` | The first book |
| `/catalog/book/title` | Both title elements |
| `//book` | Book elements anywhere in the document |

Select text in the query box to run only that selection. The settings dialog controls automatic search, namespace handling, and XML formatting. XML namespaces affect unprefixed XPath names; use the plugin's namespace settings when working with namespaced documents.

Return to the [project's x64 branch](https://github.com/debadattanaik/XPatherizerNPP-x64/tree/x64).
