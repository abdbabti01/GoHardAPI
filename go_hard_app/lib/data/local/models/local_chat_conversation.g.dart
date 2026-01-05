// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'local_chat_conversation.dart';

// **************************************************************************
// IsarCollectionGenerator
// **************************************************************************

// coverage:ignore-file
// ignore_for_file: duplicate_ignore, non_constant_identifier_names, constant_identifier_names, invalid_use_of_protected_member, unnecessary_cast, prefer_const_constructors, lines_longer_than_80_chars, require_trailing_commas, inference_failure_on_function_invocation, unnecessary_parenthesis, unnecessary_raw_strings, unnecessary_null_checks, join_return_with_assignment, prefer_final_locals, avoid_js_rounded_ints, avoid_positional_boolean_parameters, always_specify_types

extension GetLocalChatConversationCollection on Isar {
  IsarCollection<LocalChatConversation> get localChatConversations =>
      this.collection();
}

const LocalChatConversationSchema = CollectionSchema(
  name: r'LocalChatConversation',
  id: 2471199554598539442,
  properties: {
    r'createdAt': PropertySchema(
      id: 0,
      name: r'createdAt',
      type: IsarType.dateTime,
    ),
    r'isArchived': PropertySchema(
      id: 1,
      name: r'isArchived',
      type: IsarType.bool,
    ),
    r'isSynced': PropertySchema(id: 2, name: r'isSynced', type: IsarType.bool),
    r'lastMessageAt': PropertySchema(
      id: 3,
      name: r'lastMessageAt',
      type: IsarType.dateTime,
    ),
    r'lastModifiedLocal': PropertySchema(
      id: 4,
      name: r'lastModifiedLocal',
      type: IsarType.dateTime,
    ),
    r'lastModifiedServer': PropertySchema(
      id: 5,
      name: r'lastModifiedServer',
      type: IsarType.dateTime,
    ),
    r'lastSyncAttempt': PropertySchema(
      id: 6,
      name: r'lastSyncAttempt',
      type: IsarType.dateTime,
    ),
    r'serverId': PropertySchema(id: 7, name: r'serverId', type: IsarType.long),
    r'syncError': PropertySchema(
      id: 8,
      name: r'syncError',
      type: IsarType.string,
    ),
    r'syncRetryCount': PropertySchema(
      id: 9,
      name: r'syncRetryCount',
      type: IsarType.long,
    ),
    r'syncStatus': PropertySchema(
      id: 10,
      name: r'syncStatus',
      type: IsarType.string,
    ),
    r'title': PropertySchema(id: 11, name: r'title', type: IsarType.string),
    r'type': PropertySchema(id: 12, name: r'type', type: IsarType.string),
    r'userId': PropertySchema(id: 13, name: r'userId', type: IsarType.long),
  },
  estimateSize: _localChatConversationEstimateSize,
  serialize: _localChatConversationSerialize,
  deserialize: _localChatConversationDeserialize,
  deserializeProp: _localChatConversationDeserializeProp,
  idName: r'localId',
  indexes: {
    r'type': IndexSchema(
      id: 5117122708147080838,
      name: r'type',
      unique: false,
      replace: false,
      properties: [
        IndexPropertySchema(
          name: r'type',
          type: IndexType.hash,
          caseSensitive: true,
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
  getId: _localChatConversationGetId,
  getLinks: _localChatConversationGetLinks,
  attach: _localChatConversationAttach,
  version: '3.1.0+1',
);

int _localChatConversationEstimateSize(
  LocalChatConversation object,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  var bytesCount = offsets.last;
  {
    final value = object.syncError;
    if (value != null) {
      bytesCount += 3 + value.length * 3;
    }
  }
  bytesCount += 3 + object.syncStatus.length * 3;
  bytesCount += 3 + object.title.length * 3;
  bytesCount += 3 + object.type.length * 3;
  return bytesCount;
}

void _localChatConversationSerialize(
  LocalChatConversation object,
  IsarWriter writer,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  writer.writeDateTime(offsets[0], object.createdAt);
  writer.writeBool(offsets[1], object.isArchived);
  writer.writeBool(offsets[2], object.isSynced);
  writer.writeDateTime(offsets[3], object.lastMessageAt);
  writer.writeDateTime(offsets[4], object.lastModifiedLocal);
  writer.writeDateTime(offsets[5], object.lastModifiedServer);
  writer.writeDateTime(offsets[6], object.lastSyncAttempt);
  writer.writeLong(offsets[7], object.serverId);
  writer.writeString(offsets[8], object.syncError);
  writer.writeLong(offsets[9], object.syncRetryCount);
  writer.writeString(offsets[10], object.syncStatus);
  writer.writeString(offsets[11], object.title);
  writer.writeString(offsets[12], object.type);
  writer.writeLong(offsets[13], object.userId);
}

LocalChatConversation _localChatConversationDeserialize(
  Id id,
  IsarReader reader,
  List<int> offsets,
  Map<Type, List<int>> allOffsets,
) {
  final object = LocalChatConversation(
    createdAt: reader.readDateTime(offsets[0]),
    isArchived: reader.readBoolOrNull(offsets[1]) ?? false,
    isSynced: reader.readBoolOrNull(offsets[2]) ?? false,
    lastMessageAt: reader.readDateTimeOrNull(offsets[3]),
    lastModifiedLocal: reader.readDateTime(offsets[4]),
    lastModifiedServer: reader.readDateTimeOrNull(offsets[5]),
    lastSyncAttempt: reader.readDateTimeOrNull(offsets[6]),
    serverId: reader.readLongOrNull(offsets[7]),
    syncError: reader.readStringOrNull(offsets[8]),
    syncRetryCount: reader.readLongOrNull(offsets[9]) ?? 0,
    syncStatus: reader.readStringOrNull(offsets[10]) ?? 'pending_create',
    title: reader.readString(offsets[11]),
    type: reader.readString(offsets[12]),
    userId: reader.readLong(offsets[13]),
  );
  object.localId = id;
  return object;
}

P _localChatConversationDeserializeProp<P>(
  IsarReader reader,
  int propertyId,
  int offset,
  Map<Type, List<int>> allOffsets,
) {
  switch (propertyId) {
    case 0:
      return (reader.readDateTime(offset)) as P;
    case 1:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 2:
      return (reader.readBoolOrNull(offset) ?? false) as P;
    case 3:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 4:
      return (reader.readDateTime(offset)) as P;
    case 5:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 6:
      return (reader.readDateTimeOrNull(offset)) as P;
    case 7:
      return (reader.readLongOrNull(offset)) as P;
    case 8:
      return (reader.readStringOrNull(offset)) as P;
    case 9:
      return (reader.readLongOrNull(offset) ?? 0) as P;
    case 10:
      return (reader.readStringOrNull(offset) ?? 'pending_create') as P;
    case 11:
      return (reader.readString(offset)) as P;
    case 12:
      return (reader.readString(offset)) as P;
    case 13:
      return (reader.readLong(offset)) as P;
    default:
      throw IsarError('Unknown property with id $propertyId');
  }
}

Id _localChatConversationGetId(LocalChatConversation object) {
  return object.localId;
}

List<IsarLinkBase<dynamic>> _localChatConversationGetLinks(
  LocalChatConversation object,
) {
  return [];
}

void _localChatConversationAttach(
  IsarCollection<dynamic> col,
  Id id,
  LocalChatConversation object,
) {
  object.localId = id;
}

extension LocalChatConversationQueryWhereSort
    on QueryBuilder<LocalChatConversation, LocalChatConversation, QWhere> {
  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhere>
  anyLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(const IdWhereClause.any());
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhere>
  anyIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        const IndexWhereClause.any(indexName: r'isSynced'),
      );
    });
  }
}

extension LocalChatConversationQueryWhere
    on
        QueryBuilder<
          LocalChatConversation,
          LocalChatConversation,
          QWhereClause
        > {
  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  localIdEqualTo(Id localId) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.between(lower: localId, upper: localId),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
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

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  localIdGreaterThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.greaterThan(lower: localId, includeLower: include),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  localIdLessThan(Id localId, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IdWhereClause.lessThan(upper: localId, includeUpper: include),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
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

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  typeEqualTo(String type) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'type', value: [type]),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  typeNotEqualTo(String type) {
    return QueryBuilder.apply(this, (query) {
      if (query.whereSort == Sort.asc) {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'type',
                lower: [],
                upper: [type],
                includeUpper: false,
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'type',
                lower: [type],
                includeLower: false,
                upper: [],
              ),
            );
      } else {
        return query
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'type',
                lower: [type],
                includeLower: false,
                upper: [],
              ),
            )
            .addWhereClause(
              IndexWhereClause.between(
                indexName: r'type',
                lower: [],
                upper: [type],
                includeUpper: false,
              ),
            );
      }
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  isSyncedEqualTo(bool isSynced) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'isSynced', value: [isSynced]),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
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

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
  syncStatusEqualTo(String syncStatus) {
    return QueryBuilder.apply(this, (query) {
      return query.addWhereClause(
        IndexWhereClause.equalTo(indexName: r'syncStatus', value: [syncStatus]),
      );
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterWhereClause>
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

extension LocalChatConversationQueryFilter
    on
        QueryBuilder<
          LocalChatConversation,
          LocalChatConversation,
          QFilterCondition
        > {
  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  createdAtEqualTo(DateTime value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'createdAt', value: value),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  isArchivedEqualTo(bool value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'isArchived', value: value),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtIsNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNull(property: r'lastMessageAt'),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtIsNotNull() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        const FilterCondition.isNotNull(property: r'lastMessageAt'),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtEqualTo(DateTime? value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'lastMessageAt', value: value),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtGreaterThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'lastMessageAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtLessThan(DateTime? value, {bool include = false}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'lastMessageAt',
          value: value,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  lastMessageAtBetween(
    DateTime? lower,
    DateTime? upper, {
    bool includeLower = true,
    bool includeUpper = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'lastMessageAt',
          lower: lower,
          includeLower: includeLower,
          upper: upper,
          includeUpper: includeUpper,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
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
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'title',
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
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'title',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'title',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'title', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  titleIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'title', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeEqualTo(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeGreaterThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(
          include: include,
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeLessThan(
    String value, {
    bool include = false,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.lessThan(
          include: include,
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeBetween(
    String lower,
    String upper, {
    bool includeLower = true,
    bool includeUpper = true,
    bool caseSensitive = true,
  }) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.between(
          property: r'type',
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
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeStartsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.startsWith(
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeEndsWith(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.endsWith(
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeContains(String value, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.contains(
          property: r'type',
          value: value,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeMatches(String pattern, {bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.matches(
          property: r'type',
          wildcard: pattern,
          caseSensitive: caseSensitive,
        ),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeIsEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'type', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  typeIsNotEmpty() {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.greaterThan(property: r'type', value: ''),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
  userIdEqualTo(int value) {
    return QueryBuilder.apply(this, (query) {
      return query.addFilterCondition(
        FilterCondition.equalTo(property: r'userId', value: value),
      );
    });
  }

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

  QueryBuilder<
    LocalChatConversation,
    LocalChatConversation,
    QAfterFilterCondition
  >
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

extension LocalChatConversationQueryObject
    on
        QueryBuilder<
          LocalChatConversation,
          LocalChatConversation,
          QFilterCondition
        > {}

extension LocalChatConversationQueryLinks
    on
        QueryBuilder<
          LocalChatConversation,
          LocalChatConversation,
          QFilterCondition
        > {}

extension LocalChatConversationQuerySortBy
    on QueryBuilder<LocalChatConversation, LocalChatConversation, QSortBy> {
  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByCreatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByIsArchived() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isArchived', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByIsArchivedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isArchived', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastMessageAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastMessageAt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastMessageAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastMessageAt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByTitle() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'title', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByTitleDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'title', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByType() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByTypeDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  sortByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.desc);
    });
  }
}

extension LocalChatConversationQuerySortThenBy
    on QueryBuilder<LocalChatConversation, LocalChatConversation, QSortThenBy> {
  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByCreatedAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'createdAt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByIsArchived() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isArchived', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByIsArchivedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isArchived', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByIsSyncedDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'isSynced', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastMessageAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastMessageAt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastMessageAtDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastMessageAt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastModifiedLocalDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedLocal', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastModifiedServerDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastModifiedServer', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLastSyncAttemptDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'lastSyncAttempt', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLocalId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByLocalIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'localId', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByServerIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'serverId', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncError() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncErrorDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncError', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncRetryCountDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncRetryCount', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncStatus() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenBySyncStatusDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'syncStatus', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByTitle() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'title', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByTitleDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'title', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByType() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByTypeDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'type', Sort.desc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.asc);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QAfterSortBy>
  thenByUserIdDesc() {
    return QueryBuilder.apply(this, (query) {
      return query.addSortBy(r'userId', Sort.desc);
    });
  }
}

extension LocalChatConversationQueryWhereDistinct
    on QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct> {
  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByCreatedAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'createdAt');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByIsArchived() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isArchived');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByIsSynced() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'isSynced');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByLastMessageAt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastMessageAt');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByLastModifiedLocal() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByLastModifiedServer() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByLastSyncAttempt() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByServerId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'serverId');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctBySyncError({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncError', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctBySyncRetryCount() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctBySyncStatus({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'syncStatus', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByTitle({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'title', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByType({bool caseSensitive = true}) {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'type', caseSensitive: caseSensitive);
    });
  }

  QueryBuilder<LocalChatConversation, LocalChatConversation, QDistinct>
  distinctByUserId() {
    return QueryBuilder.apply(this, (query) {
      return query.addDistinctBy(r'userId');
    });
  }
}

extension LocalChatConversationQueryProperty
    on
        QueryBuilder<
          LocalChatConversation,
          LocalChatConversation,
          QQueryProperty
        > {
  QueryBuilder<LocalChatConversation, int, QQueryOperations> localIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'localId');
    });
  }

  QueryBuilder<LocalChatConversation, DateTime, QQueryOperations>
  createdAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'createdAt');
    });
  }

  QueryBuilder<LocalChatConversation, bool, QQueryOperations>
  isArchivedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isArchived');
    });
  }

  QueryBuilder<LocalChatConversation, bool, QQueryOperations>
  isSyncedProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'isSynced');
    });
  }

  QueryBuilder<LocalChatConversation, DateTime?, QQueryOperations>
  lastMessageAtProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastMessageAt');
    });
  }

  QueryBuilder<LocalChatConversation, DateTime, QQueryOperations>
  lastModifiedLocalProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedLocal');
    });
  }

  QueryBuilder<LocalChatConversation, DateTime?, QQueryOperations>
  lastModifiedServerProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastModifiedServer');
    });
  }

  QueryBuilder<LocalChatConversation, DateTime?, QQueryOperations>
  lastSyncAttemptProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'lastSyncAttempt');
    });
  }

  QueryBuilder<LocalChatConversation, int?, QQueryOperations>
  serverIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'serverId');
    });
  }

  QueryBuilder<LocalChatConversation, String?, QQueryOperations>
  syncErrorProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncError');
    });
  }

  QueryBuilder<LocalChatConversation, int, QQueryOperations>
  syncRetryCountProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncRetryCount');
    });
  }

  QueryBuilder<LocalChatConversation, String, QQueryOperations>
  syncStatusProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'syncStatus');
    });
  }

  QueryBuilder<LocalChatConversation, String, QQueryOperations>
  titleProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'title');
    });
  }

  QueryBuilder<LocalChatConversation, String, QQueryOperations> typeProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'type');
    });
  }

  QueryBuilder<LocalChatConversation, int, QQueryOperations> userIdProperty() {
    return QueryBuilder.apply(this, (query) {
      return query.addPropertyName(r'userId');
    });
  }
}
