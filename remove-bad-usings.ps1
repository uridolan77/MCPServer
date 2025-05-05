# Get all service files in the Features folder
$serviceFiles = Get-ChildItem -Path "C:\dev\MCPServer\MCPServer.Core\Features" -Recurse -Filter "*.cs"

foreach ($file in $serviceFiles) {
    Write-Host "Processing file: $($file.FullName)"
    
    # Read the file content
    $content = Get-Content -Path $file.FullName -Raw
    
    # Check if there are any incorrectly inserted using statements
    if ($content -match "using MCPServer\.Core\.Features\.[^;]+\.Services\.Interfaces;\r?\n") {
        Write-Host "  Removing incorrectly inserted using statements"
        
        # Remove any incorrectly inserted using statements in the middle of the file
        $content = $content -replace "using MCPServer\.Core\.Features\.[^;]+\.Services\.Interfaces;\r?\n", ""
        
        # Write the updated content back to the file
        Set-Content -Path $file.FullName -Value $content
    }
}

Write-Host "Cleanup completed."
