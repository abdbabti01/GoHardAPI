START TRANSACTION;


ALTER TABLE "Sessions" ADD COLUMN IF NOT EXISTS "ClientOperationId" uuid NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Sessions_UserId_ClientOperationId"
    ON "Sessions" ("UserId", "ClientOperationId")
    WHERE "ClientOperationId" IS NOT NULL;

CREATE TABLE IF NOT EXISTS "SessionCreateOperations" (
    "Id" serial NOT NULL,
    "UserId" integer NOT NULL,
    "ClientOperationId" uuid NOT NULL,
    "SessionId" integer NULL,
    "CompletedAt" timestamp without time zone NULL,
    "CanceledAt" timestamp without time zone NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_SessionCreateOperations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SessionCreateOperations_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SessionCreateOperations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SessionCreateOperations_SessionId" ON "SessionCreateOperations" ("SessionId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SessionCreateOperations_UserId_ClientOperationId" ON "SessionCreateOperations" ("UserId", "ClientOperationId");


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260903195625_AddSessionCreateOperationAndClientOperationId', '8.0.10');

COMMIT;
