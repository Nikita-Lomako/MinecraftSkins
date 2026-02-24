# RUN TESTS

## Recommended launch mode

Use the test project directly, not the whole solution:

```bat
dotnet test "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --no-restore -v minimal
```

This avoids frontend test discoverer issues from `minecraftskins.front.esproj`.

## Useful commands

Run only unit tests:

```bat
dotnet test "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --filter "FullyQualifiedName~UnitTests" -v minimal
```

Run only integration tests:

```bat
dotnet test "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --filter "FullyQualifiedName~IntegrationTests" -v minimal
```

Run in a single worker (for debugging flaky behavior):

```bat
dotnet test "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" -m:1 -v normal
```

Clean + rerun:

```bat
dotnet clean "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj"
dotnet test  "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --no-restore -v minimal
```

Collect coverage:

```bat
dotnet test "D:\учусь программировать\CSharp_GoodProjects\MinecraftSkins\MinecraftSkins.Tests\MinecraftSkins.Tests.csproj" --collect:"XPlat Code Coverage"
```

## Visual Studio / Rider

- Open `MinecraftSkins.Backend.slnf` for backend test runs.
- In Test Explorer, run tests from `MinecraftSkins.Tests` project scope, not solution-wide `Run All`.

## Typical failures and diagnostics

### 1) `JavascriptProjectTestDiscoverer` / `Method not found ... Microsoft.IO.Path.GetFileName(ReadOnlySpan<char>)`

Cause:
- solution-level discovery touches `minecraftskins.front.esproj`.

Fix:
- run tests by `MinecraftSkins.Tests.csproj` command;
- or open backend-only `MinecraftSkins.Backend.slnf`.

### 2) Many integration tests fail quickly

Cause:
- Docker/Testcontainers dependency is unavailable.

Check:

```bat
docker ps
docker info
```

If Docker is down, start Docker Desktop and rerun integration tests.

### 3) `xUnit1051` warnings

Cause:
- async APIs with `CancellationToken` are called without `TestContext.Current.CancellationToken`.

Fix:
- pass `TestContext.Current.CancellationToken` into all async methods that accept cancellation token.

### 4) Authentication/authorization integration failures (`401/403`)

Cause:
- test user/roles/token setup mismatch.

Check:
- user creation path in `IntegrationTestBase`;
- role assignment before token request;
- `Authorization` header is set on test client.

### 5) Database state leaking between tests

Cause:
- missing cleanup between tests.

Check:
- `InitializeAsync` in integration test classes;
- `CleanupAsync` is called before each test case.

