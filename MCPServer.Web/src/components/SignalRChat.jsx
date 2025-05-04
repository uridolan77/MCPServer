import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

const SignalRChat = () => {
  const [connection, setConnection] = useState(null);
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState('web-session-' + Math.random().toString(36).substring(2, 9));
  const [connectionStatus, setConnectionStatus] = useState('Disconnected');
  const messagesEndRef = useRef(null);

  // Initialize SignalR connection
  useEffect(() => {
    // Create a new connection
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/mcp', {
        accessTokenFactory: () => localStorage.getItem('token')
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up event handlers
    newConnection.on('ReceiveMessage', (response) => {
      if (response.sessionId === sessionId) {
        setMessages(prevMessages => {
          // Check if this is a streaming chunk or a new message
          if (response.isComplete) {
            // This is a complete message, add it as a new message
            return [...prevMessages, { role: 'assistant', content: response.output }];
          } else {
            // This is a streaming chunk, update the last message if it's from assistant
            const updatedMessages = [...prevMessages];
            const lastMessage = updatedMessages[updatedMessages.length - 1];
            
            if (lastMessage && lastMessage.role === 'assistant') {
              // Update the last message content
              lastMessage.content += response.output;
            } else {
              // Add a new assistant message
              updatedMessages.push({ role: 'assistant', content: response.output });
            }
            
            return updatedMessages;
          }
        });
        
        if (response.isComplete) {
          setLoading(false);
        }
      }
    });

    newConnection.on('ReceiveError', (errorMessage) => {
      console.error('SignalR Error:', errorMessage);
      setMessages(prevMessages => [
        ...prevMessages, 
        { role: 'system', content: `Error: ${errorMessage}`, isError: true }
      ]);
      setLoading(false);
    });

    // Start the connection
    newConnection.start()
      .then(() => {
        console.log('SignalR Connected');
        setConnectionStatus('Connected');
        setConnection(newConnection);
      })
      .catch(err => {
        console.error('SignalR Connection Error:', err);
        setConnectionStatus(`Connection Error: ${err.message}`);
      });

    // Clean up on unmount
    return () => {
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, [sessionId]);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!input.trim() || !connection || connection.state !== signalR.HubConnectionState.Connected) return;

    // Add user message to chat
    const userMessage = { role: 'user', content: input };
    setMessages([...messages, userMessage]);
    setLoading(true);

    try {
      // Send message via SignalR
      await connection.invoke('SendMessage', {
        sessionId: sessionId,
        userInput: input,
        stream: true,
        metadata: {
          source: 'web-client'
        }
      });
      
      // Clear input after sending
      setInput('');
    } catch (error) {
      console.error('Error sending message:', error);
      setMessages([
        ...messages, 
        userMessage,
        { role: 'system', content: `Error sending message: ${error.message}`, isError: true }
      ]);
      setLoading(false);
    }
  };

  return (
    <div className="llm-chat">
      <div className="connection-status">
        Status: {connectionStatus}
      </div>
      <div className="chat-messages">
        {messages.map((message, index) => (
          <div 
            key={index} 
            className={`message ${message.role} ${message.isError ? 'error' : ''}`}
          >
            <div className="message-content">{message.content}</div>
          </div>
        ))}
        {loading && <div className="loading">Thinking...</div>}
        <div ref={messagesEndRef} />
      </div>
      <form onSubmit={handleSubmit} className="chat-input-form">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Type your message..."
          disabled={loading || connection?.state !== signalR.HubConnectionState.Connected}
        />
        <button 
          type="submit" 
          disabled={loading || !input.trim() || connection?.state !== signalR.HubConnectionState.Connected}
        >
          Send
        </button>
      </form>
    </div>
  );
};

export default SignalRChat;
