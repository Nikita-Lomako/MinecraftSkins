using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using MinecraftSkins.Application;
using MinecraftSkins.Api.Handlers;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchUnitNET.xUnit;

namespace MinecraftSkins.Tests.UnitTests.Architecture;

public class CleanArchitectureTests
{
    private static readonly global::ArchUnitNET.Domain.Architecture LoadedArchitecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Skin).Assembly,
            typeof(MappingConfig).Assembly,
            typeof(AppDbContext).Assembly,
            typeof(GlobalExceptionHandler).Assembly)
        .Build();

    [Fact]
    public void Domain_ShouldNotDependOnOuterLayers()
    {
        var forbiddenTypes = TypesByNamespacePrefixes(
            "MinecraftSkins.Application",
            "MinecraftSkins.Infrastructure",
            "MinecraftSkins.Api");

        var rule = Types().That()
            .ResideInNamespace("MinecraftSkins.Domain..")
            .Should().NotDependOnAny(forbiddenTypes)
            .WithoutRequiringPositiveResults();

        rule.Check(LoadedArchitecture);
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi()
    {
        var forbiddenTypes = TypesByNamespacePrefixes(
            "MinecraftSkins.Infrastructure",
            "MinecraftSkins.Api");

        var rule = Types().That()
            .ResideInNamespace("MinecraftSkins.Application..")
            .Should().NotDependOnAny(forbiddenTypes)
            .WithoutRequiringPositiveResults();

        rule.Check(LoadedArchitecture);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var forbiddenTypes = TypesByNamespacePrefixes("MinecraftSkins.Api");

        var rule = Types().That()
            .ResideInNamespace("MinecraftSkins.Infrastructure..")
            .Should().NotDependOnAny(forbiddenTypes)
            .WithoutRequiringPositiveResults();

        rule.Check(LoadedArchitecture);
    }

    private static IType[] TypesByNamespacePrefixes(params string[] namespacePrefixes)
    {
        return LoadedArchitecture.Types
            .Where(t => t.FullName is not null && namespacePrefixes.Any(prefix => t.FullName.StartsWith(prefix, StringComparison.Ordinal)))
            .Cast<IType>()
            .ToArray();
    }
}

