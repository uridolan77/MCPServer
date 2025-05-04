import { useState, useCallback } from 'react';

interface UseConfirmDialogReturn {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  confirmColor: 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning';
  isLoading: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  showDialog: (options: ShowDialogOptions) => void;
  hideDialog: () => void;
  setLoading: (isLoading: boolean) => void;
}

interface ShowDialogOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmColor?: 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning';
  onConfirm: () => void;
  onCancel?: () => void;
}

const useConfirmDialog = (): UseConfirmDialogReturn => {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');
  const [confirmLabel, setConfirmLabel] = useState('Confirm');
  const [cancelLabel, setCancelLabel] = useState('Cancel');
  const [confirmColor, setConfirmColor] = useState<'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'>('primary');
  const [isLoading, setIsLoading] = useState(false);
  const [confirmCallback, setConfirmCallback] = useState<() => void>(() => () => {});
  const [cancelCallback, setCancelCallback] = useState<() => void>(() => () => {});

  const showDialog = useCallback(
    ({
      title,
      message,
      confirmLabel = 'Confirm',
      cancelLabel = 'Cancel',
      confirmColor = 'primary',
      onConfirm,
      onCancel = () => {},
    }: ShowDialogOptions) => {
      setTitle(title);
      setMessage(message);
      setConfirmLabel(confirmLabel);
      setCancelLabel(cancelLabel);
      setConfirmColor(confirmColor);
      setConfirmCallback(() => onConfirm);
      setCancelCallback(() => onCancel);
      setIsOpen(true);
      setIsLoading(false);
    },
    []
  );

  const hideDialog = useCallback(() => {
    setIsOpen(false);
  }, []);

  const handleConfirm = useCallback(() => {
    confirmCallback();
  }, [confirmCallback]);

  const handleCancel = useCallback(() => {
    hideDialog();
    cancelCallback();
  }, [hideDialog, cancelCallback]);

  const setLoading = useCallback((loading: boolean) => {
    setIsLoading(loading);
  }, []);

  return {
    isOpen,
    title,
    message,
    confirmLabel,
    cancelLabel,
    confirmColor,
    isLoading,
    onConfirm: handleConfirm,
    onCancel: handleCancel,
    showDialog,
    hideDialog,
    setLoading,
  };
};

export default useConfirmDialog;
