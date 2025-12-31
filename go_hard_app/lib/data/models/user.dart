import 'package:json_annotation/json_annotation.dart';

part 'user.g.dart';

@JsonSerializable()
class User {
  final int id;
  final String name;
  final String email;
  final DateTime dateCreated;
  final double? height;
  final double? weight;
  final String? goals;

  User({
    required this.id,
    required this.name,
    required this.email,
    required this.dateCreated,
    this.height,
    this.weight,
    this.goals,
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);
  Map<String, dynamic> toJson() => _$UserToJson(this);
}
