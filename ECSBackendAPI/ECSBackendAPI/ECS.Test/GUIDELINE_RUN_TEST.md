# ECS.Test – Automated Test Suite for ECS Backend API

This project contains **unit** and **integration tests** for the ECS Backend API.  
It ensures correctness, reliability, and maintainability of the business logic, API endpoints, and validation rules.

## 📦 Technologies Used

- [xUnit](https://xunit.net/) – Test framework
- [Moq](https://github.com/moq/moq4) – Mocking dependencies
- [FluentAssertions](https://fluentassertions.com/) – Expressive assertions
- [coverlet](https://github.com/coverlet-coverage/coverlet) – Code coverage collection
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator) – HTML coverage reports
- [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) – Integration tests with `WebApplicationFactory`

## 📁 Project Structure

```
ECS.Test/
├── Controllers/                 # API endpoint tests (e.g., AuthController)
├── Services/                    # Business logic tests (e.g., LoginService)
├── Validators/                  # Request validation tests (e.g., LoginRequestValidator)
├── Helpers/                     # Test utilities (AsyncQueryable, etc.)
├── MockData/                    # Reusable test data factories
├── coverlet.runsettings         # Coverage settings (disables shadow copy)
├── ECS.Test.csproj              # Project file (targets .NET 10.0)
├── run-tests.bat                # Test & coverage runner script (Windows)
└── README.md                    # This file
```

## 🚀 Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or later)
- Windows OS (if using `run-tests.bat`; otherwise manual commands work on any OS)

## 🧪 Running Tests & Generating Coverage Reports

### Option 1: Use the Automated Script (Windows)

Simply run the batch file from the `ECS.Test` folder:

```cmd
.\run-tests.bat
```

**What it does:**
- Cleans previous `TestResults` folder
- Builds the solution in `Release` mode
- Runs all tests with **code coverage** (`XPlat Code Coverage`)
- Generates an **HTML coverage report** (dark theme) using ReportGenerator
- Automatically opens the report in your default browser
- Removes temporary folders that may contain machine‑specific information (privacy)

**Outputs:**
- Coverage report: `TestResults\coverage-report\index.html`
- Coverage badge: `TestResults\coverage-report\badge.svg`
- Raw test results: `TestResults\test-results.trx`

### Option 2: Manual Command Line (Cross‑platform)

Run the following commands from the `ECS.Test` directory:

```bash
# Clean previous results
rm -rf TestResults

# Run tests with coverage (without script)
dotnet test ECS.Test.csproj \
  --configuration Release \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory TestResults \
  --collect:"XPlat Code Coverage"

# Install ReportGenerator if not already installed
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  "-reports:TestResults/**/coverage.cobertura.xml" \
  "-targetdir:TestResults/coverage-report" \
  "-reporttypes:Html_Dark;Badges" \
  "-title:ECS Backend Code Coverage" \
  "-assemblyfilters:+ECS.*" \
  "-classfilters:+ECS.*"
```

## 📊 Understanding the Coverage Report

The generated HTML report (`index.html`) provides:

- **Overall line & branch coverage** percentages
- **Coverage by namespace, class, and method**
- **Source code view** with color‑coded lines (green = covered, red = uncovered)
- **Badge** (`badge.svg`) that can be embedded in the main README

To navigate: click on a package → class → method → see precise line coverage.

## 🔐 Security & Privacy

The `run-tests.bat` script **automatically deletes** any temporary folders that contain:
- Computer names (e.g., `DESKTOP-7DCHHJ6`)
- Timestamps
- GUIDs

Only the final `coverage-report` folder and `test-results.trx` remain. No sensitive information is exposed in the output.

## ✍️ Writing New Tests

### Naming Convention
`[Feature]_[Scenario]_[ExpectedResult]`

Example: `Login_ValidCredentials_ReturnsSuccessWithToken`

### Test Categories

| Category       | Location         | Description                                      |
|----------------|------------------|--------------------------------------------------|
| Controller     | `Controllers/`   | Tests API responses, status codes, and routing   |
| Service        | `Services/`      | Tests business logic using mocked repositories   |
| Validator      | `Validators/`    | Tests data annotation and FluentValidation rules |

### Example: Unit Test with Moq

```csharp
[Fact]
public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
{
    // Arrange
    var user = UserMockData.GetValidActiveUser();
    var request = LoginRequestMockData.GetValidRequest();
    _userRepoMock.Setup(x => x.Execute(It.IsAny<string>())).ReturnsAsync(user);
    _jwtMock.Setup(x => x.GenerateToken(user)).Returns("fake-token");

    // Act
    var result = await _loginService.Proccess(request);

    // Assert
    result.Should().NotBeNull();
    result.CodeMessage.Should().Be(GeneralCode.APP_MESSAGE_2000.ToString());
}
```

### Using MockData

Always use the `MockData` classes to keep test data consistent and maintainable:

- `LoginRequestMockData` – pre‑configured valid/invalid login requests
- `UserMockData` – active/inactive/doctor user instances

## ❓ Troubleshooting

| Problem                                      | Solution                                                                                  |
|----------------------------------------------|-------------------------------------------------------------------------------------------|
| `'Coverage' is not recognized`               | Re‑download the latest `run-tests.bat` (the script has been fixed).                       |
| No coverage file generated                   | Verify that `coverlet.collector` is referenced in `ECS.Test.csproj`.                      |
| ReportGenerator fails to install             | Run `dotnet tool install -g dotnet-reportgenerator-globaltool` manually.                  |
| Tests pass locally but fail on CI            | Run `dotnet clean` then rebuild; check for environment‑specific settings.                 |
| HTML report does not open automatically      | Open `TestResults\coverage-report\index.html` manually in your browser.                   |

## 🧹 Cleaning Everything

To remove all generated files (test results, coverage, binaries):

```cmd
dotnet clean
rmdir /s /q TestResults
```

## 📄 License

This test suite is part of the **ECS Backend** project and follows the same licensing terms as the main repository.