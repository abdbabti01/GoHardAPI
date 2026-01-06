import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import '../../../providers/profile_provider.dart';
import '../../../data/models/profile_update_request.dart';
import '../../../core/enums/profile_enums.dart';
import '../../../core/utils/unit_converter.dart';
import '../../../core/constants/api_config.dart';

/// Edit profile screen for updating user information
class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _imagePicker = ImagePicker();

  // Controllers
  late TextEditingController _nameController;
  late TextEditingController _bioController;
  late TextEditingController _heightController;
  late TextEditingController _favoriteExercisesController;

  // Form values
  DateTime? _dateOfBirth;
  Gender? _gender;
  ExperienceLevel? _experienceLevel;
  FitnessGoal? _primaryGoal;
  UnitPreference _unitPreference = UnitPreference.metric;
  String? _themePreference;
  File? _selectedImage;

  @override
  void initState() {
    super.initState();
    final provider = context.read<ProfileProvider>();
    final user = provider.currentUser;

    // Initialize unit preference
    _unitPreference = UnitPreference.fromString(user?.unitPreference);

    // Initialize controllers with current values (converted to display units)
    _nameController = TextEditingController(text: user?.name ?? '');
    _bioController = TextEditingController(text: user?.bio ?? '');

    // Convert height from metric (backend storage) to display units
    final displayHeight = UnitConverter.convertMetricHeightToDisplay(
      user?.height,
      _unitPreference.serverValue,
    );

    _heightController = TextEditingController(
      text: displayHeight != null ? displayHeight.toStringAsFixed(1) : '',
    );
    _favoriteExercisesController = TextEditingController(
      text: user?.favoriteExercises ?? '',
    );

    // Initialize other values
    _dateOfBirth = user?.dateOfBirth;
    _gender = Gender.fromString(user?.gender);
    _experienceLevel = ExperienceLevel.fromString(user?.experienceLevel);
    _primaryGoal = FitnessGoal.fromString(user?.primaryGoal);
    _themePreference = user?.themePreference;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _bioController.dispose();
    _heightController.dispose();
    _favoriteExercisesController.dispose();
    super.dispose();
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final XFile? image = await _imagePicker.pickImage(
        source: source,
        maxWidth: 800,
        maxHeight: 800,
        imageQuality: 85,
      );

      if (image != null) {
        setState(() {
          _selectedImage = File(image.path);
        });

        // Upload immediately
        if (!mounted) return;
        final provider = context.read<ProfileProvider>();
        final scaffoldMessenger = ScaffoldMessenger.of(context);

        final success = await provider.uploadProfilePhoto(_selectedImage!);

        if (!mounted) return;
        if (success) {
          scaffoldMessenger.showSnackBar(
            const SnackBar(content: Text('Profile photo updated successfully')),
          );
        } else {
          scaffoldMessenger.showSnackBar(
            SnackBar(
              content: Text(provider.errorMessage ?? 'Failed to upload photo'),
              backgroundColor: Colors.red,
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        final scaffoldMessenger = ScaffoldMessenger.of(context);
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text('Error picking image: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  void _showImagePickerOptions() {
    showModalBottomSheet(
      context: context,
      builder:
          (context) => SafeArea(
            child: Wrap(
              children: [
                ListTile(
                  leading: const Icon(Icons.photo_library),
                  title: const Text('Choose from Gallery'),
                  onTap: () {
                    Navigator.pop(context);
                    _pickImage(ImageSource.gallery);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.camera_alt),
                  title: const Text('Take a Photo'),
                  onTap: () {
                    Navigator.pop(context);
                    _pickImage(ImageSource.camera);
                  },
                ),
              ],
            ),
          ),
    );
  }

  Future<void> _selectDateOfBirth() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _dateOfBirth ?? DateTime(2000),
      firstDate: DateTime(1900),
      lastDate: DateTime.now(),
      helpText: 'Select Date of Birth',
    );

    if (picked != null) {
      setState(() {
        _dateOfBirth = picked;
      });
    }
  }

  void _toggleUnitPreference() {
    setState(() {
      final currentHeight = double.tryParse(_heightController.text);

      // Convert values when toggling
      if (_unitPreference == UnitPreference.metric) {
        // Convert metric to imperial
        _unitPreference = UnitPreference.imperial;
        if (currentHeight != null) {
          _heightController.text = UnitConverter.cmToInches(
            currentHeight,
          ).toStringAsFixed(1);
        }
      } else {
        // Convert imperial to metric
        _unitPreference = UnitPreference.metric;
        if (currentHeight != null) {
          _heightController.text = UnitConverter.inchesToCm(
            currentHeight,
          ).toStringAsFixed(1);
        }
      }
    });
  }

  Future<void> _saveProfile() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    // Convert display values to metric for backend storage
    final height = double.tryParse(_heightController.text);

    final request = ProfileUpdateRequest(
      name: _nameController.text.trim(),
      bio:
          _bioController.text.trim().isEmpty
              ? null
              : _bioController.text.trim(),
      dateOfBirth: _dateOfBirth,
      gender: _gender?.serverValue,
      height: UnitConverter.convertInputHeightToMetric(
        height,
        _unitPreference.serverValue,
      ),
      experienceLevel: _experienceLevel?.serverValue,
      primaryGoal: _primaryGoal?.serverValue,
      unitPreference: _unitPreference.serverValue,
      themePreference: _themePreference,
      favoriteExercises:
          _favoriteExercisesController.text.trim().isEmpty
              ? null
              : _favoriteExercisesController.text.trim(),
    );

    final provider = context.read<ProfileProvider>();
    final scaffoldMessenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    final success = await provider.updateProfile(request);

    if (success && mounted) {
      scaffoldMessenger.showSnackBar(
        const SnackBar(content: Text('Profile updated successfully')),
      );
      navigator.pop();
    } else if (!success && mounted) {
      scaffoldMessenger.showSnackBar(
        SnackBar(
          content: Text(provider.errorMessage ?? 'Failed to update profile'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<ProfileProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Edit Profile'),
        actions: [
          if (provider.isUpdating)
            const Center(
              child: Padding(
                padding: EdgeInsets.only(right: 16),
                child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
            )
          else
            TextButton(
              onPressed: _saveProfile,
              child: const Text('Save', style: TextStyle(fontSize: 16)),
            ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Photo section
              _buildPhotoSection(provider),
              const SizedBox(height: 24),

              // Personal Details
              _buildSectionHeader('Personal Details'),
              const SizedBox(height: 16),
              _buildPersonalDetailsCard(),
              const SizedBox(height: 24),

              // Height & Units
              _buildSectionHeader('Height & Units'),
              const SizedBox(height: 16),
              _buildBodyMetricsCard(),
              const SizedBox(height: 24),

              // Fitness Profile
              _buildSectionHeader('Fitness Profile'),
              const SizedBox(height: 16),
              _buildFitnessProfileCard(),
              const SizedBox(height: 24),

              // Preferences
              _buildSectionHeader('Preferences'),
              const SizedBox(height: 16),
              _buildPreferencesCard(),
              const SizedBox(height: 24),

              // Bio & Social
              _buildSectionHeader('Bio & Social'),
              const SizedBox(height: 16),
              _buildBioCard(),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPhotoSection(ProfileProvider provider) {
    return Center(
      child: Stack(
        children: [
          CircleAvatar(
            radius: 60,
            backgroundColor: Theme.of(
              context,
            ).colorScheme.primary.withValues(alpha: 0.2),
            backgroundImage:
                _selectedImage != null
                    ? FileImage(_selectedImage!)
                    : provider.currentUser?.profilePhotoUrl != null
                    ? NetworkImage(
                      ApiConfig.getPhotoUrl(
                        provider.currentUser!.profilePhotoUrl!,
                      ),
                    )
                    : null,
            child:
                _selectedImage == null &&
                        provider.currentUser?.profilePhotoUrl == null
                    ? Icon(
                      Icons.person,
                      size: 60,
                      color: Theme.of(context).colorScheme.primary,
                    )
                    : null,
          ),
          Positioned(
            bottom: 0,
            right: 0,
            child: CircleAvatar(
              backgroundColor: Theme.of(context).colorScheme.primary,
              radius: 20,
              child: IconButton(
                icon: const Icon(Icons.camera_alt, size: 20),
                color: Colors.white,
                onPressed:
                    provider.isUploadingPhoto ? null : _showImagePickerOptions,
              ),
            ),
          ),
          if (provider.isUploadingPhoto)
            Positioned.fill(
              child: CircleAvatar(
                radius: 60,
                backgroundColor: Colors.black54,
                child: const CircularProgressIndicator(color: Colors.white),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildSectionHeader(String title) {
    return Text(
      title,
      style: Theme.of(
        context,
      ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
    );
  }

  Widget _buildPersonalDetailsCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            TextFormField(
              controller: _nameController,
              decoration: const InputDecoration(
                labelText: 'Name',
                prefixIcon: Icon(Icons.person),
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return 'Name is required';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            InkWell(
              onTap: _selectDateOfBirth,
              child: InputDecorator(
                decoration: const InputDecoration(
                  labelText: 'Date of Birth',
                  prefixIcon: Icon(Icons.cake),
                ),
                child: Text(
                  _dateOfBirth != null
                      ? '${_dateOfBirth!.day}/${_dateOfBirth!.month}/${_dateOfBirth!.year}'
                      : 'Select date',
                  style: TextStyle(
                    color: _dateOfBirth != null ? null : Colors.grey,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<Gender>(
              value: _gender,
              decoration: const InputDecoration(
                labelText: 'Gender',
                prefixIcon: Icon(Icons.wc),
              ),
              items:
                  Gender.values
                      .map(
                        (gender) => DropdownMenuItem(
                          value: gender,
                          child: Text(gender.displayName),
                        ),
                      )
                      .toList(),
              onChanged: (value) => setState(() => _gender = value),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBodyMetricsCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            // Unit toggle
            SwitchListTile(
              title: Text('Units: ${_unitPreference.displayName}'),
              subtitle: const Text('Toggle between Metric and Imperial'),
              value: _unitPreference == UnitPreference.imperial,
              onChanged: (value) => _toggleUnitPreference(),
            ),
            const Divider(),
            const SizedBox(height: 8),
            TextFormField(
              controller: _heightController,
              decoration: InputDecoration(
                labelText: 'Height',
                prefixIcon: const Icon(Icons.height),
                suffixText: UnitConverter.getHeightUnit(
                  _unitPreference.serverValue,
                ),
              ),
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFitnessProfileCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            DropdownButtonFormField<ExperienceLevel>(
              value: _experienceLevel,
              decoration: const InputDecoration(
                labelText: 'Experience Level',
                prefixIcon: Icon(Icons.star),
              ),
              items:
                  ExperienceLevel.values
                      .map(
                        (level) => DropdownMenuItem(
                          value: level,
                          child: Text(level.displayName),
                        ),
                      )
                      .toList(),
              onChanged: (value) => setState(() => _experienceLevel = value),
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<FitnessGoal>(
              value: _primaryGoal,
              decoration: const InputDecoration(
                labelText: 'Primary Goal',
                prefixIcon: Icon(Icons.track_changes),
              ),
              items:
                  FitnessGoal.values
                      .map(
                        (goal) => DropdownMenuItem(
                          value: goal,
                          child: Text(goal.displayName),
                        ),
                      )
                      .toList(),
              onChanged: (value) => setState(() => _primaryGoal = value),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPreferencesCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            DropdownButtonFormField<String>(
              value: _themePreference,
              decoration: const InputDecoration(
                labelText: 'Theme',
                prefixIcon: Icon(Icons.palette),
              ),
              items: const [
                DropdownMenuItem(value: 'Light', child: Text('Light')),
                DropdownMenuItem(value: 'Dark', child: Text('Dark')),
                DropdownMenuItem(value: 'System', child: Text('System')),
              ],
              onChanged: (value) => setState(() => _themePreference = value),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBioCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            TextFormField(
              controller: _bioController,
              decoration: const InputDecoration(
                labelText: 'Bio',
                prefixIcon: Icon(Icons.info),
                hintText: 'Tell us about yourself...',
              ),
              maxLines: 4,
              maxLength: 500,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _favoriteExercisesController,
              decoration: const InputDecoration(
                labelText: 'Favorite Exercises',
                prefixIcon: Icon(Icons.favorite),
                hintText: 'e.g., Squats, Deadlifts, Bench Press',
              ),
              maxLines: 2,
            ),
          ],
        ),
      ),
    );
  }
}
