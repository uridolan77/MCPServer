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
  TableFooter,
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
  useTheme
} from '@mui/material';
import {
  Search as SearchIcon,
  Visibility as VisibilityIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { LlmUsageLog } from '@/api/llmProviderApi';
import { ChatUsageLog } from '@/api/analyticsApi';
import { useUsageContext } from './UsageContext';

const UsageTable: React.FC = () => {
  const theme = useTheme();
  const { usageLogs, chatUsageLogs, models, isLoading, setFilteredChatLogs, setFilteredUsageLogs, filteredChatLogs, filteredUsageLogs } = useUsageContext();
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedLog, setSelectedLog] = useState<any | null>(null); // Can be LlmUsageLog or ChatUsageLog
  const [detailsOpen, setDetailsOpen] = useState(false);

  // Handle search input change
  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
    setPage(0); // Reset to first page on search
  };

  // Helper function to determine which logs to use
  const determineLogsSource = () => {
    return chatUsageLogs && chatUsageLogs.length > 0
      ? { logs: filteredChatLogs || [], type: 'chat' }
      : { logs: filteredUsageLogs || [], type: 'llm' };
  };

  // Get appropriate logs based on priority
  const { logs, type } = determineLogsSource();

  // Filter logs based on search term
  const filteredLogs = Array.isArray(logs) 
    ? logs.filter(log => {
        const searchLower = searchTerm.toLowerCase();
        
        if (type === 'chat') {
          const chatLog = log as ChatUsageLog;
          return (
            chatLog.modelName?.toLowerCase().includes(searchLower) ||
            chatLog.providerName?.toLowerCase().includes(searchLower) ||
            (chatLog.success === true ? 'success' : 'failed').includes(searchLower) ||
            chatLog.errorMessage?.toLowerCase().includes(searchLower) ||
            chatLog.sessionId?.toLowerCase().includes(searchLower)
          );
        } else {
          const llmLog = log as LlmUsageLog;
          const modelName = models.find(m => m.id === llmLog.modelId)?.name?.toLowerCase() || '';
          
          return (
            modelName.includes(searchLower) ||
            llmLog.status?.toLowerCase().includes(searchLower) ||
            llmLog.errorMessage?.toLowerCase().includes(searchLower)
          );
        }
      })
    : [];

  // Update filtered logs in context when they change
  React.useEffect(() => {
    if (type === 'chat') {
      setFilteredChatLogs(filteredLogs as ChatUsageLog[]);
    } else {
      setFilteredUsageLogs(filteredLogs as LlmUsageLog[]);
    }
  }, [filteredLogs, type, setFilteredChatLogs, setFilteredUsageLogs]);

  // Handle pagination
  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  // Handle opening log details dialog
  const handleOpenDetails = (log: any) => {
    setSelectedLog(log);
    setDetailsOpen(true);
  };

  // Handle closing log details dialog
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

  // Get model name from model ID for legacy logs
  const getModelName = (modelId: number) => {
    const model = models.find(m => m.id === modelId);
    return model ? model.name : `Unknown (ID: ${modelId})`;
  };

  // Calculate totals for ALL filtered logs, not just the current page
  const calculateTotals = () => {
    return filteredLogs.reduce(
      (totals, log) => {
        if (type === 'chat') {
          const chatLog = log as ChatUsageLog;
          totals.inputTokens += chatLog.inputTokenCount || 0;
          totals.outputTokens += chatLog.outputTokenCount || 0;
          totals.totalTokens += (chatLog.inputTokenCount + chatLog.outputTokenCount) || 0;
          totals.estimatedCost += chatLog.estimatedCost || 0;
          totals.totalDuration += chatLog.duration || 0;
          totals.totalSuccessful += chatLog.success ? 1 : 0;
          totals.totalRequests += 1;
        } else {
          const llmLog = log as LlmUsageLog;
          totals.inputTokens += llmLog.inputTokens || 0;
          totals.outputTokens += llmLog.outputTokens || 0;
          totals.totalTokens += llmLog.totalTokens || 0;
          totals.estimatedCost += llmLog.estimatedCost || 0;
          totals.totalDuration += llmLog.durationMs || 0;
          totals.totalSuccessful += (llmLog.status === 'Success' || llmLog.status === 'Succeeded') ? 1 : 0;
          totals.totalRequests += 1;
        }
        return totals;
      },
      { 
        inputTokens: 0, 
        outputTokens: 0, 
        totalTokens: 0, 
        estimatedCost: 0,
        totalDuration: 0,
        totalSuccessful: 0,
        totalRequests: 0
      }
    );
  };

  const totals = calculateTotals();

  // Render table based on the type of logs
  const renderTableContent = () => {
    if (isLoading) {
      return (
        <TableRow>
          <TableCell colSpan={8} align="center">
            <Typography variant="body2" color="text.secondary" py={2}>
              Loading usage data...
            </Typography>
          </TableCell>
        </TableRow>
      );
    }

    if (filteredLogs.length === 0) {
      return (
        <TableRow>
          <TableCell colSpan={8} align="center">
            <Typography variant="body2" color="text.secondary" py={2}>
              No usage data found
            </Typography>
          </TableCell>
        </TableRow>
      );
    }

    // Chat logs display
    if (type === 'chat') {
      return filteredLogs
        .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
        .map((log: ChatUsageLog) => (
          <TableRow key={log.id}>
            <TableCell>{formatDateTime(log.timestamp)}</TableCell>
            <TableCell>{log.modelName}</TableCell>
            <TableCell sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {log.message?.length > 50 ? `${log.message.substring(0, 50)}...` : log.message}
            </TableCell>
            <TableCell sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {log.response?.length > 50 ? `${log.response.substring(0, 50)}...` : log.response}
            </TableCell>
            <TableCell>{log.duration} ms</TableCell>
            <TableCell>{log.inputTokenCount?.toLocaleString() || 0}</TableCell>
            <TableCell>{log.outputTokenCount?.toLocaleString() || 0}</TableCell>
            <TableCell>{(log.inputTokenCount + log.outputTokenCount)?.toLocaleString() || 0}</TableCell>
            <TableCell>${log.estimatedCost?.toFixed(5) || "0.00000"}</TableCell>
            <TableCell>
              <Chip
                size="small"
                label={log.success ? "Success" : "Failed"}
                color={log.success ? 'success' : 'error'}
                icon={log.success ? <SuccessIcon /> : <ErrorIcon />}
              />
            </TableCell>
            <TableCell align="right">
              <IconButton 
                size="small" 
                onClick={() => handleOpenDetails(log)}
                title="View Details"
              >
                <VisibilityIcon fontSize="small" />
              </IconButton>
            </TableCell>
          </TableRow>
        ));
    }
    
    // Legacy LLM logs display
    return filteredLogs
      .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
      .map((log: LlmUsageLog) => (
        <TableRow key={log.id}>
          <TableCell>{formatDateTime(log.requestTimestamp)}</TableCell>
          <TableCell>{getModelName(log.modelId)}</TableCell>
          <TableCell sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {log.prompt?.length > 50 ? `${log.prompt.substring(0, 50)}...` : log.prompt || "-"}
          </TableCell>
          <TableCell sx={{ maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {log.completion?.length > 50 ? `${log.completion.substring(0, 50)}...` : log.completion || "-"}
          </TableCell>
          <TableCell>{log.durationMs || "-"} ms</TableCell>
          <TableCell>{log.inputTokens?.toLocaleString() || 0}</TableCell>
          <TableCell>{log.outputTokens?.toLocaleString() || 0}</TableCell>
          <TableCell>{log.totalTokens?.toLocaleString() || 0}</TableCell>
          <TableCell>${log.estimatedCost?.toFixed(5) || "0.00000"}</TableCell>
          <TableCell>
            <Chip
              size="small"
              label={log.status || "Unknown"}
              color={log.status === 'Success' ? 'success' : 'error'}
              icon={log.status === 'Success' ? <SuccessIcon /> : <ErrorIcon />}
            />
          </TableCell>
          <TableCell align="right">
            <IconButton 
              size="small" 
              onClick={() => handleOpenDetails(log)}
              title="View Details"
            >
              <VisibilityIcon fontSize="small" />
            </IconButton>
          </TableCell>
        </TableRow>
      ));
  };

  // Render dialog content based on log type
  const renderDialogContent = () => {
    if (!selectedLog) return null;

    if (type === 'chat') {
      // Chat usage log details
      const chatLog = selectedLog as ChatUsageLog;
      return (
        <Box>
          <Table size="small">
            <TableBody>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Request ID
                </TableCell>
                <TableCell>{chatLog.id}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Timestamp
                </TableCell>
                <TableCell>{formatDateTime(chatLog.timestamp)}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Session ID
                </TableCell>
                <TableCell>{chatLog.sessionId}</TableCell>
              </TableRow>
              {chatLog.userId && (
                <TableRow>
                  <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                    User
                  </TableCell>
                  <TableCell>{chatLog.userId}</TableCell>
                </TableRow>
              )}
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Model
                </TableCell>
                <TableCell>{chatLog.modelName}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Provider
                </TableCell>
                <TableCell>{chatLog.providerName}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Input Tokens
                </TableCell>
                <TableCell>{chatLog.inputTokenCount?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Output Tokens
                </TableCell>
                <TableCell>{chatLog.outputTokenCount?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Total Tokens
                </TableCell>
                <TableCell>{(chatLog.inputTokenCount + chatLog.outputTokenCount)?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Duration
                </TableCell>
                <TableCell>{chatLog.duration?.toFixed(2) || 0} ms</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Estimated Cost
                </TableCell>
                <TableCell>${chatLog.estimatedCost?.toFixed(5) || "0.00000"}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Status
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={chatLog.success ? "Success" : "Failed"}
                    color={chatLog.success ? 'success' : 'error'}
                  />
                </TableCell>
              </TableRow>
              {chatLog.errorMessage && (
                <TableRow>
                  <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                    Error Message
                  </TableCell>
                  <TableCell>{chatLog.errorMessage}</TableCell>
                </TableRow>
              )}
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Message
                </TableCell>
                <TableCell sx={{ whiteSpace: 'pre-wrap', maxHeight: '150px', overflow: 'auto' }}>
                  {chatLog.message}
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Response
                </TableCell>
                <TableCell sx={{ whiteSpace: 'pre-wrap', maxHeight: '150px', overflow: 'auto' }}>
                  {chatLog.response}
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </Box>
      );
    } else {
      // Legacy LLM log details
      const llmLog = selectedLog as LlmUsageLog;
      return (
        <Box>
          <Table size="small">
            <TableBody>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Request ID
                </TableCell>
                <TableCell>{llmLog.id}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Timestamp
                </TableCell>
                <TableCell>{formatDateTime(llmLog.requestTimestamp)}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Model
                </TableCell>
                <TableCell>{getModelName(llmLog.modelId)}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Input Tokens
                </TableCell>
                <TableCell>{llmLog.inputTokens?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Output Tokens
                </TableCell>
                <TableCell>{llmLog.outputTokens?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Total Tokens
                </TableCell>
                <TableCell>{llmLog.totalTokens?.toLocaleString() || 0}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Estimated Cost
                </TableCell>
                <TableCell>${llmLog.estimatedCost?.toFixed(5) || "0.00000"}</TableCell>
              </TableRow>
              <TableRow>
                <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                  Status
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={llmLog.status || "Unknown"}
                    color={llmLog.status === 'Success' ? 'success' : 'error'}
                  />
                </TableCell>
              </TableRow>
              {llmLog.errorMessage && (
                <TableRow>
                  <TableCell component="th" scope="row" sx={{ fontWeight: 'bold' }}>
                    Error Message
                  </TableCell>
                  <TableCell>{llmLog.errorMessage}</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      );
    }
  };

  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Usage Logs</Typography>
            <TextField
              placeholder="Search..."
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
                  <TableCell>Input Tokens</TableCell>
                  <TableCell>Output Tokens</TableCell>
                  <TableCell>Total Tokens</TableCell>
                  <TableCell>Cost</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {renderTableContent()}
              </TableBody>
              {filteredLogs.length > 0 && (
                <TableFooter>
                  <TableRow sx={{ 
                    backgroundColor: theme.palette.background.default,
                    '& .MuiTableCell-root': { 
                      fontWeight: 'bold',
                      borderTop: `1px solid ${theme.palette.divider}`
                    } 
                  }}>
                    <TableCell colSpan={5} align="right">Totals ({totals.totalRequests} requests):</TableCell>
                    <TableCell>{totals.inputTokens.toLocaleString()}</TableCell>
                    <TableCell>{totals.outputTokens.toLocaleString()}</TableCell>
                    <TableCell>{totals.totalTokens.toLocaleString()}</TableCell>
                    <TableCell>${totals.estimatedCost.toFixed(5)}</TableCell>
                    <TableCell colSpan={2}>
                      {Math.round((totals.totalSuccessful / totals.totalRequests) * 100)}% success
                    </TableCell>
                  </TableRow>
                  <TableRow sx={{
                    backgroundColor: `${theme.palette.primary.main}10`,
                    '& .MuiTableCell-root': {
                      fontWeight: 'medium',
                      fontSize: '0.8rem',
                      color: theme.palette.text.secondary
                    }
                  }}>
                    <TableCell colSpan={5} align="right">Averages:</TableCell>
                    <TableCell>{totals.totalRequests ? Math.round(totals.inputTokens / totals.totalRequests).toLocaleString() : 0}</TableCell>
                    <TableCell>{totals.totalRequests ? Math.round(totals.outputTokens / totals.totalRequests).toLocaleString() : 0}</TableCell>
                    <TableCell>{totals.totalRequests ? Math.round(totals.totalTokens / totals.totalRequests).toLocaleString() : 0}</TableCell>
                    <TableCell>${totals.totalRequests ? (totals.estimatedCost / totals.totalRequests).toFixed(5) : "0.00000"}</TableCell>
                    <TableCell colSpan={2}>
                      {totals.totalRequests ? Math.round(totals.totalDuration / totals.totalRequests) : 0} ms avg
                    </TableCell>
                  </TableRow>
                </TableFooter>
              )}
            </Table>
          </TableContainer>

          <TablePagination
            rowsPerPageOptions={[5, 10, 25, 50]}
            component="div"
            count={filteredLogs.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
          />
        </CardContent>
      </Card>

      {/* Details Dialog */}
      <Dialog open={detailsOpen} onClose={handleCloseDetails} maxWidth="md" fullWidth>
        <DialogTitle>
          Usage Log Details
        </DialogTitle>
        <DialogContent>
          {renderDialogContent()}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDetails}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default UsageTable;