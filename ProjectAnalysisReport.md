# **Mzinga project analysis report**

## **Unit testing and code coverage**

The **Mzinga** project utilizes the **MSTest** framework for its existing unit tests. Alongside the test framework, **Coverlet** is integrated as a cross-platform code coverage library for .NET. 

To streamline the execution and reporting of unit tests, a PowerShell script [run_unit_tests.ps1](./Unit Tests/run_unit_tests.ps1) was created in the [Unit Tests](./Unit Tests) directory. This script automates test discovery, runs tests with Coverlet code coverage data collection, and generates an HTML visual report using **ReportGenerator**.

The script accepts the following arguments:
- **`Target`**: Indicates which context of tests to evaluate. Results and reports are separately cached under `Results/{Target}` and `CoverageReport/{Target}` directories.
  - `original`: Evaluates only existing unit tests located in the `Mzinga/src` folder.
  - `new`: Evaluates only the new tests located in the `Tests` folder.
  - `all` (default): Evaluates both original and new tests.
- **`-Visualize`**: Generates an HTML coverage report based on the selected target's results and opens it in the browser.
- **`-Rerun`**: Deletes the previous results and runs them from scratch. By default, the script skips test execution if results already exist for the selected target.
- **`-Clean`**: Deletes all test results and coverage reports for all targets. If provided, all other arguments are ignored.

### **Current coverage baseline**

Before introducing additional unit tests to the codebase, it is crucial to establish the current test coverage baseline. We will run only the original tests and generate the visual report:

```powershell
& ".\Unit Tests\run_unit_tests.ps1" original -Visualize
```

[Image 1](#img1) displays general code coverage results, before introducing new tests. We can see that around two thirds of lines (68%) and branches (64%) are covered, which is a good start but can be improved.

<figure id="img1" style="text-align: center;">
  <img src="Unit Tests/Images/report1.png" alt="Original code coverage general results">
  <figcaption>Image 1: Original code coverage general results</figcaption>
</figure>

[Image 2](#img2) shows detailed code coverage by main classes. We can see that main game and AI logic are covered with high coverage percentage. This includes `Mzinga.Core.Board` which covers board state, `Mzinga.Core.Move` which validates and executes moves, `Mzinga.Core.AI.GameAI` which implements algorithms for game AI, as well as some classes with 100% code coverage such as `PieceMetrics`.

On the other hand, there are classes that are not covered as much or not covered at all. The goal is to reach over 80% total code coverage. To achieve this, testing should focus on the following areas:

1. `Mzinga.Engine` namespace: Class `Mzinga.Engine.Engine` is not covered at all. Testing basic interactions, Universal Hive Protocol commands, and engine states is important. Furthermore, `Mzinga.Engine.EngineConfig` is only covered by 39.5%. Testing the parsing of default options, validations, and profile configs will also provide a significant coverage bump.
2. `Exception` classes: Exception classes such as `CommandException`, `GameOverException`, `NoBoardException` `PerfInvalidDepthException`, and `UndoInvalidNumberOfMovesException` have 0% coverage. This clearly shows that error handling and invalid state edge cases are currently not being verified by the test suite.
3. Core components with partial coverage: Classes like `Mzinga.Core.GameMetadata` (50% coverage) and `Mzinga.Core.AI.MetricWeights` (64.3% coverage) have gaps that are not tested.
4. Utility classes: Simple utility structures like `Mzinga.AppInfo`, `Mzinga.VersionUtils`, `Mzinga.Core.CacheMetricsSet` and `Mzinga.Core.MoveSet` currently have 0% coverage but require minimal effort to verify.

<figure id="img2" style="text-align: center;">
  <img src="Unit Tests/Images/report2.png" alt="Original code coverage detailed results">
  <figcaption>Image 2: Original code coverage detailed results</figcaption>
</figure>

### **Adding new tests**

A new unit test project named [Mzinga.Tests.New](./Unit Tests/Mzinga.Tests.New) was initialized within the `Tests` directory. This MSTest project will hold new test cases focused on covering lines and branches the original tests do not cover. Tests were organized by classes:

#### **EngineConfig**
This class handles engine configurations, validation and storing parameters like `MaxHelperThreads` and `GameAI` metrics.
-   **`EngineConfig_DefaultConstructor`**: Verifies that when an `EngineConfig` is initialized using its default constructor, essential properties like `MetricWeightSet` are instantiated and core fallbacks are behaving as expected.
-   **`EngineConfig_LoadConfig_ValidXmlStream`**: Tests the parsing functionality of the config system. It provides an in-memory stream containing an XML configuration matching the `GameAI` schema and verifies that options like `MaxBranchingFactor`, `MaxHelperThreads`, and enumerations like `PonderDuringIdle` are correctly read without errors and mapped on the resulting configuration state.
-   **`EngineConfig_LoadConfig_InvalidXmlStream`**: Checks the robustness of the parsing mechanism by intentionally providing malformed XML config payload (missing closing tag). It tests if the code throws a `System.Xml.XmlException` to avoid application corruption.
-   **`EngineConfig_ParseMaxHelperThreads`**: Checks variations of parsing `MaxHelperThreads` edge values.
-   **`EngineConfig_GetOptionsClone`**: Validates deep cloning for game settings ensuring config state boundaries.
-   **`EngineConfig_CopyOptionsFrom`**: Ensures properties are copied successfully between varying configuration states.
-   **`EngineConfig_SaveConfig`**: Serializes active configuration internally and confirms that the output stream stores configurations appropriately.

#### **Engine**
The main coordinator handling inputs and output stream communications based on Universal Hive Protocol. The real target in code is a standard output stream, so all Engine tests utilize a `MockConsoleOut` delegate. This allows simulating and observing terminal output without requiring real environment attachments, validating if correct strings are yielded safely and checking if states internally persist correctly per command.
-   **`Engine_Constructor`**: Confirms safe initialization without active games.
-   **`Engine_ParseCommand_Info`**: Validates the standard "info" command parses and yields correct output engine identification strings.
-   **`Engine_ParseCommand_Help`**: Triggers different variations of the "help" protocol. It checks the basic `help` and detailed `help command_name` commands and asserts that invalid variations return a `CommandException`.
-   **`Engine_ParseCommand_NewGame` / `Engine_ParseCommand_NewGameWithGameType` / `Engine_ParseCommand_NewGameWithGameString`**: Dispatches new game requests ensuring boards and game states are properly instantiated internally with default settings, specific game types or state strings.
-   **`Engine_ParseCommand_Exit`**: Tests if the "exit" command triggers the shutdown request.
-   **`Engine_ParseCommand_Invalid`**: Ensures that invalid commands produce an error string output.
-   **`Engine_ParseCommand_Licences`**: Checks the specific licenses commands and verifies that configured license contents are emitted on execution.
-   **`Engine_ParseCommand_NoBoardException` / `Engine_ParseCommand_Pass_NoBoardWhenNoGame` / `Engine_ParseCommand_Play_NoBoardWhenNoGame`**: Verifies that issuing commands requiring an active board (`validmoves`, `play`, `pass`) before calling `newgame` outputs the expected "No game in progress" error message.
-   **`Engine_ParseCommand_PlayWithoutArgs` / `Engine_ParseCommand_UndoWithoutArgs`**: Checks responses against playing and undoing actions when completely missing target parameters.
-   **`Engine_ParseCommand_UndoInvalidNumberOfMovesException`**: Tests the error handling when attempting to `undo` more moves than have been played, ensuring the specific "Unable to undo N moves" error message.
-   **`Engine_ParseCommand_PlayUndo`**: Confirms correct undoing of the last played move.
-   **`Engine_ParseCommand_PerftInvalidDepthException` / `Engine_ParseCommand_PerftWithArgs`**: Checks limits on `perft` command depth, ensuring failure prints for invalid negative values and accepting valid depth values.
-   **`Engine_ParseCommand_BestMoveTime`**: Confirms executing constrained limited best moves queries returns successfully against initialized boards. 
-   **`Engine_ParseCommand_ArgumentNullException`**: Validates the core engine loop against empty inputs, asserting that an `ArgumentNullException` is directly thrown on malformed arguments.
-   **`Engine_ParseCommand_BestMoveWithInvalidArgs_CommandException` / `Engine_ParseCommand_OptionsWithInvalidArgs_CommandException`**: Checks if misformatted protocol variations yield expected error messages.
-   **`Engine_ParseCommand_Options` / `Engine_ParseCommand_OptionsGet` / `Engine_ParseCommand_OptionsSet`**: An aggregate check around `options` command variations. Assigns valid string configurations like variables, depths, flag bounds to confirm states propagate directly to the Engine's main config structure. Incorporates multithreading behavior checks, bounds checks and exception logic flow ensuring invalid inputs are handled as an `ArgumentException`.

#### **GameMetadata**
Manages metadata tags embedded inside games containing event specifics, usernames, results and move commentaries.
-   **`GameMetaData_SetTag` / `GameMetaData_GetTag`**: Checks boundary validation on adding fields and verifying string consistency on retrieval.
-   **`GameMetaData_SetTag_ArgumentNullException`**: Checks exception propagation when providing null tag arguments.
-   **`GameMetaData_MoveCommentary`**: Tests move comments are correctly added.
-   **`GameMetaData_Clone`**: Checks tags and structures deep copying.

#### **AppInfo**
Utility container storing read-only product and assembly version info.
-   **`AppInfo_Properties`**: Validates that all metadata strings (including Hive and MIT internal License Texts, Assembly paths and Version numbers) are non-null and correctly populated.

#### **MoveSet**
Utility container holding distinct potential movements in memory using arrays.
-   **`MoveSet_Add` / `MoveSet_Clear`**: Checks if adding and clearing moves to the move set works correctly. Relies on reflection, because `MoveSet` methods are private

### **Results and comparison**

After adding the new unit tests, we ran the test suite against both the original and new tests using the script:

```powershell
& ".\Unit Tests\run_unit_tests.ps1" all -Visualize
```

The general coverage results have significantly improved. As we can see in [Image 3](#img3), line coverage increased from 68.4% to 83.3%, and branch coverage increased from 64.4% to 80.4%. This successfully achieved our goal of reaching over 80% total code coverage.

<figure id="img3" style="text-align: center;">
  <img src="Unit Tests/Images/report3.png" alt="Final code coverage general results">
  <figcaption>Image 3: Final code coverage general results</figcaption>
</figure>

Looking at the detailed results for the classes where tests were added, we achieved substantial improvements:
- **`Mzinga.Engine.Engine`** increased from 0% to 66.4% line coverage.
- **`Mzinga.Engine.EngineConfig`** increased from 39.5% to 83.5% line coverage.
- **`CommandException`**, **`NoBoardException`**, **`PerfInvalidDepthException`** and **`UndoInvalidNumberOfMovesException`** all increased from 0% to 100% line coverage.
- **`Mzinga.Core.GameMetadata`**: increased from 50% to 66.1% line coverage.
- **`Mzinga.AppInfo`** increased from 0% to 100% line coverage.
- **`Mzinga.VersionUtils`** increased from 0% to 52.9% line coverage.
- **`Mzinga.Core.MoveSet`** increased from 0% to 45% line coverage.

<figure id="img4" style="text-align: center;">
  <img src="Unit Tests/Images/report4.png" alt="Final code coverage detailed results">
  <figcaption>Image 4: Final code coverage detailed results</figcaption>
</figure>

## **Mutation testing**

**Mutation testing** is a technique used to evaluate the quality of existing unit tests. It works by injecting small, artificial defects (**mutations**) into the source code, such as changing `>` to `>=`, modifying logic or altering return values. A reliable test suite should fail (**"kill" the mutant**) when these alterations are introduced. If the tests still pass (**the mutant "survives"**), it indicates a weakness or gap in the test suite that needs addressing.

### **Stryker.NET**

To perform mutation testing on the Mzinga project, we will use **Stryker.NET** (`dotnet-stryker`). Stryker is a popular, open-source mutation testing framework specifically designed for .NET and C# applications. It automates the generation of mutants, runs the tests against them and produces reports highlighting which mutants survived and where potential gaps in the test suite exist.

To simplify the execution of Stryker, a PowerShell script ([run_mutation_test.ps1](./Mutation Testing/run_mutation_test.ps1)) was created. This script handles Stryker installation, runs the tool and manages reporting. Because running mutation testing over the entire project can take extremely long (sometimes days), the execution script was internally configured to focus exclusively on the `Mzinga.Engine` namespace. Specifically, it targets the `Engine` and `EngineConfig` classes, as these are one of the classes where we previously introduced new unit tests.

The script accepts the following arguments:
- **`-Rerun`**: Forces Stryker to execute again and ignores previously generated results for the testing target.
- **`-Visualize`**: Automatically opens the generated HTML report in the default browser once execution completes.

### **Results**

We will run the execution script with visualizing the results:

```powershell
& ".\Mutation Testing\run_mutation_test.ps1" -Visualize
```

Before we review the results, here's a brief explanation of the terms found in the report:
- **Killed**: Mutants that caused a test to fail.
- **Survived**: Mutants that did not cause any test to fail, indicating gaps in test assertions.
- **Timeout**: Mutants that caused tests to run infinitely or exceed time limits, usually due to infinite loops.
- **No coverage**: Mutants in code that is not executed by any unit test.
- **Ignored**: Mutants explicitly skipped by configuration.
- **Runtime errors**: Mutants that caused application crashes.
- **Compile errors**: Mutants that broke the compilation process.
- **Detected**: The sum of killed and timeout mutants.
- **Undetected**: The sum of survived and no coverage mutants.
- **Total**: The total number of valid mutants.

The mutation report (Images [5](#img5) and [6](#img6)) show the overall mutation score for the `Engine` classes is **26.24%**. This is a poor result, indicating that the unit tests are not effectively catching most of the injected defects.

Looking at the detailed breakdown:
- **`EngineConfig.cs`** achieved a mutation score of **51.82%** (19 out of 191 mutants killed).
- **`Engine.cs`** achieved a mutation score of **18.39%** (3 out of 558 mutants killed).

<figure id="img5" style="text-align: center;">
  <img src="./Mutation Testing/Images/report1.png" alt="Mutation testing results (Mutants)">
  <figcaption>Image 5: Mutation testing results (Mutants)</figcaption>
</figure> 

<figure id="img6" style="text-align: center;">
  <img src="./Mutation Testing/Images/report2.png" alt="Mutation testing results (Tests">
  <figcaption>Image 6: Mutation testing results (Tests)</figcaption>
</figure>

To further improve these tests, new test boundaries should be added to assert specific state behaviors based on those surviving mutants. If we go into a specific file we can check the "Survived" option ([Image 7](#img7)), which will mark the lines that were mutated and show what change was made. This tells us exactly which additional assertions we need to include.

<figure id="img7" style="text-align: center;">
  <img src="./Mutation Testing/Images/report3.png" alt="Setting up mutant search">
  <figcaption>Image 7: Setting up mutant search</figcaption>
</figure>

We can see that the common mutants introduced include switching function calls with an empty command, negating expressions, changing string messages etc. (Images [8](#img8), [9](#img9) and [10](#img10)).

<figure id="img8" style="text-align: center;">
  <img src="./Mutation Testing/Images/report4.png" alt="Survived mutant example 1">
  <figcaption>Image 8: Survived mutant example 1</figcaption>
</figure>

<figure id="img9" style="text-align: center;">
  <img src="./Mutation Testing/Images/report5.png" alt="Survived mutant example 2">
  <figcaption>Image 9: Survived mutant example 2</figcaption>
</figure>

<figure id="img10" style="text-align: center;">
  <img src="./Mutation Testing/Images/report6.png" alt="Survived mutant example 3">
  <figcaption>Image 10: Survived mutant example 3</figcaption>
</figure>

Based on the mutants generated and tests covering them, we can think about possible solutions for making the tests better.

1. **Empty string mutants in Exception constructors**: In the test `Engine_ParseCommand_PerftInvalidDepthException`, the exception message string (`"Unable to calculate perft({0})."`) was changed to an empty string, but the mutant survived. This means the test only verifies if the `PerftInvalidDepthException` is thrown, but does not verify its content. We should add an assertion to explicitly check that the `Exception.Message` property matches the expected error text.
2. **Logical condition inversions**: In `Engine_ParseCommand_PerftWithArgs` and `Engine_ParseCommand_PerftInvalidDepthException`, an `if (_board is null)` condition was mutated to `if (_board is not null)`. The mutant survived, meaning the tests execute the logic but do not strictly assert the engine's behavior differences between having an active board and not having one. We should assert the specific console stream output messages for both scenarios to catch inverted logical flows.
3. **Removed function calls**: Function calls like `StopPonder()` inside `BestMove` or `Play` were replaced with empty commands (`;`), yet the tests still passed. This indicates the current tests only verify the main routine outcome but ignore secondary state. We should add assertions to check the specific side-effects of those operations, such as verifying that the engine state transitions correctly and any background pondering tasks are tracked and actually terminated.
4. **State flag inversions**: In `Engine_ParseCommand_Exit`, the boolean assignment `ExitRequested = true` was mutated to `ExitRequested = false`. The test verifies that the command parses without crashing, but forgets to assert that the `ExitRequested` boolean flag flips to `true` on the instantiated engine object. We should assert the `ExitRequested` property state after processing an "exit" command.
5. **Data string deletions**: In `Engine_ParseCommand_OptionsGet` targeting `GetMaxBranchingFactorValue`, the string literal `type = "int";` was mutated to an empty string `type = "";`. This survival means the test asks for the options list but does not explicitly validate the metadata content format of those options. We need to parse the generated output string and strictly assert that the property types (like `"int"`, `"string"`, `"enum"`) are present in the output.

## **Code formatting**

The original Mzinga codebase contains an [.editorconfig](./Mzinga/src/.editorconfig) file in its `src/` directory. This existing configuration only disables a few specific C# features, such as implicit object creation, range operators and switch expressions, but it completely lacks standard formatting rules to ensure code consistency.

To properly format the code and enforce stricter style checks, the built-in **`dotnet format`** tool from the .NET SDK is used. A new, custom configuration ([.editorconfig](./Code Formatting/.editorconfig)) is located in the [Code Formatting](./Code Formatting) directory. All formatting options are explained in the [official .NET documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options). New format config file introduces the following code quality improvements:
- **Encoding and spacing**: Forces UTF-8 representation, trailing whitespace removal and mandatory newlines at the end of file. This is applied to all files, while following rules apply to `.cs` files only.
- **Indentation**: Forces standard 4-space indentation across all code components.
- **Newline structure**: Forces placing open braces on new lines for all control flow expressions (`if`, `else`, `catch`, `finally`, etc.). It also forces explicit new lines between query expression clauses.
- **Layout rules**: Forces padding around binary operators, no spaces around declaration statements and no spaces between method parameter list parentheses or general parentheses. It also forces sorting of system directives first and separate import directive groups. It warns if braces are missing for single-line statements and warns about implicit access modifiers (requiring `public`, `private`, etc.).
- **Naming conventions**: Warns if interfaces do not start with `I` and if private/internal fields do not start with an underscore.
- **Modern syntax conventions**: Warns if modern `throw` expressions, null-coalescing (`??`) and null-conditional (`?.`) operators should be used. It also checks if `is null` expressions are used instead of reference equality methods.

To streamline formatting and style verification a PowerShell script ([run_dotnet_format.ps1](./Code Formatting/run_dotnet_format.ps1)) was created. This script applies custom styling rules from the local [.editorconfig](./Code Formatting/.editorconfig) format file, runs the apply or check process and generates detailed reports in `JSON` and `HTML` format.

The script accepts the following arguments:
- **`Mode`** (required): Determines the type of formatting execution.
  - `check`: Runs in a verify-only mode. It reports errors and generates a log without modifying original source code.
  - `apply`: Directly formats and modifies the original files according to the rules defined in the format config file and applies the changes to the source repository.
- **`TargetDir`** (required): Specifies the name of the subdirectory inside the [Code Formatting/Results](./Code Formatting/Results) folder where the output report will be saved.
- **`-Visualize`**: Parses the generated JSON report into an HTML document and automatically opens it in the default web browser.

### **Results**

First, we will run the formatting script with `check` option and visualization to see which files and lines break defined rules.

```powershell
& ".\Code Formatting\run_dotnet_format.ps1" check InitialCheck -Visualize
```

As a result, `JSON` and `HTML` reports are generated in [Code Formatting/Results/InitialCheck](./Code Formatting/Results/InitialCheck) folder. Images [11](#img11), [12](#img12), [13](#img13) and [14](#img14) show different formatting rule breaks reported, such as broken import order, name rule violations, unnecessary whitespaces, invalid charset characters and missing accessibility modifiers.

<figure id="img11" style="text-align: center;">
  <img src="./Code Formatting/Images/report1.png" alt="Initial format check results">
  <figcaption>Image 11: Initial format check results</figcaption>
</figure>

<figure id="img12" style="text-align: center;">
  <img src="./Code Formatting/Images/report2.png" alt="Initial format check results">
  <figcaption>Image 12: Initial format check results</figcaption>
</figure>

<figure id="img13" style="text-align: center;">
  <img src="./Code Formatting/Images/report3.png" alt="Initial format check results">
  <figcaption>Image 13: Initial format check results</figcaption>
</figure>

<figure id="img14" style="text-align: center;">
  <img src="./Code Formatting/Images/report4.png" alt="Initial format check results">
  <figcaption>Image 14: Initial format check results</figcaption>
</figure>

Now, we will run the same script in apply mode, which will actually apply formatting rules to the original code.

```powershell
& ".\Code Formatting\run_dotnet_format.ps1" apply FormatApply -Visualize
```

We can see that `dotnet format` reports it can't fix `IDE 1006` warnings, which represent name rule violations ([Image 15](#img15)). The reason for this is that renaming symbols is a complex refactoring operation and automatic renaming could potentially break the codebase if those symbols are used in reflection, serialization or exposed via public APIs, so the tool refuses to fix them automatically and requires manual fixing. We won't be manually fixing naming violations in this analysis.

<figure id="img15" style="text-align: center;">
  <img src="./Code Formatting/Images/apply.png" alt="Format apply warning">
  <figcaption>Image 15: Format apply warning</figcaption>
</figure>

Generated report in [Code Formatting/Results/FormatApply](./Code Formatting/Results/FormatApply/) shows which erros and warnings were fixed. As we can see, everything but the name rule violations were fixed. To verify that, we can run the script in check mode again.

```powershell
& ".\Code Formatting\run_dotnet_format.ps1" check FinalCheck -Visualize
```

Generated report in [Code Formatting/Results/FinalCheck](./Code Formatting/Results/FinalCheck) shows that only warnings left are name rule violations. As was mentioned before, we will not fix these manually.

**Note**: If you want to recreate these steps (or any others) more than once, you need to discard changes made in the original [Mzinga](./Mzinga) repo.

```bash
git submodule update --init --recursive --force
```

## **Static Code Analysis**

Static code analysis involves examining source code without executing it, typically to find potential vulnerabilities or deviations from coding standards. For .NET projects, **Roslyn Analyzers** provide a powerful mechanism for this. **[Roslynator](https://github.com/dotnet/roslynator)** is an open-source collection of over 500 analyzers and refactorings for C# that we will integrate into the Mzinga project.

A PowerShell script ([run_roslynator.ps1](./Static Code Analysis/run_roslynator.ps1)) was created in the [Static Code Analysis](./Static Code Analysis) directory. This script first injects the required Roslynator NuGet packages into the main `.csproj` files, and then wraps the `dotnet format analyzers` command, similar to how we format code, but focuses strictly on logical and design analysis.

The script accepts the following arguments:
- **`Mode`** (required): Determines the type of execution.
  - `check`: Discovers and reports analyzer warnings without making any changes.
  - `apply`: Automatically applies fixes for any known warnings and updates the source code.
- **`TargetDir`** (required): Specifies the name of the subdirectory inside the [Static Code Analysis/Results](./Static Code Analysis/Results) folder where the output report will be saved.
- **`-Visualize`**: Parses the generated JSON output into an HTML report and automatically opens it in the default web browser.

### **Initial check**

Running the script in `check` mode allows us to see what issues exist before any changes are applied.

```powershell
& ".\Static Code Analysis\run_roslynator.ps1" check InitialCheck -Visualize
```

The output revealed a surprisingly small number of codebase issues. Finding only a few warnings, none of which represent critical architectural flaws or security vulnerabilities, indicates that the original Mzinga project is already in excellent condition. The identified issues were:
- **Empty catch blocks** (RCS1075): Exceptions were caught but ignored completely, which can hide application failures.
- **Static classes** (RCS1102): Classes containing only static members but not declared as `static`.
- **String comparisons** (RCS1155): String comparisons lacking explicit `StringComparison` arguments, potentially causing culture-specific bugs.
- **Exception constructors** (RCS1194): Custom exception implementations missing standard `.NET` exception constructors.

[Image 16](#img16) shows the generated HTML report. 

<figure id="img16" style="text-align: center;">
  <img src="./Static Code Analysis/Images/report1.png" alt="Initial static analysis report">
  <figcaption>Image 16: Initial static analysis report</figcaption>
</figure>

### **Applying changes**

After initial check, we will run the tool in `apply` mode to automatically refactor the code and resolve the warnings.

```powershell
& ".\Static Code Analysis\run_roslynator.ps1" apply FormatApply -Visualize
```

As we can see on the [Image 17](#img17), static class and string comparison warnings were resolved, but analyzer could not automatically fix empty catch blocks and exception constructor warnings.

<figure id="img17" style="text-align: center;">
  <img src="./Static Code Analysis/Images/report2.png" alt="Remaining static analysis warnings">
  <figcaption>Image 17: Remaining static analysis warnings</figcaption>
</figure>

Resolving `RCS1075` (Avoid empty catch clause) requires human intervention to determine the appropriate error-handling strategy, so no automatic fix exists. Depending on the intent, potential resolutions include tracking the error using logging mechanisms, rethrowing the exception or replacing the generic `System.Exception` with a specific error type.

```csharp
// Before
try {
    // ...
} catch (Exception ex) {
}

// After
try {
    // ...
} catch (Exception ex) {
    Log.Warning("message")
}
```

Resolving `RCS1194` (Implement exception constructors) cannot be done automatically because generating those constructors affects the public API of a class. Standard [.NET guidelines](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions) dictate that custom exceptions should provide at least three default constructors - a default constructor, one that takes a string message and one that takes both a string message and an inner exception.

```csharp
// Before
public class CommandException : Exception
{
    public CommandException(string message) : base(message) { }
}

// After
public class CommandException : Exception
{
    public CommandException() { }
    public CommandException(string message) : base(message) { }
    public CommandException(string message, Exception innerException) : base(message, innerException) { }
}
```

Once the appropriate manual fixes for the empty catch blocks and exception constructors are chosen and applied to the source files, executing the `check` mode of the script once more will confirm that all static analysis warnings have been successfully resolved.

## **Performance profiling**

**Performance profiling** is a technique used to measure the computational resources an application consumes at runtime. By recording CPU time and the execution frequency of specific functions, we can identify computational bottlenecks and optimize the code. In applications heavily reliant on heuristic search trees, like Mzinga's artificial intelligence search, profiling is essential to ensure maximum search depth within given time limits. For this analysis, we will use the **`dotnet-trace`** CLI tool, which is a cross-platform performance profiling tool from the .NET SDK.

### **Profiling setup and execution**

To gather relevant engine data, we need to simulate a realistic processing load. Since the bottleneck happens during calculation of deep heuristic search trees, our goal is to mimic the engine's behavior under heavy load. 

A PowerShell script ([run_profiler.ps1](./Performance%20Profiling/run_profiler.ps1)) was created in the [Performance Profiling](./Performance%20Profiling) directory. This script automates launching the engine and collecting diagnostic metrics using the `dotnet-trace` tool. 

The script executes the following steps:
1. Installs `dotnet-trace` globally, if it doesn't already exist.
2. Builds the `Mzinga.Engine` in **Release** configuration.
3. Launches the Engine process and redirects Standard input and output to issue commands.
4. Starts a new game and instructs the engine to play 4 turns without profiling, allowing maximum of 60 seconds of search time per turn. This populates the board and reaches a complex state.
5. In the background, it attaches the `dotnet-trace` profiler to the active engine process ID.
6. Simulates another 4 turns, extracting profiled data with a maximum processing time of 60 seconds per turn.
7. Stops the engine which automatically signals the profiler to finish the diagnostic collection and save the `.nettrace` file. 

The script accepts the following arguments:
- **`-Visualize`**: Automatically opens the generated report inside Visual Studio once execution completes.
- **`-Rerun`**: Deletes the previous report and runs the entire automated sequence over. By default, the script skips execution if the profile result already exists.

We will run the execution script with visualizing the results:

```powershell
& ".\Performance Profiling\run_profiler.ps1" -Visualize
```

### **Results**

The generated report ([Report.nettrace](./Performance%20Profiling/Results/Report.nettrace)) provides a report of CPU time spent per function. Looking at the **Top Functions** section ([Image 18](#img18)), we can clearly see the primary performance bottlenecks located within the `Mzinga.Core.Board` class:

1. **`GetValidSlides`**: This is by far the most expensive method. It calculates valid sliding moves for board pieces. Sliding in Hive requires continuous checks of adjacent spaces to ensure the piece physically fits during the slide.
2. **`IsPinned` / `IsOneHive`**: These functions check the "One Hive Rule" - ensuring that moving a piece does not split the hive in two. It consumes extremely high CPU resources because it must simulate board connectivity dynamically on every piece placement.
3. **`CalculateValidPlacements`**: Calculating valid placements for playing new pieces requires evaluating board adjacencies to make sure no opposing piece touches the newly placed colored piece.

<figure id="img18" style="text-align: center;">
  <img src="./Performance Profiling/Images/report1.png" alt="Profiling report">
  <figcaption>Image 18: Profiling report</figcaption>
</figure>

To confirm how we can optimize this, we need to locate the root execution source triggering these operations. To open the call tree view, click `Open Details...` and select `Call Tree` in the `Current View` section. Keep on clicking the `Expand Hot Path` option until the primary bottlenecks are reached. This will expand the execution trace from thread initialization down to the deeply nested engine methods ([Image 20](#img20)).

<figure id="img20" style="text-align: center;">
  <img src="./Performance Profiling/Images/report2.png" alt="Call tree">
  <figcaption>Image 20: Call tree</figcaption>
</figure>

Bottleneck happens because of the way `GameAI` generates its search tree. It evaluates millions of game boards by trying different move combinations (`PrincipalVariationSearchAsync`) and when it reaches leaf nodes it initiates deeper tactical checks (`QuiescenceSearchAsync`). To evaluate who is winning in a specific board state, the engine calculates a heuristic score using `BoardMetrics`.

However, the current architecture forces `GetBoardMetrics()` to call `GetValidMoves()` for every piece on the board on every single node. Because the tree traverses millions of nodes per second, evaluating `GetValidMoves` repeatedly triggers recalculations of massive logic queries like `GetValidSlides` and `IsPinned`.

Since the board state changes predictably with every single move (only one bug shifts location per turn), dynamically recalculating the entire move list from scratch every time it seeks heuristics is inefficient. 

### **Possible optimization**

To resolve this inefficiency, the `Board` class should to implement incremental move generation. Instead of recalculating all valid moves from scratch on every evaluated turn, the engine should store and incrementally update a `ValidMoves` cache. 

Upon playing or undoing a piece, it should identify only the specific bugs directly affected by the change (the moved piece itself, its direct neighbors and pieces that were "pinned" but are now free). It would then recalculate valid moves exclusively for those pieces, leaving the rest of the board's precalculated moves completely intact. 

This approach would drastically decrease the required CPU cycling, allowing the AI to search significantly deeper in the same time limit. Introducing incremental move generation is a complex architectural change that spans across the core game logic and is not easy to implement. We won't be implementing it in this analysis and will leave it as a suggestion for the author.

## **Architecture-as-Code Testing**

**Architecture-as-Code (AaC)** is a technique that allows treating application architecture and design rules as part of the codebase. Through automated tests, we can define system boundaries, dependency rules, naming conventions and directory structures. This approach prevents architectural degradation over time by ensuring that system components do not invoke methods or reference classes they are not supposed to communicate with. **[ArchUnitNET](https://archunitnet.readthedocs.io/en/stable/)** is an AaC tool made for C# and .NET that allows developers to write architecture tests as executable code. Its biggest strength lies in its declarative API, making architectural rules easy to write and understand. For the Mzinga project, we will use this tool to ensure that isolated subsystems do not depend on each other outside of the defined rules, preventing confusion and highly coupled code.

To automate this process, a separate test project [Mzinga.Architecture.Tests](./Architecture-as-Code%20Testing/Mzinga.Architecture.Tests) was created within the [Architecture-as-Code Testing](./Architecture-as-Code%20Testing) directory. A PowerShell script ([run_arch_tests.ps1](./Architecture-as-Code%20Testing/run_arch_tests.ps1)) was also created to streamline the execution of these tests and generate visual reports. The script handles the `dotnet test` command execution, and if asked to generate a report, it automatically installs the required ReportGenerator globally (if it doesn't already exist) to process the `.trx` results.

The script accepts the following arguments:
- **`TargetDir`** (required): Specifies the name of the subdirectory inside the [Architecture-as-Code Testing/Results](./Architecture-as-Code%20Testing/Results) directory where the test results and the HTML output report will be saved.
- **`-Visualize`**: Parses the generated `.trx` test results into an HTML document using the **trxlog2html** tool and automatically opens it in the default web browser.

### **Defining architecture rules**

Unlike traditional unit tests that evaluate the runtime behavior and logic of individual methods, architecture tests evaluate the structural characteristics of the codebase itself. To perform this, we must first instruct ArchUnitNET to target specific components by loading their assemblies. This processes the IL (Intermediate Language) code to build a cached, in-memory object model of classes, namespaces and their relationships.

After that, we define architecture rules similar to the way unit tests are written. Following rules were added:

1. **Core Layer Isolation Rule**: `Mzinga.Core` namespace components represent base fundamentals without high-level context, so they should not depend on execution logic built in the `Mzinga.Engine` namespace and on presentation logic built in the `Mzinga.Viewer` namespace.
2. **Engine Layer Isolation Rule**: `Mzinga.Engine` namespace classes hold computation and execution logic but they should not depend directly on the GUI from `Mzinga.Viewer`.
3. **Testing Code Isolation Rule**: Production source code modules (`Mzinga.Core` and `Mzinga.Engine`) must not depend on any test utilities or mocks found in testing namespaces (`Mzinga.Test`). This guarantees test dependencies naturally fall away when releasing to production.
4. **Exception Hierarchy Rule**: All classes that end with `Exception` must inherit from the base `System.Exception` class. This guarantees all custom exceptions integrate natively with the .NET error handling pipeline.
5. **Core I/O Logic Rule**: `Mzinga.Core` should not directly reference system input/output namespaces (`System.IO`). File streams, console inputs and general IO flows must be securely contained entirely in the Engine and Viewer abstraction layers.
6. **Circular Dependency Rule**: Top-level project namespaces must not contain circular dependencies. This means that if `Engine` depends on `Core`, the `Core` layer cannot depend back on the `Engine`.

### **Results**

To run the tests and inspect results, we will run the script with visualization.

```powershell
& ".\Architecture-as-Code Testing\run_arch_tests.ps1" InitialCheck -Visualize
```

As shown on the [image 21](#img21), all architectural rules passed successfully.

<figure id="img21" style="text-align: center;">
  <img src="./Architecture-as-Code Testing/Images/report.png" alt="Architecture-as-Code test results">
  <figcaption>Image 21: Architecture-as-Code test results</figcaption>
</figure>

This confirms that the Mzinga codebase is robust, clean and maintainable. The core game rules (`Core`) are completely isolated from the execution logic (`Engine`) and neither of the backend layers interacts directly with the user interface (`Viewer`). The complete absence of circular dependencies guarantees an acyclic dependency graph. This prevents the codebase from evolving into tightly coupled 'spaghetti code' over time and makes future extension or refactoring safer. Overall, these results show that Mzinga is built upon a solid architectural foundation, successfully protecting its core from external dependencies and UI logic.
