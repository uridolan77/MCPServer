# Define the mapping of services to feature folders
$serviceMapping = @{
    # Auth Feature
    "UserService.cs" = "Auth";
    "CredentialService.cs" = "Auth";
    
    # Chat Feature
    "ChatPlaygroundService.cs" = "Chat";
    "ChatStreamingService.cs" = "Chat";
    
    # Llm Feature
    "LlmService.cs" = "Llm";
    
    # Models Feature
    "ModelService.cs" = "Models";
    
    # Providers Feature
    "LlmProviderService.cs" = "Providers";
    "LlmProviderSeeder.cs" = "Providers";
    
    # Sessions Feature
    "SessionService.cs" = "Sessions";
    "SessionContextService.cs" = "Sessions";
    "MySqlContextService.cs" = "Sessions";
    
    # Usage Feature
    "ChatUsageService.cs" = "Usage";
    
    # Shared Feature
    "DatabaseInitializer.cs" = "Shared";
    "TokenManager.cs" = "Auth";
}

# Define the mapping of interfaces to feature folders
$interfaceMapping = @{
    # Auth Feature
    "IUserService.cs" = "Auth";
    "ICredentialService.cs" = "Auth";
    "ITokenManager.cs" = "Auth";
    
    # Chat Feature
    "IChatPlaygroundService.cs" = "Chat";
    "IChatStreamingService.cs" = "Chat";
    
    # Llm Feature
    "ILlmService.cs" = "Llm";
    
    # Models Feature
    "IModelService.cs" = "Models";
    
    # Providers Feature
    "ILlmProviderService.cs" = "Providers";
    
    # Sessions Feature
    "ISessionService.cs" = "Sessions";
    "ISessionContextService.cs" = "Sessions";
    "IContextService.cs" = "Sessions";
    
    # Usage Feature
    "IChatUsageService.cs" = "Usage";
    
    # Rag Feature
    "IDocumentService.cs" = "Rag";
    "IEmbeddingService.cs" = "Rag";
    "IRagService.cs" = "Rag";
    "IVectorDbService.cs" = "Rag";
}

# Base paths
$servicesPath = "C:\dev\MCPServer\MCPServer.Core\Services"
$interfacesPath = "C:\dev\MCPServer\MCPServer.Core\Services\Interfaces"
$targetBasePath = "C:\dev\MCPServer\MCPServer.Core\Features"

# Create directories if they don't exist
foreach ($feature in ($serviceMapping.Values + $interfaceMapping.Values) | Select-Object -Unique) {
    $featureServicesPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services"
    $featureInterfacesPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\Interfaces"
    
    if (-not (Test-Path $featureServicesPath)) {
        New-Item -Path $featureServicesPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $featureServicesPath"
    }
    
    if (-not (Test-Path $featureInterfacesPath)) {
        New-Item -Path $featureInterfacesPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $featureInterfacesPath"
    }
}

# Move service files
foreach ($file in $serviceMapping.Keys) {
    $feature = $serviceMapping[$file]
    $sourceFilePath = Join-Path -Path $servicesPath -ChildPath $file
    $targetFilePath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\$file"
    
    if (Test-Path $sourceFilePath) {
        # Read the file content
        $content = Get-Content -Path $sourceFilePath -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services", "namespace MCPServer.Core.Features.$feature.Services"
        
        # Update using statements for interfaces
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.$feature.Services.Interfaces;"
        
        # Write the updated content to the target file
        Set-Content -Path $targetFilePath -Value $content
        
        Write-Host "Moved and updated: $file to $targetFilePath"
    } else {
        Write-Host "Source file not found: $sourceFilePath" -ForegroundColor Yellow
    }
}

# Move interface files
foreach ($file in $interfaceMapping.Keys) {
    $feature = $interfaceMapping[$file]
    $sourceFilePath = Join-Path -Path $interfacesPath -ChildPath $file
    $targetFilePath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\Interfaces\$file"
    
    if (Test-Path $sourceFilePath) {
        # Read the file content
        $content = Get-Content -Path $sourceFilePath -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services.Interfaces", "namespace MCPServer.Core.Features.$feature.Services.Interfaces"
        
        # Write the updated content to the target file
        Set-Content -Path $targetFilePath -Value $content
        
        Write-Host "Moved and updated: $file to $targetFilePath"
    } else {
        Write-Host "Source file not found: $sourceFilePath" -ForegroundColor Yellow
    }
}

# Move Llm subfolder
$llmSourcePath = "C:\dev\MCPServer\MCPServer.Core\Services\Llm"
$llmTargetPath = "C:\dev\MCPServer\MCPServer.Core\Features\Llm\Services\Llm"

if (Test-Path $llmSourcePath) {
    # Create target directory if it doesn't exist
    if (-not (Test-Path $llmTargetPath)) {
        New-Item -Path $llmTargetPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $llmTargetPath"
    }
    
    # Copy all files from the Llm subfolder
    Get-ChildItem -Path $llmSourcePath -File | ForEach-Object {
        $sourceFilePath = $_.FullName
        $targetFilePath = Join-Path -Path $llmTargetPath -ChildPath $_.Name
        
        # Read the file content
        $content = Get-Content -Path $sourceFilePath -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services.Llm", "namespace MCPServer.Core.Features.Llm.Services.Llm"
        
        # Update using statements
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.Llm.Services.Interfaces;"
        $content = $content -replace "using MCPServer.Core.Services.Llm;", "using MCPServer.Core.Features.Llm.Services.Llm;"
        
        # Write the updated content to the target file
        Set-Content -Path $targetFilePath -Value $content
        
        Write-Host "Moved and updated: $($_.Name) to $targetFilePath"
    }
}

# Move Rag subfolder
$ragSourcePath = "C:\dev\MCPServer\MCPServer.Core\Services\Rag"
$ragTargetPath = "C:\dev\MCPServer\MCPServer.Core\Features\Rag\Services\Rag"

if (Test-Path $ragSourcePath) {
    # Create target directory if it doesn't exist
    if (-not (Test-Path $ragTargetPath)) {
        New-Item -Path $ragTargetPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $ragTargetPath"
    }
    
    # Copy all files from the Rag subfolder
    Get-ChildItem -Path $ragSourcePath -File | ForEach-Object {
        $sourceFilePath = $_.FullName
        $targetFilePath = Join-Path -Path $ragTargetPath -ChildPath $_.Name
        
        # Read the file content
        $content = Get-Content -Path $sourceFilePath -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services.Rag", "namespace MCPServer.Core.Features.Rag.Services.Rag"
        
        # Update using statements
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.Rag.Services.Interfaces;"
        $content = $content -replace "using MCPServer.Core.Services.Rag;", "using MCPServer.Core.Features.Rag.Services.Rag;"
        
        # Write the updated content to the target file
        Set-Content -Path $targetFilePath -Value $content
        
        Write-Host "Moved and updated: $($_.Name) to $targetFilePath"
    }
}

Write-Host "Service migration completed."
