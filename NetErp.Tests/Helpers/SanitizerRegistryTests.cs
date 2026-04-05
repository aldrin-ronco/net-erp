using System;
using Common.Helpers;
using FluentAssertions;
using Xunit;

namespace NetErp.Tests.Helpers;

public class SanitizerRegistryTests : IDisposable
{
    public void Dispose()
    {
        SanitizerRegistry.ResetForTesting();
    }

    [Fact]
    public void Sanitize_NoRegistration_ReturnsOriginalValue()
    {
        var result = SanitizerRegistry.Sanitize(typeof(object), "Prop", "hello");

        result.Should().Be("hello");
    }

    [Fact]
    public void RegisterType_String_TrimsValue()
    {
        SanitizerRegistry.RegisterType<string>(s => s?.Trim());

        var result = SanitizerRegistry.Sanitize(typeof(object), "Prop", "  hello  ");

        result.Should().Be("hello");
    }

    [Fact]
    public void RegisterProperty_OverridesTypeSanitizer()
    {
        SanitizerRegistry.RegisterType<string>(s => s?.Trim());
        SanitizerRegistry.RegisterProperty<string>(typeof(MyVm), "Name", s => s?.ToUpper());

        var typeResult = SanitizerRegistry.Sanitize(typeof(object), "Name", "hello");
        var propResult = SanitizerRegistry.Sanitize(typeof(MyVm), "Name", "hello");

        typeResult.Should().Be("hello", "tipo sin property sanitizer usa el de tipo");
        propResult.Should().Be("HELLO", "property sanitizer tiene precedencia");
    }

    [Fact]
    public void Sanitize_NullValue_WithTypeSanitizer_ReturnsNull()
    {
        // When value is null, value?.GetType() is null, so type sanitizer is not invoked.
        // Null passes through without sanitization.
        SanitizerRegistry.RegisterType<string>(s => "sanitized");

        var result = SanitizerRegistry.Sanitize(typeof(object), "Prop", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Sanitize_NullValue_WithPropertySanitizer_InvokesSanitizer()
    {
        // Property sanitizer IS invoked for null because it matches by (type, propertyName), not by value type.
        SanitizerRegistry.RegisterProperty<string>(typeof(MyVm), "Name", s => s ?? "was null");

        var result = SanitizerRegistry.Sanitize(typeof(MyVm), "Name", null);

        result.Should().Be("was null");
    }

    [Fact]
    public void Sanitize_ValueTypeNotRegistered_ReturnsOriginal()
    {
        SanitizerRegistry.RegisterType<string>(s => s?.Trim());

        var result = SanitizerRegistry.Sanitize(typeof(object), "Prop", 42);

        result.Should().Be(42);
    }

    [Fact]
    public void RegisterType_ReplacesExistingSanitizer()
    {
        SanitizerRegistry.RegisterType<string>(s => "first");
        SanitizerRegistry.RegisterType<string>(s => "second");

        var result = SanitizerRegistry.Sanitize(typeof(object), "Prop", "input");

        result.Should().Be("second");
    }

    private class MyVm { }
}
