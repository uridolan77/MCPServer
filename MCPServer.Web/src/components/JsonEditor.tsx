import React, { useState, useEffect, useCallback, useRef } from 'react';
import { ChevronDown, ChevronRight, Plus, Trash2, Edit, Save, X, Copy, Clipboard, FileText, Code, Check, Info, AlertCircle, Box, LayoutGrid, ZoomIn, ZoomOut, Maximize } from 'lucide-react';

// Entity Relationship Diagram Component
const EntityRelationshipDiagram = ({ jsonData, setSelectedPath }) => {
  const containerRef = useRef(null);
  const [scale, setScale] = useState(1);
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const [dragging, setDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [entityPositions, setEntityPositions] = useState({});
  
  // Extract entities and relationships from the JSON data
  const extractEntitiesAndRelationships = useCallback(() => {
    let entities = [];
    let relationships = [];
    
    if (jsonData.entities && Array.isArray(jsonData.entities)) {
      entities = jsonData.entities;
    }
    
    if (jsonData.relationships && Array.isArray(jsonData.relationships)) {
      relationships = jsonData.relationships;
    }
    
    return { entities, relationships };
  }, [jsonData]);
  
  const { entities, relationships } = extractEntitiesAndRelationships();
  
  // Initialize entity positions
  useEffect(() => {
    if (entities.length > 0 && Object.keys(entityPositions).length === 0) {
      // Simple grid layout
      const positions = {};
      const cols = Math.ceil(Math.sqrt(entities.length));
      const spacing = 250;
      
      entities.forEach((entity, index) => {
        const row = Math.floor(index / cols);
        const col = index % cols;
        positions[entity.name || entity.id] = {
          x: col * spacing,
          y: row * spacing
        };
      });
      
      setEntityPositions(positions);
    }
  }, [entities, entityPositions]);
  
  // Handle mouse events for dragging the canvas
  const handleMouseDown = (e) => {
    if (e.target === containerRef.current) {
      setDragging(true);
      setDragStart({ x: e.clientX, y: e.clientY });
    }
  };
  
  const handleMouseMove = (e) => {
    if (dragging) {
      const dx = e.clientX - dragStart.x;
      const dy = e.clientY - dragStart.y;
      
      setPosition({
        x: position.x + dx,
        y: position.y + dy
      });
      
      setDragStart({ x: e.clientX, y: e.clientY });
    }
  };
  
  const handleMouseUp = () => {
    setDragging(false);
  };
  
  // Handle entity drag
  const handleEntityDragStart = (e, entityName) => {
    e.stopPropagation();
    e.dataTransfer.setData('entityName', entityName);
  };
  
  const handleEntityDragOver = (e) => {
    e.preventDefault();
  };
  
  const handleEntityDrop = (e) => {
    e.preventDefault();
    const entityName = e.dataTransfer.getData('entityName');
    
    if (entityName) {
      const rect = containerRef.current.getBoundingClientRect();
      const x = (e.clientX - rect.left - position.x) / scale;
      const y = (e.clientY - rect.top - position.y) / scale;
      
      setEntityPositions(prev => ({
        ...prev,
        [entityName]: { x, y }
      }));
    }
  };
  
  // Zoom functions
  const zoomIn = () => {
    setScale(prev => Math.min(prev + 0.1, 2.0));
  };
  
  const zoomOut = () => {
    setScale(prev => Math.max(prev - 0.1, 0.5));
  };
  
  const resetView = () => {
    setScale(1);
    setPosition({ x: 0, y: 0 });
  };
  
  // Expose methods to parent component
  useEffect(() => {
    if (containerRef.current) {
      containerRef.current.zoomIn = zoomIn;
      containerRef.current.zoomOut = zoomOut;
      containerRef.current.resetView = resetView;
      containerRef.current.classList.add('entity-diagram');
    }
    
    return () => {
      if (containerRef.current) {
        delete containerRef.current.zoomIn;
        delete containerRef.current.zoomOut;
        delete containerRef.current.resetView;
        containerRef.current.classList.remove('entity-diagram');
      }
    };
  }, []);
  
  // Find relationships for an entity
  const getEntityRelationships = (entityName) => {
    return relationships.filter(rel => {
      const domain = rel.domain || rel.source;
      const range = rel.range || rel.target;
      return domain === entityName || range === entityName;
    });
  };
  
  // Handle entity selection
  const handleEntityClick = (entity) => {
    const path = `entities.${entities.indexOf(entity)}`;
    setSelectedPath(path);
  };
  
  // Draw relationship lines
  const renderRelationshipLines = () => {
    return relationships.map((rel, index) => {
      const sourceName = rel.domain || rel.source;
      const targetName = rel.range || rel.target;
      
      const sourcePosition = entityPositions[sourceName];
      const targetPosition = entityPositions[targetName];
      
      if (!sourcePosition || !targetPosition) return null;
      
      // Calculate line coordinates
      const sourceX = sourcePosition.x + 150; // Half entity width
      const sourceY = sourcePosition.y + 100; // Entity height
      const targetX = targetPosition.x + 150; // Half entity width
      const targetY = targetPosition.y;
      
      // Line path with arrow
      const path = `M${sourceX},${sourceY} C${sourceX},${sourceY + 50} ${targetX},${targetY - 50} ${targetX},${targetY}`;
      
      return (
        <g key={`rel-${index}`} className="relationship-line">
          <path 
            d={path} 
            fill="none" 
            stroke="#6B7280" 
            strokeWidth="2" 
            markerEnd="url(#arrowhead)" 
          />
          
          {/* Relationship label */}
          <text 
            x={(sourceX + targetX) / 2} 
            y={(sourceY + targetY) / 2 - 10}
            textAnchor="middle"
            fill="#4B5563"
            fontSize="12"
            className="bg-white px-1"
          >
            {rel.label || rel.name}
          </text>
        </g>
      );
    });
  };
  
  if (entities.length === 0) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-gray-500">
          No entities found in the JSON structure
        </div>
      </div>
    );
  }
  
  return (
    <div 
      ref={containerRef}
      className="relative w-full h-full cursor-grab overflow-auto"
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      onDragOver={handleEntityDragOver}
      onDrop={handleEntityDrop}
      style={{ 
        backgroundImage: 'radial-gradient(circle, #e5e7eb 1px, transparent 1px)',
        backgroundSize: '20px 20px',
      }}
    >
      <div 
        className="absolute transform origin-top-left"
        style={{ 
          transform: `translate(${position.x}px, ${position.y}px) scale(${scale})`,
          minWidth: '3000px',
          minHeight: '2000px'
        }}
      >
        <svg className="absolute top-0 left-0 w-full h-full pointer-events-none">
          <defs>
            <marker
              id="arrowhead"
              viewBox="0 0 10 10"
              refX="9"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto"
            >
              <path d="M 0 0 L 10 5 L 0 10 z" fill="#6B7280" />
            </marker>
          </defs>
          {renderRelationshipLines()}
        </svg>
        
        {/* Entities */}
        {entities.map((entity, index) => {
          const entityName = entity.name || entity.id;
          const position = entityPositions[entityName] || { x: 0, y: 0 };
          const entityRelationships = getEntityRelationships(entityName);
          
          return (
            <div
              key={`entity-${index}`}
              className="absolute bg-white border-2 border-blue-500 rounded-lg shadow-md w-72 cursor-pointer overflow-hidden"
              style={{ 
                left: `${position.x}px`, 
                top: `${position.y}px`,
              }}
              draggable
              onDragStart={(e) => handleEntityDragStart(e, entityName)}
              onClick={() => handleEntityClick(entity)}
            >
              <div className="bg-blue-500 text-white font-bold p-2 text-center">
                {entityName}
              </div>
              
              <div className="p-2 max-h-60 overflow-y-auto">
                {/* Properties */}
                {entity.properties && entity.properties.map((prop, propIndex) => (
                  <div key={`prop-${propIndex}`} className="text-sm mb-1 pb-1 border-b border-gray-200">
                    <div className="font-medium">{prop.name}</div>
                    <div className="text-xs text-gray-600">
                      {prop.type}
                      {prop.required && <span className="text-red-600 ml-1">*</span>}
                    </div>
                  </div>
                ))}
              </div>
              
              {/* Relationship count indicator */}
              {entityRelationships.length > 0 && (
                <div className="absolute top-2 right-2 bg-gray-700 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs">
                  {entityRelationships.length}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

// Main component
const JSONStructureEditor = () => {
  const [jsonData, setJsonData] = useState(null);
  const [expandedNodes, setExpandedNodes] = useState({});
  const [selectedPath, setSelectedPath] = useState(null);
  const [editMode, setEditMode] = useState(false);
  const [editValue, setEditValue] = useState('');
  const [error, setError] = useState(null);
  const [jsonText, setJsonText] = useState('');
  const [viewMode, setViewMode] = useState('tree'); // tree, text, or visual
  const [clipboard, setClipboard] = useState(null);
  const [notification, setNotification] = useState(null);
  const [schemaValidation, setSchemaValidation] = useState(true);

  // Load sample data
  useEffect(() => {
    async function loadSampleData() {
      try {
        const response = await window.fs.readFile('slod-meta-schema.json', { encoding: 'utf8' });
        const data = JSON.parse(response);
        setJsonData(data);
        setJsonText(JSON.stringify(data, null, 2));
        
        // Auto-expand first level
        const initialExpanded = {};
        Object.keys(data).forEach(key => {
          initialExpanded[key] = true;
        });
        setExpandedNodes(initialExpanded);
      } catch (err) {
        setError("Failed to load sample data");
        console.error(err);
      }
    }
    
    loadSampleData();
  }, []);

  // Show notification
  const showNotification = (message, type = 'info') => {
    setNotification({ message, type });
    setTimeout(() => setNotification(null), 3000);
  };

  // Toggle expand/collapse
  const toggleExpand = (path) => {
    setExpandedNodes(prev => ({
      ...prev,
      [path]: !prev[path]
    }));
  };

  // Handle node selection
  const handleSelect = (path) => {
    setSelectedPath(path);
    setEditMode(false);
  };

  // Start editing a value
  const startEditing = (path, value) => {
    setSelectedPath(path);
    setEditMode(true);
    setEditValue(typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value));
  };

  // Save edited value
  const saveEdit = () => {
    try {
      if (!selectedPath) return;
      
      const pathParts = selectedPath.split('.');
      let newData = { ...jsonData };
      let current = newData;
      
      // Navigate to the parent of the target
      for (let i = 0; i < pathParts.length - 1; i++) {
        const part = pathParts[i];
        if (Array.isArray(current)) {
          current = current[parseInt(part)];
        } else {
          current = current[part];
        }
      }
      
      // Update the value
      const lastPart = pathParts[pathParts.length - 1];
      let parsedValue;
      
      try {
        // Try to parse as JSON first
        parsedValue = JSON.parse(editValue);
      } catch (e) {
        // If not valid JSON, use the raw value
        if (editValue === 'true') parsedValue = true;
        else if (editValue === 'false') parsedValue = false;
        else if (editValue === 'null') parsedValue = null;
        else if (!isNaN(Number(editValue)) && editValue.trim() !== '') parsedValue = Number(editValue);
        else parsedValue = editValue;
      }
      
      if (Array.isArray(current)) {
        current[parseInt(lastPart)] = parsedValue;
      } else {
        current[lastPart] = parsedValue;
      }
      
      setJsonData(newData);
      setJsonText(JSON.stringify(newData, null, 2));
      setEditMode(false);
      showNotification("Value updated successfully", "success");
    } catch (err) {
      setError("Failed to save edit: " + err.message);
    }
  };

  // Cancel editing
  const cancelEdit = () => {
    setEditMode(false);
    setError(null);
  };

  // Add a new property
  const addProperty = (path, isArray = false) => {
    try {
      const pathParts = path ? path.split('.') : [];
      let newData = { ...jsonData };
      let current = newData;
      
      // Navigate to the target object
      for (let i = 0; i < pathParts.length; i++) {
        const part = pathParts[i];
        if (Array.isArray(current)) {
          current = current[parseInt(part)];
        } else {
          current = current[part];
        }
      }
      
      if (isArray) {
        // Add item to array
        if (Array.isArray(current)) {
          current.push(null);
        }
      } else {
        // Add property to object
        if (typeof current === 'object' && current !== null && !Array.isArray(current)) {
          let newKey = 'newProperty';
          let counter = 1;
          
          // Ensure unique key
          while (current[newKey] !== undefined) {
            newKey = `newProperty${counter}`;
            counter++;
          }
          
          current[newKey] = null;
          
          // Auto expand and select the new property
          setExpandedNodes(prev => ({
            ...prev,
            [path]: true
          }));
          
          const newPath = path ? `${path}.${newKey}` : newKey;
          setSelectedPath(newPath);
          startEditing(newPath, null);
        }
      }
      
      setJsonData(newData);
      setJsonText(JSON.stringify(newData, null, 2));
      showNotification("Property added", "success");
    } catch (err) {
      setError("Failed to add property: " + err.message);
    }
  };

  // Remove a property
  const removeProperty = (path) => {
    try {
      const pathParts = path.split('.');
      let newData = { ...jsonData };
      let current = newData;
      
      // Handle special case of root element
      if (pathParts.length === 1) {
        const rootKey = pathParts[0];
        if (typeof newData === 'object' && newData !== null && !Array.isArray(newData)) {
          const { [rootKey]: removed, ...rest } = newData;
          setJsonData(rest);
          setJsonText(JSON.stringify(rest, null, 2));
          setSelectedPath(null);
          return;
        }
      }
      
      // Navigate to the parent of the target
      for (let i = 0; i < pathParts.length - 1; i++) {
        const part = pathParts[i];
        if (Array.isArray(current)) {
          current = current[parseInt(part)];
        } else {
          current = current[part];
        }
      }
      
      // Remove the property
      const lastPart = pathParts[pathParts.length - 1];
      if (Array.isArray(current)) {
        current.splice(parseInt(lastPart), 1);
      } else {
        const { [lastPart]: removed, ...rest } = current;
        Object.assign(current, rest);
      }
      
      setJsonData(newData);
      setJsonText(JSON.stringify(newData, null, 2));
      setSelectedPath(null);
      showNotification("Property removed", "success");
    } catch (err) {
      setError("Failed to remove property: " + err.message);
    }
  };

  // Copy a node to clipboard
  const copyNode = (path) => {
    try {
      const pathParts = path.split('.');
      let current = jsonData;
      
      // Navigate to the target
      for (let i = 0; i < pathParts.length; i++) {
        const part = pathParts[i];
        if (Array.isArray(current)) {
          current = current[parseInt(part)];
        } else {
          current = current[part];
        }
      }
      
      setClipboard({
        key: pathParts[pathParts.length - 1],
        value: current
      });
      
      showNotification("Copied to clipboard", "success");
    } catch (err) {
      setError("Failed to copy: " + err.message);
    }
  };

  // Paste from clipboard
  const pasteNode = (path) => {
    if (!clipboard) return;
    
    try {
      const pathParts = path.split('.');
      let newData = { ...jsonData };
      let current = newData;
      
      // Navigate to the target
      for (let i = 0; i < pathParts.length; i++) {
        const part = pathParts[i];
        if (Array.isArray(current)) {
          current = current[parseInt(part)];
        } else {
          current = current[part];
        }
      }
      
      // Paste the value
      if (typeof current === 'object' && current !== null) {
        if (Array.isArray(current)) {
          current.push(clipboard.value);
        } else {
          let key = clipboard.key;
          let counter = 1;
          
          // Ensure unique key
          while (current[key] !== undefined) {
            key = `${clipboard.key}${counter}`;
            counter++;
          }
          
          current[key] = clipboard.value;
        }
      }
      
      setJsonData(newData);
      setJsonText(JSON.stringify(newData, null, 2));
      showNotification("Pasted from clipboard", "success");
    } catch (err) {
      setError("Failed to paste: " + err.message);
    }
  };

  // Update JSON from text editor
  const updateJsonFromText = () => {
    try {
      const newData = JSON.parse(jsonText);
      setJsonData(newData);
      setError(null);
      showNotification("JSON updated from text", "success");
    } catch (err) {
      setError("Invalid JSON: " + err.message);
    }
  };

  // Load a file
  const handleFileUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const content = e.target.result;
        const data = JSON.parse(content);
        setJsonData(data);
        setJsonText(JSON.stringify(data, null, 2));
        setError(null);
        
        // Reset expanded nodes
        const initialExpanded = {};
        Object.keys(data).forEach(key => {
          initialExpanded[key] = true;
        });
        setExpandedNodes(initialExpanded);
        
        showNotification(`Loaded ${file.name}`, "success");
      } catch (err) {
        setError("Failed to parse file: " + err.message);
      }
    };
    reader.readAsText(file);
  };

  // Download the current JSON
  const downloadJson = () => {
    const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(jsonData, null, 2));
    const downloadAnchorNode = document.createElement('a');
    downloadAnchorNode.setAttribute("href", dataStr);
    downloadAnchorNode.setAttribute("download", "data.json");
    document.body.appendChild(downloadAnchorNode);
    downloadAnchorNode.click();
    downloadAnchorNode.remove();
    showNotification("JSON downloaded", "success");
  };

  // Render the JSON tree recursively
  const renderTree = useCallback((data, path = '') => {
    if (data === null) {
      return (
        <div className="flex items-center text-gray-500 ml-4">
          <span>null</span>
        </div>
      );
    }

    if (typeof data !== 'object') {
      return (
        <div 
          className={`flex items-center ml-4 ${selectedPath === path ? 'bg-blue-100 rounded' : ''}`}
          onClick={(e) => { e.stopPropagation(); handleSelect(path); }}
          onDoubleClick={(e) => { e.stopPropagation(); startEditing(path, data); }}
        >
          {typeof data === 'string' ? (
            <span className="text-green-600">"{data}"</span>
          ) : typeof data === 'number' ? (
            <span className="text-blue-600">{data}</span>
          ) : typeof data === 'boolean' ? (
            <span className="text-purple-600">{String(data)}</span>
          ) : (
            <span>{String(data)}</span>
          )}
        </div>
      );
    }

    const isArray = Array.isArray(data);
    const isEmpty = Object.keys(data).length === 0;
    
    // For empty objects/arrays
    if (isEmpty) {
      return (
        <div className="flex items-center ml-4 text-gray-500">
          {isArray ? '[]' : '{}'}
        </div>
      );
    }

    return (
      <div>
        {Object.keys(data).map((key, index) => {
          const childPath = path ? `${path}.${key}` : key;
          const isExpanded = expandedNodes[childPath];
          const value = data[key];
          const isObject = value !== null && typeof value === 'object';
          
          return (
            <div key={childPath} className="ml-4">
              <div 
                className={`flex items-center hover:bg-gray-100 rounded cursor-pointer py-1 ${selectedPath === childPath ? 'bg-blue-100' : ''}`}
                onClick={(e) => { e.stopPropagation(); handleSelect(childPath); }}
              >
                {isObject ? (
                  <button 
                    className="p-1 focus:outline-none"
                    onClick={(e) => { e.stopPropagation(); toggleExpand(childPath); }}
                  >
                    {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                  </button>
                ) : (
                  <span className="w-6"></span>
                )}
                
                {isArray ? (
                  <span className="text-gray-500">[{key}]</span>
                ) : (
                  <span className="font-medium">{key}</span>
                )}
                
                {isObject ? (
                  <span className="text-gray-500 ml-2">
                    {Array.isArray(value) ? `Array(${value.length})` : 'Object'}
                  </span>
                ) : (
                  <span className="mx-2">:</span>
                )}
                
                {!isObject && (
                  <>
                    {typeof value === 'string' ? (
                      <span className="text-green-600">"{value}"</span>
                    ) : typeof value === 'number' ? (
                      <span className="text-blue-600">{value}</span>
                    ) : typeof value === 'boolean' ? (
                      <span className="text-purple-600">{String(value)}</span>
                    ) : value === null ? (
                      <span className="text-gray-500">null</span>
                    ) : (
                      <span>{String(value)}</span>
                    )}
                  </>
                )}
                
                {selectedPath === childPath && (
                  <div className="ml-auto flex">
                    <button 
                      className="p-1 text-gray-600 hover:text-blue-600"
                      onClick={(e) => { e.stopPropagation(); startEditing(childPath, value); }}
                      title="Edit"
                    >
                      <Edit size={14} />
                    </button>
                    <button 
                      className="p-1 text-gray-600 hover:text-red-600"
                      onClick={(e) => { e.stopPropagation(); removeProperty(childPath); }}
                      title="Remove"
                    >
                      <Trash2 size={14} />
                    </button>
                    <button 
                      className="p-1 text-gray-600 hover:text-blue-600"
                      onClick={(e) => { e.stopPropagation(); copyNode(childPath); }}
                      title="Copy"
                    >
                      <Copy size={14} />
                    </button>
                    {clipboard && (
                      <button 
                        className="p-1 text-gray-600 hover:text-green-600"
                        onClick={(e) => { e.stopPropagation(); pasteNode(childPath); }}
                        title="Paste"
                      >
                        <Clipboard size={14} />
                      </button>
                    )}
                    {isObject && (
                      <button 
                        className="p-1 text-gray-600 hover:text-green-600"
                        onClick={(e) => { e.stopPropagation(); addProperty(childPath, Array.isArray(value)); }}
                        title={Array.isArray(value) ? "Add item" : "Add property"}
                      >
                        <Plus size={14} />
                      </button>
                    )}
                  </div>
                )}
              </div>
              
              {isObject && isExpanded && (
                <div className="border-l-2 border-gray-200 pl-2 py-1">
                  {renderTree(value, childPath)}
                </div>
              )}
            </div>
          );
        })}
      </div>
    );
  }, [expandedNodes, selectedPath, editMode, clipboard]);

  // Loading state
  if (!jsonData) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="text-center">
          <div className="text-lg mb-2">Loading JSON Structure Editor...</div>
          <div className="w-12 h-12 border-4 border-t-blue-500 border-b-blue-700 rounded-full animate-spin mx-auto"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-screen max-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white shadow-sm p-4">
        <div className="container mx-auto flex justify-between items-center">
          <h1 className="text-xl font-bold text-gray-800">JSON Structure Editor</h1>
          
          <div className="flex space-x-2">
            <input
              type="file"
              id="fileInput"
              className="hidden"
              accept=".json"
              onChange={handleFileUpload}
            />
            <label 
              htmlFor="fileInput"
              className="px-3 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 cursor-pointer flex items-center"
            >
              <FileText size={16} className="mr-1" />
              Open
            </label>
            
            <button 
              className="px-3 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 flex items-center"
              onClick={downloadJson}
            >
              <Save size={16} className="mr-1" />
              Save
            </button>
            
            <div className="flex rounded bg-gray-200">
              <button 
                className={`px-3 py-2 flex items-center ${viewMode === 'tree' ? 'bg-blue-500 text-white rounded-l' : 'text-gray-700'}`}
                onClick={() => setViewMode('tree')}
              >
                <ChevronRight size={16} className="mr-1" />
                Tree
              </button>
              <button 
                className={`px-3 py-2 flex items-center ${viewMode === 'text' ? 'bg-blue-500 text-white' : 'text-gray-700'}`}
                onClick={() => setViewMode('text')}
              >
                <Code size={16} className="mr-1" />
                Text
              </button>
              <button 
                className={`px-3 py-2 flex items-center ${viewMode === 'visual' ? 'bg-blue-500 text-white rounded-r' : 'text-gray-700'}`}
                onClick={() => setViewMode('visual')}
              >
                <LayoutGrid size={16} className="mr-1" />
                Visual
              </button>
            </div>
          </div>
        </div>
      </div>
      
      {/* Notification */}
      {notification && (
        <div className={`fixed top-4 right-4 p-3 rounded shadow-lg flex items-center ${
          notification.type === 'success' ? 'bg-green-100 text-green-800' : 
          notification.type === 'error' ? 'bg-red-100 text-red-800' : 
          'bg-blue-100 text-blue-800'
        }`}>
          {notification.type === 'success' ? (
            <Check size={18} className="mr-2" />
          ) : notification.type === 'error' ? (
            <AlertCircle size={18} className="mr-2" />
          ) : (
            <Info size={18} className="mr-2" />
          )}
          {notification.message}
        </div>
      )}
      
      {/* Main Content */}
      <div className="flex-1 overflow-hidden flex">
        {viewMode === 'tree' ? (
          <div className="flex-1 overflow-auto p-4">
            {/* Tree View */}
            <div className="bg-white rounded-lg shadow p-4 h-full overflow-auto">
              {error && (
                <div className="bg-red-100 text-red-800 p-2 mb-4 rounded">
                  {error}
                </div>
              )}
              
              {/* JSON Tree */}
              <div className="font-mono text-sm">
                {renderTree(jsonData)}
              </div>
              
              {/* Edit Modal */}
              {editMode && selectedPath && (
                <div className="fixed inset-0 bg-black bg-opacity-30 flex items-center justify-center z-10">
                  <div className="bg-white rounded-lg shadow-lg p-6 max-w-2xl w-full">
                    <div className="flex justify-between items-center mb-4">
                      <h2 className="text-lg font-semibold">Edit Value</h2>
                      <button 
                        className="text-gray-500 hover:text-gray-700"
                        onClick={cancelEdit}
                      >
                        <X size={20} />
                      </button>
                    </div>
                    
                    <div className="mb-4">
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Path: {selectedPath}
                      </label>
                      <textarea
                        className="w-full h-40 p-2 border rounded font-mono"
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                      />
                    </div>
                    
                    <div className="flex justify-end space-x-2">
                      <button 
                        className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300"
                        onClick={cancelEdit}
                      >
                        Cancel
                      </button>
                      <button 
                        className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
                        onClick={saveEdit}
                      >
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : viewMode === 'text' ? (
          <div className="flex-1 overflow-auto p-4">
            {/* Text View */}
            <div className="bg-white rounded-lg shadow h-full flex flex-col">
              {error && (
                <div className="bg-red-100 text-red-800 p-2 m-4 rounded">
                  {error}
                </div>
              )}
              
              <textarea
                className="flex-1 p-4 font-mono text-sm resize-none focus:outline-none"
                value={jsonText}
                onChange={(e) => setJsonText(e.target.value)}
              />
              
              <div className="p-4 border-t">
                <button 
                  className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
                  onClick={updateJsonFromText}
                >
                  Update
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex-1 overflow-auto p-4">
            {/* Visual Entity-Relationship View */}
            <div className="bg-white rounded-lg shadow h-full flex flex-col">
              {error && (
                <div className="bg-red-100 text-red-800 p-2 m-4 rounded">
                  {error}
                </div>
              )}
              
              <div className="p-4 flex space-x-2 border-b">
                <button 
                  className="px-3 py-1 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 flex items-center"
                  title="Zoom In"
                  onClick={() => {
                    const diagram = document.querySelector('.entity-diagram');
                    if (diagram) {
                      diagram.zoomIn();
                    }
                  }}
                >
                  <ZoomIn size={16} />
                </button>
                <button 
                  className="px-3 py-1 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 flex items-center"
                  title="Zoom Out"
                  onClick={() => {
                    const diagram = document.querySelector('.entity-diagram');
                    if (diagram) {
                      diagram.zoomOut();
                    }
                  }}
                >
                  <ZoomOut size={16} />
                </button>
                <button 
                  className="px-3 py-1 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 flex items-center"
                  title="Reset View"
                  onClick={() => {
                    const diagram = document.querySelector('.entity-diagram');
                    if (diagram) {
                      diagram.resetView();
                    }
                  }}
                >
                  <Maximize size={16} />
                </button>
              </div>
              
              <div className="flex-1 overflow-auto p-4">
                <div className="bg-blue-50 border-blue-200 border p-3 mb-4 rounded text-sm">
                  <h3 className="font-bold mb-1">Entity-Relationship Diagram</h3>
                  <p>This view shows entities as boxes and their relationships as connecting lines. You can:</p>
                  <ul className="list-disc ml-5 mt-1">
                    <li>Drag entities to rearrange them</li>
                    <li>Click an entity to select it in the editor</li>
                    <li>Use mouse wheel or zoom buttons to zoom in/out</li>
                    <li>Drag the background to pan the view</li>
                  </ul>
                </div>
                <EntityRelationshipDiagram jsonData={jsonData} setSelectedPath={setSelectedPath} />
              </div>
            </div>
          </div>
        )}
      </div>
      
      {/* Status Bar */}
      <div className="bg-gray-200 p-2 text-sm">
        {selectedPath ? (
          <div className="flex justify-between">
            <div>Selected: {selectedPath}</div>
            <div>
              {schemaValidation ? (
                <span className="text-green-600 flex items-center">
                  <Check size={14} className="mr-1" />
                  Valid schema
                </span>
              ) : (
                <span className="text-red-600 flex items-center">
                  <AlertCircle size={14} className="mr-1" />
                  Invalid schema
                </span>
              )}
            </div>
          </div>
        ) : (
          <div className="flex justify-between">
            <div>Ready</div>
            <div>
              {Object.keys(jsonData).length} root properties
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default JSONStructureEditor;
