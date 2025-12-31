// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'local_exercise_set.dart';

// **************************************************************************
// IsarCollectionGenerator
// **************************************************************************

// coverage:ignore-file
// ignore_for_file: duplicate_ignore, non_constant_identifier_names, constant_identifier_names, invalid_use_of_protected_member, unnecessary_cast, prefer_const_constructors, lines_longer_than_80_chars, require_trailing_commas, inference_failure_on_function_invocation, unnecessary_parenthesis, unnecessary_raw_strings, unnecessary_null_checks, join_return_with_assignment, prefer_final_locals, avoid_js_rounded_ints, avoid_positional_boolean_parameters, always_specify_types

extension GetLocalExerciseSetCollection on Isar {
  IsarCollection<LocalExerciseSet> get localExerciseSets => this.collection();
}

const LocalExerciseSetSchema = CollectionSchema(
  name: r'LocalExerciseSet',
  id: 3909731644278117134,
  properties: {
    r'completedAt': PropertySchema(
      id: 0,
      name: r'completedAt',
      type: IsarType.dateTime,
    ),
    r'duration': PropertySchema(
      id: 1,
      name: r'duration',
      type: IsarType.long,
    ),
    r'exerciseLocalId': PropertySchema(
      id: 2,
      name: r'exerciseLocalId',
      type: IsarType.long,
    ),
    r'exerciseServerId': PropertySchema(
      id: 3,
      name: r'exerciseServerId',
      type: IsarType.long,
    ),
    r'isCompleted': PropertySchema(
      id: 4,
      name: r'isCompleted',
      type: IsarType.bool,
    ),
    r'isSynced': PropertySchema(
      id: 5,
      name: r'isSynced',
      type: IsarType.bool,
    ),
    r'lastModifiedLocal': PropertySchema(
      id: 6,
      name: r'lastModifiedLocal',
      type: IsarType.dateTime,
    ),
    r'lastModifiedServer': PropertySchema(
      id: 7,
      name: r'lastModifiedServer',
      type: IsarType.dateTime,
    ),
    r'lastSyncAttempt': PropertySchema(
      id: 8,
      name: r'lastSyncAttempt',
      type: IsarType.dateTime,
    ),
    r'notes': PropertySchema(
      id: 9,
      name: r'notes',
      type: IsarType.string,
    ),
    r'reps': PropertySchema(
      id: 10,
      name: r'reps',
      type: IsarType.long,
    ),
    r'serverId': PropertySchema(
      id: 11,
      name: r'serverId',
      type: IsarType.long,
    ),
    r'setNumber': PropertySchema(
      id: 12,
      name: r'setNumber',
      type: IsarType.long,
    ),
    r'syncError': PropertySchema(
      id: 13,
      name: r'syncError',
      type: IsarType.string,
    ),
    r'syncRetryCount': PropertySchema(
      id: 14,
      name: r'syncRetryCount',
      type: IsarType.long,
    ),
    r'syncStatus': PropertySchema(
      id: 15,
      name: r'syncStatus',
      type: IsarType.string,
    ),
    r'weight': PropertySchema(
      id: 16,
      name: r'weight',
      type: IsarType.double,
    )
  },
  estimateSize: _localExerciseSetEstimateSize,
  serialize: _localExerciseSetSerialize,
  deserialize: _localExerciseSetDeserialize,
  deserializeProp: _localExerciseSetDeserializeProp,
  idName: r'localId',
  indexes: {
    r'isSynced': IndexSchema(
      id: -39763503327887510,
      name: r'isSynced',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'isSynced',
          type: IndexType.value,
          caseSensitive: false,
        )
      ],
    ),
    r'syncStatus': IndexSchema(
      id: 8239539375045684509,
      name: r'syncStatus',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'syncStatus',
          type: IndexType.hash,
          caseSensitive: true,
        )
      ],
    )
  },
  links: {},
  embeddedSchemas: {},
  getId: _localExerciseSetGetId,
  getLinks: _localExerciseSetGetLinks,
  attach: _localExerciseSetAttach,
  version: '3.1.0+1',
);

int _localExerciseSetEstimateSize(
  LocalExerciseSet object,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  var bytesCount = offsets.last;
  {
    final value = object.notes;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.syncError;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.syncStatus.length * 3;
  return bytesCount;
}

void _localExerciseSetSerialize(
  LocalExerciseSet object,
  IsarWriter writer,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  writer.writeDateTime(offsets[0], object.completedAt);
  writer.writeLong(offsets[1], object.duration);
  writer.writeLong(offsets[2], object.exerciseLocalId);
  writer.writeLong(offsets[3], object.exerciseServerId);
  writer.writeBool(offsets[4], object.isCompleted);
  writer.writeBool(offsets[5], object.isSynced);
  writer.writeDateTime(offsets[6], object.lastModifiedLocal);
  writer.writeDateTime(offsets[7], object.lastModifiedServer);
  writer.writeDateTime(offsets[8], object.lastSyncAttempt);
  writer.writeString(offsets[9], object.notes);
  writer.writeLong(offsets[10], object.reps);
  writer.writeLong(offsets[11], object.serverId);
  writer.writeLong(offsets[12], object.setNumber);
  writer.writeString(offsets[13], object.syncError);
  writer.writeLong(offsets[14], object.syncRetryCount);
  writer.writeString(offsets[15], object.syncStatus);
  writer.writeDouble(offsets[16], object.weight);
}

LocalExerciseSet _localExerciseSetDeserialize(
  Id id,
  IsarReader reader,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  final object = LocalExerciseSet(
    completedAt: reader.readDateTimeOrNull(offsets[0]),
    duration: reader.readLongOrNull(offsets[1]),
    exerciseLocalId: reader.readLong(offsets[2]),
    exerciseServerId: reader.readLongOrNull(offsets[3]),
    isCompleted: reader.readBoolOrNull(offsets[4]) ?? false,
    isSynced: reader.readBoolOrNull(offsets[5]) ?? false,
    lastModifiedLocal: reader.readDateTime(offsets[6]),
    lastModifiedServer: reader.readDateTimeOrNull(offsets[7]),
    lastSyncAttempt: reader.readDateTimeOrNull(offsets[8]),
    notes: reader.readStringOrNull(offsets[9]),
    reps: reader.readLongOrNull(offsets[10]),
    serverId: reader.readLongOrNull(offsets[11]),
    setNumber: reader.readLong(offsets[12]),
    syncError: reader.readStringOrNull(offsets[13]),
    syncRetryCount: reader.readLongOrNull(offsets[14]) ?? 0,
    syncStatus: reader.readStringOrNull(offsets[15]) ?? 'pending_create',
    weight: reader.readDoubleOrNull(offsets[16]),
  );
  object.localId = id;
  return object;
}

P _localExerciseSetDeserializeProp<P>(
  IsarReader reader,
  int propertyId,
  int offset,
  Map<Type, List<int>> allOffsets,
) {
  switch (propertyId) {
    case 0:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 1:
      return (reader.readLongOrNull(offset)) as P;
    case 2:
      return (reader.readLong(offset)) as P;
    case 3:
      return (reader.readLongOrNull(offset)) as P;
    case 4:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 5:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 6:
      return (reader.readDateTime(offset)) as P;
    case 7:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 8:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 9:
      return (reader.readStringOrNull(offset)) as P;
    case 10:
      return (reader.readLongOrNull(offset)) as P;
    case 11:
      return (reader.readLongOrNull(offset)) as P;
    case 12:
      return (reader.readLong(offset)) as P;
    case 13:
      return (reader.readStringOrNull(offset)) as P;
    case 14:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 15:
      return (reader.readStringOrNull(offset) ?? 'pending_create') as P;
    case 16:
      return (reader.readDoubleOrNull(offset)) as P;
    default:
      throw IsarError('Unknown property with id $propertyId');
  }
}

Id _localExerciseSetGetId(LocalExerciseSet object) {
  return object.localId;
}

List<IsarLinkBase<dynamic>> _localExerciseSetGetLinks(LocalExerciseSet object) {
  return [];
}

void _localExerciseSetAttach(
    IsarCollection<dynamic> col, Id id, LocalExerciseSet object) {
  object.localId = id;
}

extension LocalExerciseSetQueryWhereSort
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QWhere> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhere> anyLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(const IdWhereClause.any());
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhere> anyIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'isSynced'),
      );
    });
  }
}

extension LocalExerciseSetQueryWhere
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QWhereClause> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      localIdEqualTo(Id localId) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IdWhereClause.between(
        lower: localId,
        upper: localId,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      localIdNotEqualTo(Id localId) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IdWhereClause.lessThan(upper: localId, includeUpper: false),
            )
            .addWhereClause(
              IdWhereClause.greaterThan(lower: localId, includeLower: false),
            );
      } else {
        return query
            .addWhereClause(
              IdWhereClause.greaterThan(lower: localId, includeLower: false),
            )
            .addWhereClause(
              IdWhereClause.lessThan(upper: localId, includeUpper: false),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      localIdGreaterThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.greaterThan(lower: localId, includeLower: include),
      );
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      localIdLessThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.lessThan(upper: localId, includeUpper: include),
      );
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      localIdBetween(
    Id lowerLocalId,
    Id upperLocalId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IdWhereClause.between(
        lower: lowerLocalId,
        includeLower: includeLower,
        upper: upperLocalId,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      isSyncedEqualTo(bool isSynced) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.equalTo(
        indexName: r'isSynced',
        value: [isSynced],
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      isSyncedNotEqualTo(bool isSynced) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'isSynced',
              lower: [],
              upper: [isSynced],
              includeUpper: false,
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'isSynced',
              lower: [isSynced],
              includeLower: false,
              upper: [],
            ));
      } else {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'isSynced',
              lower: [isSynced],
              includeLower: false,
              upper: [],
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'isSynced',
              lower: [],
              upper: [isSynced],
              includeUpper: false,
            ));
      }
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      syncStatusEqualTo(String syncStatus) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(IndexWhereClause.equalTo(
        indexName: r'syncStatus',
        value: [syncStatus],
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterWhereClause>
      syncStatusNotEqualTo(String syncStatus) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'syncStatus',
              lower: [],
              upper: [syncStatus],
              includeUpper: false,
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'syncStatus',
              lower: [syncStatus],
              includeLower: false,
              upper: [],
            ));
      } else {
        return query
            .addWhereClause(IndexWhereClause.between(
              indexName: r'syncStatus',
              lower: [syncStatus],
              includeLower: false,
              upper: [],
            ))
            .addWhereClause(IndexWhereClause.between(
              indexName: r'syncStatus',
              lower: [],
              upper: [syncStatus],
              includeUpper: false,
            ));
      }
    });
  }
}

extension LocalExerciseSetQueryFilter
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QFilterCondition> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'completedAt',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'completedAt',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'completedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtGreaterThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'completedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtLessThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'completedAt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      completedAtBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'completedAt',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'duration',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'duration',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'duration',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationGreaterThan(
    int? value, {
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

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationLessThan(
    int? value, {
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

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      durationBetween(
    int? lower,
    int? upper, {
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

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseLocalIdEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'exerciseLocalId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseLocalIdGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'exerciseLocalId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseLocalIdLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'exerciseLocalId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseLocalIdBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'exerciseLocalId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'exerciseServerId',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'exerciseServerId',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'exerciseServerId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdGreaterThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'exerciseServerId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdLessThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'exerciseServerId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      exerciseServerIdBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'exerciseServerId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      isCompletedEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'isCompleted',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      isSyncedEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'isSynced',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedLocalEqualTo(DateTime value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'lastModifiedLocal',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedLocalGreaterThan(
    DateTime value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'lastModifiedLocal',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedLocalLessThan(
    DateTime value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'lastModifiedLocal',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedLocalBetween(
    DateTime lower,
    DateTime upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'lastModifiedLocal',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'lastModifiedServer',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'lastModifiedServer',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'lastModifiedServer',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerGreaterThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'lastModifiedServer',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerLessThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'lastModifiedServer',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastModifiedServerBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'lastModifiedServer',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'lastSyncAttempt',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'lastSyncAttempt',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'lastSyncAttempt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptGreaterThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'lastSyncAttempt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptLessThan(
    DateTime? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'lastSyncAttempt',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      lastSyncAttemptBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'lastSyncAttempt',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      localIdEqualTo(Id value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'localId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      localIdGreaterThan(
    Id value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'localId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      localIdLessThan(
    Id value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'localId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      localIdBetween(
    Id lower,
    Id upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'localId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'notes',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'notes',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesEqualTo(
    String? value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'notes',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'notes',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'notes',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'notes',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      notesIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'notes',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'reps',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'reps',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'reps',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsGreaterThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'reps',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsLessThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'reps',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      repsBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'reps',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'serverId',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'serverId',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'serverId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdGreaterThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'serverId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdLessThan(
    int? value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'serverId',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      serverIdBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'serverId',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      setNumberEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'setNumber',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      setNumberGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'setNumber',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      setNumberLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'setNumber',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      setNumberBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'setNumber',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'syncError',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'syncError',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorEqualTo(
    String? value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'syncError',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'syncError',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'syncError',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'syncError',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncErrorIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'syncError',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncRetryCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'syncRetryCount',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncRetryCountGreaterThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'syncRetryCount',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncRetryCountLessThan(
    int value, {
    bool include = false,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'syncRetryCount',
        value: value,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncRetryCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'syncRetryCount',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusEqualTo(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'syncStatus',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusStartsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.startsWith(
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusEndsWith(
    String value, {
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.endsWith(
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.contains(
        property: r'syncStatus',
        value: value,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.matches(
        property: r'syncStatus',
        wildcard: pattern,
        caseSensitive: caseSensitive,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'syncStatus',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      syncStatusIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        property: r'syncStatus',
        value: '',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNull(
        property: r'weight',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(const FilterCondition.isNotNull(
        property: r'weight',
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightEqualTo(
    double? value, {
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.equalTo(
        property: r'weight',
        value: value,
        epsilon: epsilon,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightGreaterThan(
    double? value, {
    bool include = false,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.greaterThan(
        include: include,
        property: r'weight',
        value: value,
        epsilon: epsilon,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightLessThan(
    double? value, {
    bool include = false,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.lessThan(
        include: include,
        property: r'weight',
        value: value,
        epsilon: epsilon,
      ));
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterFilterCondition>
      weightBetween(
    double? lower,
    double? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    double epsilon = Query.epsilon,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(FilterCondition.between(
        property: r'weight',
        lower: lower,
        includeLower: includeLower,
        upper: upper,
        includeUpper: includeUpper,
        epsilon: epsilon,
      ));
    });
  }
}

extension LocalExerciseSetQueryObject
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QFilterCondition> {}

extension LocalExerciseSetQueryLinks
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QFilterCondition> {}

extension LocalExerciseSetQuerySortBy
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QSortBy> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByCompletedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'completedAt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByCompletedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'completedAt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByExerciseLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseLocalId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByExerciseLocalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseLocalId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByExerciseServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseServerId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByExerciseServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseServerId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByIsCompleted() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCompleted', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByIsCompletedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCompleted', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy> sortByNotes() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'notes', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByNotesDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'notes', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy> sortByReps() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'reps', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByRepsDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'reps', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySetNumber() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'setNumber', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySetNumberDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'setNumber', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByWeight() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'weight', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      sortByWeightDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'weight', Sort.desc);
    });
  }
}

extension LocalExerciseSetQuerySortThenBy
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QSortThenBy> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByCompletedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'completedAt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByCompletedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'completedAt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByDurationDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'duration', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByExerciseLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseLocalId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByExerciseLocalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseLocalId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByExerciseServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseServerId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByExerciseServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'exerciseServerId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByIsCompleted() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCompleted', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByIsCompletedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCompleted', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByLocalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy> thenByNotes() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'notes', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByNotesDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'notes', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy> thenByReps() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'reps', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByRepsDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'reps', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySetNumber() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'setNumber', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySetNumberDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'setNumber', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByWeight() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'weight', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QAfterSortBy>
      thenByWeightDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'weight', Sort.desc);
    });
  }
}

extension LocalExerciseSetQueryWhereDistinct
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct> {
  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByCompletedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'completedAt');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByDuration() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'duration');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByExerciseLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'exerciseLocalId');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByExerciseServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'exerciseServerId');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByIsCompleted() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isCompleted');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isSynced');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct> distinctByNotes(
      {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'notes', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct> distinctByReps() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'reps');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'serverId');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctBySetNumber() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'setNumber');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctBySyncError({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncError', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctBySyncStatus({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncStatus', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseSet, LocalExerciseSet, QDistinct>
      distinctByWeight() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'weight');
    });
  }
}

extension LocalExerciseSetQueryProperty
    on QueryBuilder<LocalExerciseSet, LocalExerciseSet, QQueryProperty> {
  QueryBuilder<LocalExerciseSet, int, QQueryOperations> localIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'localId');
    });
  }

  QueryBuilder<LocalExerciseSet, DateTime?, QQueryOperations>
      completedAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'completedAt');
    });
  }

  QueryBuilder<LocalExerciseSet, int?, QQueryOperations> durationProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'duration');
    });
  }

  QueryBuilder<LocalExerciseSet, int, QQueryOperations>
      exerciseLocalIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'exerciseLocalId');
    });
  }

  QueryBuilder<LocalExerciseSet, int?, QQueryOperations>
      exerciseServerIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'exerciseServerId');
    });
  }

  QueryBuilder<LocalExerciseSet, bool, QQueryOperations> isCompletedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isCompleted');
    });
  }

  QueryBuilder<LocalExerciseSet, bool, QQueryOperations> isSyncedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isSynced');
    });
  }

  QueryBuilder<LocalExerciseSet, DateTime, QQueryOperations>
      lastModifiedLocalProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalExerciseSet, DateTime?, QQueryOperations>
      lastModifiedServerProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalExerciseSet, DateTime?, QQueryOperations>
      lastSyncAttemptProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalExerciseSet, String?, QQueryOperations> notesProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'notes');
    });
  }

  QueryBuilder<LocalExerciseSet, int?, QQueryOperations> repsProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'reps');
    });
  }

  QueryBuilder<LocalExerciseSet, int?, QQueryOperations> serverIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'serverId');
    });
  }

  QueryBuilder<LocalExerciseSet, int, QQueryOperations> setNumberProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'setNumber');
    });
  }

  QueryBuilder<LocalExerciseSet, String?, QQueryOperations>
      syncErrorProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncError');
    });
  }

  QueryBuilder<LocalExerciseSet, int, QQueryOperations>
      syncRetryCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalExerciseSet, String, QQueryOperations>
      syncStatusProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncStatus');
    });
  }

  QueryBuilder<LocalExerciseSet, double?, QQueryOperations> weightProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'weight');
    });
  }
}
