// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'shared_workout.dart';

// **************************************************************************
// IsarCollectionGenerator
// **************************************************************************

// coverage:ignore-file
// ignore_for_file: duplicate_ignore, non_constant_identifier_names, constant_identifier_names, invalid_use_of_protected_member, unnecessary_cast, prefer_const_constructors, lines_longer_than_80_chars, require_trailing_commas, inference_failure_on_function_invocation, unnecessary_parenthesis, unnecessary_raw_strings, unnecessary_null_checks, join_return_with_assignment, prefer_final_locals, avoid_js_rounded_ints, avoid_positional_boolean_parameters, always_specify_types

extension GetSharedWorkoutCollection on Isar {
  IsarCollection<SharedWorkout> get sharedWorkouts => this.collection();
}

const SharedWorkoutSchema = CollectionSchema(
  name: r'SharedWorkout',
  id: 5198773065541368135,
  properties: {
    r'category': PropertySchema(
      id: 0,
      name: r'category',
      type: IsarType.string,
    ),
    r'commentCount': PropertySchema(
      id: 1,
      name: r'commentCount',
      type: IsarType.long,
    ),
    r'description': PropertySchema(
      id: 2,
      name: r'description',
      type: IsarType.string,
    ),
    r'difficulty': PropertySchema(
      id: 3,
      name: r'difficulty',
      type: IsarType.string,
    ),
    r'duration': PropertySchema(
      id: 4,
      name: r'duration',
      type: IsarType.long,
    ),
    r'exercisesJson': PropertySchema(
      id: 5,
      name: r'exercisesJson',
      type: IsarType.string,
    ),
    r'formattedDuration': PropertySchema(
      id: 6,
      name: r'formattedDuration',
      type: IsarType.string,
    ),
    r'isLikedByCurrentUser': PropertySchema(
      id: 7,
      name: r'isLikedByCurrentUser',
      type: IsarType.bool,
    ),
    r'isSavedByCurrentUser': PropertySchema(
      id: 8,
      name: r'isSavedByCurrentUser',
      type: IsarType.bool,
    ),
    r'likeCount': PropertySchema(
      id: 9,
      name: r'likeCount',
      type: IsarType.long,
    ),
    r'originalId': PropertySchema(
      id: 10,
      name: r'originalId',
      type: IsarType.long,
    ),
    r'saveCount': PropertySchema(
      id: 11,
      name: r'saveCount',
      type: IsarType.long,
    ),
    r'sharedAt': PropertySchema(
      id: 12,
      name: r'sharedAt',
      type: IsarType.dateTime,
    ),
    r'sharedByUserId': PropertySchema(
      id: 13,
      name: r'sharedByUserId',
      type: IsarType.long,
    ),
    r'sharedByUserName': PropertySchema(
      id: 14,
      name: r'sharedByUserName',
      type: IsarType.string,
    ),
    r'timeSinceShared': PropertySchema(
      id: 15,
      name: r'timeSinceShared',
      type: IsarType.string,
    ),
    r'type': PropertySchema(
      id: 16,
      name: r'type',
      type: IsarType.string,
    ),
    r'workoutName': PropertySchema(
      id: 17,
      name: r'workoutName',
      type: IsarType.string,
    )
  },
  estimateSize: _sharedWorkoutEstimateSize,
  serialize: _sharedWorkoutSerialize,
  deserialize: _sharedWorkoutDeserialize,
  deserializeProp: _sharedWorkoutDeserializeProp,
  idName: r'id',
  indexes: {
    r'sharedByUserId': IndexSchema(
      id: 2057187170528208220,
      name: r'sharedByUserId',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'sharedByUserId',
          type: IndexType.value,
          caseSensitive: false,
        )
      ],
    )
  },
  links: {},
  embeddedSchemas: {},
  getId: _sharedWorkoutGetId,
  getLinks: _sharedWorkoutGetLinks,
  attach: _sharedWorkoutAttach,
  version: '3.1.0+1',
);

int _sharedWorkoutEstimateSize(
  SharedWorkout object,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  var bytesCount = offsets.last;
  bytesCount += 3 + object.category.length * 3;
  {
    final value = object.description;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.difficulty;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.exercisesJson.length * 3;
  bytesCount += 3 + object.formattedDuration.length * 3;
  bytesCount += 3 + object.sharedByUserName.length * 3;
  bytesCount += 3 + object.timeSinceShared.length * 3;
  bytesCount += 3 + object.type.length * 3;
  bytesCount += 3 + object.workoutName.length * 3;
  return bytesCount;
}

void _sharedWorkoutSerialize(
  SharedWorkout object,
  IsarWriter writer,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  writer.writeString(offsets[0], object.category);
  writer.writeLong(offsets[1], object.commentCount);
  writer.writeString(offsets[2], object.description);
  writer.writeString(offsets[3], object.difficulty);
  writer.writeLong(offsets[4], object.duration);
  writer.writeString(offsets[5], object.exercisesJson);
  writer.writeString(offsets[6], object.formattedDuration);
  writer.writeBool(offsets[7], object.isLikedByCurrentUser);
  writer.writeBool(offsets[8], object.isSavedByCurrentUser);
  writer.writeLong(offsets[9], object.likeCount);
  writer.writeLong(offsets[10], object.originalId);
  writer.writeLong(offsets[11], object.saveCount);
  writer.writeDateTime(offsets[12], object.sharedAt);
  writer.writeLong(offsets[13], object.sharedByUserId);
  writer.writeString(offsets[14], object.sharedByUserName);
  writer.writeString(offsets[15], object.timeSinceShared);
  writer.writeString(offsets[16], object.type);
  writer.writeString(offsets[17], object.workoutName);
}

SharedWorkout _sharedWorkoutDeserialize(
  Id id,
  IsarReader reader,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  final object = SharedWorkout(
    category: reader.readString(offsets[0]),
    commentCount: reader.readLongOrNull(offsets[1]) ?? 0,
    description: reader.readStringOrNull(offsets[2]),
    difficulty: reader.readStringOrNull(offsets[3]),
    duration: reader.readLong(offsets[4]),
    exercisesJson: reader.readString(offsets[5]),
    id: id,
    isLikedByCurrentUser: reader.readBoolOrNull(offsets[7]) ?? false,
    isSavedByCurrentUser: reader.readBoolOrNull(offsets[8]) ?? false,
    likeCount: reader.readLongOrNull(offsets[9]) ?? 0,
    originalId: reader.readLong(offsets[10]),
    saveCount: reader.readLongOrNull(offsets[11]) ?? 0,
    sharedAt: reader.readDateTime(offsets[12]),
    sharedByUserId: reader.readLong(offsets[13]),
    sharedByUserName: reader.readString(offsets[14]),
    type: reader.readString(offsets[16]),
    workoutName: reader.readString(offsets[17]),
  );
  return object;
}

P _sharedWorkoutDeserializeProp<P>(
  IsarReader reader,
  int propertyId,
  int offset,
  Map<Type, List<int>> allOffsets,
) {
  switch (propertyId) {
    case 0:
      return (reader.readString(offset)) as P;
    case 1:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 2:
      return (reader.readStringOrNull(offset)) as P;
    case 3:
      return (reader.readStringOrNull(offset)) as P;
    case 4:
      return (reader.readLong(offset)) as P;
    case 5:
      return (reader.readString(offset)) as P;
    case 6:
      return (reader.readString(offset)) as P;
    case 7:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 8:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 9:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 10:
      return (reader.readLong(offset)) as P;
    case 11:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 12:
      return (reader.readDateTime(offset)) as P;
    case 13:
      return (reader.readLong(offset)) as P;
    case 14:
      return (reader.readString(offset)) as P;
    case 15:
      return (reader.readString(offset)) as P;
    case 16:
      return (reader.readString(offset)) as P;
    case 17:
      return (reader.readString(offset)) as P;
    default:
      throw IsarError('Unknown property with id $propertyId');
  }
}

Id _sharedWorkoutGetId(SharedWorkout object) {
  return object.id;
}

List<IsarLinkBase<dynamic>> _sharedWorkoutGetLinks(SharedWorkout object) {
  return [];
}

void _sharedWorkoutAttach(
    IsarCollection<dynamic> col, Id id, SharedWorkout object) {
  object.id = id;
}

extension SharedWorkoutQueryWhereSort
    on QueryBuilder<SharedWorkout, SharedWorkout, QWhere> {
  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhere> anyId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(const IdWhereClause.any());
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhere> anySharedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'sharedByUserId'),
      );
    });
  }
}

extension SharedWorkoutQueryWhere
    on QueryBuilder<SharedWorkout, SharedWorkout, QWhereClause> {
  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause> idEqualTo(
      Id id) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IdWhereClause.between(
        lower: id,
        upper: id,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause> idNotEqualTo(
      Id id) {
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

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause> idGreaterThan(
      Id id,
      {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.greaterThan(lower: id, includeLower: include),
      );
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause> idLessThan(
      Id id,
      {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.lessThan(upper: id, includeUpper: include),
      );
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause> idBetween(
    Id lowerId,
    Id upperId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IdWhereClause.between(
        lower: lowerId,
        includeLower: includeLower,
        upper: upperId,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause>
      sharedByUserIdEqualTo(int sharedByUserId) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.equalTo(
        indexName: r'sharedByUserId',
        value: [sharedByUserId],
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause>
      sharedByUserIdNotEqualTo(int sharedByUserId) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'sharedByUserId',
              lower: [],
              upper: [sharedByUserId],
              includeUpper: false,
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'sharedByUserId',
              lower: [sharedByUserId],
              includeLower: false,
              upper: [],
            ));
      } else {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'sharedByUserId',
              lower: [sharedByUserId],
              includeLower: false,
              upper: [],
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'sharedByUserId',
              lower: [],
              upper: [sharedByUserId],
              includeUpper: false,
            ));
      }
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause>
      sharedByUserIdGreaterThan(
    int sharedByUserId, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.between(
        indexName: r'sharedByUserId',
        lower: [sharedByUserId],
        includeLower: include,
        upper: [],
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause>
      sharedByUserIdLessThan(
    int sharedByUserId, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.between(
        indexName: r'sharedByUserId',
        lower: [],
        upper: [sharedByUserId],
        includeUpper: include,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterWhereClause>
      sharedByUserIdBetween(
    int lowerSharedByUserId,
    int upperSharedByUserId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.between(
        indexName: r'sharedByUserId',
        lower: [lowerSharedByUserId],
        includeLower: includeLower,
        upper: [upperSharedByUserId],
        includeUpper: includeUpper,
      ));
    });
  }
}

extension SharedWorkoutQueryFilter
    on QueryBuilder<SharedWorkout, SharedWorkout, QFilterCondition> {
  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'category',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'category',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'category',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'category',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      categoryIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'category',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      commentCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'commentCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      commentCountGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'commentCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      commentCountLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'commentCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      commentCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'commentCount',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'description',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'description',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionEqualTo(
    String? value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'description',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'description',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'description',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'description',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      descriptionIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'description',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'difficulty',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'difficulty',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyEqualTo(
    String? value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'difficulty',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'difficulty',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'difficulty',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'difficulty',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      difficultyIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'difficulty',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      durationEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'duration',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      durationGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'duration',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      durationLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'duration',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      durationBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'duration',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'exercisesJson',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'exercisesJson',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'exercisesJson',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'exercisesJson',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      exercisesJsonIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'exercisesJson',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'formattedDuration',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'formattedDuration',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'formattedDuration',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'formattedDuration',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      formattedDurationIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'formattedDuration',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> idEqualTo(
      Id value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'id',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      idGreaterThan(
    Id value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'id',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> idLessThan(
    Id value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'id',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> idBetween(
    Id lower,
    Id upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'id',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      isLikedByCurrentUserEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'isLikedByCurrentUser',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      isSavedByCurrentUserEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'isSavedByCurrentUser',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      likeCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'likeCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      likeCountGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'likeCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      likeCountLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'likeCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      likeCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'likeCount',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      originalIdEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'originalId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      originalIdGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'originalId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      originalIdLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'originalId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      originalIdBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'originalId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      saveCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'saveCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      saveCountGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'saveCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      saveCountLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'saveCount',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      saveCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'saveCount',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedAtEqualTo(DateTime value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'sharedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedAtGreaterThan(
    DateTime value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'sharedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedAtLessThan(
    DateTime value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'sharedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedAtBetween(
    DateTime lower,
    DateTime upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'sharedAt',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserIdEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'sharedByUserId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserIdGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'sharedByUserId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserIdLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'sharedByUserId',
        value: value,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserIdBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'sharedByUserId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'sharedByUserName',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'sharedByUserName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'sharedByUserName',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'sharedByUserName',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      sharedByUserNameIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'sharedByUserName',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'timeSinceShared',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'timeSinceShared',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'timeSinceShared',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'timeSinceShared',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      timeSinceSharedIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'timeSinceShared',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> typeEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> typeBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'type',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'type',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition> typeMatches(
      String pattern,
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'type',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'type',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      typeIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'type',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'workoutName',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'workoutName',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'workoutName',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'workoutName',
        value: '',
      ));
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterFilterCondition>
      workoutNameIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'workoutName',
        value: '',
      ));
    });
  }
}

extension SharedWorkoutQueryObject
    on QueryBuilder<SharedWorkout, SharedWorkout, QFilterCondition> {}

extension SharedWorkoutQueryLinks
    on QueryBuilder<SharedWorkout, SharedWorkout, QFilterCondition> {}

extension SharedWorkoutQuerySortBy
    on QueryBuilder<SharedWorkout, SharedWorkout, QSortBy> {
  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByCommentCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'commentCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByCommentCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'commentCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByDifficulty() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByDifficultyDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByExercisesJson() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByExercisesJsonDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByFormattedDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'formattedDuration', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByFormattedDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'formattedDuration', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByIsLikedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isLikedByCurrentUser', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByIsLikedByCurrentUserDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isLikedByCurrentUser', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByIsSavedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSavedByCurrentUser', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByIsSavedByCurrentUserDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSavedByCurrentUser', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByLikeCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'likeCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByLikeCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'likeCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByOriginalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'originalId', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByOriginalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'originalId', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortBySaveCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'saveCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySaveCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'saveCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortBySharedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedAt', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySharedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedAt', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySharedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserId', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySharedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserId', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySharedByUserName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserName', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortBySharedByUserNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserName', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByTimeSinceShared() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'timeSinceShared', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByTimeSinceSharedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'timeSinceShared', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByType() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByTypeDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> sortByWorkoutName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'workoutName', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      sortByWorkoutNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'workoutName', Sort.desc);
    });
  }
}

extension SharedWorkoutQuerySortThenBy
    on QueryBuilder<SharedWorkout, SharedWorkout, QSortThenBy> {
  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByCommentCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'commentCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByCommentCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'commentCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByDifficulty() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByDifficultyDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByExercisesJson() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByExercisesJsonDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exercisesJson', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByFormattedDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'formattedDuration', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByFormattedDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'formattedDuration', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenById() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'id', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'id', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByIsLikedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isLikedByCurrentUser', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByIsLikedByCurrentUserDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isLikedByCurrentUser', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByIsSavedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSavedByCurrentUser', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByIsSavedByCurrentUserDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSavedByCurrentUser', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByLikeCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'likeCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByLikeCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'likeCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByOriginalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'originalId', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByOriginalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'originalId', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenBySaveCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'saveCount', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySaveCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'saveCount', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenBySharedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedAt', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySharedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedAt', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySharedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserId', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySharedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserId', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySharedByUserName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserName', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenBySharedByUserNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'sharedByUserName', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByTimeSinceShared() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'timeSinceShared', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByTimeSinceSharedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'timeSinceShared', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByType() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByTypeDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.desc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy> thenByWorkoutName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'workoutName', Sort.asc);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QAfterSortBy>
      thenByWorkoutNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'workoutName', Sort.desc);
    });
  }
}

extension SharedWorkoutQueryWhereDistinct
    on QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> {
  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByCategory(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'category', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctByCommentCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'commentCount');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByDescription(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'description', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByDifficulty(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'difficulty', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'duration');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByExercisesJson(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'exercisesJson',
          caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctByFormattedDuration({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'formattedDuration',
          caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctByIsLikedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isLikedByCurrentUser');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctByIsSavedByCurrentUser() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isSavedByCurrentUser');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByLikeCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'likeCount');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByOriginalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'originalId');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctBySaveCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'saveCount');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctBySharedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'sharedAt');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctBySharedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'sharedByUserId');
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctBySharedByUserName({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'sharedByUserName',
          caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct>
      distinctByTimeSinceShared({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'timeSinceShared',
          caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByType(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'type', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<SharedWorkout, SharedWorkout, QDistinct> distinctByWorkoutName(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'workoutName', caseSensitive: caseSensitive);
    });
  }
}

extension SharedWorkoutQueryProperty
    on QueryBuilder<SharedWorkout, SharedWorkout, QQueryProperty> {
  QueryBuilder<SharedWorkout, int, QQueryOperations> idProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'id');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations> categoryProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'category');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> commentCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'commentCount');
    });
  }

  QueryBuilder<SharedWorkout, String?, QQueryOperations> descriptionProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'description');
    });
  }

  QueryBuilder<SharedWorkout, String?, QQueryOperations> difficultyProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'difficulty');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> durationProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'duration');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations>
      exercisesJsonProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'exercisesJson');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations>
      formattedDurationProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'formattedDuration');
    });
  }

  QueryBuilder<SharedWorkout, bool, QQueryOperations>
      isLikedByCurrentUserProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isLikedByCurrentUser');
    });
  }

  QueryBuilder<SharedWorkout, bool, QQueryOperations>
      isSavedByCurrentUserProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isSavedByCurrentUser');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> likeCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'likeCount');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> originalIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'originalId');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> saveCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'saveCount');
    });
  }

  QueryBuilder<SharedWorkout, DateTime, QQueryOperations> sharedAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'sharedAt');
    });
  }

  QueryBuilder<SharedWorkout, int, QQueryOperations> sharedByUserIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'sharedByUserId');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations>
      sharedByUserNameProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'sharedByUserName');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations>
      timeSinceSharedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'timeSinceShared');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations> typeProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'type');
    });
  }

  QueryBuilder<SharedWorkout, String, QQueryOperations> workoutNameProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'workoutName');
    });
  }
}
