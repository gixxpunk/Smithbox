﻿using ImGuiNET;
using StudioCore.UserProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StudioCore.Editors.TextureViewer.Toolbar;

public class TextureToolbar_Configuration
{
    public TextureToolbar_Configuration() { }

    public void OnGui()
    {
        if (Project.Type == ProjectType.Undefined)
            return;

        ImGui.PushStyleColor(ImGuiCol.Text, CFG.Current.ImGui_Default_Text_Color);
        ImGui.SetNextWindowSize(new Vector2(300.0f, 200.0f) * Smithbox.GetUIScale(), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Toolbar: Configuration##Toolbar_TextureViewer_Configuration"))
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

        TexAction_ExportTexture.Configure();

        TexAction_ExportTexture.Act();
    }
}