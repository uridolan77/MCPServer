import React, { useState, useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Slider,
  Grid,
  IconButton,
  Divider,
  CircularProgress,
  Card,
  CardContent,
  FormControlLabel,
  Switch,
  Alert,
  AlertTitle,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import DeleteIcon from '@mui/icons-material/Delete';
import SettingsIcon from '@mui/icons-material/Settings';
import BugReportIcon from '@mui/icons-material/BugReport';
import RefreshIcon from '@mui/icons-material/Refresh';
import { PageHeader } from '@/components';
import { chatPlaygroundApi, Message } from '@/api/chatPlaygroundApi';
import { LlmModel } from '@/api/llmProviderApi';

const ChatPlaygroundPage: React.FC = () => {
  // State for chat
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [sessionId] = useState(`web-session-${Math.random().toString(36).substring(2, 9)}`);
  const [useStreaming, setUseStreaming] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // State for settings
  const [selectedModelId, setSelectedModelId] = useState<number | null>(null);
  const [temperature, setTemperature] = useState<number>(0.7);
  const [maxTokens, setMaxTokens] = useState<number>(2000);
  const [systemPrompt, setSystemPrompt] = useState<string>('');
  const [showSettings, setShowSettings] = useState(true); // Show settings by default
  const [lastRefreshTime, setLastRefreshTime] = useState<Date>(new Date());

  // Fetch available models
  const { 
    data: modelsData, 
    isLoading: modelsLoading, 
    error: modelsError,
    refetch: refetchModels
  } = useQuery<LlmModel[]>({
    queryKey: ['chat-playground-models', lastRefreshTime.toISOString()],
    queryFn: async () => {
      try {
        console.log('Fetching models from chat playground API...');
        const result = await chatPlaygroundApi.getAvailableModels();
        console.log('Models fetched:', result);
        return result;
      } catch (error) {
        console.error('Error fetching models:', error);
        throw error;
      }
    },
    retry: 3,
    retryDelay: 1000,
    staleTime: 30000, // 30 seconds
  });

  // Log any errors and check authentication
  useEffect(() => {
    if (modelsError) {
      console.error('Error in models query:', modelsError);
    }

    // Check if we have a token
    const token = localStorage.getItem('token');
    console.log('Authentication token exists:', !!token);
    if (token) {
      console.log('Token starts with:', token.substring(0, 10) + '...');
    }
  }, [modelsError]);

  // Ensure models is always an array
  const models = React.useMemo(() => {
    if (!modelsData) return [];
    if (Array.isArray(modelsData)) return modelsData;
    return [];
  }, [modelsData]);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Set first available model as default when models are loaded
  useEffect(() => {
    if (models.length > 0 && !selectedModelId) {
      console.log('Setting default model to:', models[0].name, '(ID:', models[0].id, ')');
      setSelectedModelId(models[0].id);
    }
  }, [models, selectedModelId]);

  const handleRefreshModels = () => {
    setLastRefreshTime(new Date());
    refetchModels();
  };

  const handleSendMessage = async () => {
    if (!input.trim() || loading) return;

    // Add user message to chat
    const userMessage: Message = { role: 'user', content: input };
    setMessages((prev) => [...prev, userMessage]);
    setLoading(true);
    setInput('');

    try {
      if (useStreaming) {
        // Handle streaming response
        let responseContent = '';
        let responseAdded = false;

        await chatPlaygroundApi.streamMessage(
          {
            message: input,
            sessionId,
            history: messages,
            modelId: selectedModelId || undefined,
            temperature,
            maxTokens,
            systemPrompt: systemPrompt || undefined,
          },
          (data) => {
            // Log received chunk for debugging
            console.log('Received chunk:', data.chunk.length, 'chars:', data.chunk.substring(0, 20) + '...');
            
            if (!responseAdded) {
              // Add an empty assistant message that we'll update
              setMessages((prev) => [...prev, { role: 'assistant', content: '' }]);
              responseAdded = true;
            }

            // Use the exact chunk rather than concatenating to avoid duplicates
            // This fixes the issue where the backend might be sending the full response each time
            responseContent = data.chunk;

            // Update the last message with the current chunk content
            setMessages((prev) => {
              const updated = [...prev];
              updated[updated.length - 1] = {
                role: 'assistant',
                content: responseContent,
              };
              return updated;
            });

            if (data.isComplete) {
              setLoading(false);
            }
          }
        );
      } else {
        try {
          // Handle non-streaming response
          const response = await chatPlaygroundApi.sendMessage({
            message: input,
            sessionId,
            history: messages,
            modelId: selectedModelId || undefined,
            temperature,
            maxTokens,
            systemPrompt: systemPrompt || undefined,
          });

          // Add assistant response to chat
          setMessages((prev) => [...prev, { role: 'assistant', content: response.message }]);
        } catch (error) {
          console.error('Error with non-streaming response:', error);
          // Add error message
          setMessages((prev) => [
            ...prev,
            { role: 'assistant', content: 'Sorry, there was an error processing your request.' },
          ]);
        } finally {
          setLoading(false);
        }
      }
    } catch (error) {
      console.error('Error sending message:', error);
      // Add error message
      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: 'Sorry, there was an error processing your request.' },
      ]);
      setLoading(false);
    }
  };

  const handleClearChat = () => {
    setMessages([]);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  // Debug function to test API connectivity
  const handleDebugClick = async () => {
    try {
      console.log('Testing API connectivity...');

      // Check token
      const token = localStorage.getItem('token');
      console.log('Token exists:', !!token);

      // Try to fetch models directly from LLM provider endpoint
      console.log('Fetching models from /llm/models endpoint...');
      const llmModelsResponse = await fetch('http://localhost:2000/api/llm/models', {
        headers: {
          'Authorization': token ? `Bearer ${token}` : '',
        }
      });

      console.log('LLM models response status:', llmModelsResponse.status);
      let llmModelsData;
      let llmModelsError = null;

      try {
        llmModelsData = await llmModelsResponse.json();
        console.log('LLM models data:', llmModelsData);
      } catch (err) {
        llmModelsError = err;
        console.error('Error parsing LLM models response:', err);
      }

      // Try to fetch models from chat playground endpoint
      console.log('Fetching models from /chat-playground/models endpoint...');
      const chatModelsResponse = await fetch('http://localhost:2000/api/chat-playground/models', {
        headers: {
          'Authorization': token ? `Bearer ${token}` : '',
        }
      });

      console.log('Chat playground models response status:', chatModelsResponse.status);
      let chatModelsData;
      let chatModelsError = null;

      try {
        chatModelsData = await chatModelsResponse.json();
        console.log('Chat playground models data:', chatModelsData);
      } catch (err) {
        chatModelsError = err;
        console.error('Error parsing chat models response:', err);
      }

      // Show alert with results
      let message = `API Test Results:\n`;
      message += `LLM Models Endpoint: ${llmModelsResponse.status}`;

      if (llmModelsError) {
        message += ` (Error: ${llmModelsError.message})`;
      } else if (Array.isArray(llmModelsData)) {
        message += ` (${llmModelsData.length} models)`;
      } else if (llmModelsData && llmModelsData.error) {
        message += ` (Error: ${llmModelsData.error})`;
      }

      message += `\nChat Models Endpoint: ${chatModelsResponse.status}`;

      if (chatModelsError) {
        message += ` (Error: ${chatModelsError.message})`;
      } else if (Array.isArray(chatModelsData)) {
        message += ` (${chatModelsData.length} models)`;
      } else if (chatModelsData && chatModelsData.error) {
        message += ` (Error: ${chatModelsData.error})`;
        if (chatModelsData.details) {
          message += `\nDetails: ${chatModelsData.details}`;
        }
      }

      message += `\nSee console for details.`;

      alert(message);

    } catch (error) {
      console.error('Debug test failed:', error);
      alert(`API Test Failed: ${error.message}\nSee console for details.`);
    }
  };

  // Render error message if there's an issue loading models
  const renderModelLoadError = () => {
    if (modelsError) {
      return (
        <Alert 
          severity="error" 
          sx={{ mb: 2 }}
          action={
            <Button 
              color="inherit" 
              size="small" 
              onClick={handleRefreshModels}
              startIcon={<RefreshIcon />}
            >
              Retry
            </Button>
          }
        >
          <AlertTitle>Error Loading Models</AlertTitle>
          Failed to load LLM models. Please check your API configuration and credentials.
        </Alert>
      );
    }
    return null;
  };

  return (
    <Box sx={{ p: 3 }}>
      <PageHeader title="Chat Playground" />

      {renderModelLoadError()}

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Model Settings</Typography>
          <Box>
            <IconButton
              onClick={handleRefreshModels}
              sx={{ mr: 1 }}
              title="Refresh models"
              disabled={modelsLoading}
            >
              <RefreshIcon />
            </IconButton>
            <IconButton
              onClick={handleDebugClick}
              color="warning"
              sx={{ mr: 1 }}
              title="Test API connectivity"
            >
              <BugReportIcon />
            </IconButton>
            <IconButton
              onClick={() => setShowSettings(!showSettings)}
              title="Toggle settings"
            >
              <SettingsIcon />
            </IconButton>
          </Box>
        </Box>

        <Grid container spacing={2} sx={{ display: showSettings ? 'flex' : 'none' }}>
          <Grid item xs={12} md={4}>
            <FormControl fullWidth>
              <InputLabel id="model-select-label">Model</InputLabel>
              <Select
                labelId="model-select-label"
                value={selectedModelId || ''}
                label="Model"
                onChange={(e) => setSelectedModelId(e.target.value as number)}
                disabled={modelsLoading || loading}
              >
                {modelsLoading ? (
                  <MenuItem value="">
                    <CircularProgress size={20} /> Loading...
                  </MenuItem>
                ) : models.length === 0 ? (
                  <MenuItem value="" disabled>
                    No models available
                  </MenuItem>
                ) : (
                  models.map((model) => (
                    <MenuItem key={model.id} value={model.id}>
                      {model.provider?.displayName || model.provider?.name || 'Unknown'} - {model.name}
                    </MenuItem>
                  ))
                )}
              </Select>
            </FormControl>
          </Grid>

          <Grid item xs={12} md={4}>
            <Typography gutterBottom>Temperature: {temperature}</Typography>
            <Slider
              value={temperature}
              onChange={(_, value) => setTemperature(value as number)}
              min={0}
              max={2}
              step={0.1}
              valueLabelDisplay="auto"
              disabled={loading}
            />
          </Grid>

          <Grid item xs={12} md={4}>
            <Typography gutterBottom>Max Tokens: {maxTokens}</Typography>
            <Slider
              value={maxTokens}
              onChange={(_, value) => setMaxTokens(value as number)}
              min={100}
              max={8000}
              step={100}
              valueLabelDisplay="auto"
              disabled={loading}
            />
          </Grid>

          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={useStreaming}
                  onChange={(e) => setUseStreaming(e.target.checked)}
                  disabled={loading}
                />
              }
              label="Use Streaming"
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              fullWidth
              label="System Prompt"
              value={systemPrompt}
              onChange={(e) => setSystemPrompt(e.target.value)}
              placeholder="Enter a system prompt to guide the model's behavior"
              disabled={loading}
            />
          </Grid>
        </Grid>
      </Paper>

      <Paper sx={{ height: 'calc(100vh - 300px)', display: 'flex', flexDirection: 'column' }}>
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            p: 2,
            borderBottom: '1px solid rgba(0, 0, 0, 0.12)',
          }}
        >
          <Typography variant="h6">Chat</Typography>
          <Button
            startIcon={<DeleteIcon />}
            onClick={handleClearChat}
            disabled={loading || messages.length === 0}
          >
            Clear Chat
          </Button>
        </Box>

        <Box
          sx={{
            flexGrow: 1,
            overflow: 'auto',
            p: 2,
            display: 'flex',
            flexDirection: 'column',
            gap: 2,
          }}
        >
          {messages.length === 0 ? (
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                height: '100%',
                color: 'text.secondary',
              }}
            >
              {modelsLoading ? (
                <Box sx={{ textAlign: 'center' }}>
                  <CircularProgress size={40} sx={{ mb: 2 }} />
                  <Typography variant="body1">Loading models...</Typography>
                </Box>
              ) : models.length === 0 ? (
                <Box sx={{ textAlign: 'center', maxWidth: '80%' }}>
                  <Typography variant="body1" color="error" gutterBottom>
                    No LLM models available. Please check your API configuration.
                  </Typography>
                  <Button
                    variant="contained"
                    color="primary"
                    startIcon={<RefreshIcon />}
                    onClick={handleRefreshModels}
                    sx={{ mt: 2 }}
                  >
                    Refresh Models
                  </Button>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                    Make sure you've set up API credentials in the API Keys section.
                  </Typography>
                </Box>
              ) : (
                <Typography variant="body1">
                  Start a conversation with the selected LLM model.
                </Typography>
              )}
            </Box>
          ) : (
            messages.map((message, index) => (
              <Card
                key={index}
                sx={{
                  maxWidth: '80%',
                  alignSelf: message.role === 'user' ? 'flex-end' : 'flex-start',
                  bgcolor: message.role === 'user' ? 'primary.light' : 'background.paper',
                  color: message.role === 'user' ? 'primary.contrastText' : 'text.primary',
                }}
              >
                <CardContent>
                  <Typography
                    variant="body1"
                    sx={{ whiteSpace: 'pre-wrap' }}
                  >
                    {message.content}
                  </Typography>
                </CardContent>
              </Card>
            ))
          )}
          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}>
              <CircularProgress size={24} />
            </Box>
          )}
          <div ref={messagesEndRef} />
        </Box>

        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            handleSendMessage();
          }}
          sx={{
            p: 2,
            borderTop: '1px solid rgba(0, 0, 0, 0.12)',
            display: 'flex',
            gap: 1,
          }}
        >
          <TextField
            fullWidth
            multiline
            maxRows={4}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={models.length === 0 ? "Please configure LLM models first" : "Type your message..."}
            disabled={loading || !selectedModelId || models.length === 0}
            sx={{ flexGrow: 1 }}
          />
          <Button
            variant="contained"
            color="primary"
            endIcon={<SendIcon />}
            onClick={handleSendMessage}
            disabled={loading || !input.trim() || !selectedModelId || models.length === 0}
          >
            Send
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};

export default ChatPlaygroundPage;
