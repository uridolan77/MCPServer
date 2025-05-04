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
  DialogContentText,
  DialogActions,
  Button,
  Grid,
  useTheme,
  LinearProgress
} from '@mui/material';
import {
  Search as SearchIcon,
  Visibility as VisibilityIcon,
  Delete as DeleteIcon,
  Schedule as ScheduleIcon,
  ChatBubble as ChatIcon
} from '@mui/icons-material';
import { format } from 'date-fns';
import { SessionData } from '@/api/analyticsApi';
import { useUsageContext } from './UsageContext';
import { analyticsApi } from '@/api/analyticsApi';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useErrorHandler } from '@/hooks';

const SessionsView: React.FC = () => {
  const theme = useTheme();
  const { handleError } = useErrorHandler();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedSession, setSelectedSession] = useState<SessionData | null>(null);
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null);

  // Get sessions data using React Query
  const { data: sessions = [], isLoading } = useQuery({
    queryKey: ['sessions'],
    queryFn: async () => {
      try {
        const allSessions = await analyticsApi.getAllSessions();
        return allSessions;
      } catch (error) {
        handleError(error, 'Failed to fetch sessions data');
        return [];
      }
    }
  });

  // Delete session mutation
  const deleteMutation = useMutation({
    mutationFn: (sessionId: string) => {
      return analyticsApi.deleteSession(sessionId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
    },
    onError: (error) => {
      handleError(error, 'Failed to delete session');
    }
  });

  // Filter sessions based on search term
  const filteredSessions = sessions.filter(session => 
    session.sessionId.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Handle search input change
  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
    setPage(0); // Reset to first page on search
  };

  // Handle pagination
  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  // Handle opening session details dialog
  const handleOpenDetails = (session: SessionData) => {
    setSelectedSession(session);
    setDetailsOpen(true);
  };

  // Handle closing session details dialog
  const handleCloseDetails = () => {
    setDetailsOpen(false);
  };

  // Handle delete session confirmation dialog
  const handleDeleteConfirmation = (sessionId: string) => {
    setSessionToDelete(sessionId);
    setDeleteDialogOpen(true);
  };

  // Handle actual deletion
  const handleDeleteSession = async () => {
    if (sessionToDelete) {
      deleteMutation.mutate(sessionToDelete);
      setDeleteDialogOpen(false);
      setSessionToDelete(null);
    }
  };

  // Cancel deletion
  const handleCancelDelete = () => {
    setDeleteDialogOpen(false);
    setSessionToDelete(null);
  };

  // Format date time
  const formatDateTime = (dateTimeString: string) => {
    try {
      return format(new Date(dateTimeString), 'MMM d, yyyy HH:mm:ss');
    } catch (error) {
      return 'Invalid date';
    }
  };

  // Format session data (which is stored as JSON string)
  const formatSessionData = (data: string) => {
    try {
      const parsedData = JSON.parse(data);
      return JSON.stringify(parsedData, null, 2);
    } catch (error) {
      return data;
    }
  };

  // Try to extract message count from session data
  const getMessageCount = (sessionData: string) => {
    try {
      const parsedData = JSON.parse(sessionData);
      if (parsedData.messages && Array.isArray(parsedData.messages)) {
        return parsedData.messages.length;
      }
      return '?';
    } catch (error) {
      return '?';
    }
  };

  // Calculate time since last activity
  const getTimeSinceLastActivity = (lastUpdatedAt: string) => {
    try {
      const lastUpdate = new Date(lastUpdatedAt);
      const now = new Date();
      const diffMs = now.getTime() - lastUpdate.getTime();
      
      // Convert to appropriate unit
      const diffSecs = Math.floor(diffMs / 1000);
      if (diffSecs < 60) return `${diffSecs} sec`;
      
      const diffMins = Math.floor(diffSecs / 60);
      if (diffMins < 60) return `${diffMins} min`;
      
      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) return `${diffHours} hr`;
      
      const diffDays = Math.floor(diffHours / 24);
      return `${diffDays} days`;
    } catch (error) {
      return 'Unknown';
    }
  };

  // Check if session is active or expired
  const isSessionActive = (expiresAt: string | null) => {
    if (!expiresAt) return false;
    
    try {
      const expirationDate = new Date(expiresAt);
      return expirationDate > new Date();
    } catch (error) {
      return false;
    }
  };

  return (
    <Box>
      <Card>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Chat Sessions</Typography>
            <TextField
              placeholder="Search by Session ID..."
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

          {isLoading ? (
            <Box sx={{ width: '100%', mb: 2 }}>
              <LinearProgress />
            </Box>
          ) : null}

          <TableContainer component={Paper} elevation={0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Session ID</TableCell>
                  <TableCell>Created</TableCell>
                  <TableCell>Last Activity</TableCell>
                  <TableCell>Messages</TableCell>
                  <TableCell>Expires</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredSessions.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      <Typography variant="body2" color="text.secondary" py={2}>
                        No sessions found
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredSessions
                    .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
                    .map((session) => (
                      <TableRow key={session.sessionId}>
                        <TableCell>
                          <Typography variant="body2" sx={{ 
                            maxWidth: 200, 
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap'
                          }}>
                            {session.sessionId}
                          </Typography>
                        </TableCell>
                        <TableCell>{formatDateTime(session.createdAt)}</TableCell>
                        <TableCell>
                          <Box display="flex" alignItems="center">
                            <ScheduleIcon fontSize="small" sx={{ mr: 0.5, opacity: 0.6 }} />
                            {getTimeSinceLastActivity(session.lastUpdatedAt)} ago
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Box display="flex" alignItems="center">
                            <ChatIcon fontSize="small" sx={{ mr: 0.5, opacity: 0.6 }} />
                            {getMessageCount(session.data)}
                          </Box>
                        </TableCell>
                        <TableCell>
                          {session.expiresAt ? formatDateTime(session.expiresAt) : 'Never'}
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={isSessionActive(session.expiresAt) ? "Active" : "Expired"}
                            color={isSessionActive(session.expiresAt) ? "success" : "default"}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <IconButton 
                            size="small" 
                            onClick={() => handleOpenDetails(session)}
                            title="View Details"
                          >
                            <VisibilityIcon fontSize="small" />
                          </IconButton>
                          <IconButton 
                            size="small" 
                            onClick={() => handleDeleteConfirmation(session.sessionId)}
                            title="Delete Session"
                            color="error"
                            sx={{ ml: 1 }}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))
                )}
              </TableBody>
              {filteredSessions.length > 0 && (
                <TableFooter>
                  <TableRow sx={{ 
                    backgroundColor: theme.palette.background.default,
                    '& .MuiTableCell-root': { 
                      fontWeight: 'bold',
                      borderTop: `1px solid ${theme.palette.divider}`
                    } 
                  }}>
                    <TableCell colSpan={7}>
                      Total: {filteredSessions.length} sessions
                    </TableCell>
                  </TableRow>
                </TableFooter>
              )}
            </Table>
          </TableContainer>

          <TablePagination
            rowsPerPageOptions={[5, 10, 25, 50]}
            component="div"
            count={filteredSessions.length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
          />
        </CardContent>
      </Card>

      {/* Session Details Dialog */}
      <Dialog 
        open={detailsOpen} 
        onClose={handleCloseDetails} 
        maxWidth="md" 
        fullWidth
      >
        <DialogTitle>
          Session Details
        </DialogTitle>
        <DialogContent>
          {selectedSession && (
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Grid item xs={12}>
                <Typography variant="subtitle1" fontWeight="bold">Session ID:</Typography>
                <Typography variant="body2">{selectedSession.sessionId}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle1" fontWeight="bold">Created:</Typography>
                <Typography variant="body2">{formatDateTime(selectedSession.createdAt)}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle1" fontWeight="bold">Last Activity:</Typography>
                <Typography variant="body2">
                  {formatDateTime(selectedSession.lastUpdatedAt)} 
                  ({getTimeSinceLastActivity(selectedSession.lastUpdatedAt)} ago)
                </Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle1" fontWeight="bold">Expires:</Typography>
                <Typography variant="body2">
                  {selectedSession.expiresAt ? formatDateTime(selectedSession.expiresAt) : 'Never'}
                </Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle1" fontWeight="bold">Status:</Typography>
                <Chip
                  size="small"
                  label={isSessionActive(selectedSession.expiresAt) ? "Active" : "Expired"}
                  color={isSessionActive(selectedSession.expiresAt) ? "success" : "default"}
                />
              </Grid>
              {selectedSession.userId && (
                <Grid item xs={12}>
                  <Typography variant="subtitle1" fontWeight="bold">User ID:</Typography>
                  <Typography variant="body2">{selectedSession.userId}</Typography>
                </Grid>
              )}
              <Grid item xs={12}>
                <Typography variant="subtitle1" fontWeight="bold">Session Data:</Typography>
                <Paper 
                  sx={{ 
                    p: 2, 
                    mt: 1, 
                    maxHeight: '300px', 
                    overflow: 'auto',
                    backgroundColor: theme.palette.background.default 
                  }} 
                  variant="outlined"
                >
                  <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                    {formatSessionData(selectedSession.data)}
                  </pre>
                </Paper>
              </Grid>
            </Grid>
          )}
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={() => handleDeleteConfirmation(selectedSession?.sessionId || '')}
            color="error"
            startIcon={<DeleteIcon />}
          >
            Delete
          </Button>
          <Button onClick={handleCloseDetails}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteDialogOpen}
        onClose={handleCancelDelete}
      >
        <DialogTitle>Confirm Session Deletion</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete this session? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCancelDelete}>Cancel</Button>
          <Button 
            onClick={handleDeleteSession} 
            color="error" 
            variant="contained"
            autoFocus
          >
            Delete Session
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SessionsView;