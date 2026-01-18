## Improve Test Reporting & Analytics

To preserve history, download the previous `allure-report/history` artifact before `allure generate`, then upload the new `allure-report` as an artifact. For ReportPortal, configure `ReportPortal.config.json` and ensure the adapter is enabled during `dotnet test`.

Integrate advanced reporting tools for better visibility and analytics:

- **Allure**: Add Allure test reporting to your .NET test projects.
	- Install the Allure adapter NuGet package: `dotnet add package Allure.NUnit` or `dotnet add package Allure.Xunit`.
	- Annotate your tests with `[AllureSuite]`, `[AllureFeature]`, etc. for richer reports.
	- After running tests, generate the report:
		```bash
		allure generate --clean
		allure open
		```
- Allure is ideal for local and team reporting, providing rich, interactive HTML reports with test history, attachments, and step-by-step details. It’s lightweight and easy to integrate.

- **ReportPortal**: For more advanced analytics and dashboards, consider integrating ReportPortal.
	- Add the ReportPortal adapter NuGet package: `dotnet add package ReportPortal.Xunit`.
	- Configure ReportPortal endpoint and project in your test settings.
    - ReportPortal is best for larger teams or CI/CD environments, offering dashboards, analytics, historical trends, and real-time reporting. It’s cloud/server-based and supports collaboration.

Track test metrics and trends over time:
- Use CI/CD pipelines to publish test reports after each run.
- Archive historical reports for regression analysis.

These tools will help you visualize test results, track failures, and improve overall test quality.
# PlcLab-Docker-OPC-Blazor: Test Coverage Improvements

## 1. TestRunOrchestrator Coverage
- Add unit tests for:
	- Empty test plans
	- Failed signal reads
	- Exception handling in test execution
	- Event dispatch logic (TestCaseStarted, TestCasePassed, TestCaseFailed)
- Add integration tests for:
	- Full test plan execution (multiple cases)
	- Database result storage and retrieval
	- Simulated OPC UA server interactions

## 2. Domain Logic Validation
- Add tests for domain entities:
	- TestPlan, TestCase, TestRun, TestResult, SignalSnapshot
- Add tests for TestCaseValidator:
	- Required signals validation
	- Limits and timeout validation
	- Error message correctness
- Add edge case tests:
	- Large test plans
	- Invalid or missing input data
	- Regression tests for previously reported bugs

## 3. UI and API Integration
- Add end-to-end tests for:
	- Creating, running, and retrieving test plans/results via API
	- UI workflows for test plan management

## 4. Layer and Architecture Rules
- Expand architectural tests:
	- Dependency checks between layers
	- SOLID principle enforcement

---

## TwinCat Integration (OpcUaSim)

For TwinCAT integration, here’s a focused, low-friction starter plan:

Identify simulatable PLC surface: list the key TwinCAT tasks/POUs/functions you need for tests (I/O maps, PLC variables, watchdogs).
Add a mock/sim OPC UA server profile that mirrors your TwinCAT nodes (namespaces, node IDs, data types). You already read signals by NodeId; we can add a small in-proc simulator for CI (e.g., lightweight UA server seeded with tags).
Wire CI to run TwinCAT-sim tests: add a dedicated TwinCAT.Sim.Tests project (or mark a subset in existing tests) that uses the simulator, and a GitHub Actions job matrix entry to run it.
Artifacts and logs: capture simulator logs and test TRX as artifacts for diagnosing PLC interaction failures.
Future step: hardware-in-the-loop: keep the same test harness but allow switching endpoint/credentials via env vars; CI runs the sim, self-hosted runner can point to real hardware.
If you want, I can scaffold:

A minimal OPC UA simulator helper for test runs (C# in tests folder).
A new test class that exercises a “read/write round-trip” against the simulator.
A CI job entry to run the sim tests.

## Automated Dependency Updates (Dependabot)

This project uses [Dependabot](https://docs.github.com/en/code-security/supply-chain-security/keeping-your-dependencies-updated-automatically/about-dependabot-version-updates) to keep dependencies and GitHub Actions up to date automatically:

- **NuGet**: Dependabot checks for new versions of NuGet packages in your .NET projects every week and opens pull requests to update them.
- **GitHub Actions**: Dependabot also checks for updates to GitHub Actions used in your workflow YAML files and proposes updates weekly.

Configuration is in `.github/dependabot.yml`. Keeping dependencies updated helps ensure security, stability, and access to the latest features and bug fixes.
