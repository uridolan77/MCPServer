# Define the mapping of services to feature folders
$serviceMapping = @{
    # Auth Feature
    "UserService.cs" = "Auth";
    "IUserService.cs" = "Auth";
    "CredentialService.cs" = "Auth";
    "ICredentialService.cs" = "Auth";
    
    # Chat Feature
    "ChatPlaygroundService.cs" = "Chat";
    "IChatPlaygroundService.cs" = "Chat";
    "ChatStreamingService.cs" = "Chat";
    "IChatStreamingService.cs" = "Chat";
    
    # Llm Feature
    "LlmService.cs" = "Llm";
    "ILlmService.cs" = "Llm";
    
    # Models Feature
    "ModelService.cs" = "Models";
    "IModelService.cs" = "Models";
    
    # Providers Feature
    "LlmProviderService.cs" = "Providers";
    "ILlmProviderService.cs" = "Providers";
    "LlmProviderSeeder.cs" = "Providers";
    
    # Sessions Feature
    "SessionService.cs" = "Sessions";
    "ISessionService.cs" = "Sessions";
    "SessionContextService.cs" = "Sessions";
    "ISessionContextService.cs" = "Sessions";
    "MySqlContextService.cs" = "Sessions";
    "IContextService.cs" = "Sessions";
    
    # Usage Feature
    "ChatUsageService.cs" = "Usage";
    "IChatUsageService.cs" = "Usage";
    
    # Shared Feature
    "DatabaseInitializer.cs" = "Shared";
    "TokenManager.cs" = "Auth";
    "ITokenManager.cs" = "Auth";
}

# Base paths
$sourcePath = "C:\dev\MCPServer\MCPServer.Core\Services"
$interfacesPath = "C:\dev\MCPServer\MCPServer.Core\Services\Interfaces"
$targetBasePath = "C:\dev\MCPServer\MCPServer.Core\Features"

# Create directories if they don't exist
foreach ($feature in $serviceMapping.Values | Select-Object -Unique) {
    $servicesPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services"
    $interfacesPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\Interfaces"
    
    if (-not (Test-Path $servicesPath)) {
        New-Item -Path $servicesPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $servicesPath"
    }
    
    if (-not (Test-Path $interfacesPath)) {
        New-Item -Path $interfacesPath -ItemType Directory -Force | Out-Null
        Write-Host "Created directory: $interfacesPath"
    }
}

# Move service files
foreach ($file in $serviceMapping.Keys) {
    $feature = $serviceMapping[$file]
    
    # Determine source and target paths
    if ($file.StartsWith("I") -and $file.EndsWith("Service.cs")) {
        $sourcePath = Join-Path -Path $interfacesPath -ChildPath $file
        $targetPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\Interfaces\$file"
    } else {
        $sourcePath = Join-Path -Path $sourcePath -ChildPath $file
        $targetPath = Join-Path -Path $targetBasePath -ChildPath "$feature\Services\$file"
    }
    
    # Check if source file exists
    if (Test-Path $sourcePath) {
        # Read the file content
        $content = Get-Content -Path $sourcePath -Raw
        
        # Update namespace
        $oldNamespace = "namespace MCPServer.Core.Services"
        $newNamespace = "namespace MCPServer.Core.Features.$feature.Services"
        
        if ($file.StartsWith("I") -and $file.EndsWith("Service.cs")) {
            $oldNamespace = "namespace MCPServer.Core.Services.Interfaces"
            $newNamespace = "namespace MCPServer.Core.Features.$feature.Services.Interfaces"
        }
        
        $content = $content -replace [regex]::Escape($oldNamespace), $newNamespace
        
        # Update using statements
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.$feature.Services.Interfaces;"
        
        # Write the updated content to the target file
        Set-Content -Path $targetPath -Value $content
        
        Write-Host "Moved and updated: $file to $targetPath"
    } else {
        Write-Host "Source file not found: $sourcePath" -ForegroundColor Yellow
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
        $content = Get-Content -Path $_.FullName -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services.Llm", "namespace MCPServer.Core.Features.Llm.Services.Llm"
        
        # Update using statements
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.Llm.Services.Interfaces;"
        $content = $content -replace "using MCPServer.Core.Services.Llm;", "using MCPServer.Core.Features.Llm.Services.Llm;"
        
        # Write the updated content to the target file
        $targetFilePath = Join-Path -Path $llmTargetPath -ChildPath $_.Name
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
        $content = Get-Content -Path $_.FullName -Raw
        
        # Update namespace
        $content = $content -replace "namespace MCPServer.Core.Services.Rag", "namespace MCPServer.Core.Features.Rag.Services.Rag"
        
        # Update using statements
        $content = $content -replace "using MCPServer.Core.Services.Interfaces;", "using MCPServer.Core.Features.Rag.Services.Interfaces;"
        $content = $content -replace "using MCPServer.Core.Services.Rag;", "using MCPServer.Core.Features.Rag.Services.Rag;"
        
        # Write the updated content to the target file
        $targetFilePath = Join-Path -Path $ragTargetPath -ChildPath $_.Name
        Set-Content -Path $targetFilePath -Value $content
        
        Write-Host "Moved and updated: $($_.Name) to $targetFilePath"
    }
}

Write-Host "Service migration completed."
