using System.Reflection;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class LayeringTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICceDbContext).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Application")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_Microsoft_AspNetCore_Mvc()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore.Mvc")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Application_does_not_depend_on_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
