// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'workout_template.dart';

// **************************************************************************
// IsarCollectionGenerator
// **************************************************************************

// coverage:ignore-file
// ignore_for_file: duplicate_ignore, non_constant_identifier_names, constant_identifier_names, invalid_use_of_protected_member, unnecessary_cast, prefer_const_constructors, lines_longer_than_80_chars, require_trailing_commas, inference_failure_on_function_invocation, unnecessary_parenthesis, unnecessary_raw_strings, unnecessary_null_checks, join_return_with_assignment, prefer_final_locals, avoid_js_rounded_ints, avoid_positional_boolean_parameters, always_specify_types

extension GetWorkoutTemplateCollection on Isar {
  IsarCollection<WorkoutTemplate> get workoutTemplates => this.collection();
}

const WorkoutTemplateSchema = CollectionSchema(
  name: r'WorkoutTemplate',
  id: 9152743543952114156,
  properties: {
    r'category': PropertySchema(
      id: 0,
      name: r'category',
      type: IsarType.string,
    ),
    r'createdAt': PropertySchema(
      id: 1,
      name: r'createdAt',
      type: IsarType.dateTime,
    ),
    r'createdByUserId': PropertySchema(
      id: 2,
      name: r'createdByUserId',
      type: IsarType.long,
    ),
    r'daysOfWeek': PropertySchema(
      id: 3,
      name: r'daysOfWeek',
      type: IsarType.string,
    ),
    r'daysOfWeekList': PropertySchema(
      id: 4,
      name: r'daysOfWeekList',
      type: IsarType.longList,
    ),
    r'description': PropertySchema(
      id: 5,
      name: r'description',
      type: IsarType.string,
    ),
    r'estimatedDuration': PropertySchema(
      id: 6,
      name: r'estimatedDuration',
      type: IsarType.long,
    ),
    r'exercisesJson': PropertySchema(
      id: 7,
      name: r'exercisesJson',
      type: IsarType.string,
    ),
    r'intervalDays': PropertySchema(
      id: 8,
      name: r'intervalDays',
      type: IsarType.long,
    ),
    r'isActive': PropertySchema(id: 9, name: r'isActive', type: IsarType.bool),
    r'isCommunity': PropertySchema(
      id: 10,
      name: r'isCommunity',
      type: IsarType.bool,
    ),
    r'name': PropertySchema(id: 11, name: r'name', type: IsarType.string),
    r'rating': PropertySchema(id: 12, name: r'rating', type: IsarType.double),
    r'ratingCount': PropertySchema(
      id: 13,
      name: r'ratingCount',
      type: IsarType.long,
    ),
    r'recurrenceDisplay': PropertySchema(
      id: 14,
      name: r'recurrenceDisplay',
      type: IsarType.string,
    ),
    r'recurrencePattern': PropertySchema(
      id: 15,
      name: r'recurrencePattern',
      type: IsarType.string,
    ),
    r'updatedAt': PropertySchema(
      id: 16,
      name: r'updatedAt',
      type: IsarType.dateTime,
    ),
    r'usageCount': PropertySchema(
      id: 17,
      name: r'usageCount',
      type: IsarType.long,
    ),
    r'userId': PropertySchema(id: 18, name: r'userId', type: IsarType.long),
  },
  estimateSize: _workoutTemplateEstimateSize,
  serialize: _workoutTemplateSerialize,
  deserialize: _workoutTemplateDeserialize,
  deserializeProp: _workoutTemplateDeserializeProp,
  idName: r'id',
  indexes: {
    r'userId': IndexSchema(
      id: -2005826577402374815,
      name: r'userId',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'userId',
          type: IndexType.value,
          caseSensitive: false,
        ),
      ],
    ),
    r'isActive': IndexSchema(
      id: 8092228061260947457,
      name: r'isActive',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'isActive',
          type: IndexType.value,
          caseSensitive: false,
        ),
      ],
    ),
  },
  links: {},
  embeddedSchemas: {},
  getId: _workoutTemplateGetId,
  getLinks: _workoutTemplateGetLinks,
  attach: _workoutTemplateAttach,
  version: '3.1.0+1',
);

int _workoutTemplateEstimateSize(
  WorkoutTemplate object,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  var bytesCount = offsets.last;
  {
    final value = object.category;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.daysOfWeek;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.daysOfWeekList.length * 8;
  {
    final value = object.description;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.exercisesJson.length * 3;
  bytesCount += 3 + object.name.length * 3;
  bytesCount += 3 + object.recurrenceDisplay.length * 3;
  bytesCount += 3 + object.recurrencePattern.length * 3;
  return bytesCount;
}

void _workoutTemplateSerialize(
  WorkoutTemplate object,
  IsarWriter writer,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  writer.writeString(offsets[0], object.category);
  writer.writeDateTime(offsets[1], object.createdAt);
  writer.writeLong(offsets[2], object.createdByUserId);
  writer.writeString(offsets[3], object.daysOfWeek);
  writer.writeLongList(offsets[4], object.daysOfWeekList);
  writer.writeString(offsets[5], object.description);
  writer.writeLong(offsets[6], object.estimatedDuration);
  writer.writeString(offsets[7], object.exercisesJson);
  writer.writeLong(offsets[8], object.intervalDays);
  writer.writeBool(offsets[9], object.isActive);
  writer.writeBool(offsets[10], object.isCommunity);
  writer.writeString(offsets[11], object.name);
  writer.writeDouble(offsets[12], object.rating);
  writer.writeLong(offsets[13], object.ratingCount);
  writer.writeString(offsets[14], object.recurrenceDisplay);
  writer.writeString(offsets[15], object.recurrencePattern);
  writer.writeDateTime(offsets[16], object.updatedAt);
  writer.writeLong(offsets[17], object.usageCount);
  writer.writeLong(offsets[18], object.userId);
}

WorkoutTemplate _workoutTemplateDeserialize(
  Id id,
  IsarReader reader,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  final object = WorkoutTemplate(
    category: reader.readStringOrNull(offsets[0]),
    createdAt: reader.readDateTime(offsets[1]),
    createdByUserId: reader.readLongOrNull(offsets[2]),
    daysOfWeek: reader.readStringOrNull(offsets[3]),
    description: reader.readStringOrNull(offsets[5]),
    estimatedDuration: reader.readLongOrNull(offsets[6]),
    exercisesJson: reader.readString(offsets[7]),
    id: id,
    intervalDays: reader.readLongOrNull(offsets[8]),
    isActive: reader.readBoolOrNull(offsets[9]) ?? true,
    isCommunity: reader.readBoolOrNull(offsets[10]) ?? false,
    name: reader.readString(offsets[11]),
    rating: reader.readDoubleOrNull(offsets[12]),
    ratingCount: reader.readLongOrNull(offsets[13]),
    recurrencePattern: reader.readString(offsets[15]),
    updatedAt: reader.readDateTimeOrNull(offsets[16]),
    usageCount: reader.readLongOrNull(offsets[17]) ?? 0,
    userId: reader.readLong(offsets[18]),
  );
  object.daysOfWeekList = reader.readLongList(offsets[4]) ?? [];
  return object;
}

P _workoutTemplateDeserializeProp<P>(
  IsarReader reader,
  int propertyId,
  int offset,
  Map<Type, List<int>> allOffsets,
) {
  switch (propertyId) {
    case 0:
      return (reader.readStringOrNull(offset)) as P;
    case 1:
      return (reader.readDateTime(offset)) as P;
    case 2:
      return (reader.readLongOrNull(offset)) as P;
    case 3:
      return (reader.readStringOrNull(offset)) as P;
    case 4:
      return (reader.readLongList(offset) ?? []) as P;
    case 5:
      return (reader.readStringOrNull(offset)) as P;
    case 6:
      return (reader.readLongOrNull(offset)) as P;
    case 7:
      return (reader.readString(offset)) as P;
    case 8:
      return (reader.readLongOrNull(offset)) as P;
    case 9:
      return (reader.readBoolOrNull(offset) ?? true) as P;
    case 10:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 11:
      return (reader.readString(offset)) as P;
    case 12:
      return (reader.readDoubleOrNull(offset)) as P;
    case 13:
      return (reader.readLongOrNull(offset)) as P;
    case 14:
      return (reader.readString(offset)) as P;
    case 15:
      return (reader.readString(offset)) as P;
    case 16:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 17:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 18:
      return (reader.readLong(offset)) as P;
    default:
      throw IsarError('Unknown property with id $propertyId');
  }
}

Id _workoutTemplateGetId(WorkoutTemplate object) {
  return object.id;
}

List<IsarLinkBase<dynamic>> _workoutTemplateGetLinks(WorkoutTemplate object) {
  return [];
}

void _workoutTemplateAttach(
  IsarCollection<dynamic> col,
  Id id,
  WorkoutTemplate object,
) {
  object.id = id;
}

extension WorkoutTemplateQueryWhereSort
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QWhere> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhere> anyId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(const IdWhereClause.any());
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhere> anyUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'userId'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhere> anyIsActive() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'isActive'),
      );
    });
  }
}

extension WorkoutTemplateQueryWhere
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QWhereClause> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause> idEqualTo(
    Id id,
  ) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IdWhereClause.between(lower: id, upper: id));
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  idNotEqualTo(Id id) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IdWhereClause.lessThan(upper: id, includeUpper: false),
            )
            .addWhereClause(
              IdWhereClause.greaterThan(lower: id, includeLower: false),
            );
      } else {
        return query
            .addWhereClause(
              IdWhereClause.greaterThan(lower: id, includeLower: false),
            )
            .addWhereClause(
              IdWhereClause.lessThan(upper: id, includeUpper: false),
            );
      }
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  idGreaterThan(Id id, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.greaterThan(lower: id, includeLower: include),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause> idLessThan(
    Id id, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.lessThan(upper: id, includeUpper: include),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause> idBetween(
    Id lowerId,
    Id upperId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.between(
          lower: lowerId,
          includeLower: includeLower,
          upper: upperId,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  userIdEqualTo(int userId) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'userId', value: [userId]),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  userIdNotEqualTo(int userId) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'userId',
                lower: [],
                upper: [userId],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'userId',
                lower: [userId],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'userId',
                lower: [userId],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'userId',
                lower: [],
                upper: [userId],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  userIdGreaterThan(int userId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.between(
          indexName: r'userId',
          lower: [userId],
          includeLower: include,
          upper: [],
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  userIdLessThan(int userId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.between(
          indexName: r'userId',
          lower: [],
          upper: [userId],
          includeUpper: include,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  userIdBetween(
    int lowerUserId,
    int upperUserId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.between(
          indexName: r'userId',
          lower: [lowerUserId],
          includeLower: includeLower,
          upper: [upperUserId],
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  isActiveEqualTo(bool isActive) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'isActive', value: [isActive]),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterWhereClause>
  isActiveNotEqualTo(bool isActive) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isActive',
                lower: [],
                upper: [isActive],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isActive',
                lower: [isActive],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isActive',
                lower: [isActive],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isActive',
                lower: [],
                upper: [isActive],
                includeUpper: false,
              ),
            );
      }
    });
  }
}

extension WorkoutTemplateQueryFilter
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QFilterCondition> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'category'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'category'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'category',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'category',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'category',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'category', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  categoryIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'category', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdAtEqualTo(DateTime value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'createdAt', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdAtGreaterThan(DateTime value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'createdAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdAtLessThan(DateTime value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'createdAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdAtBetween(
    DateTime lower,
    DateTime upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'createdAt',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'createdByUserId'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'createdByUserId'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'createdByUserId', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdGreaterThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'createdByUserId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdLessThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'createdByUserId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  createdByUserIdBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'createdByUserId',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'daysOfWeek'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'daysOfWeek'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'daysOfWeek',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'daysOfWeek',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'daysOfWeek',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'daysOfWeek', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'daysOfWeek', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListElementEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'daysOfWeekList', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListElementGreaterThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'daysOfWeekList',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListElementLessThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'daysOfWeekList',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListElementBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'daysOfWeekList',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListLengthEqualTo(int length) {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(r'daysOfWeekList', length, true, length, true);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(r'daysOfWeekList', 0, true, 0, true);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(r'daysOfWeekList', 0, false, 999999, true);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListLengthLessThan(int length, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(r'daysOfWeekList', 0, true, length, include);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListLengthGreaterThan(int length, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(r'daysOfWeekList', length, include, 999999, true);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  daysOfWeekListLengthBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.listLength(
        r'daysOfWeekList',
        lower,
        includeLower,
        upper,
        includeUpper,
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'description'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'description'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'description',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'description',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'description',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'description', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  descriptionIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'description', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'estimatedDuration'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'estimatedDuration'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'estimatedDuration', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationGreaterThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'estimatedDuration',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationLessThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'estimatedDuration',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  estimatedDurationBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'estimatedDuration',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'exercisesJson',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'exercisesJson',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'exercisesJson',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'exercisesJson', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  exercisesJsonIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'exercisesJson', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  idEqualTo(Id value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'id', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  idGreaterThan(Id value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'id',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  idLessThan(Id value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'id',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  idBetween(
    Id lower,
    Id upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'id',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'intervalDays'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'intervalDays'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'intervalDays', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysGreaterThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'intervalDays',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysLessThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'intervalDays',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  intervalDaysBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'intervalDays',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  isActiveEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'isActive', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  isCommunityEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'isCommunity', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'name',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'name',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'name',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'name', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  nameIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'name', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'rating'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'rating'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingEqualTo(double? value, {double epsilon = Query.epsilon}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'rating',
          value: value,
          epsilon: epsilon,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingGreaterThan(
    double? value, {
    bool include = false,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'rating',
          value: value,
          epsilon: epsilon,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingLessThan(
    double? value, {
    bool include = false,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'rating',
          value: value,
          epsilon: epsilon,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingBetween(
    double? lower,
    double? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'rating',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          epsilon: epsilon,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'ratingCount'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'ratingCount'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'ratingCount', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountGreaterThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'ratingCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountLessThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'ratingCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  ratingCountBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'ratingCount',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'recurrenceDisplay',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'recurrenceDisplay',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'recurrenceDisplay',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'recurrenceDisplay', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrenceDisplayIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'recurrenceDisplay', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'recurrencePattern',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'recurrencePattern',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'recurrencePattern',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'recurrencePattern', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  recurrencePatternIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'recurrencePattern', value: ''),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'updatedAt'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'updatedAt'),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'updatedAt', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtGreaterThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'updatedAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtLessThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'updatedAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  updatedAtBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'updatedAt',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  usageCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'usageCount', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  usageCountGreaterThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'usageCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  usageCountLessThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'usageCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  usageCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'usageCount',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  userIdEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'userId', value: value),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  userIdGreaterThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'userId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  userIdLessThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'userId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterFilterCondition>
  userIdBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'userId',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }
}

extension WorkoutTemplateQueryObject
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QFilterCondition> {}

extension WorkoutTemplateQueryLinks
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QFilterCondition> {}

extension WorkoutTemplateQuerySortBy
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QSortBy> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCreatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByCreatedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByDaysOfWeek() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'daysOfWeek', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByDaysOfWeekDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'daysOfWeek', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByEstimatedDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'estimatedDuration', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByEstimatedDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'estimatedDuration', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByExercisesJson() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByExercisesJsonDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIntervalDays() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'intervalDays', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIntervalDaysDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'intervalDays', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIsActive() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isActive', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIsActiveDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isActive', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIsCommunity() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCommunity', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByIsCommunityDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCommunity', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> sortByName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> sortByRating() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'rating', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRatingDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'rating', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRatingCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'ratingCount', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRatingCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'ratingCount', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRecurrenceDisplay() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrenceDisplay', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRecurrenceDisplayDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrenceDisplay', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRecurrencePattern() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrencePattern', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByRecurrencePatternDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrencePattern', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByUpdatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'updatedAt', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByUpdatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'updatedAt', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByUsageCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'usageCount', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByUsageCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'usageCount', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> sortByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  sortByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.desc);
    });
  }
}

extension WorkoutTemplateQuerySortThenBy
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QSortThenBy> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCreatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByCreatedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByDaysOfWeek() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'daysOfWeek', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByDaysOfWeekDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'daysOfWeek', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByEstimatedDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'estimatedDuration', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByEstimatedDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'estimatedDuration', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByExercisesJson() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByExercisesJsonDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> thenById() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'id', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> thenByIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'id', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIntervalDays() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'intervalDays', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIntervalDaysDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'intervalDays', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIsActive() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isActive', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIsActiveDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isActive', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIsCommunity() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCommunity', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByIsCommunityDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCommunity', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> thenByName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> thenByRating() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'rating', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRatingDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'rating', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRatingCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'ratingCount', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRatingCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'ratingCount', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRecurrenceDisplay() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrenceDisplay', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRecurrenceDisplayDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrenceDisplay', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRecurrencePattern() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrencePattern', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByRecurrencePatternDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'recurrencePattern', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByUpdatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'updatedAt', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByUpdatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'updatedAt', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByUsageCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'usageCount', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByUsageCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'usageCount', Sort.desc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy> thenByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.asc);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QAfterSortBy>
  thenByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.desc);
    });
  }
}

extension WorkoutTemplateQueryWhereDistinct
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct> {
  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct> distinctByCategory({
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'category', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'createdAt');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'createdByUserId');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByDaysOfWeek({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'daysOfWeek', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByDaysOfWeekList() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'daysOfWeekList');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByDescription({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'description', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByEstimatedDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'estimatedDuration');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByExercisesJson({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(
        r'exercisesJson',
        caseSensitive: caseSensitive,
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByIntervalDays() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'intervalDays');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByIsActive() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isActive');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByIsCommunity() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isCommunity');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct> distinctByName({
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'name', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct> distinctByRating() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'rating');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByRatingCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'ratingCount');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByRecurrenceDisplay({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(
        r'recurrenceDisplay',
        caseSensitive: caseSensitive,
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByRecurrencePattern({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(
        r'recurrencePattern',
        caseSensitive: caseSensitive,
      );
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByUpdatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'updatedAt');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct>
  distinctByUsageCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'usageCount');
    });
  }

  QueryBuilder<WorkoutTemplate, WorkoutTemplate, QDistinct> distinctByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'userId');
    });
  }
}

extension WorkoutTemplateQueryProperty
    on QueryBuilder<WorkoutTemplate, WorkoutTemplate, QQueryProperty> {
  QueryBuilder<WorkoutTemplate, int, QQueryOperations> idProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'id');
    });
  }

  QueryBuilder<WorkoutTemplate, String?, QQueryOperations> categoryProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'category');
    });
  }

  QueryBuilder<WorkoutTemplate, DateTime, QQueryOperations>
  createdAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'createdAt');
    });
  }

  QueryBuilder<WorkoutTemplate, int?, QQueryOperations>
  createdByUserIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'createdByUserId');
    });
  }

  QueryBuilder<WorkoutTemplate, String?, QQueryOperations>
  daysOfWeekProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'daysOfWeek');
    });
  }

  QueryBuilder<WorkoutTemplate, List<int>, QQueryOperations>
  daysOfWeekListProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'daysOfWeekList');
    });
  }

  QueryBuilder<WorkoutTemplate, String?, QQueryOperations>
  descriptionProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'description');
    });
  }

  QueryBuilder<WorkoutTemplate, int?, QQueryOperations>
  estimatedDurationProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'estimatedDuration');
    });
  }

  QueryBuilder<WorkoutTemplate, String, QQueryOperations>
  exercisesJsonProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'exercisesJson');
    });
  }

  QueryBuilder<WorkoutTemplate, int?, QQueryOperations> intervalDaysProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'intervalDays');
    });
  }

  QueryBuilder<WorkoutTemplate, bool, QQueryOperations> isActiveProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isActive');
    });
  }

  QueryBuilder<WorkoutTemplate, bool, QQueryOperations> isCommunityProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isCommunity');
    });
  }

  QueryBuilder<WorkoutTemplate, String, QQueryOperations> nameProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'name');
    });
  }

  QueryBuilder<WorkoutTemplate, double?, QQueryOperations> ratingProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'rating');
    });
  }

  QueryBuilder<WorkoutTemplate, int?, QQueryOperations> ratingCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'ratingCount');
    });
  }

  QueryBuilder<WorkoutTemplate, String, QQueryOperations>
  recurrenceDisplayProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'recurrenceDisplay');
    });
  }

  QueryBuilder<WorkoutTemplate, String, QQueryOperations>
  recurrencePatternProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'recurrencePattern');
    });
  }

  QueryBuilder<WorkoutTemplate, DateTime?, QQueryOperations>
  updatedAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'updatedAt');
    });
  }

  QueryBuilder<WorkoutTemplate, int, QQueryOperations> usageCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'usageCount');
    });
  }

  QueryBuilder<WorkoutTemplate, int, QQueryOperations> userIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'userId');
    });
  }
}
