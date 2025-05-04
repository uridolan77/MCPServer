// ChatMessagesTable.tsx
import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  Paper,
  TextField,
  InputAdornment,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  useTheme,
  Tooltip
} from '@mui/material';
import {
  Search as SearchIcon,
  Visibility as VisibilityIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Comment as CommentIcon,
  QuestionAnswer as QuestionAnswerIcon
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ChatUsageLog } from '@/api/analyticsApi';
import { useUsageContext } from './UsageContext';

const ChatMessagesTable: React.FC = () => {
  const theme = useTheme();
  const { chatUsageLogs, isLoading, setFilteredChatLogs } = useUsageContext();
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedMessage, setSelectedMessage] = useState<ChatUsageLog | null>(null);
  const [detailsOpen, setDetailsOpen] = useState(false);

  // Debugging helper function to directly inspect the message data
  React.useEffect(() => {
    if (Array.isArray(chatUsageLogs) && chatUsageLogs.length > 0) {
      console.log('Chat messages data sample:', {
        firstItem: chatUsageLogs[0],
        messageField: chatUsageLogs[0]?.message,
        responseField: chatUsageLogs[0]?.response,
        messageLength: chatUsageLogs[0]?.message?.length || 0,
        responseLength: chatUsageLogs[0]?.response?.length || 0
      });
    }
  }, [chatUsageLogs]);

  // Handle search input change
  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
    setPage(0); // Reset to first page on search
  };

  // Filter logs based on search term
  const filteredMessages = Array.isArray(chatUsageLogs)
    ? chatUsageLogs.filter(log => {
        const searchLower = searchTerm.toLowerCase();
        return (
          (log.message && log.message.toLowerCase().includes(searchLower)) ||
          (log.response && log.response.toLowerCase().includes(searchLower)) ||
          (log.modelName && log.modelName.toLowerCase().includes(searchLower)) ||
          (log.providerName && log.providerName.toLowerCase().includes(searchLower)) ||
          (log.sessionId && log.sessionId.toLowerCase().includes(searchLower))
        );
      })
    : [];

  // Update filtered messages in context
  React.useEffect(() => {
    setFilteredChatLogs(filteredMessages);
  }, [filteredMessages, setFilteredChatLogs]);

  // Handle pagination
  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  // Handle opening message details dialog
  const handleOpenDetails = (message: ChatUsageLog) => {
    setSelectedMessage(message);
    setDetailsOpen(true);
  };

  // Handle closing message details dialog
  const handleCloseDetails = () => {
    setDetailsOpen(false);
  };

  // Get formatted date-time
  const formatDateTime = (dateTimeString: string) => {
    try {
      return format(new Date(dateTimeString), 'MMM d, yyyy HH:mm:ss');
    } catch (error) {
      return 'Invalid date';
    }
  };

  // Truncate text to a maximum length
  const truncateText = (text: string, maxLength: number = 50) => {
    if (!text) return '';
    return text.length > maxLength ? `${text.substring(0, maxLength)}...` : text;
  };

  // Render table content
  const renderTableContent = () => {
    if (isLoading) {
      return (
        <TableRow>
          <TableCell colSpan={7} align="center">
            <Typography variant="body2" color="text.secondary" py={2}>
              Loading chat messages...
            </Typography>
          </TableCell>
        </TableRow>
      );
    }

    if (filteredMessages.length === 0) {
      return (
        <TableRow>
          <TableCell colSpan={7} align="center">
            <Typography variant="body2" color="text.secondary" py={2}>
              No chat messages found
            </Typography>
          </TableCell>
        </TableRow>
      );
    }

    return filteredMessages
      .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
      .map((message) => (
        <TableRow key={message.id}>
          <TableCell>{formatDateTime(message.timestamp)}</TableCell>
          <TableCell>{message.modelName}</TableCell>
          <TableCell>
            <Tooltip title={message.message}>
              <Typography sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {truncateText(message.message, 30)}
              </Typography>
            </Tooltip>
          </TableCell>
          <TableCell>
            <Tooltip title={message.response}>
              <Typography sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {truncateText(message.response, 30)}
              </Typography>
            </Tooltip>
          </TableCell>
          <TableCell>{message.duration} ms</TableCell>
          <TableCell>
            <Chip
              size="small"
              label={message.success ? "Success" : "Failed"}
              color={message.success ? 'success' : 'error'}
              icon={message.success ? <SuccessIcon /> : <ErrorIcon />}
            />
          </TableCell>
          <TableCell align="right">
            <IconButton
              size="small"
              onClick={() => handleOpenDetails(message)}
              title="View Message Details"
            >
              <QuestionAnswerIcon fontSize="small" />
            </IconButton>
          </TableCell>
        </TableRow>
      ));
  };

  const renderMessageDialog = () => {
    if (!selectedMessage) return null;

    return (
      <Box>
        <Typography variant="h6" gutterBottom>Chat Message Details</Typography>
        <Box mb={2}>
          <Typography variant="subtitle2">Time</Typography>
          <Typography variant="body2" gutterBottom>{formatDateTime(selectedMessage.timestamp)}</Typography>
          
          <Typography variant="subtitle2">Session ID</Typography>
          <Typography variant="body2" gutterBottom>{selectedMessage.sessionId}</Typography>
          
          <Typography variant="subtitle2">Model</Typography>
          <Typography variant="body2" gutterBottom>{selectedMessage.modelName} ({selectedMessage.providerName})</Typography>
          
          <Typography variant="subtitle2">Status</Typography>
          <Chip
            size="small"
            label={selectedMessage.success ? "Success" : "Failed"}
            color={selectedMessage.success ? 'success' : 'error'}
            sx={{ mb: 1 }}
          />
          
          {selectedMessage.errorMessage && (
            <>
              <Typography variant="subtitle2">Error</Typography>
              <Typography variant="body2" color="error.main" gutterBottom>
                {selectedMessage.errorMessage}
              </Typography>
            </>
          )}

          <Typography variant="subtitle2">Performance</Typography>
          <Typography variant="body2" gutterBottom>
            Duration: {selectedMessage.duration} ms | 
            Tokens: {selectedMessage.inputTokenCount} in / {selectedMessage.outputTokenCount} out | 
            Cost: ${selectedMessage.estimatedCost?.toFixed(5) || "0.00000"}
          </Typography>
        </Box>

        <Box sx={{ 
          display: 'flex', 
          flexDirection: 'column', 
          gap: 2, 
          maxHeight: '60vh', 
          overflow: 'auto' 
        }}>
          <Paper sx={{ p: 2, backgroundColor: theme.palette.grey[50] }}>
            <Typography variant="subtitle1" sx={{ mb: 1, display: 'flex', alignItems: 'center' }}>
              <CommentIcon fontSize="small" sx={{ mr: 1 }} /> User Message
            </Typography>
            <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
              {selectedMessage.message}
            </Typography>
          </Paper>
          
          <Paper sx={{ p: 2, backgroundColor: theme.palette.mode === 'light' ? theme.palette.primary.light : theme.palette.primary.dark }}>
            <Typography variant="subtitle1" sx={{ 
              mb: 1, 
              display: 'flex', 
              alignItems: 'center',
              color: theme.palette.getContrastText(theme.palette.mode === 'light' ? theme.palette.primary.light : theme.palette.primary.dark) 
            }}>
              <QuestionAnswerIcon fontSize="small" sx={{ mr: 1 }} /> AI Response
            </Typography>
            <Typography variant="body2" sx={{ 
              whiteSpace: 'pre-wrap',
              color: theme.palette.getContrastText(theme.palette.mode === 'light' ? theme.palette.primary.light : theme.palette.primary.dark) 
            }}>
              {selectedMessage.response}
            </Typography>
          </Paper>
        </Box>
      </Box>
    );
  };

  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Chat Messages</Typography>
            <TextField
              placeholder="Search messages..."
              size="small"
              value={searchTerm}
              onChange={handleSearchChange}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" />
                  </InputAdornment>
                ),
              }}
              sx={{ width: 250 }}
            />
          </Box>

          <TableContainer component={Paper} elevation={0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Time</TableCell>
                  <TableCell>Model</TableCell>
                  <TableCell>Message</TableCell>
                  <TableCell>Response</TableCell>
                  <TableCell>Duration</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {renderTableContent()}
              </TableBody>
            </Table>
          </TableContainer>

          <TablePagination
            rowsPerPageOptions={[5, 10, 25, 50]}
            component="div"
            count={filteredMessages.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
          />
        </CardContent>
      </Card>

      {/* Message Details Dialog */}
      <Dialog open={detailsOpen} onClose={handleCloseDetails} maxWidth="md" fullWidth>
        <DialogTitle>
          Chat Message
        </DialogTitle>
        <DialogContent>
          {renderMessageDialog()}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDetails}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ChatMessagesTable;