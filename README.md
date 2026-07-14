# "Mzinga" project analysis

This repository contains an analysis of the **Mzinga** project, conducted as a project assignment for the Master's course **Software Verification** at the **Faculty of Mathematics, University of Belgrade**.

**Mzinga** is an open-source implementation of the board game "**Hive**", built with C# and the .NET framework. A detailed description of the game and its source code are available in the [official repository](https://github.com/jonthysell/Mzinga). The analysis was performed on the `main` branch (commit SHA: `d57e5e903f5ca0e0eb6d7f08b25abea95f49bf2a`).

## Tools used

The following software verification techniques and tools were applied:
1. **MSTest**, **Coverlet** and **ReportGenerator** for unit testing, code coverage tracking and report generation.
2. **Stryker.NET** for mutation testing to evaluate unit test effectiveness.
3. **dotnet format** for code formatting and standardizing code style.
4. **Roslynator** for static code analysis to find bugs and design issues.
5. **dotnet-trace** for performance profiling to identify bottlenecks.
6. **ArchUnitNET** and **trxlog2html** for Architecture-as-Code (AaC) testing and report generation.

## Instructions

Each tool has an accompanying PowerShell script located in its respective directory. Detailed instructions for running these scripts and reproducing the results are located in the **[Project Analysis Report](./ProjectAnalysisReport.md)**.

## Conclusions

Based on the analysis, the following conclusions were drawn:
- **Unit testing and code coverage**: Introducing new unit tests improved the overall line coverage from 68.4% to over 83%.
- **Mutation testing**: Despite high code coverage, the test suite only caught 26% of injected mutations. This proves that high coverage does not guarantee quality, indicating that specific unit test assertions need to be stricter to actually test side-effects and component behavior.
- **Code formatting**: Scanning with `dotnet format` revealed only minor formatting inconsistencies and naming rule violations.
- **Static code analysis**: Scanning with `Roslynator` revealed a few minor issues (empty catch blocks, missing standard exception constructors). The underlying codebase aligns with .NET best practices and is very well-written and easy to maintain.
- **Performance profiling**: A computational bottleneck was identified in the `Mzinga.Core.Board` class, which spends significant time repeatedly recalculating valid moves inside the AI heuristic search tree. Implementing an incremental move generation could greatly improve performance.
- **Architecture-as-Code**: Architectural tests confirmed that the project has a good and robust structure. The core game rules (`Core`), execution logic (`Engine`) and user interface (`Viewer`) are completely isolated without any circular dependencies, preventing tightly coupled code.

## Author

**David Toholj 1013/2025 (*mi251013@alas.matf.bg.ac.rs*)**
