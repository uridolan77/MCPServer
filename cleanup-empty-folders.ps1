# Get all empty folders in the Features directory
$emptyFolders = Get-ChildItem -Path C:\dev\MCPServer\MCPServer.Core\Features -Recurse -Directory | 
    Where-Object { (Get-ChildItem -Path $_.FullName -Recurse -File).Count -eq 0 } |
    Select-Object -ExpandProperty FullName

# Remove each empty folder
foreach ($folder in $emptyFolders) {
    Write-Host "Removing empty folder: $folder"
    Remove-Item -Path $folder -Force -Recurse
}

Write-Host "Empty folders cleanup completed."
