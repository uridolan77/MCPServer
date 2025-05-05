# Get all repository implementation files
$repositoryFiles = Get-ChildItem -Path C:\dev\MCPServer\MCPServer.Core\Data\Repositories -Filter "*.cs" | Select-Object -ExpandProperty FullName

# Update each file
foreach ($file in $repositoryFiles) {
    Write-Host "Updating file: $file"
    
    # Read the file content
    $content = Get-Content -Path $file -Raw
    
    # Replace the using statements
    $content = $content -replace "using MCPServer\.Core\.Services;", "using MCPServer.Core.Features.Shared.Services;"
    $content = $content -replace "using MCPServer\.Core\.Services\.Interfaces;", "using MCPServer.Core.Features.Shared.Services.Interfaces;"
    
    # Write the updated content back to the file
    Set-Content -Path $file -Value $content
}

Write-Host "Repository files update completed."
