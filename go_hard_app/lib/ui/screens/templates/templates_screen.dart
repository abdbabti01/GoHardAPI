import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/workout_template_provider.dart';
import '../../../data/models/workout_template.dart';
import '../../widgets/common/offline_banner.dart';
import 'template_form_dialog.dart';

/// Screen for managing workout templates
class TemplatesScreen extends StatefulWidget {
  const TemplatesScreen({super.key});

  @override
  State<TemplatesScreen> createState() => _TemplatesScreenState();
}

class _TemplatesScreenState extends State<TemplatesScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);

    // Load data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<WorkoutTemplateProvider>();
      provider.loadTemplates();
      provider.loadCommunityTemplates();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkoutTemplateProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Workout Templates'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => provider.refresh(),
            tooltip: 'Refresh',
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'My Templates', icon: Icon(Icons.list)),
            Tab(text: 'Community', icon: Icon(Icons.people)),
          ],
        ),
      ),
      body: Column(
        children: [
          if (!provider.isOnline) const OfflineBanner(),
          Expanded(
            child:
                provider.isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : provider.errorMessage != null
                    ? _buildErrorView(provider)
                    : TabBarView(
                      controller: _tabController,
                      children: [
                        _buildMyTemplatesTab(provider),
                        _buildCommunityTab(provider),
                      ],
                    ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _createNewTemplate,
        icon: const Icon(Icons.add),
        label: const Text('Create Template'),
      ),
    );
  }

  Widget _buildErrorView(WorkoutTemplateProvider provider) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.error_outline, size: 64, color: Colors.red),
          const SizedBox(height: 16),
          Text(provider.errorMessage!),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            onPressed: () => provider.refresh(),
            icon: const Icon(Icons.refresh),
            label: const Text('Retry'),
          ),
        ],
      ),
    );
  }

  Widget _buildMyTemplatesTab(WorkoutTemplateProvider provider) {
    final templates = provider.templates;

    if (templates.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.fitness_center, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'No templates yet',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Create your first workout template!',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => provider.loadTemplates(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: templates.length,
        itemBuilder: (context, index) {
          return _buildTemplateCard(
            context,
            templates[index],
            provider,
            isMyTemplate: true,
          );
        },
      ),
    );
  }

  Widget _buildCommunityTab(WorkoutTemplateProvider provider) {
    final templates = provider.communityTemplates;

    if (templates.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.people_outline, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'No community templates',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Check back later for shared templates',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => provider.loadCommunityTemplates(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: templates.length,
        itemBuilder: (context, index) {
          return _buildTemplateCard(context, templates[index], provider);
        },
      ),
    );
  }

  Widget _buildTemplateCard(
    BuildContext context,
    WorkoutTemplate template,
    WorkoutTemplateProvider provider, {
    bool isMyTemplate = false,
  }) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: () => _showTemplateDetails(template),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          template.name,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        if (template.description != null) ...[
                          const SizedBox(height: 4),
                          Text(
                            template.description!,
                            style: TextStyle(color: Colors.grey[700]),
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ],
                      ],
                    ),
                  ),
                  if (isMyTemplate)
                    Switch(
                      value: template.isActive,
                      onChanged: (_) => provider.toggleActive(template.id),
                    ),
                ],
              ),
              const SizedBox(height: 12),

              // Schedule info
              Row(
                children: [
                  Icon(Icons.schedule, size: 16, color: Colors.grey[600]),
                  const SizedBox(width: 4),
                  Text(
                    template.recurrenceDisplay,
                    style: TextStyle(color: Colors.grey[700]),
                  ),
                  const SizedBox(width: 16),
                  if (template.estimatedDuration != null) ...[
                    Icon(Icons.timer, size: 16, color: Colors.grey[600]),
                    const SizedBox(width: 4),
                    Text(
                      '${template.estimatedDuration} min',
                      style: TextStyle(color: Colors.grey[700]),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: 8),

              // Tags
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  if (template.category != null)
                    Chip(
                      label: Text(template.category!),
                      visualDensity: VisualDensity.compact,
                    ),
                  if (!isMyTemplate && template.rating != null)
                    Chip(
                      label: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.star, size: 14, color: Colors.amber),
                          const SizedBox(width: 4),
                          Text(template.rating!.toStringAsFixed(1)),
                        ],
                      ),
                      visualDensity: VisualDensity.compact,
                    ),
                  Chip(
                    label: Text('Used ${template.usageCount}x'),
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Actions
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  if (isMyTemplate) ...[
                    TextButton.icon(
                      onPressed: () => _editTemplate(template),
                      icon: const Icon(Icons.edit, size: 18),
                      label: const Text('Edit'),
                    ),
                    const SizedBox(width: 8),
                    TextButton.icon(
                      onPressed: () => _confirmDelete(template, provider),
                      icon: const Icon(Icons.delete_outline, size: 18),
                      label: const Text('Delete'),
                      style: TextButton.styleFrom(foregroundColor: Colors.red),
                    ),
                  ] else ...[
                    if (provider.isOnline)
                      TextButton.icon(
                        onPressed: () => _rateTemplate(template, provider),
                        icon: const Icon(Icons.star_outline, size: 18),
                        label: const Text('Rate'),
                      ),
                  ],
                  const SizedBox(width: 8),
                  ElevatedButton.icon(
                    onPressed: () {
                      provider.incrementUsageCount(template.id);
                      _useTemplate(template);
                    },
                    icon: const Icon(Icons.play_arrow, size: 18),
                    label: const Text('Use'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showTemplateDetails(WorkoutTemplate template) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text(template.name),
            content: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (template.description != null) ...[
                    Text(template.description!),
                    const SizedBox(height: 16),
                  ],
                  Text('Schedule: ${template.recurrenceDisplay}'),
                  if (template.estimatedDuration != null)
                    Text('Duration: ${template.estimatedDuration} minutes'),
                  if (template.category != null)
                    Text('Category: ${template.category}'),
                  Text('Used: ${template.usageCount} times'),
                  const SizedBox(height: 16),
                  const Text(
                    'Exercises:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    template.exercisesJson,
                  ), // TODO: Parse and display properly
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(context);
                  _useTemplate(template);
                },
                child: const Text('Use Template'),
              ),
            ],
          ),
    );
  }

  void _createNewTemplate() {
    showDialog(
      context: context,
      builder: (context) => const TemplateFormDialog(),
    );
  }

  void _editTemplate(WorkoutTemplate template) {
    showDialog(
      context: context,
      builder: (context) => TemplateFormDialog(template: template),
    );
  }

  void _useTemplate(WorkoutTemplate template) {
    // Navigate to create a new session with template data
    // For now, show a confirmation dialog
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Use Template'),
            content: Text(
              'Create a new workout session from "${template.name}"?\n\n'
              'This will start a new workout with the exercises from this template.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(context);
                  _createSessionFromTemplate(template);
                },
                child: const Text('Start Workout'),
              ),
            ],
          ),
    );
  }

  Future<void> _createSessionFromTemplate(WorkoutTemplate template) async {
    final messenger = ScaffoldMessenger.of(context);

    // TODO: Integrate with SessionsProvider to create actual session
    // For now, show success message
    messenger.showSnackBar(
      SnackBar(
        content: Text(
          'Workout session created from "${template.name}"!\n'
          'Navigate to Active Workout to start.',
        ),
        duration: const Duration(seconds: 3),
        action: SnackBarAction(
          label: 'GO',
          onPressed: () {
            // Navigate to active workout screen
            Navigator.pushNamed(context, '/active-workout');
          },
        ),
      ),
    );
  }

  void _confirmDelete(
    WorkoutTemplate template,
    WorkoutTemplateProvider provider,
  ) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Delete Template'),
            content: Text(
              'Are you sure you want to delete "${template.name}"?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () async {
                  Navigator.pop(context);
                  final messenger = ScaffoldMessenger.of(context);
                  final success = await provider.deleteTemplate(template.id);
                  if (success && mounted) {
                    messenger.showSnackBar(
                      const SnackBar(content: Text('Template deleted')),
                    );
                  }
                },
                style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                child: const Text('Delete'),
              ),
            ],
          ),
    );
  }

  void _rateTemplate(
    WorkoutTemplate template,
    WorkoutTemplateProvider provider,
  ) {
    double rating = 3.0;

    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text('Rate ${template.name}'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text('How would you rate this template?'),
                const SizedBox(height: 16),
                StatefulBuilder(
                  builder: (context, setState) {
                    return Column(
                      children: [
                        Slider(
                          value: rating,
                          min: 1,
                          max: 5,
                          divisions: 4,
                          label: rating.toString(),
                          onChanged: (value) => setState(() => rating = value),
                        ),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: List.generate(
                            5,
                            (index) => Icon(
                              index < rating ? Icons.star : Icons.star_border,
                              color: Colors.amber,
                              size: 32,
                            ),
                          ),
                        ),
                      ],
                    );
                  },
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () async {
                  Navigator.pop(context);
                  final messenger = ScaffoldMessenger.of(context);
                  final success = await provider.rateTemplate(
                    template.id,
                    rating,
                  );
                  if (success && mounted) {
                    messenger.showSnackBar(
                      const SnackBar(content: Text('Rating submitted')),
                    );
                  }
                },
                child: const Text('Submit'),
              ),
            ],
          ),
    );
  }
}
