import 'package:flutter/foundation.dart';
import 'package:just_audio/just_audio.dart';
import 'package:audio_service/audio_service.dart';

/// Service for playing workout music with background playback support
/// Integrates with system media controls (lock screen, notifications)
class MusicPlayerService {
  final AudioPlayer _audioPlayer = AudioPlayer();

  List<MediaItem> _playlist = [];
  int _currentIndex = 0;
  bool _isInitialized = false;

  // Getters
  AudioPlayer get audioPlayer => _audioPlayer;
  bool get isPlaying => _audioPlayer.playing;
  bool get isInitialized => _isInitialized;
  MediaItem? get currentTrack =>
      _playlist.isNotEmpty && _currentIndex < _playlist.length
          ? _playlist[_currentIndex]
          : null;
  Duration get duration => _audioPlayer.duration ?? Duration.zero;
  Duration get position => _audioPlayer.position;
  Stream<PlayerState> get playerStateStream => _audioPlayer.playerStateStream;
  Stream<Duration> get positionStream => _audioPlayer.positionStream;

  /// Initialize music player with default workout playlist
  Future<void> initialize() async {
    if (_isInitialized) return;

    try {
      // Create default workout playlist
      _playlist = _createDefaultPlaylist();

      // Set up playlist
      if (_playlist.isNotEmpty) {
        await _loadTrack(_currentIndex);
      }

      // Listen for track completion
      _audioPlayer.playerStateStream.listen((state) {
        if (state.processingState == ProcessingState.completed) {
          next();
        }
      });

      _isInitialized = true;
      debugPrint('🎵 Music player initialized with ${_playlist.length} tracks');
    } catch (e) {
      debugPrint('❌ Failed to initialize music player: $e');
      rethrow;
    }
  }

  /// Create default workout playlist with royalty-free music
  List<MediaItem> _createDefaultPlaylist() {
    return [
      const MediaItem(
        id: 'workout_1',
        title: 'Epic Motivational Workout',
        artist: 'Workout Music',
        duration: Duration(minutes: 3, seconds: 30),
        artUri: null,
        extras: {
          'source': 'url',
          'path':
              'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3',
        },
      ),
      const MediaItem(
        id: 'workout_2',
        title: 'Power Training Beat',
        artist: 'Workout Music',
        duration: Duration(minutes: 4, seconds: 15),
        artUri: null,
        extras: {
          'source': 'url',
          'path':
              'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3',
        },
      ),
      const MediaItem(
        id: 'workout_3',
        title: 'Cardio Energy',
        artist: 'Workout Music',
        duration: Duration(minutes: 3, seconds: 45),
        artUri: null,
        extras: {
          'source': 'url',
          'path':
              'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3',
        },
      ),
    ];
  }

  /// Load track by index
  Future<void> _loadTrack(int index) async {
    if (index < 0 || index >= _playlist.length) return;

    final track = _playlist[index];
    final source = track.extras?['source'] as String?;
    final path = track.extras?['path'] as String?;

    if (source == null || path == null) return;

    try {
      if (source == 'asset') {
        // Load from assets
        await _audioPlayer.setAsset(path);
        debugPrint('🎵 Loaded asset: $path');
      } else if (source == 'url') {
        // Load from URL (streaming)
        await _audioPlayer.setUrl(path);
        debugPrint('🎵 Loaded URL: ${track.title}');
      }

      _currentIndex = index;
      debugPrint('🎵 Track ready: ${track.title}');
    } catch (e) {
      debugPrint('❌ Failed to load track: $e');
      // Continue anyway - UI will show the track even if loading fails
    }
  }

  /// Play current track
  Future<void> play() async {
    if (!_isInitialized) {
      await initialize();
    }

    try {
      await _audioPlayer.play();
      debugPrint('▶️ Playing: ${currentTrack?.title}');
    } catch (e) {
      debugPrint('❌ Failed to play: $e');
    }
  }

  /// Pause playback
  Future<void> pause() async {
    try {
      await _audioPlayer.pause();
      debugPrint('⏸️ Paused: ${currentTrack?.title}');
    } catch (e) {
      debugPrint('❌ Failed to pause: $e');
    }
  }

  /// Stop playback
  Future<void> stop() async {
    try {
      await _audioPlayer.stop();
      debugPrint('⏹️ Stopped playback');
    } catch (e) {
      debugPrint('❌ Failed to stop: $e');
    }
  }

  /// Skip to next track
  Future<void> next() async {
    if (_playlist.isEmpty) return;

    final nextIndex = (_currentIndex + 1) % _playlist.length;
    await _loadTrack(nextIndex);

    // Auto-play if currently playing
    if (isPlaying) {
      await play();
    }
  }

  /// Skip to previous track
  Future<void> previous() async {
    if (_playlist.isEmpty) return;

    final prevIndex = (_currentIndex - 1 + _playlist.length) % _playlist.length;
    await _loadTrack(prevIndex);

    // Auto-play if currently playing
    if (isPlaying) {
      await play();
    }
  }

  /// Seek to position
  Future<void> seek(Duration position) async {
    try {
      await _audioPlayer.seek(position);
    } catch (e) {
      debugPrint('❌ Failed to seek: $e');
    }
  }

  /// Set volume (0.0 to 1.0)
  Future<void> setVolume(double volume) async {
    try {
      await _audioPlayer.setVolume(volume.clamp(0.0, 1.0));
    } catch (e) {
      debugPrint('❌ Failed to set volume: $e');
    }
  }

  /// Add custom track to playlist
  void addTrack(MediaItem track) {
    _playlist.add(track);
    debugPrint('➕ Added track: ${track.title}');
  }

  /// Clear playlist
  void clearPlaylist() {
    _playlist.clear();
    _currentIndex = 0;
    stop();
    debugPrint('🗑️ Playlist cleared');
  }

  /// Dispose resources
  Future<void> dispose() async {
    await _audioPlayer.dispose();
    _isInitialized = false;
    debugPrint('🎵 Music player disposed');
  }
}
