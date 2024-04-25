using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nuke.Cola.BuildGui;

public record GuiItem(string Name, Action<BuildGuiContext> Widget);

public class GuiCategory
{
    public string Name = "None";
    public GuiCategory? Parent = null;
    public List<GuiCategory> SubCategories = new();
    public List<GuiItem> Items = new();
}