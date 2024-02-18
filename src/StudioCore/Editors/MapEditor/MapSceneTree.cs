﻿using ImGuiNET;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Configuration;
using StudioCore.Banks.AliasBank;
using StudioCore.Gui;
using StudioCore.AssetLocator;
using StudioCore.Platform;
using StudioCore.UserProject;
using StudioCore.Scene;
using StudioCore.Settings;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using StudioCore.Banks;
using StudioCore.Editors.ParamEditor;
using StudioCore.MsbEditor;

namespace StudioCore.Editors.MapEditor;

public struct DragDropPayload
{
    public Entity Entity;
}

public struct DragDropPayloadReference
{
    public int Index;
}

public interface SceneTreeEventHandler
{
    public void OnEntityContextMenu(Entity ent);
}

public class MapSceneTree : IActionEventHandler
{
    public enum Configuration
    {
        MapEditor,
        ModelEditor
    }

    public enum ViewMode
    {
        Hierarchy,
        Flat,
        ObjectType
    }

    private readonly Configuration _configuration;
    private readonly List<Entity> _dragDropDestObjects = new();
    private readonly List<int> _dragDropDests = new();
    private readonly Dictionary<int, DragDropPayload> _dragDropPayloads = new();

    private readonly List<Entity> _dragDropSources = new();
    private readonly ViewportActionManager _editorActionManager;

    private readonly SceneTreeEventHandler _handler;

    private readonly string _id;
    private readonly ViewportSelection _selection;

    // Keep track of open tree nodes for selection management purposes
    private readonly HashSet<Entity> _treeOpenEntities = new();
    private readonly Universe _universe;

    private readonly string[] _viewModeStrings = { "Hierarchy View", "Flat View", "Type View" };

    private readonly IViewport _viewport;

    private Dictionary<string, Dictionary<MsbEntity.MsbEntityType, Dictionary<Type, List<MsbEntity>>>>
        _cachedTypeView;

    private bool _chaliceLoadError;

    private string _chaliceMapID = "m29_";
    private int _dragDropPayloadCounter;

    private bool _initiatedDragDrop;

    private ulong
        _mapEnt_ImGuiID; // Needed to avoid issue with identical IDs during keyboard navigation. May be unecessary when ImGUI is updated.

    private string _mapNameSearchStr = "";

    private ISelectable _pendingClick;
    private bool _pendingDragDrop;

    private bool _setNextFocus;

    private ViewMode _viewMode = ViewMode.ObjectType;

    private Dictionary<string, string> _chrAliasCache;
    private Dictionary<string, string> _objAliasCache;
    private Dictionary<string, string> _mapPieceAliasCache;


    public MapSceneTree(Configuration configuration, SceneTreeEventHandler handler, string id, Universe universe, ViewportSelection sel, ViewportActionManager aman, IViewport vp)
    {
        _handler = handler;
        _id = id;
        _universe = universe;
        _selection = sel;
        _editorActionManager = aman;
        _viewport = vp;
        _configuration = configuration;

        if (_configuration == Configuration.ModelEditor)
        {
            _viewMode = ViewMode.Hierarchy;
        }

        _chrAliasCache = null;
        _objAliasCache = null;
        _mapPieceAliasCache = null;
    }

    public void OnActionEvent(ActionEvent evt)
    {
        if (evt.HasFlag(ActionEvent.ObjectAddedRemoved))
        {
            _cachedTypeView = null;
        }
    }


    private void RebuildTypeViewCache(Map map)
    {
        if (_cachedTypeView == null)
        {
            _cachedTypeView =
                new Dictionary<string, Dictionary<MsbEntity.MsbEntityType, Dictionary<Type, List<MsbEntity>>>>();
        }

        Dictionary<MsbEntity.MsbEntityType, Dictionary<Type, List<MsbEntity>>> mapcache = new();
        mapcache.Add(MsbEntity.MsbEntityType.Part, new Dictionary<Type, List<MsbEntity>>());
        mapcache.Add(MsbEntity.MsbEntityType.Region, new Dictionary<Type, List<MsbEntity>>());
        mapcache.Add(MsbEntity.MsbEntityType.Event, new Dictionary<Type, List<MsbEntity>>());
        if (Project.Type is ProjectType.BB or ProjectType.DS3 or ProjectType.SDT
            or ProjectType.ER or ProjectType.AC6)
        {
            mapcache.Add(MsbEntity.MsbEntityType.Light, new Dictionary<Type, List<MsbEntity>>());
        }
        else if (Project.Type is ProjectType.DS2S)
        {
            mapcache.Add(MsbEntity.MsbEntityType.Light, new Dictionary<Type, List<MsbEntity>>());
            mapcache.Add(MsbEntity.MsbEntityType.DS2Event, new Dictionary<Type, List<MsbEntity>>());
            mapcache.Add(MsbEntity.MsbEntityType.DS2EventLocation, new Dictionary<Type, List<MsbEntity>>());
            mapcache.Add(MsbEntity.MsbEntityType.DS2Generator, new Dictionary<Type, List<MsbEntity>>());
            mapcache.Add(MsbEntity.MsbEntityType.DS2GeneratorRegist, new Dictionary<Type, List<MsbEntity>>());
        }

        foreach (Entity obj in map.Objects)
        {
            if (obj is MsbEntity e && mapcache.ContainsKey(e.Type))
            {
                Type typ = e.WrappedObject.GetType();
                if (!mapcache[e.Type].ContainsKey(typ))
                {
                    mapcache[e.Type].Add(typ, new List<MsbEntity>());
                }

                mapcache[e.Type][typ].Add(e);
            }
        }

        if (!_cachedTypeView.ContainsKey(map.Name))
        {
            _cachedTypeView.Add(map.Name, mapcache);
        }
        else
        {
            _cachedTypeView[map.Name] = mapcache;
        }
    }

    private void ChaliceDungeonImportButton()
    {
        ImGui.Selectable($@"   {ForkAwesome.PlusCircle} Load Chalice Dungeon...", false);
        if (ImGui.BeginPopupContextItem("chalice", 0))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Chalice ID (m29_xx_xx_xx): ");
            ImGui.SameLine();
            var pname = _chaliceMapID;
            ImGui.SetNextItemWidth(100);
            if (_chaliceLoadError)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            }

            if (ImGui.InputText("##chalicename", ref pname, 12))
            {
                _chaliceMapID = pname;
            }

            if (_chaliceLoadError)
            {
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                if (!_universe.LoadMap(_chaliceMapID))
                {
                    _chaliceLoadError = true;
                }
                else
                {
                    ImGui.CloseCurrentPopup();
                    _chaliceLoadError = false;
                    _chaliceMapID = "m29_";
                }
            }

            ImGui.EndPopup();
        }
    }

    private unsafe void MapObjectSelectable(Entity e, bool visicon, bool hierarchial = false)
    {
        var scale = Smithbox.GetUIScale();

        // Main selectable
        if (e is MsbEntity me)
        {
            ImGui.PushID(me.Type + e.Name);
        }
        else
        {
            ImGui.PushID(e.Name);
        }

        var doSelect = false;
        if (_setNextFocus)
        {
            ImGui.SetItemDefaultFocus();
            _setNextFocus = false;
            doSelect = true;
        }

        var nodeopen = false;
        var padding = hierarchial ? "   " : "    ";
        if (hierarchial && e.Children.Count > 0)
        {
            ImGuiTreeNodeFlags treeflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (_selection.GetSelection().Contains(e))
            {
                treeflags |= ImGuiTreeNodeFlags.Selected;
            }

            nodeopen = ImGui.TreeNodeEx(e.PrettyName, treeflags);
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                if (e.RenderSceneMesh != null)
                {
                    _viewport.FrameBox(e.RenderSceneMesh.GetBounds());
                }
            }
        }
        else
        {
            _mapEnt_ImGuiID++;

            string name = e.PrettyName;
            string aliasedName = name;
            var modelName = e.GetPropertyValue<string>("ModelName");

            if (modelName == null)
                modelName = "";

            if (CFG.Current.MapEditor_Show_Character_Names_in_Scene_Tree)
            {
                if (e.IsPartEnemy())
                {
                    if (_chrAliasCache != null && _chrAliasCache.ContainsKey(modelName))
                    {
                        aliasedName = $"{name} {_chrAliasCache[modelName]}";
                    }
                    else
                    {
                        foreach (var entry in ModelAliasBank.Bank.AliasNames.GetEntries("Characters"))
                        {
                            if (modelName == entry.id)
                            {
                                aliasedName = $" {{ {entry.name} }}";

                                if (_chrAliasCache == null)
                                {
                                    _chrAliasCache = new Dictionary<string, string>();
                                }

                                if (!_chrAliasCache.ContainsKey(entry.id))
                                {
                                    _chrAliasCache.Add(modelName, aliasedName);
                                }
                            }
                        }
                    }
                }
            }

            if (ImGui.Selectable(padding + aliasedName + "##" + _mapEnt_ImGuiID,
                    _selection.GetSelection().Contains(e),
                    ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowItemOverlap))
            {
                // If double clicked frame the selection in the viewport
                if (ImGui.IsMouseDoubleClicked(0))
                {
                    if (e.RenderSceneMesh != null)
                    {
                        _viewport.FrameBox(e.RenderSceneMesh.GetBounds());
                    }
                }
            }
        }

        if (ImGui.IsItemClicked(0))
        {
            _pendingClick = e;
        }

        if (_pendingClick == e && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (ImGui.IsItemHovered())
            {
                doSelect = true;
            }

            _pendingClick = null;
        }

        // Up/Down arrow mass selection
        var arrowKeySelect = false;
        if (ImGui.IsItemFocused()
            && (InputTracker.GetKey(Key.Up) || InputTracker.GetKey(Key.Down)))
        {
            doSelect = true;
            arrowKeySelect = true;
        }

        if (hierarchial && doSelect)
        {
            if (nodeopen && !_treeOpenEntities.Contains(e) ||
                !nodeopen && _treeOpenEntities.Contains(e))
            {
                doSelect = false;
            }

            if (nodeopen && !_treeOpenEntities.Contains(e))
            {
                _treeOpenEntities.Add(e);
            }
            else if (!nodeopen && _treeOpenEntities.Contains(e))
            {
                _treeOpenEntities.Remove(e);
            }
        }

        if (_selection.ShouldGoto(e))
        {
            // By default, this places the item at 50% in the frame. Use 0 to place it on top.
            ImGui.SetScrollHereY();
            _selection.ClearGotoTarget();
        }

        if (ImGui.BeginPopupContextItem())
        {
            _handler.OnEntityContextMenu(e);
            ImGui.EndPopup();
        }

        if (ImGui.BeginDragDropSource())
        {
            ImGui.Text(e.PrettyName);
            // Kinda meme
            DragDropPayload p = new();
            p.Entity = e;
            _dragDropPayloads.Add(_dragDropPayloadCounter, p);
            DragDropPayloadReference r = new();
            r.Index = _dragDropPayloadCounter;
            _dragDropPayloadCounter++;
            GCHandle handle = GCHandle.Alloc(r, GCHandleType.Pinned);
            ImGui.SetDragDropPayload("entity", handle.AddrOfPinnedObject(), (uint)sizeof(DragDropPayloadReference));
            ImGui.EndDragDropSource();
            handle.Free();
            _initiatedDragDrop = true;
        }

        if (hierarchial && ImGui.BeginDragDropTarget())
        {
            ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("entity");
            if (payload.NativePtr != null)
            {
                var h = (DragDropPayloadReference*)payload.Data;
                DragDropPayload pload = _dragDropPayloads[h->Index];
                _dragDropPayloads.Remove(h->Index);
                _dragDropSources.Add(pload.Entity);
                _dragDropDestObjects.Add(e);
                _dragDropDests.Add(e.Children.Count);
            }

            ImGui.EndDragDropTarget();
        }

        // Visibility icon
        if (visicon)
        {
            ImGui.SetItemAllowOverlap();
            var visible = e.EditorVisible;
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 18.0f * Smithbox.GetUIScale());
            ImGui.PushStyleColor(ImGuiCol.Text, visible
                ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.TextWrapped(visible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
            ImGui.PopStyleColor();
            if (ImGui.IsItemClicked(0))
            {
                e.EditorVisible = !e.EditorVisible;
                doSelect = false;
            }
        }

        // If the visibility icon wasn't clicked, perform the selection
        Utils.EntitySelectionHandler(_selection, e, doSelect, arrowKeySelect);

        // Invisible item to be a drag drop target between nodes
        if (_pendingDragDrop)
        {
            if (e is MsbEntity me2)
            {
                ImGui.SetItemAllowOverlap();
                ImGui.InvisibleButton(me2.Type + e.Name, new Vector2(-1, 3.0f) * scale);
            }
            else
            {
                ImGui.SetItemAllowOverlap();
                ImGui.InvisibleButton(e.Name, new Vector2(-1, 3.0f) * scale);
            }

            if (ImGui.IsItemFocused())
            {
                _setNextFocus = true;
            }

            if (ImGui.BeginDragDropTarget())
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("entity");
                if (payload.NativePtr != null) //todo: never passes
                {
                    var h = (DragDropPayloadReference*)payload.Data;
                    DragDropPayload pload = _dragDropPayloads[h->Index];
                    _dragDropPayloads.Remove(h->Index);
                    if (hierarchial)
                    {
                        _dragDropSources.Add(pload.Entity);
                        _dragDropDestObjects.Add(e.Parent);
                        _dragDropDests.Add(e.Parent.ChildIndex(e) + 1);
                    }
                    else
                    {
                        _dragDropSources.Add(pload.Entity);
                        _dragDropDests.Add(pload.Entity.Container.Objects.IndexOf(e) + 1);
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        // If there's children then draw them
        if (nodeopen)
        {
            HierarchyView(e);
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    private void HierarchyView(Entity entity)
    {
        foreach (Entity obj in entity.Children)
        {
            if (obj is Entity e)
            {
                MapObjectSelectable(e, true, true);
            }
        }
    }

    private void FlatView(Map map)
    {
        foreach (Entity obj in map.Objects)
        {
            if (obj is MsbEntity e)
            {
                MapObjectSelectable(e, true);
            }
        }
    }

    private void TypeView(Map map)
    {
        if (_cachedTypeView == null || !_cachedTypeView.ContainsKey(map.Name))
        {
            RebuildTypeViewCache(map);
        }

        foreach (KeyValuePair<MsbEntity.MsbEntityType, Dictionary<Type, List<MsbEntity>>> cats in
                 _cachedTypeView[map.Name].OrderBy(q => q.Key.ToString()))
        {
            if (cats.Value.Count > 0)
            {
                if (ImGui.TreeNodeEx(cats.Key.ToString(), ImGuiTreeNodeFlags.OpenOnArrow))
                {
                    foreach (KeyValuePair<Type, List<MsbEntity>> typ in cats.Value.OrderBy(q => q.Key.Name))
                    {
                        if (typ.Value.Count > 0)
                        {
                            // Regions don't have multiple types in certain games
                            if (cats.Key == MsbEntity.MsbEntityType.Region &&
                                Project.Type is ProjectType.DES
                                    or ProjectType.DS1
                                    or ProjectType.DS1R
                                    or ProjectType.BB)
                            {
                                foreach (MsbEntity obj in typ.Value)
                                {
                                    MapObjectSelectable(obj, true);
                                }
                            }
                            else if (cats.Key == MsbEntity.MsbEntityType.Light)
                            {
                                foreach (Entity parent in map.BTLParents)
                                {
                                    var parentAD = (AssetDescription)parent.WrappedObject;
                                    if (ImGui.TreeNodeEx($"{typ.Key.Name} {parentAD.AssetName}",
                                            ImGuiTreeNodeFlags.OpenOnArrow))
                                    {
                                        ImGui.SetItemAllowOverlap();
                                        var visible = parent.EditorVisible;
                                        ImGui.SameLine();
                                        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X -
                                                            18.0f * Smithbox.GetUIScale());
                                        ImGui.PushStyleColor(ImGuiCol.Text, visible
                                            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                                            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                                        ImGui.TextWrapped(visible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
                                        ImGui.PopStyleColor();
                                        if (ImGui.IsItemClicked(0))
                                        {
                                            // Hide/Unhide all lights within this BTL.
                                            parent.EditorVisible = !parent.EditorVisible;
                                        }

                                        foreach (Entity obj in parent.Children)
                                        {
                                            MapObjectSelectable(obj, true);
                                        }

                                        ImGui.TreePop();
                                    }
                                    else
                                    {
                                        ImGui.SetItemAllowOverlap();
                                        var visible = parent.EditorVisible;
                                        ImGui.SameLine();
                                        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X -
                                                            18.0f * Smithbox.GetUIScale());
                                        ImGui.PushStyleColor(ImGuiCol.Text, visible
                                            ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                                            : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                                        ImGui.TextWrapped(visible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
                                        ImGui.PopStyleColor();
                                        if (ImGui.IsItemClicked(0))
                                        {
                                            // Hide/Unhide all lights within this BTL.
                                            parent.EditorVisible = !parent.EditorVisible;
                                        }
                                    }
                                }
                            }
                            else if (ImGui.TreeNodeEx(typ.Key.Name, ImGuiTreeNodeFlags.OpenOnArrow))
                            {
                                foreach (MsbEntity obj in typ.Value)
                                {
                                    MapObjectSelectable(obj, true);
                                }

                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.Text($@"   {typ.Key}");
                        }
                    }

                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.Text($@"   {cats.Key.ToString()}");
            }
        }
    }

    public void OnGui()
    {
        var scale = Smithbox.GetUIScale();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.145f, 0.145f, 0.149f, 1.0f));
        if (_configuration == Configuration.MapEditor)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 2.0f) * scale);
        }

        var titleString = _configuration == Configuration.MapEditor
            ? $@"Map Object List##{_id}"
            : $@"Model Hierarchy##{_id}";
        if (ImGui.Begin(titleString))
        {
            if (_initiatedDragDrop)
            {
                _initiatedDragDrop = false;
                _pendingDragDrop = true;
            }

            if (_pendingDragDrop && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _pendingDragDrop = false;
            }

            ImGui.PopStyleVar();

            if (Smithbox.LowRequirementsMode)
            {
                ImGui.NewLine();
                ImGui.Text("  This editor is not available in low requirements mode.");
                ImGui.End();
                ImGui.PopStyleColor();
                return;
            }

            if (_configuration == Configuration.MapEditor)
            {
                if (Project.Type is ProjectType.DS2S)
                {
                    if (ParamBank.PrimaryBank.IsLoadingParams)
                    {
                        ImGui.NewLine();
                        ImGui.Text("  Please wait for params to finish loading.");
                        ImGui.End();
                        ImGui.PopStyleColor();
                        return;
                    }
                }

                ImGui.Spacing();
                ImGui.Indent(30 * scale);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("List Sorting Style:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);

                var mode = (int)_viewMode;
                if (ImGui.Combo("##typecombo", ref mode, _viewModeStrings, _viewModeStrings.Length))
                {
                    _viewMode = (ViewMode)mode;
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Map ID Search:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##treeSearch", ref _mapNameSearchStr, 99);

                ImGui.Unindent(30 * scale);
            }

            ImGui.BeginChild("listtree");
            if (_configuration == Configuration.MapEditor && _universe.LoadedObjectContainers.Count == 0)
            {
                if (_universe.GameType == ProjectType.Undefined)
                {
                    ImGui.Text("No project loaded. File -> New Project");
                }
                else
                {
                    ImGui.Text("This Editor requires unpacked game files. Use UXM");
                }
            }

            if (_configuration == Configuration.MapEditor && Project.Type == ProjectType.AC6 &&
                FeatureFlags.AC6_MSB == false)
            {
                ImGui.Indent();
                ImGui.Spacing();
                ImGui.Text("AC6 map editing is unsupported for now.");
                ImGui.Spacing();
                ImGui.BeginDisabled();
            }

            IOrderedEnumerable<KeyValuePair<string, MapObjectContainer>> orderedMaps =
                _universe.LoadedObjectContainers.OrderBy(k => k.Key);

            _mapEnt_ImGuiID = 0;
            foreach (KeyValuePair<string, MapObjectContainer> lm in orderedMaps)
            {
                var metaName = "";
                MapObjectContainer map = lm.Value;
                var mapid = lm.Key;
                if (mapid == null)
                {
                    continue;
                }

                if (MapAliasBank.Bank.MapNames != null)
                {
                    if (MapAliasBank.Bank.MapNames.ContainsKey(mapid))
                    {
                        metaName = MapAliasBank.Bank.MapNames[mapid];
                    }
                }

                // Map name search filter
                if (_mapNameSearchStr != ""
                    && (!CFG.Current.MapEditor_Always_List_Loaded_Maps || map == null)
                    && !lm.Key.Contains(_mapNameSearchStr, StringComparison.CurrentCultureIgnoreCase)
                    && !metaName.Contains(_mapNameSearchStr, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                Entity mapRoot = map?.RootObject;
                MapObjectContainerReference mapRef = new(mapid, _universe);
                ISelectable selectTarget = (ISelectable)mapRoot ?? mapRef;

                ImGuiTreeNodeFlags treeflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                var selected = _selection.GetSelection().Contains(mapRoot) ||
                               _selection.GetSelection().Contains(mapRef);
                if (selected)
                {
                    treeflags |= ImGuiTreeNodeFlags.Selected;
                }

                var nodeopen = false;
                var unsaved = map != null && map.HasUnsavedChanges ? "*" : "";
                ImGui.BeginGroup();
                if (map != null)
                {
                    nodeopen = ImGui.TreeNodeEx($@"{ForkAwesome.Cube} {mapid}", treeflags,
                        $@"{ForkAwesome.Cube} {mapid}{unsaved}");
                }
                else
                {
                    ImGui.Selectable($@"   {ForkAwesome.Cube} {mapid}", selected);
                }

                if (metaName != "")
                {
                    ImGui.SameLine();
                    ImGui.PushTextWrapPos();
                    if (metaName.StartsWith("--")) // Marked as normally unused (use red text)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), @$"<{metaName.Replace("--", "")}>");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), @$"<{metaName}>");
                    }

                    ImGui.PopTextWrapPos();
                }

                ImGui.EndGroup();
                if (_selection.ShouldGoto(mapRoot) || _selection.ShouldGoto(mapRef))
                {
                    ImGui.SetScrollHereY();
                    _selection.ClearGotoTarget();
                }

                if (nodeopen)
                {
                    ImGui.Indent(); //TreeNodeEx fails to indent as it is inside a group / indentation is reset
                }

                // Right click context menu
                if (ImGui.BeginPopupContextItem($@"mapcontext_{mapid}"))
                {
                    if (map == null)
                    {
                        if (ImGui.Selectable("Load Map"))
                        {
                            if (selected)
                            {
                                _selection.ClearSelection();
                            }

                            _universe.LoadMap(mapid, selected);
                        }
                    }
                    else if (map is Map m)
                    {
                        if (ImGui.Selectable("Save Map"))
                        {
                            try
                            {
                                if (Project.Type == ProjectType.AC6 && FeatureFlags.AC6_MSB_Saving == false)
                                {
                                    TaskLogs.AddLog("AC6 Map saving has been disabled", LogLevel.Warning, TaskLogs.LogPriority.Normal);
                                }
                                else
                                {
                                    _universe.SaveMap(m);
                                }
                            }
                            catch (SavingFailedException e)
                            {
                                ((MapEditorScreen)_handler).HandleSaveException(e);
                            }
                        }

                        if (ImGui.Selectable("Unload Map"))
                        {
                            _selection.ClearSelection();
                            _editorActionManager.Clear();
                            _universe.UnloadContainer(m);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }
                    }

                    if (_universe.GameType is ProjectType.ER)
                    {
                        if (mapid.StartsWith("m60"))
                        {
                            if (ImGui.Selectable("Load Related Maps"))
                            {
                                if (selected)
                                {
                                    _selection.ClearSelection();
                                }

                                _universe.LoadMap(mapid);
                                _universe.LoadRelatedMapsER(mapid, _universe.LoadedObjectContainers);
                            }
                        }
                    }
                    else if (_universe.GameType is ProjectType.AC6)
                    {
                        //TODO AC6
                    }

                    if (_universe.GetLoadedMapCount() > 1)
                    {
                        if (ImGui.Selectable("Unload All Maps"))
                        {
                            DialogResult result = PlatformUtils.Instance.MessageBox("Unload all maps?", "Confirm",
                                MessageBoxButtons.YesNo);
                            if (result == DialogResult.Yes)
                            {
                                _selection.ClearSelection();
                                _editorActionManager.Clear();
                                _universe.UnloadAllMaps();
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }
                        }
                    }

                    ImGui.EndPopup();
                }

                if (ImGui.IsItemClicked())
                {
                    _pendingClick = selectTarget;
                }

                if (ImGui.IsMouseDoubleClicked(0) && _pendingClick != null && mapRoot == _pendingClick)
                {
                    _viewport.FramePosition(mapRoot.GetLocalTransform().Position, 10f);
                }

                if ((_pendingClick == mapRoot || mapRef.Equals(_pendingClick)) &&
                    ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    if (ImGui.IsItemHovered())
                    {
                        // Only select if a node is not currently being opened/closed
                        if (mapRoot == null ||
                            nodeopen && _treeOpenEntities.Contains(mapRoot) ||
                            !nodeopen && !_treeOpenEntities.Contains(mapRoot))
                        {
                            if (InputTracker.GetKey(Key.ControlLeft) || InputTracker.GetKey(Key.ControlRight))
                            {
                                // Toggle Selection
                                if (_selection.GetSelection().Contains(selectTarget))
                                {
                                    _selection.RemoveSelection(selectTarget);
                                }
                                else
                                {
                                    _selection.AddSelection(selectTarget);
                                }
                            }
                            else
                            {
                                _selection.ClearSelection();
                                _selection.AddSelection(selectTarget);
                            }
                        }

                        // Update the open/closed state
                        if (mapRoot != null)
                        {
                            if (nodeopen && !_treeOpenEntities.Contains(mapRoot))
                            {
                                _treeOpenEntities.Add(mapRoot);
                            }
                            else if (!nodeopen && _treeOpenEntities.Contains(mapRoot))
                            {
                                _treeOpenEntities.Remove(mapRoot);
                            }
                        }
                    }

                    _pendingClick = null;
                }

                if (nodeopen)
                {
                    if (_pendingDragDrop)
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 0.0f) * scale);
                    }
                    else
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 3.0f) * scale);
                    }

                    if (_viewMode == ViewMode.Hierarchy)
                    {
                        HierarchyView(map.RootObject);
                    }
                    else if (_viewMode == ViewMode.Flat)
                    {
                        FlatView((Map)map);
                    }
                    else if (_viewMode == ViewMode.ObjectType)
                    {
                        TypeView((Map)map);
                    }

                    ImGui.PopStyleVar();
                    ImGui.TreePop();
                }

                // Update type cache when a map is no longer loaded
                if (_cachedTypeView != null && map == null && _cachedTypeView.ContainsKey(mapid))
                {
                    _cachedTypeView.Remove(mapid);
                }
            }

            if (Project.Type == ProjectType.BB && _configuration == Configuration.MapEditor)
            {
                ChaliceDungeonImportButton();
            }

            if (_configuration == Configuration.MapEditor && Project.Type == ProjectType.AC6 &&
                FeatureFlags.AC6_MSB == false)
            {
                ImGui.EndDisabled();
            }

            ImGui.EndChild();

            if (_dragDropSources.Count > 0)
            {
                if (_dragDropDestObjects.Count > 0)
                {
                    ChangeEntityHierarchyAction action = new(_universe, _dragDropSources, _dragDropDestObjects,
                        _dragDropDests, false);
                    _editorActionManager.ExecuteAction(action);
                    _dragDropSources.Clear();
                    _dragDropDests.Clear();
                    _dragDropDestObjects.Clear();
                }
                else
                {
                    ReorderContainerObjectsAction action = new(_universe, _dragDropSources, _dragDropDests, false);
                    _editorActionManager.ExecuteAction(action);
                    _dragDropSources.Clear();
                    _dragDropDests.Clear();
                }
            }
        }
        else
        {
            ImGui.PopStyleVar();
        }

        ImGui.End();
        ImGui.PopStyleColor();
        _selection.ClearGotoTarget();
    }
}