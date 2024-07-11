using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using System.Numerics;
using System.Reflection;
using Nuke.Common.Utilities;
using Nuke.Common;
using System.Diagnostics.CodeAnalysis;

namespace Nuke.Cola.BuildGui;

public class BuildGuiContext
{
    public string TargetsFilter = "";
    public string ParametersFilter = "";
    public bool ShowAllParameters = false;
    public List<string> SelectedTargets = new();
    public NukeBuild? BuildObject;
}

// This is pure wonder https://pthom.github.io/imgui_manual_online/manual/imgui_manual.html

public class BuildGuiApp : IDisposable
{
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _window?.Dispose();
            }
            disposedValue = true;
        }
    }

    public BuildGuiApp(NukeBuild buildObject)
    {
        _buildClass = buildObject.GetType();
        _buildObject = buildObject;
        var root = ProcessBuildClass(_buildClass, null);
        _targetsRoot = root.Targets;
        _parametersRoot = root.Parameters;
        _context.BuildObject = _buildObject;
    }

    private record BuildComponentGroup(string Name, GuiCategory Targets, GuiCategory Parameters);

    private BuildComponentGroup ProcessBuildClass(Type buildClass, BuildComponentGroup? parent)
    {
        if (_buildComponents.ContainsKey(buildClass.FullName ?? ""))
        {
            return _buildComponents[buildClass.FullName ?? ""];
        }

        var shortName = buildClass.GetDisplayShortName();
        var buildCompoent = new BuildComponentGroup(shortName, new() {Name = shortName}, new() {Name = shortName});
        _buildComponents.Add(buildClass.FullName ?? "", buildCompoent);

        if (parent != null)
        {
            buildCompoent.Targets.Parent = parent.Targets;
            buildCompoent.Parameters.Parent = parent.Parameters;
            parent.Targets.SubCategories.Add(buildCompoent.Targets);
            parent.Parameters.SubCategories.Add(buildCompoent.Parameters);
        }

        var baseType = buildClass.BaseType ?? typeof(NukeBuild);
        if (buildClass.IsClass && buildClass != typeof(NukeBuild))
        {
            var buildInterfaces = buildClass.GetInterfaces()
                .Except(baseType.GetInterfaces())
                .Where(i => i.GetInterfaces().Contains(typeof(INukeBuild)));
            
            foreach (var buildInterface in buildInterfaces)
            {
                ProcessBuildClass(buildInterface, buildCompoent);
            }

            ProcessBuildClass(baseType, buildCompoent);
        }
        else if (buildClass.IsInterface && buildClass != typeof(NukeBuild))
        {
            var buildInterfaces = buildClass.GetInterfaces()
                .Where(i => i.GetInterfaces().Contains(typeof(INukeBuild)));
            
            foreach (var buildInterface in buildInterfaces)
            {
                ProcessBuildClass(buildInterface, buildCompoent);
            }
        }

        var targetMembers = buildClass.GetProperties()
            .Where(p => p.PropertyType == typeof(Target) && p.DeclaringType == buildClass);

        foreach (var targetMember in targetMembers)
        {
            // TODO: Take dependencies into account
            // https://github.com/nuke-build/nuke/blob/6eb779618e22b0cdf1fa27538b305f750b698d88/source/Nuke.Build/Execution/Extensions/HandlePlanRequestsAttribute.cs#L52

            var name = targetMember.Name;
            var instance = targetMember.GetValue<Target>(_buildObject);
            var introspection = new TargetIntrospection();
            instance.Invoke(introspection);
            if (introspection.IsUnlisted) continue;
            var description = introspection.DescriptionStorage;

            var item = new GuiItem(name, ctx =>
            {
                if (string.IsNullOrWhiteSpace(ctx.TargetsFilter) || name.ContainsOrdinalIgnoreCase(ctx.TargetsFilter))
                {
                    bool isSelected = ctx.SelectedTargets.Contains(name);
                    if (ImGui.Selectable(name, isSelected))
                    {
                        if (isSelected)
                        {
                            ctx.SelectedTargets.Remove(name);
                        }
                        else
                        {
                            ctx.SelectedTargets.Add(name);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        ImGui.SetItemTooltip(description);
                    }
                }
            });
            buildCompoent.Targets.Items.Add(item);
        }

        var parameterMembers = buildClass.GetMembers()
            .Where(p =>
                (p.MemberType == MemberTypes.Field || p.MemberType == MemberTypes.Property)
                && p.DeclaringType == buildClass
                && p.HasCustomAttribute<ParameterAttribute>()
            );

        foreach (var parameterMember in parameterMembers)
        {
            var paramAttr = parameterMember.GetCustomAttribute<ParameterAttribute>()!;
            var name = paramAttr.Name ?? parameterMember.Name;
            var editor = ParameterEditor.MakeEditor(parameterMember);
            var item = new GuiItem(name, ctx =>
            {
                if (string.IsNullOrWhiteSpace(ctx.ParametersFilter) || name.ContainsOrdinalIgnoreCase(ctx.ParametersFilter))
                {
                    if (editor != null)
                    {
                        editor.Draw(parameterMember, name, ctx);
                    }
                    else
                    {
                        ImGui.Text(name);
                        ImGui.SameLine();
                        
                        ImGui.TextDisabled("(!)");
                        if (ImGui.BeginItemTooltip())
                        {
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                            ImGui.TextUnformatted("No editor was specified for this parameter");
                            ImGui.PopTextWrapPos();
                            ImGui.EndTooltip();
                        }
                    }
                }
            });
            buildCompoent.Parameters.Items.Add(item);
        }

        return buildCompoent;
    }

    private Dictionary<string, BuildComponentGroup> _buildComponents = new();

    private GuiCategory _targetsRoot;
    private GuiCategory _parametersRoot;
    private BuildGuiContext _context = new();

    private void DrawCategory(GuiCategory category)
    {
        ImGui.SetNextItemOpen(true, ImGuiCond.Once);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        bool opened = ImGui.TreeNode(category.Name);
        ImGui.PopStyleColor();

        if (opened)
        {
            foreach (var item in category.Items)
            {
                item.Widget(_context);
            }
            category.SubCategories.ForEach(DrawCategory);
            ImGui.TreePop();
        }
    }

    public BuildGuiApp Run()
    {
        _window = Window.Create(WindowOptions.Default);
        _window.Load += () =>
        {
            _gl = _window.CreateOpenGL();
            _inputContext = _window.CreateInput();
            _controller = new(_gl, _window, _inputContext);
        };
        _window.FramebufferResize += s => _gl?.Viewport(s);
        _window.Render += delta =>
        {
            if (_controller != null && _gl != null)
            {
                _controller.Update((float) delta);

                _gl.ClearColor(Color.FromArgb(255, 0, 0, 0));
                _gl.Clear((uint) ClearBufferMask.ColorBufferBit);

                var mainVP = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(mainVP.WorkPos);
                ImGui.SetNextWindowSize(mainVP.WorkSize);
                ImGui.SetNextWindowViewport(mainVP.ID);
                
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                {
                    if (ImGui.Begin("Target Selector",
                        ImGuiWindowFlags.NoTitleBar
                        | ImGuiWindowFlags.NoCollapse
                        | ImGuiWindowFlags.NoResize
                        | ImGuiWindowFlags.NoMove
                        | ImGuiWindowFlags.NoBringToFrontOnFocus
                        | ImGuiWindowFlags.NoNavFocus
                    ))
                    {
                        var targetWidth = mainVP.Size.X * 0.4f;
                        ImGui.BeginChild("TargetsChild", new Vector2(targetWidth, 0), true);
                        {
                            ImGui.SeparatorText("Targets");
                            ImGui.InputText("Filter", ref _context.TargetsFilter, 256);
                            if (ImGui.Button("Clear Selection"))
                            {
                                _context.SelectedTargets.Clear();
                            }
                            ImGui.BeginChild("TargetsListChild", new Vector2(0,0), false);
                            {
                                DrawCategory(_targetsRoot);
                            }
                            ImGui.EndChild();
                        }
                        ImGui.EndChild();
                        ImGui.SameLine();
                        ImGui.BeginChild("ParametersChild", new Vector2(0, 0), true);
                        {
                            ImGui.SeparatorText("Parameters");
                            ImGui.InputText("Filter", ref _context.ParametersFilter, 256);
                            ImGui.Checkbox("Show All Parameters", ref _context.ShowAllParameters);
                            ImGui.SetItemTooltip("By default only parameters shown which are related to selected targets");
                            ImGui.BeginChild("ParamsListChild", new Vector2(0,0), false);
                            {
                                DrawCategory(_parametersRoot);
                            }
                            ImGui.EndChild();
                        }
                        ImGui.EndChild();
                    }
                    ImGui.End();
                }
                ImGui.PopStyleVar(2);

                _controller.Render();
            }
        };
        _window.Closing += () =>
        {
            _controller?.Dispose();
            _inputContext?.Dispose();
            _gl?.Dispose();
        };
        _window.Run();

        return this;
    }

    private IWindow? _window;
    private ImGuiController? _controller;
    private IInputContext? _inputContext;
    private GL? _gl;

    private Type _buildClass;
    private NukeBuild _buildObject;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}