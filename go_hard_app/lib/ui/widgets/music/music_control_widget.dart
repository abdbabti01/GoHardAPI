import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/music_player_provider.dart';

/// Compact music control widget for workout screen
/// Shows current track with play/pause and skip controls
class MusicControlWidget extends StatelessWidget {
  const MusicControlWidget({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<MusicPlayerProvider>(
      builder: (context, musicProvider, child) {
        if (!musicProvider.isInitialized) {
          return const SizedBox.shrink();
        }

        final track = musicProvider.currentTrack;
        if (track == null) {
          return const SizedBox.shrink();
        }

        return Container(
          margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.1),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Track info and controls
              Row(
                children: [
                  // Music note icon
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.primary,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(
                      Icons.music_note,
                      color: Theme.of(context).colorScheme.onPrimary,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: 12),

                  // Track title and artist
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          track.title,
                          style: Theme.of(context).textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.bold),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        Text(
                          track.artist ?? 'Unknown Artist',
                          style: Theme.of(context).textTheme.bodySmall,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),

                  // Play controls
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      // Previous button
                      IconButton(
                        icon: const Icon(Icons.skip_previous),
                        onPressed: musicProvider.previous,
                        tooltip: 'Previous',
                      ),

                      // Play/Pause button
                      Container(
                        width: 48,
                        height: 48,
                        decoration: BoxDecoration(
                          color: Theme.of(context).colorScheme.primary,
                          shape: BoxShape.circle,
                        ),
                        child: IconButton(
                          icon: Icon(
                            musicProvider.isPlaying
                                ? Icons.pause
                                : Icons.play_arrow,
                            color: Theme.of(context).colorScheme.onPrimary,
                          ),
                          onPressed: musicProvider.togglePlayPause,
                          tooltip: musicProvider.isPlaying ? 'Pause' : 'Play',
                        ),
                      ),

                      // Next button
                      IconButton(
                        icon: const Icon(Icons.skip_next),
                        onPressed: musicProvider.next,
                        tooltip: 'Next',
                      ),
                    ],
                  ),
                ],
              ),

              const SizedBox(height: 8),

              // Progress bar
              _buildProgressBar(context, musicProvider),
            ],
          ),
        );
      },
    );
  }

  Widget _buildProgressBar(
    BuildContext context,
    MusicPlayerProvider musicProvider,
  ) {
    return Column(
      children: [
        SliderTheme(
          data: SliderTheme.of(context).copyWith(
            trackHeight: 3,
            thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 6),
            overlayShape: const RoundSliderOverlayShape(overlayRadius: 12),
          ),
          child: Slider(
            value: musicProvider.progress.clamp(0.0, 1.0),
            onChanged: (value) {
              final duration = musicProvider.duration;
              final position = Duration(
                milliseconds: (duration.inMilliseconds * value).round(),
              );
              musicProvider.seek(position);
            },
            activeColor: Theme.of(context).colorScheme.primary,
            inactiveColor: Theme.of(
              context,
            ).colorScheme.primary.withValues(alpha: 0.2),
          ),
        ),

        // Time labels
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                _formatDuration(musicProvider.position),
                style: Theme.of(context).textTheme.bodySmall,
              ),
              Text(
                _formatDuration(musicProvider.duration),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ],
    );
  }

  String _formatDuration(Duration duration) {
    final minutes = duration.inMinutes.toString().padLeft(2, '0');
    final seconds = duration.inSeconds.remainder(60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }
}
