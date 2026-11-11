# Testing Standards

## 1. Naming Conventions

- **SUT naming:** Name the system under test `sut` (local variable) or `_sut` (private field).
- **Variable naming:** Use `expected` for the expected value and `actual` for the result being tested.
- **Method naming:** MUST follow the `MethodName_WhenScenario_ShouldExpectedResult` pattern (e.g., `TryDeserialize_WhenValidJsonAndTypeIsPassed_ShouldDeserialize`).
- **Exception:** Omit the `WhenScenario` part (e.g., `MethodName_ShouldExpectedResult`) if there is no specific scenario or condition being tested.
- Avoid filler like `WhenCalled`.

## 2. Test Structure

- **AAA Pattern:** ALWAYS follow the **Arrange, Act, Assert** pattern. Do not include any other structural comments.
- **Constants in Arrange:** ALWAYS use constants or well-named variables for magic strings, numbers, or other literal values within the **Arrange** phase.
- **Constructors:** Use **Primary Constructors** in classes for injecting dependencies.
- **Exception:** Use a **Traditional Constructor** if member initialization (e.g., the SUT) requires referencing other fields or involves logic that cannot be expressed cleanly within a **Primary Constructor**.

## 3. Test Design

- **Test Scope:** Only test classes that contain logic. NEVER test simple properties or plain data objects.

### Data-Driven Tests

- Use `[Theory]` when the expected behavior applies to multiple different inputs.
- Prefer `[InlineData]` for constant test inputs.
- Use `TheoryDataRow<...>` only when test data requires object values that cannot be represented with `[InlineData]`.
- Keep any helper methods that provide parameterized test data at the end of the test class.

**Note:**

- If a test requires `if-else` or conditional logic within the **Assert** phase (e.g., in a `Theory`), prefer splitting it into separate `[Fact]` methods to keep assertions explicit and focused.

## 4. Mocking

- **Mocking:** Use **NSubstitute** for creating mocks and stubs.
- Use manual fakes or stubs only in special cases where the library is insufficient.

## 5. Project Structure

- **Test projects must mirror the source project structure**, including the intermediate folders (e.g., `Aspire/`, `Hosts/`, `Modules/`, `Shared/`).
- The root directory for test projects is `tests/` instead of `src/`.
- Project and namespace naming MUST follow one of these conventions:
- `{ProjectName}.UnitTests`
- `{ProjectName}.IntegrationTests`

## 6. xUnit v3 Features

- When `TheoryDataRow` is needed, prefer it or Tuples over `object[]` for strongly typed theory data.
- Use `TheoryDataRow` metadata properties (e.g., `Skip`, `Timeout`, `DisplayName`) or fluent methods (e.g., `.WithSkip(...)`) when needed.
- Use `MatrixTheoryData<...>` to automatically generate combinations of multiple data sets (2–5 sets).
- Leverage async `MemberData` when data retrieval requires asynchronous operations.
