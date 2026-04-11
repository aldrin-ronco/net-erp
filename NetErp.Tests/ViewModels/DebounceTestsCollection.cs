using Xunit;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Colección no-paralela para tests de debounce en masters. Se agrupan aquí
/// porque verifican timing de <see cref="NetErp.Helpers.DebouncedAction"/>
/// con delays cortos (30-50ms) — bajo contención del thread pool (cuando
/// el suite corre muchos tests en paralelo) las continuations del debounce
/// pueden llegar fuera del wait window, produciendo flakes.
///
/// <para>Los tests DENTRO de la colección corren secuencialmente entre sí,
/// y la colección entera NO corre en paralelo con otras colecciones (por
/// <c>DisableParallelization = true</c>).</para>
/// </summary>
[CollectionDefinition("Debounce tests", DisableParallelization = true)]
public class DebounceTestsCollection
{
    // Marker class — intentionally empty.
}
