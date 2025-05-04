import React, { createContext, useContext, useState, ReactNode } from 'react';
import { Alert, AlertColor, Snackbar } from '@mui/material';

interface Notification {
  id: number;
  message: string;
  type: AlertColor;
  autoHideDuration?: number;
}

interface NotificationContextType {
  notifications: Notification[];
  addNotification: (message: string, type?: AlertColor, autoHideDuration?: number) => void;
  removeNotification: (id: number) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [nextId, setNextId] = useState(1);

  const addNotification = (
    message: string,
    type: AlertColor = 'info',
    autoHideDuration: number = 6000
  ) => {
    const id = nextId;
    setNextId(id + 1);
    
    setNotifications((prev) => [
      ...prev,
      {
        id,
        message,
        type,
        autoHideDuration,
      },
    ]);
    
    // Auto-remove notification after duration
    if (autoHideDuration !== null) {
      setTimeout(() => {
        removeNotification(id);
      }, autoHideDuration);
    }
  };

  const removeNotification = (id: number) => {
    setNotifications((prev) => prev.filter((notification) => notification.id !== id));
  };

  return (
    <NotificationContext.Provider value={{ notifications, addNotification, removeNotification }}>
      {children}
      
      {/* Render notifications */}
      {notifications.map((notification) => (
        <Snackbar
          key={notification.id}
          open={true}
          autoHideDuration={notification.autoHideDuration}
          onClose={() => removeNotification(notification.id)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        >
          <Alert
            onClose={() => removeNotification(notification.id)}
            severity={notification.type}
            variant="filled"
            sx={{ width: '100%' }}
          >
            {notification.message}
          </Alert>
        </Snackbar>
      ))}
    </NotificationContext.Provider>
  );
};

export const useNotification = (): NotificationContextType => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
};
