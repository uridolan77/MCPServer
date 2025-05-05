# Define the mapping of interfaces to their feature folders
$interfaceMapping = @{
    # Auth Feature
    "IUserService" = "Auth";
    "ICredentialService" = "Auth";
    "ITokenManager" = "Auth";
    
    # Chat Feature
    "IChatPlaygroundService" = "Chat";
    "IChatStreamingService" = "Chat";
    
    # Llm Feature
    "ILlmService" = "Llm";
    
    # Models Feature
    "IModelService" = "Models";
    
    # Providers Feature
    "ILlmProviderService" = "Providers";
    
    # Sessions Feature
    "ISessionService" = "Sessions";
    "ISessionContextService" = "Sessions";
    "IContextService" = "Sessions";
    
    # Usage Feature
    "IChatUsageService" = "Usage";
    
    # Rag Feature
    "IDocumentService" = "Rag";
    "IEmbeddingService" = "Rag";
    "IRagService" = "Rag";
    "IVectorDbService" = "Rag";
}

# Get all service files in the Features folder
$serviceFiles = Get-ChildItem -Path "C:\dev\MCPServer\MCPServer.Core\Features" -Recurse -Filter "*.cs" | Where-Object { $_.Name -like "*Service.cs" -and $_.Name -notlike "I*Service.cs" }

foreach ($file in $serviceFiles) {
    Write-Host "Processing file: $($file.FullName)"
    
    # Read the file content
    $content = Get-Content -Path $file.FullName -Raw
    
    # Remove any incorrectly inserted using statements
    $content = $content -replace "using MCPServer\.Core\.Features\.[^;]+\.Services\.Interfaces;\r?\n", ""
    
    # Get the current feature from the file path
    $currentFeature = $file.FullName -replace ".*\\Features\\([^\\]+)\\.*", '$1'
    
    # Create a list to store the using statements we need to add
    $usingsToAdd = @()
    
    # Check which interfaces are used in the file
    foreach ($interface in $interfaceMapping.Keys) {
        $feature = $interfaceMapping[$interface]
        
        # Only add using statements for interfaces from other features
        if ($content -match $interface -and $currentFeature -ne $feature) {
            $usingStatement = "using MCPServer.Core.Features.$feature.Services.Interfaces;"
            if (-not $usingsToAdd.Contains($usingStatement)) {
                $usingsToAdd += $usingStatement
            }
        }
    }
    
    # Add the using statements after the last existing using statement
    if ($usingsToAdd.Count -gt 0) {
        Write-Host "  Adding using statements for cross-feature interfaces"
        
        # Find the last using statement
        $lastUsingIndex = $content.LastIndexOf("using ") 
        $semicolonIndex = $content.IndexOf(";", $lastUsingIndex)
        $newlineIndex = $content.IndexOf("`n", $semicolonIndex)
        
        if ($lastUsingIndex -ge 0 -and $semicolonIndex -ge 0 -and $newlineIndex -ge 0) {
            $insertPoint = $newlineIndex + 1
            $usingsText = [string]::Join("`r`n", $usingsToAdd) + "`r`n"
            $content = $content.Insert($insertPoint, $usingsText)
        }
    }
    
    # Write the updated content back to the file
    Set-Content -Path $file.FullName -Value $content
}

Write-Host "Reference fixing completed."
