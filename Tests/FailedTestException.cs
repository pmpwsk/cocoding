namespace cocoding.Tests;

/// <summary>
/// Exception type for test failures.
/// </summary>
public class TestFailedException(string description) : Exception(description)
{
}