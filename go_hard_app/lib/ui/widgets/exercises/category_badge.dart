import 'package:flutter/material.dart';
import '../../../core/utils/category_helpers.dart';

/// Category badge widget for exercise templates
/// Displays colored badge with category name and icon
class CategoryBadge extends StatelessWidget {
  final String? category;
  final bool showIcon;

  const CategoryBadge({
    super.key,
    required this.category,
    this.showIcon = true,
  });

  @override
  Widget build(BuildContext context) {
    final color = CategoryHelpers.getCategoryColor(category);
    final label = category ?? 'Unknown';

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (showIcon) ...[
            Text(
              CategoryHelpers.getCategoryIcon(category),
              style: const TextStyle(fontSize: 14),
            ),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: TextStyle(
              color: color,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
