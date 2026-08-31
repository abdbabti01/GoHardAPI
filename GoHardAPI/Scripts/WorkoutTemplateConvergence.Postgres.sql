START TRANSACTION;


CREATE TABLE IF NOT EXISTS "WorkoutTemplates" (
    "Id" serial NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "ExercisesJson" text NOT NULL,
    "RecurrencePattern" character varying(20) NOT NULL,
    "DaysOfWeek" character varying(20) NULL,
    "IntervalDays" integer NULL,
    "EstimatedDuration" integer NULL,
    "Category" character varying(50) NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "UsageCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "LastUsedAt" timestamp with time zone NULL,
    "CreatedByUserId" integer NULL,
    "IsCustom" boolean NOT NULL DEFAULT FALSE,
    "Rating" double precision NULL,
    "RatingCount" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_WorkoutTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkoutTemplates_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "WorkoutTemplateRatings" (
    "Id" serial NOT NULL,
    "WorkoutTemplateId" integer NOT NULL,
    "UserId" integer NOT NULL,
    "Rating" double precision NOT NULL,
    "RatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_WorkoutTemplateRatings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkoutTemplateRatings_WorkoutTemplates_WorkoutTemplateId" FOREIGN KEY ("WorkoutTemplateId") REFERENCES "WorkoutTemplates" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorkoutTemplateRatings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE NO ACTION
);

CREATE INDEX IF NOT EXISTS "IX_WorkoutTemplates_CreatedByUserId" ON "WorkoutTemplates" ("CreatedByUserId");
CREATE INDEX IF NOT EXISTS "IX_WorkoutTemplates_IsCustom_IsActive_Category" ON "WorkoutTemplates" ("IsCustom", "IsActive", "Category");
CREATE INDEX IF NOT EXISTS "IX_WorkoutTemplateRatings_UserId" ON "WorkoutTemplateRatings" ("UserId");

-- Unlike the SQL Server branch (which must also skip the Phase3 script's
-- IX_WorkoutTemplateRatings_TemplateId_UserId), PostgreSQL has no hand-provisioned copy of
-- these tables, so there is no alternative index name to guard against: IF NOT EXISTS on the
-- EF name is sufficient. Worst case if that ever changes is a redundant second unique index
-- over the same columns, which still enforces the constraint correctly.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkoutTemplateRatings_WorkoutTemplateId_UserId" ON "WorkoutTemplateRatings" ("WorkoutTemplateId", "UserId");

ALTER TABLE "WorkoutTemplates" ADD COLUMN IF NOT EXISTS "IsPublic" boolean NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS "IX_WorkoutTemplates_IsPublic_Category" ON "WorkoutTemplates" ("IsPublic", "Category");


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260830215026_AddWorkoutTemplateVisibilityAndConvergeSchema', '8.0.10');

COMMIT;
