import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/services/debug_logger.dart';

/// Debug screen to view notification and app logs
/// Useful for debugging on devices where console access is not available
class DebugLogsScreen extends StatefulWidget {
  const DebugLogsScreen({super.key});

  @override
  State<DebugLogsScreen> createState() => _DebugLogsScreenState();
}

class _DebugLogsScreenState extends State<DebugLogsScreen> {
  final DebugLogger _logger = DebugLogger();
  final ScrollController _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      _scrollController.animateTo(
        _scrollController.position.maxScrollExtent,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOut,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Debug Logs'),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.copy),
            onPressed: () {
              Clipboard.setData(ClipboardData(text: _logger.logsAsString));
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Logs copied to clipboard'),
                  duration: Duration(seconds: 2),
                ),
              );
            },
            tooltip: 'Copy all logs',
          ),
          IconButton(
            icon: const Icon(Icons.delete),
            onPressed: () {
              setState(() {
                _logger.clear();
              });
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Logs cleared'),
                  duration: Duration(seconds: 2),
                ),
              );
            },
            tooltip: 'Clear logs',
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              setState(() {});
              Future.delayed(
                const Duration(milliseconds: 100),
                _scrollToBottom,
              );
            },
            tooltip: 'Refresh',
          ),
        ],
      ),
      body:
          _logger.logs.isEmpty
              ? const Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.info_outline, size: 64, color: Colors.grey),
                    SizedBox(height: 16),
                    Text(
                      'No logs yet',
                      style: TextStyle(fontSize: 18, color: Colors.grey),
                    ),
                    SizedBox(height: 8),
                    Text(
                      'Logs will appear here as the app runs',
                      style: TextStyle(color: Colors.grey),
                    ),
                  ],
                ),
              )
              : Column(
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    color: Theme.of(
                      context,
                    ).primaryColor.withValues(alpha: 0.1),
                    child: Row(
                      children: [
                        const Icon(Icons.info_outline, size: 20),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            '${_logger.logs.length} log entries',
                            style: const TextStyle(fontWeight: FontWeight.w500),
                          ),
                        ),
                        TextButton.icon(
                          onPressed: _scrollToBottom,
                          icon: const Icon(Icons.arrow_downward, size: 18),
                          label: const Text('Scroll to bottom'),
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: ListView.separated(
                      controller: _scrollController,
                      padding: const EdgeInsets.all(8),
                      itemCount: _logger.logs.length,
                      separatorBuilder:
                          (context, index) => const Divider(height: 1),
                      itemBuilder: (context, index) {
                        final log = _logger.logs[index];
                        final isError = log.contains('❌') || log.contains('⚠️');
                        final isSuccess = log.contains('✅');
                        final isImportant =
                            log.contains('🍎') || log.contains('🤖');

                        Color? backgroundColor;
                        if (isError) {
                          backgroundColor = Colors.red.withValues(alpha: 0.1);
                        } else if (isSuccess) {
                          backgroundColor = Colors.green.withValues(alpha: 0.1);
                        } else if (isImportant) {
                          backgroundColor = Colors.blue.withValues(alpha: 0.1);
                        }

                        return Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 8,
                          ),
                          color: backgroundColor,
                          child: SelectableText(
                            log,
                            style: TextStyle(
                              fontFamily: 'monospace',
                              fontSize: 12,
                              color: isError ? Colors.red[700] : null,
                              fontWeight:
                                  (isError || isSuccess)
                                      ? FontWeight.w600
                                      : null,
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
      floatingActionButton:
          _logger.logs.isNotEmpty
              ? FloatingActionButton(
                onPressed: _scrollToBottom,
                tooltip: 'Scroll to bottom',
                child: const Icon(Icons.arrow_downward),
              )
              : null,
    );
  }
}
