// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'local_exercise_template.dart';

// **************************************************************************
// IsarCollectionGenerator
// **************************************************************************

// coverage:ignore-file
// ignore_for_file: duplicate_ignore, non_constant_identifier_names, constant_identifier_names, invalid_use_of_protected_member, unnecessary_cast, prefer_const_constructors, lines_longer_than_80_chars, require_trailing_commas, inference_failure_on_function_invocation, unnecessary_parenthesis, unnecessary_raw_strings, unnecessary_null_checks, join_return_with_assignment, prefer_final_locals, avoid_js_rounded_ints, avoid_positional_boolean_parameters, always_specify_types

extension GetLocalExerciseTemplateCollection on Isar {
  IsarCollection<LocalExerciseTemplate> get localExerciseTemplates =>
      this.collection();
}

const LocalExerciseTemplateSchema = CollectionSchema(
  name: r'LocalExerciseTemplate',
  id: -6355508716764623324,
  properties: {
    r'category': PropertySchema(
      id: 0,
      name: r'category',
      type: IsarType.string,
    ),
    r'createdByUserId': PropertySchema(
      id: 1,
      name: r'createdByUserId',
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
    r'equipment': PropertySchema(
      id: 4,
      name: r'equipment',
      type: IsarType.string,
    ),
    r'imageUrl': PropertySchema(
      id: 5,
      name: r'imageUrl',
      type: IsarType.string,
    ),
    r'instructions': PropertySchema(
      id: 6,
      name: r'instructions',
      type: IsarType.string,
    ),
    r'isCustom': PropertySchema(id: 7, name: r'isCustom', type: IsarType.bool),
    r'isSynced': PropertySchema(id: 8, name: r'isSynced', type: IsarType.bool),
    r'lastModifiedLocal': PropertySchema(
      id: 9,
      name: r'lastModifiedLocal',
      type: IsarType.dateTime,
    ),
    r'lastModifiedServer': PropertySchema(
      id: 10,
      name: r'lastModifiedServer',
      type: IsarType.dateTime,
    ),
    r'lastSyncAttempt': PropertySchema(
      id: 11,
      name: r'lastSyncAttempt',
      type: IsarType.dateTime,
    ),
    r'muscleGroup': PropertySchema(
      id: 12,
      name: r'muscleGroup',
      type: IsarType.string,
    ),
    r'name': PropertySchema(id: 13, name: r'name', type: IsarType.string),
    r'serverId': PropertySchema(id: 14, name: r'serverId', type: IsarType.long),
    r'syncError': PropertySchema(
      id: 15,
      name: r'syncError',
      type: IsarType.string,
    ),
    r'syncRetryCount': PropertySchema(
      id: 16,
      name: r'syncRetryCount',
      type: IsarType.long,
    ),
    r'syncStatus': PropertySchema(
      id: 17,
      name: r'syncStatus',
      type: IsarType.string,
    ),
    r'videoUrl': PropertySchema(
      id: 18,
      name: r'videoUrl',
      type: IsarType.string,
    ),
  },
  estimateSize: _localExerciseTemplateEstimateSize,
  serialize: _localExerciseTemplateSerialize,
  deserialize: _localExerciseTemplateDeserialize,
  deserializeProp: _localExerciseTemplateDeserializeProp,
  idName: r'localId',
  indexes: {
    r'name': IndexSchema(
      id: 879695947855722453,
      name: r'name',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'name',
          type: IndexType.hash,
          caseSensitive: true,
        ),
      ],
    ),
    r'category': IndexSchema(
      id: -7560358558326323820,
      name: r'category',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'category',
          type: IndexType.hash,
          caseSensitive: true,
        ),
      ],
    ),
    r'muscleGroup': IndexSchema(
      id: 9219781228009971960,
      name: r'muscleGroup',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'muscleGroup',
          type: IndexType.hash,
          caseSensitive: true,
        ),
      ],
    ),
    r'isCustom': IndexSchema(
      id: -2539866887983386444,
      name: r'isCustom',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'isCustom',
          type: IndexType.value,
          caseSensitive: false,
        ),
      ],
    ),
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
        ),
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
        ),
      ],
    ),
  },
  links: {},
  embeddedSchemas: {},
  getId: _localExerciseTemplateGetId,
  getLinks: _localExerciseTemplateGetLinks,
  attach: _localExerciseTemplateAttach,
  version: '3.1.0+1',
);

int _localExerciseTemplateEstimateSize(
  LocalExerciseTemplate object,
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
  {
    final value = object.equipment;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.imageUrl;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.instructions;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  {
    final value = object.muscleGroup;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.name.length * 3;
  {
    final value = object.syncError;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.syncStatus.length * 3;
  {
    final value = object.videoUrl;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  return bytesCount;
}

void _localExerciseTemplateSerialize(
  LocalExerciseTemplate object,
  IsarWriter writer,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  writer.writeString(offsets[0], object.category);
  writer.writeLong(offsets[1], object.createdByUserId);
  writer.writeString(offsets[2], object.description);
  writer.writeString(offsets[3], object.difficulty);
  writer.writeString(offsets[4], object.equipment);
  writer.writeString(offsets[5], object.imageUrl);
  writer.writeString(offsets[6], object.instructions);
  writer.writeBool(offsets[7], object.isCustom);
  writer.writeBool(offsets[8], object.isSynced);
  writer.writeDateTime(offsets[9], object.lastModifiedLocal);
  writer.writeDateTime(offsets[10], object.lastModifiedServer);
  writer.writeDateTime(offsets[11], object.lastSyncAttempt);
  writer.writeString(offsets[12], object.muscleGroup);
  writer.writeString(offsets[13], object.name);
  writer.writeLong(offsets[14], object.serverId);
  writer.writeString(offsets[15], object.syncError);
  writer.writeLong(offsets[16], object.syncRetryCount);
  writer.writeString(offsets[17], object.syncStatus);
  writer.writeString(offsets[18], object.videoUrl);
}

LocalExerciseTemplate _localExerciseTemplateDeserialize(
  Id id,
  IsarReader reader,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  final object = LocalExerciseTemplate(
    category: reader.readStringOrNull(offsets[0]),
    createdByUserId: reader.readLongOrNull(offsets[1]),
    description: reader.readStringOrNull(offsets[2]),
    difficulty: reader.readStringOrNull(offsets[3]),
    equipment: reader.readStringOrNull(offsets[4]),
    imageUrl: reader.readStringOrNull(offsets[5]),
    instructions: reader.readStringOrNull(offsets[6]),
    isCustom: reader.readBoolOrNull(offsets[7]) ?? false,
    isSynced: reader.readBoolOrNull(offsets[8]) ?? false,
    lastModifiedLocal: reader.readDateTime(offsets[9]),
    lastModifiedServer: reader.readDateTimeOrNull(offsets[10]),
    lastSyncAttempt: reader.readDateTimeOrNull(offsets[11]),
    muscleGroup: reader.readStringOrNull(offsets[12]),
    name: reader.readString(offsets[13]),
    serverId: reader.readLongOrNull(offsets[14]),
    syncError: reader.readStringOrNull(offsets[15]),
    syncRetryCount: reader.readLongOrNull(offsets[16]) ?? 0,
    syncStatus: reader.readStringOrNull(offsets[17]) ?? 'pending_create',
    videoUrl: reader.readStringOrNull(offsets[18]),
  );
  object.localId = id;
  return object;
}

P _localExerciseTemplateDeserializeProp<P>(
  IsarReader reader,
  int propertyId,
  int offset,
  Map<Type, List<int>> allOffsets,
) {
  switch (propertyId) {
    case 0:
      return (reader.readStringOrNull(offset)) as P;
    case 1:
      return (reader.readLongOrNull(offset)) as P;
    case 2:
      return (reader.readStringOrNull(offset)) as P;
    case 3:
      return (reader.readStringOrNull(offset)) as P;
    case 4:
      return (reader.readStringOrNull(offset)) as P;
    case 5:
      return (reader.readStringOrNull(offset)) as P;
    case 6:
      return (reader.readStringOrNull(offset)) as P;
    case 7:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 8:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 9:
      return (reader.readDateTime(offset)) as P;
    case 10:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 11:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 12:
      return (reader.readStringOrNull(offset)) as P;
    case 13:
      return (reader.readString(offset)) as P;
    case 14:
      return (reader.readLongOrNull(offset)) as P;
    case 15:
      return (reader.readStringOrNull(offset)) as P;
    case 16:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 17:
      return (reader.readStringOrNull(offset) ?? 'pending_create') as P;
    case 18:
      return (reader.readStringOrNull(offset)) as P;
    default:
      throw IsarError('Unknown property with id $propertyId');
  }
}

Id _localExerciseTemplateGetId(LocalExerciseTemplate object) {
  return object.localId;
}

List<IsarLinkBase<dynamic>> _localExerciseTemplateGetLinks(
  LocalExerciseTemplate object,
) {
  return [];
}

void _localExerciseTemplateAttach(
  IsarCollection<dynamic> col,
  Id id,
  LocalExerciseTemplate object,
) {
  object.localId = id;
}

extension LocalExerciseTemplateQueryWhereSort
    on QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QWhere> {
  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhere>
  anyLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(const IdWhereClause.any());
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhere>
  anyIsCustom() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'isCustom'),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhere>
  anyIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'isSynced'),
      );
    });
  }
}

extension LocalExerciseTemplateQueryWhere
    on
        QueryBuilder<
          LocalExerciseTemplate,
          LocalExerciseTemplate,
          QWhereClause
        > {
  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  localIdEqualTo(Id localId) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.between(lower: localId, upper: localId),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
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

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  localIdGreaterThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.greaterThan(lower: localId, includeLower: include),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  localIdLessThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.lessThan(upper: localId, includeUpper: include),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  localIdBetween(
    Id lowerLocalId,
    Id upperLocalId, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.between(
          lower: lowerLocalId,
          includeLower: includeLower,
          upper: upperLocalId,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  nameEqualTo(String name) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'name', value: [name]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  nameNotEqualTo(String name) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'name',
                lower: [],
                upper: [name],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'name',
                lower: [name],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'name',
                lower: [name],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'name',
                lower: [],
                upper: [name],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  categoryIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'category', value: [null]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  categoryIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.between(
          indexName: r'category',
          lower: [null],
          includeLower: false,
          upper: [],
        ),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  categoryEqualTo(String? category) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'category', value: [category]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  categoryNotEqualTo(String? category) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'category',
                lower: [],
                upper: [category],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'category',
                lower: [category],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'category',
                lower: [category],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'category',
                lower: [],
                upper: [category],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  muscleGroupIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'muscleGroup', value: [null]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  muscleGroupIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.between(
          indexName: r'muscleGroup',
          lower: [null],
          includeLower: false,
          upper: [],
        ),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  muscleGroupEqualTo(String? muscleGroup) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(
          indexName: r'muscleGroup',
          value: [muscleGroup],
        ),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  muscleGroupNotEqualTo(String? muscleGroup) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'muscleGroup',
                lower: [],
                upper: [muscleGroup],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'muscleGroup',
                lower: [muscleGroup],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'muscleGroup',
                lower: [muscleGroup],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'muscleGroup',
                lower: [],
                upper: [muscleGroup],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  isCustomEqualTo(bool isCustom) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'isCustom', value: [isCustom]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  isCustomNotEqualTo(bool isCustom) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isCustom',
                lower: [],
                upper: [isCustom],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isCustom',
                lower: [isCustom],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isCustom',
                lower: [isCustom],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isCustom',
                lower: [],
                upper: [isCustom],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  isSyncedEqualTo(bool isSynced) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'isSynced', value: [isSynced]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  isSyncedNotEqualTo(bool isSynced) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isSynced',
                lower: [],
                upper: [isSynced],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isSynced',
                lower: [isSynced],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isSynced',
                lower: [isSynced],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'isSynced',
                lower: [],
                upper: [isSynced],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  syncStatusEqualTo(String syncStatus) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'syncStatus', value: [syncStatus]),
      );
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterWhereClause>
  syncStatusNotEqualTo(String syncStatus) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'syncStatus',
                lower: [],
                upper: [syncStatus],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'syncStatus',
                lower: [syncStatus],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'syncStatus',
                lower: [syncStatus],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'syncStatus',
                lower: [],
                upper: [syncStatus],
                includeUpper: false,
              ),
            );
      }
    });
  }
}

extension LocalExerciseTemplateQueryFilter
    on
        QueryBuilder<
          LocalExerciseTemplate,
          LocalExerciseTemplate,
          QFilterCondition
        > {
  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  categoryIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'category'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  categoryIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'category'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  categoryIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'category', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  categoryIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'category', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  createdByUserIdIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'createdByUserId'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  createdByUserIdIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'createdByUserId'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  createdByUserIdEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'createdByUserId', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  descriptionIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'description'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  descriptionIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'description'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  descriptionIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'description', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  descriptionIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'description', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'difficulty'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'difficulty'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'difficulty',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'difficulty',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'difficulty',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'difficulty', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  difficultyIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'difficulty', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'equipment'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'equipment'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'equipment',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'equipment',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'equipment',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'equipment', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  equipmentIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'equipment', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'imageUrl'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'imageUrl'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'imageUrl',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'imageUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'imageUrl',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'imageUrl', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  imageUrlIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'imageUrl', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'instructions'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'instructions'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'instructions',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'instructions',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'instructions',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'instructions', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  instructionsIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'instructions', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  isCustomEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'isCustom', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  isSyncedEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'isSynced', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedLocalEqualTo(DateTime value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'lastModifiedLocal', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedLocalGreaterThan(DateTime value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'lastModifiedLocal',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedLocalLessThan(DateTime value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'lastModifiedLocal',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedLocalBetween(
    DateTime lower,
    DateTime upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'lastModifiedLocal',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'lastModifiedServer'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'lastModifiedServer'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'lastModifiedServer', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerGreaterThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'lastModifiedServer',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerLessThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'lastModifiedServer',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastModifiedServerBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'lastModifiedServer',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'lastSyncAttempt'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'lastSyncAttempt'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'lastSyncAttempt', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptGreaterThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'lastSyncAttempt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptLessThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'lastSyncAttempt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  lastSyncAttemptBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'lastSyncAttempt',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  localIdEqualTo(Id value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'localId', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  localIdGreaterThan(Id value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'localId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  localIdLessThan(Id value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'localId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  localIdBetween(
    Id lower,
    Id upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'localId',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'muscleGroup'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'muscleGroup'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'muscleGroup',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'muscleGroup',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'muscleGroup',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'muscleGroup', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  muscleGroupIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'muscleGroup', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  nameIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'name', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  nameIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'name', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'serverId'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'serverId'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdEqualTo(int? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'serverId', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdGreaterThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'serverId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdLessThan(int? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'serverId',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  serverIdBetween(
    int? lower,
    int? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'serverId',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'syncError'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'syncError'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'syncError',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'syncError',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'syncError',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'syncError', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncErrorIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'syncError', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncRetryCountEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'syncRetryCount', value: value),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncRetryCountGreaterThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'syncRetryCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncRetryCountLessThan(int value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'syncRetryCount',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncRetryCountBetween(
    int lower,
    int upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'syncRetryCount',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'syncStatus',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'syncStatus',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'syncStatus',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'syncStatus', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  syncStatusIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'syncStatus', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'videoUrl'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'videoUrl'),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlEqualTo(String? value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlGreaterThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlLessThan(
    String? value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlBetween(
    String? lower,
    String? upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'videoUrl',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'videoUrl',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'videoUrl',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'videoUrl', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalExerciseTemplate,
    LocalExerciseTemplate,
    QAfterFilterCondition
  >
  videoUrlIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'videoUrl', value: ''),
      );
    });
  }
}

extension LocalExerciseTemplateQueryObject
    on
        QueryBuilder<
          LocalExerciseTemplate,
          LocalExerciseTemplate,
          QFilterCondition
        > {}

extension LocalExerciseTemplateQueryLinks
    on
        QueryBuilder<
          LocalExerciseTemplate,
          LocalExerciseTemplate,
          QFilterCondition
        > {}

extension LocalExerciseTemplateQuerySortBy
    on QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QSortBy> {
  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByCreatedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByDifficulty() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByDifficultyDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByEquipment() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'equipment', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByEquipmentDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'equipment', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByImageUrl() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'imageUrl', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByImageUrlDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'imageUrl', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByInstructions() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'instructions', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByInstructionsDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'instructions', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByIsCustom() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCustom', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByIsCustomDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCustom', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByMuscleGroup() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'muscleGroup', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByMuscleGroupDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'muscleGroup', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByVideoUrl() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'videoUrl', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  sortByVideoUrlDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'videoUrl', Sort.desc);
    });
  }
}

extension LocalExerciseTemplateQuerySortThenBy
    on QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QSortThenBy> {
  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByCategory() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByCategoryDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'category', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByCreatedByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdByUserId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByDescription() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByDescriptionDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'description', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByDifficulty() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByDifficultyDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'difficulty', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByEquipment() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'equipment', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByEquipmentDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'equipment', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByImageUrl() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'imageUrl', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByImageUrlDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'imageUrl', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByInstructions() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'instructions', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByInstructionsDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'instructions', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByIsCustom() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCustom', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByIsCustomDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isCustom', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByLocalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByMuscleGroup() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'muscleGroup', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByMuscleGroupDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'muscleGroup', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByName() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByNameDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'name', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByVideoUrl() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'videoUrl', Sort.asc);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QAfterSortBy>
  thenByVideoUrlDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'videoUrl', Sort.desc);
    });
  }
}

extension LocalExerciseTemplateQueryWhereDistinct
    on QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct> {
  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByCategory({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'category', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByCreatedByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'createdByUserId');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByDescription({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'description', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByDifficulty({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'difficulty', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByEquipment({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'equipment', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByImageUrl({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'imageUrl', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByInstructions({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'instructions', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByIsCustom() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isCustom');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isSynced');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByMuscleGroup({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'muscleGroup', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByName({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'name', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'serverId');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctBySyncError({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncError', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctBySyncStatus({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncStatus', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalExerciseTemplate, LocalExerciseTemplate, QDistinct>
  distinctByVideoUrl({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'videoUrl', caseSensitive: caseSensitive);
    });
  }
}

extension LocalExerciseTemplateQueryProperty
    on
        QueryBuilder<
          LocalExerciseTemplate,
          LocalExerciseTemplate,
          QQueryProperty
        > {
  QueryBuilder<LocalExerciseTemplate, int, QQueryOperations> localIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'localId');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  categoryProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'category');
    });
  }

  QueryBuilder<LocalExerciseTemplate, int?, QQueryOperations>
  createdByUserIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'createdByUserId');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  descriptionProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'description');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  difficultyProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'difficulty');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  equipmentProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'equipment');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  imageUrlProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'imageUrl');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  instructionsProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'instructions');
    });
  }

  QueryBuilder<LocalExerciseTemplate, bool, QQueryOperations>
  isCustomProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isCustom');
    });
  }

  QueryBuilder<LocalExerciseTemplate, bool, QQueryOperations>
  isSyncedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isSynced');
    });
  }

  QueryBuilder<LocalExerciseTemplate, DateTime, QQueryOperations>
  lastModifiedLocalProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalExerciseTemplate, DateTime?, QQueryOperations>
  lastModifiedServerProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalExerciseTemplate, DateTime?, QQueryOperations>
  lastSyncAttemptProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  muscleGroupProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'muscleGroup');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String, QQueryOperations> nameProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'name');
    });
  }

  QueryBuilder<LocalExerciseTemplate, int?, QQueryOperations>
  serverIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'serverId');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  syncErrorProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncError');
    });
  }

  QueryBuilder<LocalExerciseTemplate, int, QQueryOperations>
  syncRetryCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String, QQueryOperations>
  syncStatusProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncStatus');
    });
  }

  QueryBuilder<LocalExerciseTemplate, String?, QQueryOperations>
  videoUrlProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'videoUrl');
    });
  }
}
