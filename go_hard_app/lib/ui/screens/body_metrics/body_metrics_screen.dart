import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../providers/body_metrics_provider.dart';
import '../../../data/models/body_metric.dart';

class BodyMetricsScreen extends StatefulWidget {
  const BodyMetricsScreen({super.key});

  @override
  State<BodyMetricsScreen> createState() => _BodyMetricsScreenState();
}

class _BodyMetricsScreenState extends State<BodyMetricsScreen> {
  @override
  void initState() {
    super.initState();

    // Load body metrics data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<BodyMetricsProvider>().loadBodyMetrics();
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<BodyMetricsProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Body Metrics'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showAddMetricDialog(context),
            tooltip: 'Log Metrics',
          ),
        ],
      ),
      body:
          provider.isLoading
              ? const Center(child: CircularProgressIndicator())
              : provider.errorMessage != null
              ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 64,
                      color: Colors.red,
                    ),
                    const SizedBox(height: 16),
                    Text(provider.errorMessage!),
                    const SizedBox(height: 16),
                    ElevatedButton.icon(
                      onPressed: () => provider.loadBodyMetrics(),
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              )
              : provider.metrics.isEmpty
              ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.fitness_center,
                      size: 64,
                      color: Colors.grey,
                    ),
                    const SizedBox(height: 16),
                    const Text(
                      'No body metrics logged',
                      style: TextStyle(fontSize: 18, color: Colors.grey),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Tap + to log your first measurement',
                      style: TextStyle(color: Colors.grey),
                    ),
                  ],
                ),
              )
              : Column(
                children: [
                  if (provider.latestMetric != null)
                    _buildLatestMetricCard(provider.latestMetric!),
                  Expanded(
                    child: RefreshIndicator(
                      onRefresh: () => provider.loadBodyMetrics(),
                      child: _buildMetricsList(provider.metrics),
                    ),
                  ),
                ],
              ),
    );
  }

  Widget _buildLatestMetricCard(BodyMetric metric) {
    final dateFormat = DateFormat('MMM d, y');

    return Card(
      margin: const EdgeInsets.all(16),
      color: Colors.blue[50],
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Latest Measurement',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                Text(
                  dateFormat.format(metric.recordedAt),
                  style: const TextStyle(color: Colors.grey),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Wrap(
              spacing: 16,
              runSpacing: 8,
              children: [
                if (metric.weight != null)
                  _buildMetricChip(
                    'Weight',
                    '${metric.weight!.toStringAsFixed(1)} kg',
                  ),
                if (metric.bodyFatPercentage != null)
                  _buildMetricChip(
                    'Body Fat',
                    '${metric.bodyFatPercentage!.toStringAsFixed(1)}%',
                  ),
                if (metric.chestCircumference != null)
                  _buildMetricChip(
                    'Chest',
                    '${metric.chestCircumference!.toStringAsFixed(1)} cm',
                  ),
                if (metric.waistCircumference != null)
                  _buildMetricChip(
                    'Waist',
                    '${metric.waistCircumference!.toStringAsFixed(1)} cm',
                  ),
                if (metric.armCircumference != null)
                  _buildMetricChip(
                    'Arm',
                    '${metric.armCircumference!.toStringAsFixed(1)} cm',
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMetricChip(String label, String value) {
    return Chip(
      label: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
          Text(
            value,
            style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildMetricsList(List<BodyMetric> metrics) {
    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: metrics.length,
      itemBuilder: (context, index) => _buildMetricCard(metrics[index]),
    );
  }

  Widget _buildMetricCard(BodyMetric metric) {
    final dateFormat = DateFormat('MMM d, y');

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: const CircleAvatar(child: Icon(Icons.fitness_center)),
        title: Text(dateFormat.format(metric.recordedAt)),
        subtitle: _buildMetricSummary(metric),
        trailing: PopupMenuButton<String>(
          itemBuilder:
              (context) => [
                const PopupMenuItem(
                  value: 'view',
                  child: Row(
                    children: [
                      Icon(Icons.visibility),
                      SizedBox(width: 8),
                      Text('View Details'),
                    ],
                  ),
                ),
                const PopupMenuItem(
                  value: 'delete',
                  child: Row(
                    children: [
                      Icon(Icons.delete_outline, color: Colors.red),
                      SizedBox(width: 8),
                      Text('Delete'),
                    ],
                  ),
                ),
              ],
          onSelected: (value) => _handleMetricAction(metric, value),
        ),
      ),
    );
  }

  Widget _buildMetricSummary(BodyMetric metric) {
    final parts = <String>[];

    if (metric.weight != null) {
      parts.add('${metric.weight!.toStringAsFixed(1)} kg');
    }
    if (metric.bodyFatPercentage != null) {
      parts.add('${metric.bodyFatPercentage!.toStringAsFixed(1)}% BF');
    }

    if (parts.isEmpty) {
      parts.add('No measurements');
    }

    return Text(parts.join(' • '));
  }

  void _handleMetricAction(BodyMetric metric, String action) async {
    final provider = context.read<BodyMetricsProvider>();

    switch (action) {
      case 'view':
        _showMetricDetailsDialog(metric);
        break;
      case 'delete':
        final confirmed = await _showConfirmDialog(
          'Delete Measurement',
          'Are you sure you want to delete this measurement?',
        );
        if (confirmed == true && mounted) {
          await provider.deleteBodyMetric(metric.id);
        }
        break;
    }
  }

  Future<bool?> _showConfirmDialog(String title, String message) {
    return showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text(title),
            content: Text(message),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Confirm'),
              ),
            ],
          ),
    );
  }

  void _showAddMetricDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => const AddBodyMetricDialog(),
    );
  }

  void _showMetricDetailsDialog(BodyMetric metric) {
    final dateFormat = DateFormat('MMM d, y h:mm a');

    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text('Measurement Details'),
            content: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    dateFormat.format(metric.recordedAt),
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 16),
                  if (metric.weight != null)
                    _buildDetailRow(
                      'Weight',
                      '${metric.weight!.toStringAsFixed(1)} kg',
                    ),
                  if (metric.bodyFatPercentage != null)
                    _buildDetailRow(
                      'Body Fat',
                      '${metric.bodyFatPercentage!.toStringAsFixed(1)}%',
                    ),
                  if (metric.chestCircumference != null)
                    _buildDetailRow(
                      'Chest',
                      '${metric.chestCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.waistCircumference != null)
                    _buildDetailRow(
                      'Waist',
                      '${metric.waistCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.hipCircumference != null)
                    _buildDetailRow(
                      'Hip',
                      '${metric.hipCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.armCircumference != null)
                    _buildDetailRow(
                      'Arm',
                      '${metric.armCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.thighCircumference != null)
                    _buildDetailRow(
                      'Thigh',
                      '${metric.thighCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.calfCircumference != null)
                    _buildDetailRow(
                      'Calf',
                      '${metric.calfCircumference!.toStringAsFixed(1)} cm',
                    ),
                  if (metric.notes != null && metric.notes!.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    const Text(
                      'Notes:',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 4),
                    Text(metric.notes!),
                  ],
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
            ],
          ),
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}

/// Dialog for adding a new body metric measurement
class AddBodyMetricDialog extends StatefulWidget {
  const AddBodyMetricDialog({super.key});

  @override
  State<AddBodyMetricDialog> createState() => _AddBodyMetricDialogState();
}

class _AddBodyMetricDialogState extends State<AddBodyMetricDialog> {
  final _formKey = GlobalKey<FormState>();
  final _weightController = TextEditingController();
  final _bodyFatController = TextEditingController();
  final _chestController = TextEditingController();
  final _waistController = TextEditingController();
  final _hipController = TextEditingController();
  final _armController = TextEditingController();
  final _thighController = TextEditingController();
  final _calfController = TextEditingController();
  final _notesController = TextEditingController();

  bool _isAdding = false;

  @override
  void dispose() {
    _weightController.dispose();
    _bodyFatController.dispose();
    _chestController.dispose();
    _waistController.dispose();
    _hipController.dispose();
    _armController.dispose();
    _thighController.dispose();
    _calfController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _addMetric() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    // Check if at least one measurement is provided
    if (_weightController.text.isEmpty &&
        _bodyFatController.text.isEmpty &&
        _chestController.text.isEmpty &&
        _waistController.text.isEmpty &&
        _hipController.text.isEmpty &&
        _armController.text.isEmpty &&
        _thighController.text.isEmpty &&
        _calfController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter at least one measurement'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    setState(() => _isAdding = true);

    final metric = BodyMetric(
      id: 0, // Will be assigned by backend
      userId: 0, // Will be assigned by backend
      recordedAt: DateTime.now(),
      weight:
          _weightController.text.isEmpty
              ? null
              : double.parse(_weightController.text),
      bodyFatPercentage:
          _bodyFatController.text.isEmpty
              ? null
              : double.parse(_bodyFatController.text),
      chestCircumference:
          _chestController.text.isEmpty
              ? null
              : double.parse(_chestController.text),
      waistCircumference:
          _waistController.text.isEmpty
              ? null
              : double.parse(_waistController.text),
      hipCircumference:
          _hipController.text.isEmpty
              ? null
              : double.parse(_hipController.text),
      armCircumference:
          _armController.text.isEmpty
              ? null
              : double.parse(_armController.text),
      thighCircumference:
          _thighController.text.isEmpty
              ? null
              : double.parse(_thighController.text),
      calfCircumference:
          _calfController.text.isEmpty
              ? null
              : double.parse(_calfController.text),
      notes:
          _notesController.text.trim().isEmpty
              ? null
              : _notesController.text.trim(),
      createdAt: DateTime.now(),
    );

    final provider = context.read<BodyMetricsProvider>();
    final success = await provider.createBodyMetric(metric);

    if (mounted) {
      setState(() => _isAdding = false);

      if (success) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Body metric added successfully')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(provider.errorMessage ?? 'Failed to add metric'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Log Body Measurement'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'Enter your measurements (optional fields)',
                style: TextStyle(fontSize: 12, color: Colors.grey),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _weightController,
                decoration: const InputDecoration(
                  labelText: 'Weight',
                  suffixText: 'kg',
                  prefixIcon: Icon(Icons.monitor_weight),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _bodyFatController,
                decoration: const InputDecoration(
                  labelText: 'Body Fat',
                  suffixText: '%',
                  prefixIcon: Icon(Icons.percent),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _chestController,
                decoration: const InputDecoration(
                  labelText: 'Chest',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.accessibility_new),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _waistController,
                decoration: const InputDecoration(
                  labelText: 'Waist',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.accessibility),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _hipController,
                decoration: const InputDecoration(
                  labelText: 'Hip',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.accessibility),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _armController,
                decoration: const InputDecoration(
                  labelText: 'Arm',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.fitness_center),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _thighController,
                decoration: const InputDecoration(
                  labelText: 'Thigh',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.accessibility),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _calfController,
                decoration: const InputDecoration(
                  labelText: 'Calf',
                  suffixText: 'cm',
                  prefixIcon: Icon(Icons.accessibility),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    if (double.tryParse(value) == null) {
                      return 'Please enter a valid number';
                    }
                  }
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _notesController,
                decoration: const InputDecoration(
                  labelText: 'Notes',
                  prefixIcon: Icon(Icons.note),
                  hintText: 'Add notes about this measurement...',
                ),
                maxLines: 2,
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isAdding ? null : () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: _isAdding ? null : _addMetric,
          child:
              _isAdding
                  ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                  : const Text('Save'),
        ),
      ],
    );
  }
}
