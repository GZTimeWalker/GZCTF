-- Deterministic legacy source fixture. It is intentionally read-only evidence.
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, Role)
VALUES
 ('00000000-0000-0000-0000-000000000001', 'legacy-admin', 'LEGACY-ADMIN', 'admin@legacy.test', 'ADMIN@LEGACY.TEST', TRUE, 'fixture', 'fixture', 'fixture', FALSE, FALSE, FALSE, 0, 0),
 ('00000000-0000-0000-0000-000000000002', 'legacy-user', 'LEGACY-USER', 'user@legacy.test', 'USER@LEGACY.TEST', TRUE, 'fixture', 'fixture', 'fixture', FALSE, FALSE, FALSE, 0, 1),
 ('00000000-0000-0000-0000-000000000003', 'legacy-monitor', 'LEGACY-MONITOR', 'monitor@legacy.test', 'MONITOR@LEGACY.TEST', TRUE, 'fixture', 'fixture', 'fixture', FALSE, FALSE, FALSE, 0, 2),
 ('00000000-0000-0000-0000-000000000004', 'legacy-banned', 'LEGACY-BANNED', 'banned@legacy.test', 'BANNED@LEGACY.TEST', TRUE, 'fixture', 'fixture', 'fixture', FALSE, FALSE, FALSE, 0, 3);

-- The source adapter consumes these rows from the legacy Game/GameChallenge tables.
-- Four modes, duplicate titles, hints, flags, attachments, container limits,
-- teams, participations, submissions and rankings are represented by the fixture manifest.
