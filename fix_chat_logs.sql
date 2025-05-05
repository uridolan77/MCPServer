-- Fix chat logs with error responses by setting token counts to zero only for genuinely failed requests
UPDATE ChatUsageLogs
SET Success = 0, 
    InputTokenCount = 0, 
    OutputTokenCount = 0, 
    EstimatedCost = 0
WHERE 
    (Response LIKE '%Error connecting to%' 
     OR Response LIKE '%API key not provided%'
     OR Response LIKE '%Invalid API key%'
     OR ErrorMessage IS NOT NULL)
    AND Success = 1;

-- For successful Claude requests, estimate token counts based on message length
-- Claude uses approximately 1 token per 4 characters for English text
UPDATE ChatUsageLogs
SET InputTokenCount = CEILING(LEN(Message) / 4),
    OutputTokenCount = CEILING(LEN(Response) / 4)
WHERE ProviderName LIKE '%Claude%'
  AND Success = 1
  AND (InputTokenCount = 0 OR OutputTokenCount = 0)
  AND EstimatedCost > 0;

-- For successful OpenAI/GPT requests, estimate token counts based on message length
-- GPT uses approximately 1 token per 4 characters for English text
UPDATE ChatUsageLogs
SET InputTokenCount = CEILING(LEN(Message) / 4),
    OutputTokenCount = CEILING(LEN(Response) / 4)
WHERE (ProviderName LIKE '%OpenAI%' OR ModelName LIKE '%GPT%')
  AND Success = 1
  AND (InputTokenCount = 0 OR OutputTokenCount = 0)
  AND EstimatedCost > 0;

-- Update the total tokens in usage metrics based on the updated chat logs
UPDATE UsageMetrics
SET Value = (
        SELECT SUM(InputTokenCount + OutputTokenCount)
        FROM ChatUsageLogs
        WHERE SessionId = UsageMetrics.SessionId
    ),
    AdditionalData = JSON_SET(
        JSON_SET(
            JSON_SET(
                AdditionalData,
                '$.inputTokens', (
                    SELECT SUM(InputTokenCount)
                    FROM ChatUsageLogs
                    WHERE SessionId = UsageMetrics.SessionId
                )
            ),
            '$.outputTokens', (
                SELECT SUM(OutputTokenCount)
                FROM ChatUsageLogs
                WHERE SessionId = UsageMetrics.SessionId
            )
        ),
        '$.estimatedCost', (
            SELECT SUM(EstimatedCost)
            FROM ChatUsageLogs
            WHERE SessionId = UsageMetrics.SessionId
        )
    )
WHERE 
    MetricType = 'ChatTokensUsed';
