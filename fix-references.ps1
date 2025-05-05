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
    
    # Add using statements for each interface used in the file
    foreach ($interface in $interfaceMapping.Keys) {
        $feature = $interfaceMapping[$interface]
        
        # Check if the interface is used in the file but not in the same feature
        $currentFeature = $file.FullName -replace ".*\\Features\\([^\\]+)\\.*", '$1'
        
        if ($content -match $interface -and $currentFeature -ne $feature) {
            $usingStatement = "using MCPServer.Core.Features.$feature.Services.Interfaces;"
            
            # Check if the using statement already exists
            if (-not ($content -match [regex]::Escape($usingStatement))) {
                Write-Host "  Adding using statement for $interface from $feature feature"
                
                # Add the using statement after the last using statement
                $content = $content -replace "(using [^;]+;\r?\n)(?!using)", "`$1$usingStatement`r`n"
            }
        }
    }
    
    # Write the updated content back to the file
    Set-Content -Path $file.FullName -Value $content
}

Write-Host "Reference fixing completed."
