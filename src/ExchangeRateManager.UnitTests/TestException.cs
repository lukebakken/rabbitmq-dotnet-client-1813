using System.Diagnostics.CodeAnalysis;

namespace ExchangeRateManager.Tests.UnitTests;

/// <summary>
/// Exception for test only purposes.
/// </summary>
[Serializable]
public class TestException : Exception
{
    public TestException()
    { }

    //public TestException(string? message) : base(message)
    //{ }

    //public TestException(string? message, Exception? innerException) : base(message, innerException)
    //{ }
}