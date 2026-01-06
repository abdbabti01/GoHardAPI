# Analytics Enhancement Implementation Plan

**Date Started:** 2026-01-06
**Features:** Goal Tracking, Body Metrics Tracking, Comparison Analysis
**Status:** 🚧 IN PROGRESS

---

## 📋 Overview

Implementing three major analytics enhancements:
1. **Goal Setting & Tracking** - User-defined fitness goals with progress monitoring
2. **Body Metrics Tracking** - Weight, body fat %, measurements over time
3. **Comparison Analysis** - Period-over-period comparisons (this week vs last week)

---

## 📝 Implementation Checklist

### Phase 1: Backend - Goals System
- [ ] Create Goal model
- [ ] Create GoalsController
- [ ] Add database migration
- [ ] Test endpoints

### Phase 2: Backend - Body Metrics
- [ ] Create BodyMetric model
- [ ] Create BodyMetricsController
- [ ] Add database migration
- [ ] Test endpoints

### Phase 3: Backend - Comparison Analytics
- [ ] Extend AnalyticsController
- [ ] Add comparison endpoints

### Phase 4: Mobile - Goal Models & Repository
- [ ] Create Goal Isar model
- [ ] Create GoalRepository
- [ ] Create GoalProvider

### Phase 5: Mobile - Body Metrics Models & Repository
- [ ] Create BodyMetric Isar model
- [ ] Create BodyMetricsRepository
- [ ] Create BodyMetricsProvider

### Phase 6: Mobile - Comparison
- [ ] Update AnalyticsProvider
- [ ] Create comparison models

### Phase 7: Mobile - Goals UI
- [ ] Create GoalsScreen
- [ ] Create GoalFormDialog
- [ ] Add navigation

### Phase 8: Mobile - Body Metrics UI
- [ ] Create BodyMetricsScreen
- [ ] Create MetricFormDialog
- [ ] Add charts

### Phase 9: Mobile - Comparison UI
- [ ] Add comparison section to Analytics
- [ ] Create ComparisonCard widget

---

## 🗂 Files to Create/Modify

### Backend
**New:** Goal.cs, BodyMetric.cs, GoalsController.cs, BodyMetricsController.cs
**Modified:** TrainingContext.cs, AnalyticsController.cs

### Mobile
**New:** goal.dart, body_metric.dart, goal_repository.dart, body_metrics_repository.dart, goal_provider.dart, body_metrics_provider.dart, goals_screen.dart, body_metrics_screen.dart, comparison_card.dart
**Modified:** analytics_provider.dart, analytics_screen.dart, app.dart, route_names.dart

---

**Last Updated:** 2026-01-06
