/*
   Run once against the production EnglishCenter database before deploying the
   version that hashes OTPs. Existing active OTPs are intentionally invalidated
   because they were stored in clear text by older versions.
*/
UPDATE LoginOtps SET IsVerified = 1 WHERE IsVerified = 0;
UPDATE PasswordResetOtps SET IsVerified = 1 WHERE IsVerified = 0;

ALTER TABLE LoginOtps ALTER COLUMN Otp NVARCHAR(64) NOT NULL;
ALTER TABLE PasswordResetOtps ALTER COLUMN Otp NVARCHAR(64) NOT NULL;
