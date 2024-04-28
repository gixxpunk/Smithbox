﻿using ImGuiNET;
using StudioCore.UserProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.ParamEditor.Toolbar;

public class ParamToolbar_Configuration
{
    public ParamToolbar_Configuration() { }

    public void OnGui()
    {
        if (Project.Type == ProjectType.Undefined)
            return;

        ImGui.PushStyleColor(ImGuiCol.Text, CFG.Current.ImGui_Default_Text_Color);
        ImGui.SetNextWindowSize(new Vector2(300.0f, 200.0f) * Smithbox.GetUIScale(), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Toolbar##Toolbar_ParamEditor_Configuration"))
        {
            ShowSelectedConfiguration();
        }

        ImGui.End();
        ImGui.PopStyleColor(1);
    }

    public void ShowSelectedConfiguration()
    {
        ImGui.Indent(10.0f);
        ImGui.Separator();
        ImGui.Text("Configuration");
        ImGui.Separator();

        ParamAction_DuplicateRow.Configure();
        ParamAction_SortRows.Configure();
        ParamAction_ImportRowNames.Configure();
        ParamAction_ExportRowNames.Configure();
        ParamAction_TrimRowNames.Configure();
        ParamAction_MassEdit.Configure();
        ParamAction_MassEditScripts.Configure();
        ParamAction_FindRowIdInstances.Configure();
        ParamAction_FindValueInstances.Configure();

        ParamAction_DuplicateRow.Act();
        ParamAction_SortRows.Act();
        ParamAction_ImportRowNames.Act();
        ParamAction_ExportRowNames.Act();
        ParamAction_TrimRowNames.Act();
        ParamAction_MassEdit.Act();
        ParamAction_MassEditScripts.Act();
        ParamAction_FindRowIdInstances.Act();
        ParamAction_FindValueInstances.Act();
    }
}