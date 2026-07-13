namespace Mzinga.Architecture;

using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public class ArchitectureTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(
            typeof(Mzinga.Engine.Engine).Assembly,
            typeof(Mzinga.Core.Board).Assembly
        ).Build();

    [Fact]
    public void CoreIsolationRule()
    {
        IArchRule rule = Classes().That().ResideInNamespace("Mzinga.Core")
            .Should().NotDependOnAny(Classes().That().ResideInNamespace("Mzinga.Engine")
                .Or().ResideInNamespace("Mzinga.Viewer"));
        rule.Check(Architecture);
    }

    [Fact]
    public void EngineIsolationRule()
    {
        IArchRule rule = Classes().That().ResideInNamespace("Mzinga.Engine")
            .Should().NotDependOnAny(Classes().That().ResideInNamespace("Mzinga.Viewer"));
        rule.Check(Architecture);
    }

    [Fact]
    public void TestIsolationRule()
    {
        IArchRule rule = Classes().That().ResideInNamespace("Mzinga.Core")
            .Or().ResideInNamespace("Mzinga.Engine")
            .Should().NotDependOnAny(Classes().That().ResideInNamespace("Mzinga.Test"));
        rule.Check(Architecture);
    }

    [Fact]
    public void ExceptionHierarchyRule()
    {
        IArchRule rule = Classes().That().HaveNameEndingWith("Exception")
            .Should().BeAssignableTo(typeof(System.Exception));
        rule.Check(Architecture);
    }

    [Fact]
    public void CoreIoRule()
    {
        IArchRule rule = Classes().That().ResideInNamespace("Mzinga.Core")
            .Should().NotDependOnAny(Classes().That().ResideInNamespace("System.IO"));
        rule.Check(Architecture);
    }

    [Fact]
    public void NoCircularDependenciesRule()
    {
        IArchRule rule = SliceRuleDefinition.Slices().Matching("Mzinga.(*)..")
            .Should().BeFreeOfCycles();
        rule.Check(Architecture);
    }
}
