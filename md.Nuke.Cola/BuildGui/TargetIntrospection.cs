using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Tooling;

namespace Nuke.Cola.BuildGui;

public class TargetIntrospection : ITargetDefinition
{
    public ITargetDefinition After(params Target[] targets)
    {
       return this;
    }

    public ITargetDefinition After<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition AssuredAfterFailure()
    {
        return this;
    }

    public ITargetDefinition Base()
    {
        return this;
    }

    public ITargetDefinition Before(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition Before<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition Consumes(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition Consumes<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition Consumes(Target target, params string[] artifacts)
    {
        return this;
    }

    public ITargetDefinition Consumes<T>(Func<T, Target> target, params string[] artifacts)
    {
        return this;
    }

    public ITargetDefinition Consumes<T>(params string[] artifacts)
    {
        return this;
    }

    public ITargetDefinition DependentFor(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition DependentFor<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition DependsOn(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition DependsOn<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition DependsOnContext<T>() where T : INukeBuild
    {
        return this;
    }

    public string? DescriptionStorage;

    public ITargetDefinition Description(string description)
    {
        DescriptionStorage = description;
        return this;
    }

    public ITargetDefinition Executes(params Action[] actions)
    {
        return this;
    }

    public ITargetDefinition Executes<T>(Func<T> action)
    {
        return this;
    }

    public ITargetDefinition Executes(Func<Task> action)
    {
        return this;
    }

    public ITargetDefinition Inherit(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition Inherit<T>(params Expression<Func<T, Target>>[] targets)
    {
        return this;
    }

    public ITargetDefinition OnlyWhenDynamic(Func<bool> condition, string conditionExpression)
    {
        return this;
    }

    public ITargetDefinition OnlyWhenStatic(Func<bool> condition, string conditionExpression)
    {
        return this;
    }

    public ITargetDefinition Partition(int size)
    {
        return this;
    }

    public ITargetDefinition ProceedAfterFailure()
    {
        return this;
    }

    public ITargetDefinition Produces(params string[] artifacts)
    {
        return this;
    }

    public ITargetDefinition Requires<T>(Expression<Func<T>> parameterRequirement, params Expression<Func<T>>[] parameterRequirements) where T : class
    {
        return this;
    }

    public ITargetDefinition Requires<T>(Expression<Func<T?>> parameterRequirement, params Expression<Func<T?>>[] parameterRequirements) where T : struct
    {
        return this;
    }

    public ITargetDefinition Requires(Expression<Func<bool>> requirement, params Expression<Func<bool>>[] requirements)
    {
        return this;
    }

    public ITargetDefinition Requires<T>() where T : IRequireTool
    {
        return this;
    }

    public ITargetDefinition Requires<T>(string version) where T : IRequireToolWithVersion
    {
        return this;
    }

    public ITargetDefinition Requires(Expression<Func<Tool>> tool, params Expression<Func<Tool>>[] tools)
    {
        return this;
    }

    public ITargetDefinition TriggeredBy(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition TriggeredBy<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition Triggers(params Target[] targets)
    {
        return this;
    }

    public ITargetDefinition Triggers<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryAfter<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryBefore<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryDependentFor<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryDependsOn<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryTriggeredBy<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public ITargetDefinition TryTriggers<T>(params Func<T, Target>[] targets)
    {
        return this;
    }

    public bool IsUnlisted = false;

    public ITargetDefinition Unlisted()
    {
        IsUnlisted = true;
        return this;
    }

    public ITargetDefinition WhenSkipped(DependencyBehavior dependencyBehavior)
    {
        return this;
    }
}