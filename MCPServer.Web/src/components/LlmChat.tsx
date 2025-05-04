import React, { useState } from 'react';

interface Message {
  role: 'user' | 'assistant';
  content: string;
}

interface ApiRequest {
  sessionId: string;
  userInput: string;
  metadata: {
    source: string;
    [key: string]: string;
  };
}

const LlmChat: React.FC = () => {
  const [input, setInput] = useState<string>('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim()) return;

    // Add user message to chat
    const userMessage: Message = { role: 'user', content: input };
    setMessages([...messages, userMessage]);
    setLoading(true);

    try {
      // Call the API
      const requestData: ApiRequest = {
        sessionId: 'web-session',
        userInput: input,
        metadata: {
          source: 'web-client'
        }
      };

      const response = await fetch('/api/mcp/message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ' + localStorage.getItem('token') || ''
        },
        body: JSON.stringify(requestData),
      });

      if (!response.ok) {
        throw new Error('API request failed');
      }

      const data = await response.text();

      // Add assistant response to chat
      const assistantMessage: Message = { role: 'assistant', content: data };
      setMessages([...messages, userMessage, assistantMessage]);
    } catch (error) {
      console.error('Error:', error);
      // Add error message
      const errorMessage: Message = { role: 'assistant', content: 'Sorry, there was an error processing your request.' };
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
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setInput(e.target.value)}
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