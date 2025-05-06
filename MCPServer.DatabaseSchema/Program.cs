namespace MCPServer.DatabaseSchema;

using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Example program demonstrating the use of the DatabaseSchemaExtractor
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: MCPServer.DatabaseSchema <connectionString> <databaseName> [outputFile]");
            Console.WriteLine("Example: MCPServer.DatabaseSchema \"Server=localhost;Database=MyDatabase;Trusted_Connection=True;\" MyDatabase schema.json");
            return;
        }

        string connectionString = args[0];
        string databaseName = args[1];
        string outputFile = args.Length > 2 ? args[2] : $"{databaseName}-schema.json";

        try
        {
            Console.WriteLine($"Extracting schema from database '{databaseName}'...");
            
            var extractor = new DatabaseSchemaExtractor(connectionString);
            string schemaJson = await extractor.ExtractSchemaAsJsonAsync(databaseName);
            
            File.WriteAllText(outputFile, schemaJson);
            
            Console.WriteLine($"Schema successfully extracted to '{outputFile}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting schema: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}