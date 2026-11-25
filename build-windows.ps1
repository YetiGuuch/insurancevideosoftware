param(
    [Parameter(Mandatory=$true)][string]$NodeDirectory,
    [string]$Output = (Join-Path $PSScriptRoot 'VideoActivityReview.exe')
)
$ErrorActionPreference = 'Stop'
$nodeDir = (Resolve-Path -LiteralPath $NodeDirectory).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$node = Join-Path $nodeDir 'node.exe'
$license = Join-Path $nodeDir 'LICENSE'
$compressedPath = Join-Path ([IO.Path]::GetTempPath()) ("video-review-node-" + [guid]::NewGuid() + ".gz")
try {
    $source = [IO.File]::OpenRead($node)
    try {
        $destination = [IO.File]::Create($compressedPath)
        try {
            $zip = [IO.Compression.GZipStream]::new($destination, [IO.Compression.CompressionMode]::Compress, $true)
            try { $source.CopyTo($zip) } finally { $zip.Dispose() }
        } finally { $destination.Dispose() }
    } finally { $source.Dispose() }
    $arguments = @(
        '/nologo', '/target:winexe', '/platform:x64', '/optimize+',
        '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
        "/out:$([IO.Path]::GetFullPath($Output))", "/resource:$compressedPath,node.exe.gz",
        "/resource:$license,NODE-LICENSE.txt"
    )
    foreach ($asset in 'index.html','server.mjs','engine.mjs') {
        $arguments += "/resource:$(Join-Path $PSScriptRoot $asset),$asset"
    }
    & $compiler @arguments (Join-Path $PSScriptRoot 'Launcher.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Windows executable build failed.' }
} finally {
    if (Test-Path -LiteralPath $compressedPath) { Remove-Item -LiteralPath $compressedPath }
}
