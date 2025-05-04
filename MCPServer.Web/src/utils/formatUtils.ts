export const truncateText = (text: string, maxLength: number = 50): string => {
  if (!text) return '';
  if (text.length <= maxLength) return text;
  return `${text.substring(0, maxLength)}...`;
};

export const formatNumber = (num: number, options?: Intl.NumberFormatOptions): string => {
  if (num === null || num === undefined) return '';
  return new Intl.NumberFormat('en-US', options).format(num);
};

export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes';

  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
};

export const formatBoolean = (value: boolean, trueText: string = 'Yes', falseText: string = 'No'): string => {
  return value ? trueText : falseText;
};

export const formatJson = (json: string | object): string => {
  try {
    const obj = typeof json === 'string' ? JSON.parse(json) : json;
    return JSON.stringify(obj, null, 2);
  } catch (error) {
    return typeof json === 'string' ? json : JSON.stringify(json);
  }
};

export const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(amount);
};
