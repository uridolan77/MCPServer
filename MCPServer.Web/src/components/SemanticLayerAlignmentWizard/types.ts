import { Connection } from '@/models/Connection'; // Assuming Connection model path

export interface SemanticLayerAlignmentWizardData {
  selectedConnection?: Connection;
  connectionTestResult?: { success: boolean; message: string };
  rawSchemaSelection?: { // Define more specifically based on DatabaseSchemaSelector output
    selectedTables: Array<{ name: string; columns: string[]; schema?: string; [key: string]: any }>;
    // Potentially other selections like views, procedures etc.
  };
  generatedDatabaseSchemaJson?: any; // This will be the JSON object based on db-meta-schema.json
  slodJsonContent?: string; // JSON string for the SLOD editor
  isValidSlodJson?: boolean;
}

// Props for step components if they need specific parts of formData or callbacks
export interface ConnectionSelectionStepProps {
  formData: SemanticLayerAlignmentWizardData;
  onFormDataChange: (data: Partial<SemanticLayerAlignmentWizardData>) => void;
  setStepValidated: (isValid: boolean) => void; // To control "Next" button
}

export interface DatabaseSchemaSelectionStepProps {
  formData: SemanticLayerAlignmentWizardData;
  onFormDataChange: (data: Partial<SemanticLayerAlignmentWizardData>) => void;
  setStepValidated: (isValid: boolean) => void; // To control "Next" button
}

export interface SlodJsonEditorStepProps {
  formData: SemanticLayerAlignmentWizardData;
  onFormDataChange: (data: Partial<SemanticLayerAlignmentWizardData>) => void;
  setStepValidated: (isValid: boolean) => void; // To control "Next" button
}

// Content of db-meta-schema.json (as provided in the prompt)
export const DB_META_SCHEMA_TEMPLATE = {
  "metadata": {
    "id": "database-meta-schema-v1",
    "version": "1.0.0",
    "name": "Database Meta-Schema",
    "description": "A comprehensive meta-schema for storing database structural definitions",
    "created": "2025-05-06T12:00:00Z",
    "modified": "2025-05-06T12:00:00Z"
  },
  "entities": [
    {
      "name": "Database",
      "description": "Represents a database instance",
      "properties": [
        { "name": "id", "type": "string", "description": "Unique identifier for the database", "required": true },
        { "name": "name", "type": "string", "description": "Name of the database", "required": true },
        { "name": "description", "type": "string", "description": "Description of the database purpose" },
        { "name": "vendor", "type": "string", "description": "Database vendor (SQL Server, Oracle, MySQL, etc.)" },
        { "name": "version", "type": "string", "description": "Database version" },
        { "name": "created", "type": "datetime", "description": "When the database was created" },
        { "name": "lastModified", "type": "datetime", "description": "When the database was last modified" },
        { "name": "collation", "type": "string", "description": "Default collation of the database" },
        { "name": "characterSet", "type": "string", "description": "Default character set of the database" }
      ]
    },
    {
      "name": "Schema",
      "description": "Represents a schema within a database",
      "properties": [
        { "name": "id", "type": "string", "description": "Unique identifier for the schema", "required": true },
        { "name": "databaseId", "type": "string", "description": "Reference to the parent database", "required": true },
        { "name": "name", "type": "string", "description": "Name of the schema", "required": true },
        // ... other properties from the provided db-meta-schema.json
      ]
    },
    {
      "name": "Table",
      "description": "Represents a table in a database schema",
      "properties": [
        { "name": "id", "type": "string", "description": "Unique identifier for the table", "required": true },
        { "name": "schemaId", "type": "string", "description": "Reference to the parent schema", "required": true },
        { "name": "name", "type": "string", "description": "Name of the table", "required": true },
        // ... other properties
      ]
    },
    {
      "name": "Column",
      "description": "Represents a column in a database table",
      "properties": [
        { "name": "id", "type": "string", "description": "Unique identifier for the column", "required": true },
        { "name": "tableId", "type": "string", "description": "Reference to the parent table", "required": true },
        { "name": "name", "type": "string", "description": "Name of the column", "required": true },
        { "name": "dataType", "type": "string", "description": "Data type of the column", "required": true },
        { "name": "ordinalPosition", "type": "integer", "description": "Position of the column in the table", "required": true },
        // ... other properties
      ]
    }
    // ... other entities from db-meta-schema.json (PrimaryKey, ForeignKey, etc.) would be listed here
    // For brevity, only a few are expanded. The actual implementation should use the full schema.
  ],
  "relationships": [
    // ... relationships from db-meta-schema.json
  ]
};

// Content of gaming-slod.json (as provided in the attachments)
export const GAMING_SLOD_JSON_CONTENT = `{
  "metadata": {
    "id": "gaming-industry-slod-v1",
    "version": "1.0.0",
    "name": "Gaming Industry Semantic Layer",
    "description": "Comprehensive semantic ontology for the gaming industry covering players, games, bets, transactions, and regulatory compliance",
    "domain": "Gaming",
    "created": "2025-05-06T12:00:00Z",
    "modified": "2025-05-06T12:00:00Z",
    "authors": ["ProgressPlay Data Team"],
    "license": "Proprietary"
  },
  "namespaces": {
    "gaming": "http://semantics.gaming/ontology#",
    "schema": "http://schema.org/",
    "skos": "http://www.w3.org/2004/02/skos/core#",
    "xsd": "http://www.w3.org/2001/XMLSchema#",
    "foaf": "http://xmlns.com/foaf/0.1/"
  },
  "entities": [
    {
      "name": "Player",
      "description": "Represents a player in the gaming system",
      "properties": [
        { "name": "playerId", "type": "gaming:PlayerID", "description": "Unique identifier for the player", "required": true },
        { "name": "username", "type": "xsd:string", "description": "Player's username" },
        { "name": "email", "type": "schema:Email", "description": "Player's email address" }
      ]
    }
    // ... other entities from gaming-slod.json
  ]
}`;

