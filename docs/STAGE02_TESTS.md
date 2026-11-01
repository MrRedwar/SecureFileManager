# Проверки этапа 02 — 01.11.2026

Windows / SDK 9.0.200. Полный прогон с SFM_ENVIRONMENT=Development и SFM_RUN_SQL_TESTS=1.

Всего 16: пройдено 16, ошибок 0, пропущено 0. Из них 8 реальных SQL-тестов.

| Тест | Результат |
|---|---|
| SecureFileManager.Tests.PersistenceTests.ContextRemoveSoftDeletesFileAndAuditIsAppendOnly | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.TlsAndEnvironmentPolicy(development: False, options: "Encrypt=True;TrustServerCertificate=False", allowed: True) | Passed |
| SecureFileManager.Tests.PersistenceTests.UnavailableDatabaseReturnsSafeInfrastructureFailure | Passed |
| SecureFileManager.Tests.PersistenceTests.UsernameUniqueCaseInsensitiveAndInputIsParameterized | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.CompositionRootResolvesFactoryAndAllRepositoriesWithoutOpeningSql | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.TlsAndEnvironmentPolicy(development: True, options: "Encrypt=False;TrustServerCertificate=True", allowed: False) | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.TlsAndEnvironmentPolicy(development: True, options: "Encrypt=True;TrustServerCertificate=True", allowed: True) | Passed |
| SecureFileManager.Tests.PersistenceTests.MetadataCrudSoftDeletePreservesAuditAndAllowsPathReuse | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.TlsAndEnvironmentPolicy(development: False, options: "Encrypt=True;TrustServerCertificate=True", allowed: False) | Passed |
| SecureFileManager.Tests.NavigationTests.NavigationSelectsEveryRegisteredSectionAndNotifiesBinding | Passed |
| SecureFileManager.Tests.NavigationTests.InvalidOrRepeatedSelectionDoesNotLoseCurrentScreenOrNotify | Passed |
| SecureFileManager.Tests.PersistenceTests.NullableFileAndUserAuditEventsPersistAndAreNotLeakedToOtherUsers | Passed |
| SecureFileManager.Tests.PersistenceTests.InvalidMetadataAndNonUtcDatesAreRejected | Passed |
| SecureFileManager.Tests.PersistenceTests.WrongOwnerAndStaleRowVersionCannotModifyMetadata | Passed |
| SecureFileManager.Tests.DatabaseConfigurationTests.DevelopmentCannotTrustRemoteCertificateOrTargetSystemDatabase | Passed |
| SecureFileManager.Tests.PersistenceTests.SqlForeignKeysChecksAndActivePathUniquenessAreEnforced | Passed |

Тестовая БД: SecureFileManagerTests_f24acc1b2016437ea5d6cf8df557ecc7.
Транзакции откатились: Users/Files/Operations = 0/0/0, TLS TRUE.
Полный TRX находится в tests/SecureFileManager.Tests/TestResults/stage02-final.trx (игнорируется Git).
