-- Fix all chat logs with error responses
UPDATE ChatUsageLogs
SET Success = 0, 
    InputTokenCount = 0, 
    OutputTokenCount = 0, 
    EstimatedCost = 0
WHERE 
    (Response LIKE '%Error%' OR Response LIKE '%error%' OR OutputTokenCount = 0)
    AND Success = 1;

-- Fix all usage metrics for the same sessions
UPDATE UsageMetrics
SET Value = 0,
    AdditionalData = JSON_SET(
        JSON_SET(
            JSON_SET(
                AdditionalData,
                '$.inputTokens', 0
            ),
            '$.outputTokens', 0
        ),
        '$.estimatedCost', 0
    )
WHERE 
    MetricType = 'ChatTokensUsed'
    AND SessionId IN (
        SELECT SessionId 
        FROM ChatUsageLogs 
        WHERE (Response LIKE '%Error%' OR Response LIKE '%error%' OR OutputTokenCount = 0)
    );
