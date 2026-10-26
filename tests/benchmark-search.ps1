param([Parameter(Mandatory=$true)][string]$PluginDll, [switch]$Legacy)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path $PluginDll).Path)
$main = $assembly.GetType('XPatherizerNPP.Main', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$main.GetField('settings').SetValue($null, [Activator]::CreateInstance($assembly.GetType('XPatherizerNPP.XPatherizerSettings')))
$formType = $assembly.GetType('XPatherizerNPP.XPathResultsForm', $true)
$form = [Activator]::CreateInstance($formType)
$lazyType = $assembly.GetType('XPatherizerNPP.XPathResultsForm+LazyResultNode')
$query = '/server/responses/Session.getDocumentRs/session/data/policy/line'
$builder = New-Object Text.StringBuilder
[void]$builder.AppendLine('<server><responses><Session.getDocumentRs><session><data><policy>')
for ($line=0; $line -lt 10; $line++) {
    [void]$builder.AppendLine('<line id="' + $line + '">')
    for ($item=0; $item -lt 600; $item++) {
        [void]$builder.AppendLine('  <item id="' + $item + '">')
        [void]$builder.AppendLine('    <value>' + ('x' * 75) + '</value>')
        [void]$builder.AppendLine('    <flag>true</flag>')
        [void]$builder.AppendLine('  </item>')
    }
    [void]$builder.AppendLine('</line>')
}
[void]$builder.AppendLine('</policy></data></session></Session.getDocumentRs></responses></server>')
$xml = $builder.ToString()
Write-Output ('Fixture: {0} bytes; {1} lines; query: {2}' -f [Text.Encoding]::UTF8.GetByteCount($xml), ($xml.Split("`n").Length), $query)
$samples = @()
try {
    for ($trial=0; $trial -lt 4; $trial++) {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $document = New-Object Xml.XmlDocument
        $document.LoadXml($xml)
        if ($Legacy) {
            $before = $document.OuterXml.Length
            $document = $main.GetMethod('RemoveDefaultNamespaces', [type[]]@([Xml.XmlDocument])).Invoke($null, @($document.PSObject.BaseObject))
            $removed = $document.OuterXml.Length -ne $before
        } else {
            $parameters = [object[]]@($document.PSObject.BaseObject, $false)
            $document = $main.GetMethod('RemoveDefaultNamespaces', [type[]]@([Xml.XmlDocument], [bool].MakeByRefType())).Invoke($null, $parameters)
        }
        $navigator = $document.CreateNavigator()
        $namespaces = New-Object Xml.XmlNamespaceManager($document.NameTable)
        $main.GetMethod('GetNameSpaces', $flags, $null, [type[]]@([Xml.XmlNode], [Xml.XmlNamespaceManager]), $null).Invoke($null,
            [object[]]@($document.DocumentElement.PSObject.BaseObject, $namespaces.PSObject.BaseObject)) | Out-Null
        $expression = [Xml.XPath.XPathExpression]::Compile($query, $namespaces)
        $result = $navigator.Evaluate($expression)
        if ($Legacy) { $result = $navigator.Select($query, $namespaces); $count = $result.Count }
        $matches = New-Object 'Collections.Generic.List[System.Windows.Forms.TreeNode]'
        $indices = New-Object 'Collections.Generic.Dictionary[System.Xml.XmlNode,int]'
        while ($result.MoveNext()) {
            $source = ([Xml.IHasXmlNode]$result.Current).GetNode()
            if ($Legacy) {
                $children = $formType.GetMethod('GetChildren', [Reflection.BindingFlags]'Instance,NonPublic').Invoke($form, @($source.PSObject.BaseObject, $false))
                $node = New-Object Windows.Forms.TreeNode($source.Name, [Windows.Forms.TreeNode[]]$children)
                $node.Tag = $main.GetMethod('FindXPath', $flags).Invoke($null, @($source.PSObject.BaseObject, $false))
            } else {
                $node = [Activator]::CreateInstance($lazyType, [Reflection.BindingFlags]'Instance,NonPublic', $null, [object[]]@($source.PSObject.BaseObject, $false, $indices.PSObject.BaseObject), $null)
            }
            $matches.Add($node)
        }
        $watch.Stop()
        if ($matches.Count -ne 10) { throw 'Expected ten matches' }
        if ($trial -gt 0) { $samples += $watch.Elapsed.TotalMilliseconds }
    }
    $median = ($samples | Sort-Object)[1]
    Write-Output ('Initial result construction median: {0:N2} ms ({1})' -f $median, $(if($Legacy){'legacy'}else{'optimized'}))
    if (!$Legacy) {
        foreach ($node in $matches) {
            if ($node.Nodes.Count -ne 1 -or $null -ne $node.Nodes[0].Tag) { throw 'Expected deferred children' }
            $selected = $document.SelectSingleNode([string]$node.Tag)
            if ($selected.Name -ne 'line') { throw 'Incorrect navigation/export/removal path' }
        }
        $lazyType.GetMethod('LoadChildren', [Reflection.BindingFlags]'Instance,NonPublic').Invoke($matches[0], $null)
        if ($matches[0].Nodes.Count -ne 601) { throw 'Expected one attribute plus 600 item children' }
        $last = $matches[0].Nodes[600]
        $selected = $document.SelectSingleNode([string]$last.Tag)
        if ($selected.Attributes['id'].Value -ne '599') { throw 'Incorrect sibling index' }
        if (!$selected.OuterXml.Contains('<value>')) { throw 'Incorrect export content' }
        $main.GetMethod('RemoveNode').Invoke($null, @($selected.PSObject.BaseObject))
        if ($document.SelectNodes($query + '[1]/item').Count -ne 599) { throw 'Incorrect removal path' }
        Write-Output 'PASS: 10 matches, deferred children, expansion, sibling path, export content, node removal'
        foreach ($inputXml in @(
            '<root><item id="a"><value>one</value></item><item id="b"><value>two</value></item></root>',
            '<root xmlns="urn:default"><item id="a"><value>one</value></item><item id="b"><value>two</value></item></root>',
            '<root xmlns:p="urn:prefix"><p:item id="a"><p:value>one</p:value></p:item></root>'
        )) {
            $original = New-Object Xml.XmlDocument
            $original.LoadXml($inputXml)
            $parameters = [object[]]@($original.PSObject.BaseObject, $false)
            $normalized = $main.GetMethod('RemoveDefaultNamespaces', [type[]]@([Xml.XmlDocument], [bool].MakeByRefType())).Invoke($null, $parameters)
            $manager = New-Object Xml.XmlNamespaceManager($original.NameTable)
            $manager.AddNamespace('p', 'urn:prefix')
            $indices = New-Object 'Collections.Generic.Dictionary[System.Xml.XmlNode,int]'
            $pending = New-Object 'Collections.Generic.Stack[System.Windows.Forms.TreeNode]'
            $root = [Activator]::CreateInstance($lazyType, [Reflection.BindingFlags]'Instance,NonPublic', $null,
                [object[]]@($normalized.DocumentElement.PSObject.BaseObject, [bool]$parameters[1], $indices.PSObject.BaseObject), $null)
            $pending.Push($root)
            while ($pending.Count -gt 0) {
                $node = $pending.Pop()
                $lazyType.GetMethod('LoadChildren', [Reflection.BindingFlags]'Instance,NonPublic').Invoke($node, $null)
                if ($null -eq $original.SelectSingleNode([string]$node.Tag, $manager)) { throw ('Invalid namespace/descendant path: ' + $node.Tag) }
                foreach ($child in $node.Nodes) { $pending.Push($child) }
            }
        }
        Write-Output 'PASS: full descendant expansion and result paths against original unnamespaced/default/prefixed XML'
    }
} finally { $form.Dispose() }
