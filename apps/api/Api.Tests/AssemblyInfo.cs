using Xunit;

// All test classes share the same physical Postgres test database (vcard_test) via
// TestAppFactory/ResetDatabaseAsync (TRUNCATE ... RESTART IDENTITY CASCADE before each test).
// xUnit runs different test classes in parallel by default, which lets one class's truncate
// wipe rows another class's in-flight test just created — disable parallelization so the
// integration suite against a real database stays deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
