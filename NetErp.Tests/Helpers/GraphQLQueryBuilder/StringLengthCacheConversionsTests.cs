using System;
using FluentAssertions;
using Models.Books;
using NetErp.Helpers.Cache;
using Xunit;

namespace NetErp.Tests.Helpers.GraphQLQueryBuilder;

public class StringLengthCacheConversionsTests
{
    [Theory]
    [InlineData("BusinessName", "business_name")]
    [InlineData("AccountingEntity", "accounting_entity")]
    [InlineData("Name", "name")]
    [InlineData("IsActive", "is_active")]
    public void ToSnakeCase_PascalCase_ConvertsCorrectly(string input, string expected)
    {
        StringLengthCache.ToSnakeCase(input).Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_SingleWord_LowercasesFirstChar()
    {
        StringLengthCache.ToSnakeCase("Name").Should().Be("name");
    }

    [Fact]
    public void ToSnakeCase_AlreadyLowercase_NoChange()
    {
        StringLengthCache.ToSnakeCase("name").Should().Be("name");
    }

    [Fact]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        StringLengthCache.ToSnakeCase("").Should().BeEmpty();
    }

    [Fact]
    public void ToSnakeCase_NullString_ReturnsNull()
    {
        StringLengthCache.ToSnakeCase(null!).Should().BeNull();
    }

    [Fact]
    public void ResolveEntityName_StripsGraphQLModelSuffix()
    {
        // AccountingEntityGraphQLModel → strip suffix → AccountingEntity → snake_case → accounting_entity
        var result = StringLengthCache.ResolveEntityName(typeof(AccountingEntityGraphQLModel));

        result.Should().Be("accounting_entity");
    }

    [Fact]
    public void ResolveEntityName_AppliesOverrideWhenPresent()
    {
        // WithholdingCertificateConfigGraphQLModel has an override to "withholding_certificate"
        var result = StringLengthCache.ResolveEntityName(typeof(WithholdingCertificateConfigGraphQLModel));

        result.Should().Be("withholding_certificate");
    }
}
