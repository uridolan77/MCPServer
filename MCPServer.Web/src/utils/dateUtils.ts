import { format, formatDistance, parseISO } from 'date-fns';

export const formatDate = (date: string | Date, formatString: string = 'MMM d, yyyy'): string => {
  if (!date) return '';
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return format(dateObj, formatString);
};

export const formatDateTime = (date: string | Date, formatString: string = 'MMM d, yyyy h:mm a'): string => {
  if (!date) return '';
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return format(dateObj, formatString);
};

export const formatRelativeTime = (date: string | Date): string => {
  if (!date) return '';
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return formatDistance(dateObj, new Date(), { addSuffix: true });
};
