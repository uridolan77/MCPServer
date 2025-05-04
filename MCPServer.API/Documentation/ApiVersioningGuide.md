# API Versioning Guide for MCP Server

## Overview

This document outlines the API versioning strategy for the MCP Server API. API versioning is essential for maintaining backward compatibility while allowing the API to evolve over time.

## Versioning Strategy

The MCP Server API uses a semantic versioning approach with the format `major.minor`:

- **Major version**: Incremented for breaking changes that are not backward compatible
- **Minor version**: Incremented for backward-compatible feature additions or improvements

## Implementation Details

### Version Format

API versions are specified in the format `v{major}` or `v{major}.{minor}`, for example:
- `v1`
- `v1.1`
- `v2`

### Version Specification Methods

Clients can specify the desired API version using one of the following methods (in order of precedence):

1. **URL Path Segment**: `/api/v1/controller`
2. **Query String**: `/api/controller?api-version=1.0`
3. **HTTP Header**: `X-API-Version: 1.0`

If no version is specified, the default version (currently v1.0) is used.

### Controller Versioning

Controllers are versioned using the `[ApiVersion]` attribute:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ExampleController : ApiControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        // Implementation for v1.0
    }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public ActionResult<ApiResponse<object>> GetV2()
    {
        // Implementation for v2.0
    }
}
```

### Deprecation

When a version is deprecated, it will be marked with the `[ApiVersion]` attribute's `Deprecated` property:

```csharp
[ApiVersion("1.0", Deprecated = true)]
```

Deprecated versions will continue to function but will include a deprecation notice in the API documentation and response headers.

## Version Lifecycle

1. **Active**: The current recommended version
2. **Deprecated**: Still functional but scheduled for removal
3. **Retired**: No longer available

Versions will typically remain in the deprecated state for at least 6 months before being retired.

## Documentation

Each API version has its own documentation in Swagger UI, accessible at `/swagger`.

## Example Usage

### Client Request Examples

#### URL Path Segment
```
GET /api/v1/health
```

#### Query String
```
GET /api/health?api-version=1.0
```

#### HTTP Header
```
GET /api/health
X-API-Version: 1.0
```

## Best Practices for API Consumers

1. Always specify the API version explicitly in requests
2. Monitor deprecation notices in API responses
3. Test against new API versions before migrating
4. Subscribe to the API changelog for updates

## Best Practices for API Developers

1. Never modify the behavior of an existing API version
2. Create a new version for breaking changes
3. Document all changes between versions
4. Provide migration guides for major version changes
5. Maintain backward compatibility within the same major version
