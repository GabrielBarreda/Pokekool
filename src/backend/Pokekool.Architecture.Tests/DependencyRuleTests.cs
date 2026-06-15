using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Pokekool.Architecture.Tests;

/// <summary>
/// Enforces Clean Architecture dependency rules.
/// All violations fail the CI build.
/// </summary>
public sealed class DependencyRuleTests
{
    private const string DomainNs         = "Pokekool.Domain";
    private const string ApplicationNs    = "Pokekool.Application";
    private const string InfrastructureNs = "Pokekool.Infrastructure";
    private const string ApiNs            = "Pokekool.API";
    private const string SharedNs         = "Pokekool.Shared";

    [Fact]
    public void Domain_should_not_depend_on_Application_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(Pokekool.Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must only depend on itself (and System.*). Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Domain_should_not_depend_on_EntityFrameworkCore()
    {
        var result = Types.InAssembly(typeof(Pokekool.Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "MediatR",
                "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must remain pure (zero framework). Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(Pokekool.Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not know about Infrastructure or API. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Application_should_not_depend_on_EntityFrameworkCore_or_AspNetCore()
    {
        var result = Types.InAssembly(typeof(Pokekool.Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.AspNetCore.Http.HttpContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must be transport and ORM agnostic. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Shared_should_not_depend_on_any_other_project()
    {
        var result = Types.InAssembly(typeof(Pokekool.Shared.Constants.ClaimTypes).Assembly)
            .Should()
            .NotHaveDependencyOnAny(DomainNs, ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Shared is fully isolated. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Shared_should_not_depend_on_any_framework()
    {
        var result = Types.InAssembly(typeof(Pokekool.Shared.Constants.ClaimTypes).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore",
                "MediatR",
                "FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Shared = POCO only. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_Api()
    {
        var result = Types.InAssembly(typeof(Pokekool.Infrastructure.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not know about API. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? new List<string>()));
    }
}
