import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Grid,
  Divider,
  Chip,
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';

export default function RunDetailsDialog({ open, runDetails, onClose }) {
  if (!runDetails) {
    return null;
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth>
      <DialogTitle>
        Run Details #{runDetails.runId}
      </DialogTitle>
      <DialogContent>
        <Box>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle1">Configuration: {runDetails.configurationName}</Typography>
              <Typography variant="subtitle1">
                Status: <Chip 
                  label={runDetails.status} 
                  color={
                    runDetails.status === 'Completed' ? 'success' :
                    runDetails.status === 'Failed' ? 'error' :
                    runDetails.status === 'Running' ? 'info' :
                    runDetails.status === 'CompletedWithErrors' ? 'warning' :
                    'default'
                  } 
                  size="small" 
                />
              </Typography>
              <Typography variant="subtitle1">
                Started: {new Date(runDetails.startTime).toLocaleString()}
              </Typography>
              <Typography variant="subtitle1">
                Ended: {runDetails.endTime ? new Date(runDetails.endTime).toLocaleString() : 'Running...'}
              </Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle1">
                Duration: {runDetails.elapsedMs ? `${(runDetails.elapsedMs / 1000).toFixed(2)} seconds` : 'Running...'}
              </Typography>
              <Typography variant="subtitle1">
                Tables Processed: {runDetails.totalTablesProcessed} 
                {runDetails.failedTablesCount > 0 ? ` (${runDetails.failedTablesCount} failed)` : ''}
              </Typography>
              <Typography variant="subtitle1">
                Rows Processed: {runDetails.totalRowsProcessed.toLocaleString()}
              </Typography>
              <Typography variant="subtitle1">
                Avg. Speed: {runDetails.averageRowsPerSecond ? `${runDetails.averageRowsPerSecond.toFixed(2)} rows/sec` : 'N/A'}
              </Typography>
            </Grid>
          </Grid>
          
          <Divider sx={{ mb: 2 }} />
          
          <Typography variant="h6" sx={{ mb: 2 }}>Table Metrics</Typography>
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Schema</TableCell>
                  <TableCell>Table</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Rows</TableCell>
                  <TableCell>Duration</TableCell>
                  <TableCell>Speed</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {runDetails.tableMetrics && runDetails.tableMetrics.map((metric) => (
                  <TableRow key={metric.metricId}>
                    <TableCell>{metric.schemaName}</TableCell>
                    <TableCell>{metric.tableName}</TableCell>
                    <TableCell>
                      <Chip 
                        label={metric.status} 
                        color={
                          metric.status === 'Completed' ? 'success' :
                          metric.status === 'Failed' ? 'error' :
                          metric.status === 'Running' ? 'info' :
                          'default'
                        } 
                        size="small" 
                      />
                    </TableCell>
                    <TableCell>{metric.rowsProcessed.toLocaleString()} / {metric.totalRowsToProcess.toLocaleString()}</TableCell>
                    <TableCell>
                      {metric.elapsedMs ? `${(metric.elapsedMs / 1000).toFixed(2)} seconds` : 'Running...'}
                    </TableCell>
                    <TableCell>
                      {metric.rowsPerSecond ? `${metric.rowsPerSecond.toFixed(2)} rows/sec` : 'N/A'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          
          {runDetails.logs && runDetails.logs.length > 0 && (
            <>
              <Typography variant="h6" sx={{ mt: 3, mb: 2 }}>Logs</Typography>
              <Paper sx={{ maxHeight: 300, overflow: 'auto' }}>
                <List dense>
                  {runDetails.logs.map((log) => (
                    <ListItem key={log.logId}>
                      <ListItemIcon>
                        <Chip 
                          label={log.logLevel} 
                          color={
                            log.logLevel === 'Error' ? 'error' :
                            log.logLevel === 'Warning' ? 'warning' :
                            log.logLevel === 'Information' ? 'info' :
                            'default'
                          } 
                          size="small" 
                        />
                      </ListItemIcon>
                      <ListItemText 
                        primary={log.message} 
                        secondary={new Date(log.logTime).toLocaleString()}
                      />
                    </ListItem>
                  ))}
                </List>
              </Paper>
            </>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}