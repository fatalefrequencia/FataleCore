BEGIN TRANSACTION;
DROP TABLE "CommunityFollows";

ALTER TABLE "Stations" ADD "IsChatEnabled" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "Stations" ADD "IsQueueEnabled" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260316054145_AddStationLiveFeatures', '10.0.2');

COMMIT;

