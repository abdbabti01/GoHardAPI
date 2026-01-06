import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/workout_template.dart';
import '../data/repositories/workout_template_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for workout templates management
class WorkoutTemplateProvider extends ChangeNotifier {
  final WorkoutTemplateRepository _repository;
  final ConnectivityService _connectivity;

  List<WorkoutTemplate> _templates = [];
  List<WorkoutTemplate> _communityTemplates = [];
  WorkoutTemplate? _selectedTemplate;
  bool _isLoading = false;
  String? _errorMessage;
  StreamSubscription<bool>? _connectivitySubscription;

  // Filter state
  String? _selectedCategory;
  bool _showActiveOnly = true;

  WorkoutTemplateProvider(this._repository, this._connectivity) {
    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline) {
        debugPrint('📡 Connection restored - refreshing templates');
        loadTemplates(showLoading: false);
      }
    });
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }

  // Getters
  List<WorkoutTemplate> get templates => _templates;
  List<WorkoutTemplate> get communityTemplates => _communityTemplates;
  List<WorkoutTemplate> get activeTemplates =>
      _templates.where((t) => t.isActive).toList();
  WorkoutTemplate? get selectedTemplate => _selectedTemplate;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  bool get isOnline => _connectivity.isOnline;
  String? get selectedCategory => _selectedCategory;
  bool get showActiveOnly => _showActiveOnly;

  /// Load all templates for current user
  Future<void> loadTemplates({bool showLoading = true}) async {
    if (_isLoading) return;

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final templateList = await _repository.getTemplates(
        activeOnly: _showActiveOnly,
      );
      _templates = templateList;
      debugPrint('✅ Loaded ${_templates.length} templates');
    } catch (e) {
      _errorMessage =
          'Failed to load templates: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Load templates error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      notifyListeners();
    }
  }

  /// Load community templates
  Future<void> loadCommunityTemplates({bool showLoading = true}) async {
    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final templates = await _repository.getCommunityTemplates(
        category: _selectedCategory,
        limit: 50,
      );
      _communityTemplates = templates;
      debugPrint('✅ Loaded ${_communityTemplates.length} community templates');
    } catch (e) {
      _errorMessage =
          'Failed to load community templates: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Load community templates error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      notifyListeners();
    }
  }

  /// Get a specific template by ID
  Future<void> loadTemplateById(int id) async {
    try {
      final template = await _repository.getTemplateById(id);
      _selectedTemplate = template;
      notifyListeners();
    } catch (e) {
      debugPrint('❌ Load template by ID error: $e');
    }
  }

  /// Create a new template
  Future<WorkoutTemplate?> createTemplate({
    required String name,
    String? description,
    required String exercisesJson,
    required String recurrencePattern,
    String? daysOfWeek,
    int? intervalDays,
    int? estimatedDuration,
    String? category,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final template = await _repository.createTemplate(
        name: name,
        description: description,
        exercisesJson: exercisesJson,
        recurrencePattern: recurrencePattern,
        daysOfWeek: daysOfWeek,
        intervalDays: intervalDays,
        estimatedDuration: estimatedDuration,
        category: category,
      );

      // Add to local list
      _templates.insert(0, template);
      _selectedTemplate = template;

      debugPrint('✅ Created template: ${template.name}');
      _isLoading = false;
      notifyListeners();
      return template;
    } catch (e) {
      _errorMessage =
          'Failed to create template: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Create template error: $e');
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  /// Update an existing template
  Future<bool> updateTemplate(WorkoutTemplate template) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final updated = await _repository.updateTemplate(template);

      // Update in local list
      final index = _templates.indexWhere((t) => t.id == updated.id);
      if (index != -1) {
        _templates[index] = updated;
      }

      if (_selectedTemplate?.id == updated.id) {
        _selectedTemplate = updated;
      }

      debugPrint('✅ Updated template: ${updated.name}');
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to update template: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Update template error: $e');
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Toggle active status of a template
  Future<void> toggleActive(int id) async {
    try {
      await _repository.toggleActive(id);

      // Update local list
      final template = _templates.firstWhere((t) => t.id == id);
      template.isActive = !template.isActive;

      if (_selectedTemplate?.id == id) {
        _selectedTemplate = template;
      }

      notifyListeners();
      debugPrint('✅ Toggled active status for template $id');
    } catch (e) {
      _errorMessage =
          'Failed to toggle active status: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Toggle active error: $e');
      notifyListeners();
    }
  }

  /// Delete a template
  Future<bool> deleteTemplate(int id) async {
    try {
      await _repository.deleteTemplate(id);

      // Remove from local lists
      _templates.removeWhere((t) => t.id == id);
      _communityTemplates.removeWhere((t) => t.id == id);

      if (_selectedTemplate?.id == id) {
        _selectedTemplate = null;
      }

      notifyListeners();
      debugPrint('✅ Deleted template $id');
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to delete template: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Delete template error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Increment usage count for a template
  Future<void> incrementUsageCount(int id) async {
    try {
      await _repository.incrementUsageCount(id);

      // Update local list
      final template = _findTemplateById(id);
      if (template != null) {
        template.usageCount++;
        notifyListeners();
      }

      debugPrint('✅ Incremented usage count for template $id');
    } catch (e) {
      debugPrint('❌ Increment usage count error: $e');
    }
  }

  /// Rate a community template
  Future<bool> rateTemplate(int id, double rating) async {
    if (!_connectivity.isOnline) {
      _errorMessage = 'Cannot rate template while offline';
      notifyListeners();
      return false;
    }

    try {
      await _repository.rateTemplate(id, rating);

      // Reload the template to get updated rating
      await loadTemplateById(id);

      debugPrint('✅ Rated template $id with $rating stars');
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to rate template: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Rate template error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Get templates scheduled for a specific date
  Future<List<WorkoutTemplate>> getTemplatesForDate(DateTime date) async {
    try {
      return await _repository.getTemplatesForDate(date);
    } catch (e) {
      debugPrint('❌ Get templates for date error: $e');
      return [];
    }
  }

  /// Get templates scheduled for today
  Future<List<WorkoutTemplate>> getTodayTemplates() async {
    return await getTemplatesForDate(DateTime.now());
  }

  /// Set category filter
  void setCategory(String? category) {
    if (_selectedCategory != category) {
      _selectedCategory = category;
      loadCommunityTemplates();
    }
  }

  /// Toggle active-only filter
  void toggleActiveOnly() {
    _showActiveOnly = !_showActiveOnly;
    loadTemplates();
  }

  /// Clear all filters
  void clearFilters() {
    _selectedCategory = null;
    loadCommunityTemplates();
  }

  /// Set selected template
  void selectTemplate(WorkoutTemplate? template) {
    _selectedTemplate = template;
    notifyListeners();
  }

  /// Refresh all data
  Future<void> refresh() async {
    await loadTemplates();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  // === PRIVATE HELPERS ===

  /// Find a template by ID across all lists
  WorkoutTemplate? _findTemplateById(int id) {
    try {
      return _templates.firstWhere((t) => t.id == id);
    } catch (e) {
      try {
        return _communityTemplates.firstWhere((t) => t.id == id);
      } catch (e) {
        return null;
      }
    }
  }
}
