import 'package:flutter/material.dart';
import 'package:audio_service/audio_service.dart';
import '../core/services/music_player_service.dart';

/// Provider for music player state management
/// Provides reactive updates for UI components
class MusicPlayerProvider extends ChangeNotifier {
  final MusicPlayerService _musicService = MusicPlayerService();

  bool _isInitialized = false;
  bool _isPlaying = false;
  Duration _duration = Duration.zero;
  Duration _position = Duration.zero;
  MediaItem? _currentTrack;
  String? _errorMessage;

  // Getters
  bool get isInitialized => _isInitialized;
  bool get isPlaying => _isPlaying;
  Duration get duration => _duration;
  Duration get position => _position;
  MediaItem? get currentTrack => _currentTrack;
  String? get errorMessage => _errorMessage;
  double get progress =>
      duration.inMilliseconds > 0
          ? position.inMilliseconds / duration.inMilliseconds
          : 0.0;

  /// Initialize music player
  Future<void> initialize() async {
    if (_isInitialized) return;

    try {
      await _musicService.initialize();

      // Listen to player state changes
      _musicService.playerStateStream.listen((state) {
        _isPlaying = state.playing;
        notifyListeners();
      });

      // Listen to position changes
      _musicService.positionStream.listen((pos) {
        _position = pos;
        notifyListeners();
      });

      // Listen to duration changes
      _musicService.audioPlayer.durationStream.listen((dur) {
        _duration = dur ?? Duration.zero;
        notifyListeners();
      });

      _currentTrack = _musicService.currentTrack;
      _isInitialized = true;
      notifyListeners();
    } catch (e) {
      _errorMessage = 'Failed to initialize music player: $e';
      debugPrint('❌ $_errorMessage');
      notifyListeners();
    }
  }

  /// Play music
  Future<void> play() async {
    if (!_isInitialized) {
      await initialize();
    }

    try {
      await _musicService.play();
      _currentTrack = _musicService.currentTrack;
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to play: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Pause music
  Future<void> pause() async {
    try {
      await _musicService.pause();
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to pause: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Toggle play/pause
  Future<void> togglePlayPause() async {
    if (_isPlaying) {
      await pause();
    } else {
      await play();
    }
  }

  /// Skip to next track
  Future<void> next() async {
    try {
      await _musicService.next();
      _currentTrack = _musicService.currentTrack;
      _duration = _musicService.duration;
      _position = Duration.zero;
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to skip: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Skip to previous track
  Future<void> previous() async {
    try {
      await _musicService.previous();
      _currentTrack = _musicService.currentTrack;
      _duration = _musicService.duration;
      _position = Duration.zero;
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to go back: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Seek to position
  Future<void> seek(Duration position) async {
    try {
      await _musicService.seek(position);
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to seek: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Set volume (0.0 to 1.0)
  Future<void> setVolume(double volume) async {
    try {
      await _musicService.setVolume(volume);
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to set volume: $e';
      debugPrint('❌ $_errorMessage');
    }
  }

  /// Stop music and clean up
  Future<void> stop() async {
    try {
      await _musicService.stop();
      _position = Duration.zero;
      _errorMessage = null;
    } catch (e) {
      _errorMessage = 'Failed to stop: $e';
      debugPrint('❌ $_errorMessage');
    }
    notifyListeners();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  @override
  void dispose() {
    _musicService.dispose();
    super.dispose();
  }
}
