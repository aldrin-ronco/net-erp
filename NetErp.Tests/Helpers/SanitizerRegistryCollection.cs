using Xunit;

namespace NetErp.Tests.Helpers;

/// <summary>
/// Serializes execution of all tests that touch the static <c>SanitizerRegistry</c>.
/// The registry uses non-thread-safe dictionaries, so running its dependent tests in
/// parallel (xUnit's default across classes) produces flaky races on the shared state
/// even though each test class resets the registry in its Dispose.
///
/// Any test class that calls <c>SanitizerRegistry.RegisterType</c>,
/// <c>RegisterProperty</c>, or relies on registry state must carry
/// <c>[Collection(SanitizerRegistryCollection.Name)]</c>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SanitizerRegistryCollection
{
    public const string Name = "SanitizerRegistry";
}
