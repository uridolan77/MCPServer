import React, { useState } from 'react';

const LlmChat = () => {
  const [input, setInput] = useState('');
  const [messages, setMessages] = useState([]);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!input.trim()) return;

    // Add user message to chat
    const userMessage = { role: 'user', content: input };
    setMessages([...messages, userMessage]);
    setLoading(true);

    try {
      // Call the API
      const response = await fetch('/api/mcp/message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ' + localStorage.getItem('token') || ''
        },
        body: JSON.stringify({
          sessionId: 'web-session',
          userInput: input,
          metadata: {
            source: 'web-client'
          }
        }),
      });

      if (!response.ok) {
        throw new Error('API request failed');
      }

      const data = await response.text();

      // Add assistant response to chat
      const assistantMessage = { role: 'assistant', content: data };
      setMessages([...messages, userMessage, assistantMessage]);
    } catch (error) {
      console.error('Error:', error);
      // Add error message
      const errorMessage = { role: 'assistant', content: 'Sorry, there was an error processing your request.' };
      setMessages([...messages, userMessage, errorMessage]);
    } finally {
      setLoading(false);
      setInput('');
    }
  };

  return (
    <div className="llm-chat">
      <div className="chat-messages">
        {messages.map((message, index) => (
          <div key={index} className={`message ${message.role}`}>
            <div className="message-content">{message.content}</div>
          </div>
        ))}
        {loading && <div className="loading">Thinking...</div>}
      </div>
      <form onSubmit={handleSubmit} className="chat-input-form">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Type your message..."
          disabled={loading}
        />
        <button type="submit" disabled={loading || !input.trim()}>
          Send
        </button>
      </form>
    </div>
  );
};

export default LlmChat;
