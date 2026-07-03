# Mzinga project analysis report

## Unit testing and code coverage

The **Mzinga** project utilizes the **MSTest** framework for its existing unit tests. Alongside the test framework, **Coverlet** is integrated as a cross-platform code coverage library for .NET. 

To streamline the execution and reporting of unit tests, a PowerShell script `run_tests.ps1` was created in the `Tests` directory. This script automates test discovery, runs tests with Coverlet code coverage data collection, and optionally generates an HTML visual report using **ReportGenerator**.

The script accepts the following arguments:
- **Target**: Determines which tests to run.
  - `original`: Executes only the existing unit tests inside the `Mzinga/src` project.
  - `new`: Executes only the new tests added inside the `Tests` folder.
  - `all` (default): Executes both the original and new tests sequentially.
- **-Visualize**: If provided, it generates an HTML coverage report using ReportGenerator and automatically opens it in the default web browser.
