﻿using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Core;
using StudioCore.Editor;
using StudioCore.Editors.MapEditor;
using StudioCore.Locators;
using StudioCore.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StudioCore.TextEditor;

/// <summary>
///     FMG sections in UI
/// </summary>
public enum FmgUICategory
{
    Text = 0,
    Item = 1,
    Menu = 2
}

/// <summary>
///     Entry type for Title, Summary, Description, or other.
/// </summary>
public enum FmgEntryTextType
{
    TextBody = 0,
    Title = 1,
    Summary = 2,
    Description = 3,
    ExtraText = 4
}

/// <summary>
///     Text categories used for grouping multiple FMGs or broad identification
/// </summary>
public enum FmgEntryCategory
{
    None = -1,
    Goods,
    Weapons,
    Armor,
    Rings,
    Spells,
    Characters,
    Locations,
    Gem,
    Message,
    SwordArts,
    Effect,
    ActionButtonText,
    Tutorial,
    LoadingScreen,
    Generator,
    Booster,
    FCS,
    Mission,
    Archive,

    ItemFmgDummy = 200 // Anything with this will be sorted into the item section of the editor.
}

/// <summary>
///     BND IDs for FMG files used for identification
/// </summary>
public enum FmgIDType
{
    // Note: Matching names with _DLC and _PATCH are used as identifiers for patch FMGs. This is a little dumb and patch fmg handling should probably be redone.
    None = -1,

    TitleGoods = 10,
    TitleWeapons = 11,
    TitleArmor = 12,
    TitleRings = 13,
    TitleSpells = 14,
    TitleTest = 15,
    TitleTest2 = 16,
    TitleTest3 = 17,
    TitleCharacters = 18,
    TitleLocations = 19,
    SummaryGoods = 20,
    SummaryWeapons = 21,
    SummaryArmor = 22,
    SummaryRings = 23,
    DescriptionGoods = 24,
    DescriptionWeapons = 25,
    DescriptionArmor = 26,
    DescriptionRings = 27,
    SummarySpells = 28,
    DescriptionSpells = 29,

    //
    TalkMsg = 1,
    BloodMsg = 2,
    MovieSubtitle = 3,
    Event = 30,
    MenuInGame = 70,
    MenuCommon = 76,
    MenuOther = 77,
    MenuDialog = 78,
    MenuKeyGuide = 79,
    MenuLineHelp = 80,
    MenuContext = 81,
    MenuTags = 90,
    Win32Tags = 91,
    Win32Messages = 92,
    Event_Patch = 101,
    MenuDialog_Patch = 102,
    Win32Messages_Patch = 103,
    TalkMsg_Patch = 104,
    BloodMsg_Patch = 107,
    MenuLineHelp_Patch = 121,
    MenuKeyGuide_Patch = 122,
    MenuOther_Patch = 123,
    MenuCommon_Patch = 124,

    // DS1 _DLC
    DescriptionGoods_Patch = 100,
    DescriptionSpells_Patch = 105,
    DescriptionWeapons_Patch = 106,
    DescriptionArmor_Patch = 108,
    DescriptionRings_Patch = 109,
    SummaryGoods_Patch = 110,
    TitleGoods_Patch = 111,
    SummaryRings_Patch = 112,
    TitleRings_Patch = 113,
    SummaryWeapons_Patch = 114,
    TitleWeapons_Patch = 115,
    SummaryArmor_Patch = 116,
    TitleArmor_Patch = 117,
    TitleSpells_Patch = 118,
    TitleCharacters_Patch = 119,
    TitleLocations_Patch = 120,

    // DS3 _DLC1
    TitleWeapons_DLC1 = 211,
    TitleArmor_DLC1 = 212,
    TitleRings_DLC1 = 213,
    TitleSpells_DLC1 = 214,
    TitleCharacters_DLC1 = 215,
    TitleLocations_DLC1 = 216,
    SummaryGoods_DLC1 = 217,
    SummaryRings_DLC1 = 220,
    DescriptionGoods_DLC1 = 221,
    DescriptionWeapons_DLC1 = 222,
    DescriptionArmor_DLC1 = 223,
    DescriptionRings_DLC1 = 224,
    SummarySpells_DLC1 = 225,
    DescriptionSpells_DLC1 = 226,

    //
    Modern_MenuText = 200,
    Modern_LineHelp = 201,
    Modern_KeyGuide = 202,
    Modern_SystemMessage_win64 = 203,
    Modern_Dialogues = 204,
    TalkMsg_DLC1 = 230,
    Event_DLC1 = 231,
    Modern_MenuText_DLC1 = 232,
    Modern_LineHelp_DLC1 = 233,
    Modern_SystemMessage_win64_DLC1 = 235,
    Modern_Dialogues_DLC1 = 236,
    SystemMessage_PS4_DLC1 = 237,
    SystemMessage_XboxOne_DLC1 = 238,
    BloodMsg_DLC1 = 239,

    // DS3 _DLC2
    TitleGoods_DLC2 = 250,
    TitleWeapons_DLC2 = 251,
    TitleArmor_DLC2 = 252,
    TitleRings_DLC2 = 253,
    TitleSpells_DLC2 = 254,
    TitleCharacters_DLC2 = 255,
    TitleLocations_DLC2 = 256,
    SummaryGoods_DLC2 = 257,
    SummaryRings_DLC2 = 260,
    DescriptionGoods_DLC2 = 261,
    DescriptionWeapons_DLC2 = 262,
    DescriptionArmor_DLC2 = 263,
    DescriptionRings_DLC2 = 264,
    SummarySpells_DLC2 = 265,
    DescriptionSpells_DLC2 = 266,

    //
    TalkMsg_DLC2 = 270,
    Event_DLC2 = 271,
    Modern_MenuText_DLC2 = 272,
    Modern_LineHelp_DLC2 = 273,
    Modern_SystemMessage_win64_DLC2 = 275,
    Modern_Dialogues_DLC2 = 276,
    SystemMessage_PS4_DLC2 = 277,
    SystemMessage_XboxOne_DLC2 = 278,
    BloodMsg_DLC2 = 279,

    // SDT
    Skills = 40,

    // ER - Item
    DescriptionGem = 37,
    SummarySwordArts = 43,
    WeaponEffect = 44,
    ERUnk45 = 45,
    GoodsInfo2 = 46,

    // ER - Menu
    TalkMsg_FemalePC_Alt = 4,
    NetworkMessage = 31,
    EventTextForTalk = 33,
    EventTextForMap = 34,
    TutorialTitle = 207,
    TutorialBody = 208,
    TextEmbedImageName_win64 = 209,

    // ER - Item: SOTE - DLC1
    TitleWeapons_SOTE = 310,
    SummaryWeapons_SOTE = 311,
    DescriptionWeapons_SOTE = 312,
    TitleArmor_SOTE = 313,
    SummaryArmor_SOTE = 314,
    DescriptionArmor_SOTE = 315,
    TitleGoods_SOTE = 319,
    SummaryGoods_SOTE = 320,
    DescriptionGoods_SOTE = 321,
    TitleRings_SOTE = 316,
    SummaryRings_SOTE = 317,
    DescriptionRings_SOTE = 318,
    TitleGem_SOTE = 322,
    SummaryGem_SOTE = 323,
    DescriptionGem_SOTE = 324,
    TitleSpells_SOTE = 325,
    SummarySpells_SOTE = 326,
    DescriptionSpells_SOTE = 327,
    TitleCharacters_SOTE = 328,
    TitleLocations_SOTE = 329,

    GoodsDialog = 330,
    TitleSwordArts_SOTE = 331,
    SummarySwordArts_SOTE = 332,
    WeaponEffect_SOTE = 333,
    ERUnk45_SOTE = 334,
    GoodsInfo2_SOTE = 335,

    // ER - Menu: SOTE - DLC1
    TalkMsg_SOTE = 360,
    BloodMsg_SOTE = 361,
    MovieSubtitle_SOTE = 362,
    NetworkMessage_SOTE = 364,
    ActionButtonText_SOTE = 365,
    EventTextForTalk_SOTE = 366,
    EventTextForMap_SOTE = 367,
    Modern_MenuText_SOTE = 368,
    Modern_LineHelp_SOTE = 369,
    Modern_KeyGuide_SOTE = 370,
    Modern_SystemMessage_win64_SOTE = 371,
    Modern_Dialogues_SOTE = 372,
    LoadingTitle_SOTE = 373,
    LoadingText_SOTE = 374,
    TutorialTitle_SOTE = 375,
    TutorialBody_SOTE = 376,

    // ER - Item: SOTE - DLC2
    TitleWeapons_SOTE_DLC2 = 410,
    SummaryWeapons_SOTE_DLC2 = 411,
    DescriptionWeapons_SOTE_DLC2 = 412,
    TitleArmor_SOTE_DLC2 = 413,
    SummaryArmor_SOTE_DLC2 = 414,
    DescriptionArmor_SOTE_DLC2 = 415,
    TitleGoods_SOTE_DLC2 = 419,
    SummaryGoods_SOTE_DLC2 = 420,
    DescriptionGoods_SOTE_DLC2 = 421,
    TitleRings_SOTE_DLC2 = 416,
    SummaryRings_SOTE_DLC2 = 417,
    DescriptionRings_SOTE_DLC2 = 418,
    TitleGem_SOTE_DLC2 = 422,
    SummaryGem_SOTE_DLC2 = 423,
    DescriptionGem_SOTE_DLC2 = 424,
    TitleSpells_SOTE_DLC2 = 425,
    SummarySpells_SOTE_DLC2 = 426,
    DescriptionSpells_SOTE_DLC2 = 427,
    TitleCharacters_SOTE_DLC2 = 428,
    TitleLocations_SOTE_DLC2 = 429,

    GoodsDialog_SOTE_DLC2 = 430,
    TitleSwordArts_SOTE_DLC2 = 431,
    SummarySwordArts_SOTE_DLC2 = 432,
    WeaponEffect_SOTE_DLC2 = 433,
    ERUnk45_SOTE_DLC2 = 434,
    GoodsInfo2_SOTE_DLC2 = 435,

    // ER - Menu: SOTE - DLC2
    TalkMsg_SOTE_DLC2 = 460,
    BloodMsg_SOTE_DLC2 = 461,
    MovieSubtitle_SOTE_DLC2 = 462,
    NetworkMessage_SOTE_DLC2 = 464,
    ActionButtonText_SOTE_DLC2 = 465,
    EventTextForTalk_SOTE_DLC2 = 466,
    EventTextForMap_SOTE_DLC2 = 467,
    Modern_MenuText_SOTE_DLC2 = 468,
    Modern_LineHelp_SOTE_DLC2 = 469,
    Modern_KeyGuide_SOTE_DLC2 = 470,
    Modern_SystemMessage_win64_SOTE_DLC2 = 471,
    Modern_Dialogues_SOTE_DLC2 = 472,
    LoadingTitle_SOTE_DLC2 = 473,
    LoadingText_SOTE_DLC2 = 474,
    TutorialTitle_SOTE_DLC2 = 475,
    TutorialBody_SOTE_DLC2 = 476,

    // AC6 - Item
    TitleBooster = 38,
    DescriptionBooster = 39,

    // AC6 - Menu
    RankerProfile = 50,
    TitleMission = 60,
    SummaryMission = 61,
    DescriptionMission = 62,
    MissionLocation = 63,
    TitleArchive = 65,
    DescriptionArchive = 66,
    TutorialTitle2023 = 73,
    TutorialBody2023 = 74,

    // Multiple use cases. Differences are applied in ApplyGameDifferences();
    // ER: ActionButtonText
    ReusedFMG_32 = 32, 

    // FMG 32
    // BB:  GemExtraInfo
    // DS3: ActionButtonText
    // SDT: ActionButtonText
    // ER:  ActionButtonText
    ReusedFMG_35 = 35,

    // FMG 35
    // Most: TitleGem
    // AC6:  TitleGenerator
    ReusedFMG_36 = 36,

    // FMG 36
    // Most: SummaryGem
    // AC6:  DescriptionGenerator
    ReusedFMG_41 = 41,

    // FMG 41
    // Most: TitleMessage
    // AC6:  TitleFCS
    ReusedFMG_42 = 42,

    // FMG 42
    // Most: TitleSwordArts
    // AC6:  DescriptionFCS
    ReusedFMG_210 = 210,

    // FMG 210
    // DS3: TitleGoods_DLC1
    // SDT: ?
    // ER:  ToS_win64
    // AC6: TextEmbeddedImageNames
    ReusedFMG_205 = 205,

    // FMG 205
    // DS3: SystemMessage_PS4
    // SDT: TutorialText
    // ER:  LoadingTitle
    // AC6: MenuContext
    ReusedFMG_206 = 206
    // FMG 206
    // DS3: SystemMessage_XboxOne 
    // SDT: TutorialTitle
    // ER:  LoadingText
}

/// <summary>
///     Static class that stores all the strings for a Souls game.
/// </summary>
public static partial class FMGBank
{
    /// <summary>
    ///     List of strings to compare with "FmgIDType" name to identify patch FMGs.
    /// </summary>
    private static readonly List<string> patchStrings = new() { "_Patch", "_DLC1", "_DLC2", "_SOTE", "_SOTE_DLC2" };

    public static bool IsLoaded { get; private set; }
    public static bool IsLoading { get; private set; }
    public static string LanguageFolder { get; private set; } = "";

    public static List<FMGInfo> FmgInfoBank { get; private set; } = new();

    public static Dictionary<FmgUICategory, bool> ActiveUITypes { get; private set; } = new();

    /// <summary>
    ///     Get category for grouped entries (Goods, Weapons, etc)
    /// </summary>
    public static FmgEntryCategory GetFmgCategory(int id)
    {
        switch ((FmgIDType)id)
        {
            case FmgIDType.TitleTest:
            case FmgIDType.TitleTest2:
            case FmgIDType.TitleTest3:
            case FmgIDType.ERUnk45:
                return FmgEntryCategory.None;

            case FmgIDType.DescriptionGoods:
            case FmgIDType.DescriptionGoods_Patch:
            case FmgIDType.DescriptionGoods_DLC1:
            case FmgIDType.DescriptionGoods_DLC2:
            case FmgIDType.SummaryGoods:
            case FmgIDType.SummaryGoods_Patch:
            case FmgIDType.SummaryGoods_DLC1:
            case FmgIDType.SummaryGoods_DLC2:
            case FmgIDType.TitleGoods:
            case FmgIDType.TitleGoods_Patch:
            case FmgIDType.TitleGoods_DLC2:
            case FmgIDType.GoodsInfo2:
            case FmgIDType.TitleGoods_SOTE:
            case FmgIDType.SummaryGoods_SOTE:
            case FmgIDType.DescriptionGoods_SOTE:
                return FmgEntryCategory.Goods;

            case FmgIDType.DescriptionWeapons:
            case FmgIDType.DescriptionWeapons_DLC1:
            case FmgIDType.DescriptionWeapons_DLC2:
            case FmgIDType.SummaryWeapons:
            case FmgIDType.TitleWeapons:
            case FmgIDType.TitleWeapons_DLC1:
            case FmgIDType.TitleWeapons_DLC2:
            case FmgIDType.DescriptionWeapons_Patch:
            case FmgIDType.SummaryWeapons_Patch:
            case FmgIDType.TitleWeapons_Patch:
            case FmgIDType.TitleWeapons_SOTE:
            case FmgIDType.SummaryWeapons_SOTE:
            case FmgIDType.DescriptionWeapons_SOTE:
            case FmgIDType.WeaponEffect:
            case FmgIDType.WeaponEffect_SOTE:
                return FmgEntryCategory.Weapons;

            case FmgIDType.DescriptionArmor:
            case FmgIDType.DescriptionArmor_DLC1:
            case FmgIDType.DescriptionArmor_DLC2:
            case FmgIDType.SummaryArmor:
            case FmgIDType.TitleArmor:
            case FmgIDType.TitleArmor_DLC1:
            case FmgIDType.TitleArmor_DLC2:
            case FmgIDType.DescriptionArmor_Patch:
            case FmgIDType.SummaryArmor_Patch:
            case FmgIDType.TitleArmor_Patch:
            case FmgIDType.TitleArmor_SOTE:
            case FmgIDType.SummaryArmor_SOTE:
            case FmgIDType.DescriptionArmor_SOTE:
                return FmgEntryCategory.Armor;

            case FmgIDType.DescriptionRings:
            case FmgIDType.DescriptionRings_DLC1:
            case FmgIDType.DescriptionRings_DLC2:
            case FmgIDType.SummaryRings:
            case FmgIDType.SummaryRings_DLC1:
            case FmgIDType.SummaryRings_DLC2:
            case FmgIDType.TitleRings:
            case FmgIDType.TitleRings_DLC1:
            case FmgIDType.TitleRings_DLC2:
            case FmgIDType.DescriptionRings_Patch:
            case FmgIDType.SummaryRings_Patch:
            case FmgIDType.TitleRings_Patch:
            case FmgIDType.TitleRings_SOTE:
            case FmgIDType.SummaryRings_SOTE:
            case FmgIDType.DescriptionRings_SOTE:
                return FmgEntryCategory.Rings;

            case FmgIDType.DescriptionSpells:
            case FmgIDType.DescriptionSpells_DLC1:
            case FmgIDType.DescriptionSpells_DLC2:
            case FmgIDType.SummarySpells:
            case FmgIDType.SummarySpells_DLC1:
            case FmgIDType.SummarySpells_DLC2:
            case FmgIDType.TitleSpells:
            case FmgIDType.TitleSpells_DLC1:
            case FmgIDType.TitleSpells_DLC2:
            case FmgIDType.DescriptionSpells_Patch:
            case FmgIDType.TitleSpells_Patch:
            case FmgIDType.TitleSpells_SOTE:
            case FmgIDType.SummarySpells_SOTE:
            case FmgIDType.DescriptionSpells_SOTE:
                return FmgEntryCategory.Spells;

            case FmgIDType.TitleCharacters:
            case FmgIDType.TitleCharacters_DLC1:
            case FmgIDType.TitleCharacters_DLC2:
            case FmgIDType.TitleCharacters_Patch:
            case FmgIDType.TitleCharacters_SOTE:
                return FmgEntryCategory.Characters;

            case FmgIDType.TitleLocations:
            case FmgIDType.TitleLocations_DLC1:
            case FmgIDType.TitleLocations_DLC2:
            case FmgIDType.TitleLocations_Patch:
            case FmgIDType.TitleLocations_SOTE:
                return FmgEntryCategory.Locations;

            case FmgIDType.DescriptionGem:
            case FmgIDType.TitleGem_SOTE:
            case FmgIDType.SummaryGem_SOTE:
            case FmgIDType.DescriptionGem_SOTE:
                return FmgEntryCategory.Gem;

            case FmgIDType.TitleSwordArts_SOTE:
            case FmgIDType.SummarySwordArts_SOTE:
            case FmgIDType.SummarySwordArts:
                return FmgEntryCategory.SwordArts;

            case FmgIDType.TutorialTitle:
            case FmgIDType.TutorialBody:
            case FmgIDType.TutorialTitle2023:
            case FmgIDType.TutorialBody2023:
            case FmgIDType.TutorialTitle_SOTE:
            case FmgIDType.TutorialBody_SOTE:
                return FmgEntryCategory.Tutorial;

            //return FmgEntryCategory.ItemFmgDummy;

            case FmgIDType.TitleMission:
            case FmgIDType.SummaryMission:
            case FmgIDType.DescriptionMission:
            case FmgIDType.MissionLocation:
                return FmgEntryCategory.Mission;

            case FmgIDType.TitleBooster:
            case FmgIDType.DescriptionBooster:
                return FmgEntryCategory.Booster;

            case FmgIDType.TitleArchive:
            case FmgIDType.DescriptionArchive:
                return FmgEntryCategory.Archive;

            default:
                return FmgEntryCategory.None;
        }
    }

    /// <summary>
    ///     Get entry text type (such as weapon Title, Summary, Description)
    /// </summary>
    public static FmgEntryTextType GetFmgTextType(int id)
    {
        switch ((FmgIDType)id)
        {
            case FmgIDType.DescriptionGoods:
            case FmgIDType.DescriptionGoods_DLC1:
            case FmgIDType.DescriptionGoods_DLC2:
            case FmgIDType.DescriptionWeapons:
            case FmgIDType.DescriptionWeapons_DLC1:
            case FmgIDType.DescriptionWeapons_DLC2:
            case FmgIDType.DescriptionArmor:
            case FmgIDType.DescriptionArmor_DLC1:
            case FmgIDType.DescriptionArmor_DLC2:
            case FmgIDType.DescriptionRings:
            case FmgIDType.DescriptionRings_DLC1:
            case FmgIDType.DescriptionRings_DLC2:
            case FmgIDType.DescriptionSpells:
            case FmgIDType.DescriptionSpells_DLC1:
            case FmgIDType.DescriptionSpells_DLC2:
            case FmgIDType.DescriptionArmor_Patch:
            case FmgIDType.DescriptionGoods_Patch:
            case FmgIDType.DescriptionRings_Patch:
            case FmgIDType.DescriptionSpells_Patch:
            case FmgIDType.DescriptionWeapons_Patch:
            case FmgIDType.DescriptionGem:
            case FmgIDType.SummarySwordArts: // Include as Description (for text box size)
            case FmgIDType.DescriptionBooster:
            case FmgIDType.DescriptionMission:
            case FmgIDType.DescriptionArchive:
            case FmgIDType.DescriptionWeapons_SOTE:
            case FmgIDType.DescriptionArmor_SOTE:
            case FmgIDType.DescriptionGoods_SOTE:
            case FmgIDType.DescriptionRings_SOTE:
            case FmgIDType.DescriptionGem_SOTE:
            case FmgIDType.DescriptionSpells_SOTE:
            case FmgIDType.SummarySwordArts_SOTE:
                return FmgEntryTextType.Description;

            case FmgIDType.SummaryGoods:
            case FmgIDType.SummaryGoods_DLC1:
            case FmgIDType.SummaryGoods_DLC2:
            case FmgIDType.SummaryWeapons:
            case FmgIDType.SummaryArmor:
            case FmgIDType.SummaryRings:
            case FmgIDType.SummaryRings_DLC1:
            case FmgIDType.SummaryRings_DLC2:
            case FmgIDType.SummarySpells:
            case FmgIDType.SummarySpells_DLC1:
            case FmgIDType.SummarySpells_DLC2:
            case FmgIDType.SummaryArmor_Patch:
            case FmgIDType.SummaryGoods_Patch:
            case FmgIDType.SummaryRings_Patch:
            case FmgIDType.SummaryWeapons_Patch:
            case FmgIDType.SummaryMission:
            case FmgIDType.TutorialTitle: // Include as summary (not all TutorialBody's have a title)
            case FmgIDType.TutorialTitle2023:
            case FmgIDType.SummaryWeapons_SOTE:
            case FmgIDType.SummaryArmor_SOTE:
            case FmgIDType.SummaryGoods_SOTE:
            case FmgIDType.SummaryRings_SOTE:
            case FmgIDType.SummaryGem_SOTE:
            case FmgIDType.SummarySpells_SOTE:
            case FmgIDType.TutorialTitle_SOTE:
                return FmgEntryTextType.Summary;

            case FmgIDType.TitleGoods:
            case FmgIDType.TitleGoods_DLC2:
            case FmgIDType.TitleWeapons:
            case FmgIDType.TitleWeapons_DLC1:
            case FmgIDType.TitleWeapons_DLC2:
            case FmgIDType.TitleArmor:
            case FmgIDType.TitleArmor_DLC1:
            case FmgIDType.TitleArmor_DLC2:
            case FmgIDType.TitleRings:
            case FmgIDType.TitleRings_DLC1:
            case FmgIDType.TitleRings_DLC2:
            case FmgIDType.TitleSpells:
            case FmgIDType.TitleSpells_DLC1:
            case FmgIDType.TitleSpells_DLC2:
            case FmgIDType.TitleCharacters:
            case FmgIDType.TitleCharacters_DLC1:
            case FmgIDType.TitleCharacters_DLC2:
            case FmgIDType.TitleLocations:
            case FmgIDType.TitleLocations_DLC1:
            case FmgIDType.TitleLocations_DLC2:
            case FmgIDType.TitleTest:
            case FmgIDType.TitleTest2:
            case FmgIDType.TitleTest3:
            case FmgIDType.TitleArmor_Patch:
            case FmgIDType.TitleCharacters_Patch:
            case FmgIDType.TitleGoods_Patch:
            case FmgIDType.TitleLocations_Patch:
            case FmgIDType.TitleRings_Patch:
            case FmgIDType.TitleSpells_Patch:
            case FmgIDType.TitleWeapons_Patch:
            case FmgIDType.TitleBooster:
            case FmgIDType.TitleMission:
            case FmgIDType.TitleArchive:
            case FmgIDType.TitleWeapons_SOTE:
            case FmgIDType.TitleArmor_SOTE:
            case FmgIDType.TitleGoods_SOTE:
            case FmgIDType.TitleRings_SOTE:
            case FmgIDType.TitleGem_SOTE:
            case FmgIDType.TitleSpells_SOTE:
            case FmgIDType.TitleCharacters_SOTE:
            case FmgIDType.TitleLocations_SOTE:
            case FmgIDType.TitleSwordArts_SOTE:
                return FmgEntryTextType.Title;

            case FmgIDType.GoodsInfo2:
            case FmgIDType.GoodsInfo2_SOTE:
            case FmgIDType.MissionLocation:
                return FmgEntryTextType.ExtraText;

            case FmgIDType.WeaponEffect:
            case FmgIDType.WeaponEffect_SOTE:
            case FmgIDType.TutorialBody: // Include as TextBody to make it display foremost.
            case FmgIDType.TutorialBody2023:
            case FmgIDType.TutorialBody_SOTE:
                return FmgEntryTextType.TextBody;

            default:
                return FmgEntryTextType.TextBody;
        }
    }

    /// <summary>
    ///     Removes patch/DLC identifiers from strings for the purposes of finding patch FMGs. Kinda dumb.
    /// </summary>
    private static string RemovePatchStrings(string str)
    {
        foreach (var badString in patchStrings)
        {
            str = str.Replace(badString, "", StringComparison.CurrentCultureIgnoreCase);
        }

        return str;
    }

    private static FMGInfo GenerateFMGInfo(BinderFile file)
    {
        FMG fmg = FMG.Read(file.Bytes);
        var name = Enum.GetName(typeof(FmgIDType), file.ID);
        FMGInfo info = new()
        {
            FileName = file.Name.Split("\\").Last(),
            Name = name,
            FmgID = (FmgIDType)file.ID,
            Fmg = fmg,
            EntryType = GetFmgTextType(file.ID),
            EntryCategory = GetFmgCategory(file.ID)
        };
        switch (info.EntryCategory)
        {
            case FmgEntryCategory.Goods:
            case FmgEntryCategory.Weapons:
            case FmgEntryCategory.Armor:
            case FmgEntryCategory.Rings:
            case FmgEntryCategory.Gem:
            case FmgEntryCategory.SwordArts:
            case FmgEntryCategory.Generator:
            case FmgEntryCategory.Booster:
            case FmgEntryCategory.FCS:
            case FmgEntryCategory.Archive:
                info.UICategory = FmgUICategory.Item;
                break;
            default:
                info.UICategory = FmgUICategory.Menu;
                break;
        }

        ActiveUITypes[info.UICategory] = true;

        return info;
    }

    private static void SetFMGInfoDS2(string file)
    {
        // TODO: DS2 FMG grouping & UI sorting (copy SetFMGInfo)
        FMG fmg = FMG.Read(file);
        var name = Path.GetFileNameWithoutExtension(file);
        FMGInfo info = new()
        {
            FileName = file.Split("\\").Last(),
            Name = name,
            FmgID = FmgIDType.None,
            Fmg = fmg,
            EntryType = FmgEntryTextType.TextBody,
            EntryCategory = FmgEntryCategory.None,
            UICategory = FmgUICategory.Text
        };
        ActiveUITypes[info.UICategory] = true;
        FmgInfoBank.Add(info);
    }

    /// <summary>
    ///     Loads item and menu MsgBnds from paths, generates FMGInfo, and fills FmgInfoBank.
    /// </summary>
    /// <returns>True if successful; false otherwise.</returns>
    private static bool LoadItemMenuMsgBnds(ResourceDescriptor itemMsgPath, ResourceDescriptor menuMsgPath)
    {
        if (!LoadMsgBnd(itemMsgPath.AssetPath, "item.msgbnd")
            || !LoadMsgBnd(menuMsgPath.AssetPath, "menu.msgbnd"))
        {
            return false;
        }

        foreach (FMGInfo info in FmgInfoBank)
        {
            ApplyGameDifferences(info);
            if (CFG.Current.FMG_NoGroupedFmgEntries)
            {
                info.EntryType = FmgEntryTextType.TextBody;
            }
        }

        if (!CFG.Current.FMG_NoFmgPatching)
        {
            foreach (FMGInfo info in FmgInfoBank)
            {
                SetFMGInfoPatchParent(info);
            }
        }

        if (CFG.Current.FMG_NoGroupedFmgEntries)
        {
            FmgInfoBank = FmgInfoBank.OrderBy(e => e.EntryCategory).ThenBy(e => e.FmgID).ToList();
        }
        else
        {
            FmgInfoBank = FmgInfoBank.OrderBy(e => e.Name).ToList();
        }

        HandleDuplicateEntries();

        return true;
    }

    /// <summary>
    ///     Loads MsgBnd from path, generates FMGInfo, and fills FmgInfoBank.
    /// </summary>
    /// <returns>True if successful; false otherwise.</returns>
    private static bool LoadMsgBnd(string path, string msgBndType = "UNDEFINED")
    {
        if (path == null)
        {
            if (LanguageFolder != "")
            {
                TaskLogs.AddLog(
                    $"Could locate text data files when looking for \"{msgBndType}\" in \"{LanguageFolder}\" folder",
                    LogLevel.Warning);
            }
            else
            {
                TaskLogs.AddLog(
                    $"Could not locate text data files when looking for \"{msgBndType}\" in Default English folder",
                    LogLevel.Warning);
            }

            IsLoaded = false;
            IsLoading = false;
            return false;
        }

        IBinder fmgBinder;
        if (Smithbox.ProjectType == ProjectType.DES || Smithbox.ProjectType == ProjectType.DS1 ||
            Smithbox.ProjectType == ProjectType.DS1R)
        {
            fmgBinder = BND3.Read(path);
        }
        else
        {
            fmgBinder = BND4.Read(path);
        }

        foreach (BinderFile file in fmgBinder.Files)
        {
            FmgInfoBank.Add(GenerateFMGInfo(file));
        }

        fmgBinder.Dispose();
        return true;
    }

    public static void ReloadFMGs(string languageFolder = "")
    {
        TaskManager.Run(new TaskManager.LiveTask("FMG - Load Text", TaskManager.RequeueType.WaitThenRequeue, true,
            () =>
            {
                IsLoaded = false;
                IsLoading = true;

                LanguageFolder = languageFolder;

                ActiveUITypes = new Dictionary<FmgUICategory, bool>();
                foreach (var e in Enum.GetValues(typeof(FmgUICategory)))
                {
                    ActiveUITypes.Add((FmgUICategory)e, false);
                }

                if (Smithbox.ProjectType == ProjectType.Undefined)
                {
                    return;
                }

                if (Smithbox.ProjectType == ProjectType.DS2S || Smithbox.ProjectType == ProjectType.DS2)
                {
                    if (ReloadDS2FMGs())
                    {
                        IsLoading = false;
                        IsLoaded = true;
                    }

                    return;
                }

                SetDefaultLanguagePath();

                ResourceDescriptor itemMsgPath = ResourceTextLocator.GetItemMsgbnd(LanguageFolder);
                ResourceDescriptor menuMsgPath = ResourceTextLocator.GetMenuMsgbnd(LanguageFolder);

                FmgInfoBank = new List<FMGInfo>();
                if (!LoadItemMenuMsgBnds(itemMsgPath, menuMsgPath))
                {
                    FmgInfoBank = new List<FMGInfo>();
                    IsLoaded = false;
                    IsLoading = false;
                    return;
                }

                IsLoaded = true;
                IsLoading = false;
            }));
    }

    private static bool ReloadDS2FMGs()
    {
        SetDefaultLanguagePath();
        ResourceDescriptor desc = ResourceTextLocator.GetItemMsgbnd(LanguageFolder, true);

        if (desc.AssetPath == null)
        {
            if (LanguageFolder != "")
            {
                TaskLogs.AddLog($"Could not locate text data files when using \"{LanguageFolder}\" folder",
                    LogLevel.Warning);
            }
            else
            {
                TaskLogs.AddLog("Could not locate text data files when using Default English folder",
                    LogLevel.Warning);
            }

            IsLoaded = false;
            IsLoading = false;
            return false;
        }

        List<string> files = Directory
            .GetFileSystemEntries($@"{Smithbox.GameRoot}\{desc.AssetPath}", @"*.fmg").ToList();
        FmgInfoBank = new List<FMGInfo>();
        foreach (var file in files)
        {
            var modfile = $@"{Smithbox.ProjectRoot}\{desc.AssetPath}\{Path.GetFileName(file)}";
            if (Smithbox.ProjectRoot != null && File.Exists(modfile))
            {
                var fmg = FMG.Read(modfile);
                SetFMGInfoDS2(modfile);
            }
            else
            {
                var fmg = FMG.Read(file);
                SetFMGInfoDS2(file);
            }
        }

        FmgInfoBank = FmgInfoBank.OrderBy(e => e.Name).ToList();
        HandleDuplicateEntries();
        return true;
    }

    private static void SetDefaultLanguagePath()
    {
        if (LanguageFolder == "")
        {
            // By default, try to find path to English folder.
            foreach (KeyValuePair<string, string> lang in ResourceTextLocator.GetMsgLanguages())
            {
                var folder = lang.Value.Split("\\").Last();
                if (folder.Contains("eng", StringComparison.CurrentCultureIgnoreCase))
                {
                    LanguageFolder = folder;
                    break;
                }
            }
        }
    }

    private static void SetFMGInfoPatchParent(FMGInfo info)
    {
        var strippedName = RemovePatchStrings(info.Name);

        if (strippedName != info.Name)
        {
            // This is a patch FMG, try to find parent FMG.
            foreach (FMGInfo parentInfo in FmgInfoBank)
            {
                if (parentInfo.Name == strippedName)
                {
                    info.AddParent(parentInfo);
                    return;
                }
            }

            TaskLogs.AddLog($"Couldn't find patch parent for FMG \"{info.Name}\" with ID {info.FmgID}",
                LogLevel.Error);
        }
    }

    /// <summary>
    ///     Checks and applies FMG info that differs per-game.
    /// </summary>
    private static void ApplyGameDifferences(FMGInfo info)
    {
        ProjectType gameType = Smithbox.ProjectType;
        switch (info.FmgID)
        {
            case FmgIDType.ReusedFMG_32:
                if (gameType == ProjectType.BB)
                {
                    info.Name = "GemExtraInfo";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.Gem;
                    info.EntryType = FmgEntryTextType.ExtraText;
                }
                else
                {
                    info.Name = "ActionButtonText";
                    info.EntryCategory = FmgEntryCategory.ActionButtonText;
                }

                break;
            case FmgIDType.ReusedFMG_35:
                if (gameType == ProjectType.AC6)
                {
                    info.Name = "TitleGenerator";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.Generator;
                    info.EntryType = FmgEntryTextType.Title;
                }
                else
                {
                    info.Name = "TitleGem";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.Gem;
                    info.EntryType = FmgEntryTextType.Title;
                }

                break;
            case FmgIDType.ReusedFMG_36:
                if (gameType == ProjectType.AC6)
                {
                    info.Name = "DescriptionGenerator";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.Generator;
                    info.EntryType = FmgEntryTextType.Description;
                }
                else
                {
                    info.Name = "SummaryGem";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.Gem;
                    info.EntryType = FmgEntryTextType.Summary;
                }

                break;
            case FmgIDType.ReusedFMG_41:
                if (gameType == ProjectType.AC6)
                {
                    info.Name = "TitleFCS";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.FCS;
                    info.EntryType = FmgEntryTextType.Title;
                }
                else
                {
                    info.Name = "TitleMessage";
                    info.UICategory = FmgUICategory.Menu;
                    info.EntryCategory = FmgEntryCategory.Message;
                    info.EntryType = FmgEntryTextType.TextBody;
                }

                break;
            case FmgIDType.ReusedFMG_42:
                if (gameType == ProjectType.AC6)
                {
                    info.Name = "DescriptionFCS";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.FCS;
                    info.EntryType = FmgEntryTextType.Description;
                }
                else
                {
                    info.Name = "TitleSwordArts";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryCategory = FmgEntryCategory.SwordArts;
                    info.EntryType = FmgEntryTextType.Title;
                }

                break;
            case FmgIDType.Event:
            case FmgIDType.Event_Patch:
                if (gameType is ProjectType.DES or ProjectType.DS1 or ProjectType.DS1R
                    or ProjectType.BB)
                {
                    info.EntryCategory = FmgEntryCategory.ActionButtonText;
                }

                break;
            case FmgIDType.ReusedFMG_205:
                if (gameType == ProjectType.ER)
                {
                    info.Name = "LoadingTitle";
                    info.EntryType = FmgEntryTextType.Title;
                    info.EntryCategory = FmgEntryCategory.LoadingScreen;
                }
                else if (gameType == ProjectType.SDT)
                {
                    info.Name = "LoadingText";
                    info.EntryType = FmgEntryTextType.Description;
                    info.EntryCategory = FmgEntryCategory.LoadingScreen;
                }
                else if (gameType == ProjectType.AC6)
                {
                    info.Name = "MenuContext";
                }
                else
                {
                    info.Name = "SystemMessage_PS4";
                }

                break;
            case FmgIDType.ReusedFMG_206:
                if (gameType == ProjectType.ER)
                {
                    info.Name = "LoadingText";
                    info.EntryType = FmgEntryTextType.Description;
                    info.EntryCategory = FmgEntryCategory.LoadingScreen;
                }
                else if (gameType == ProjectType.SDT)
                {
                    info.Name = "LoadingTitle";
                    info.EntryType = FmgEntryTextType.Title;
                    info.EntryCategory = FmgEntryCategory.LoadingScreen;
                }
                else
                {
                    info.Name = "SystemMessage_XboxOne";
                }

                break;
            case FmgIDType.ReusedFMG_210:
                if (gameType == ProjectType.ER)
                {
                    info.Name = "ToS_win64";
                    info.UICategory = FmgUICategory.Menu;
                    info.EntryType = FmgEntryTextType.TextBody;
                    info.EntryCategory = FmgEntryCategory.None;
                }
                else if (gameType == ProjectType.AC6)
                {
                    info.Name = "TextEmbeddedImageNames";
                    info.UICategory = FmgUICategory.Menu;
                    info.EntryType = FmgEntryTextType.TextBody;
                    info.EntryCategory = FmgEntryCategory.None;
                }
                else
                {
                    info.Name = "TitleGoods_DLC1";
                    info.UICategory = FmgUICategory.Item;
                    info.EntryType = FmgEntryTextType.Title;
                    info.EntryCategory = FmgEntryCategory.Goods;
                    FMGInfo parent = FmgInfoBank.Find(e => e.FmgID == FmgIDType.TitleGoods);
                    info.AddParent(parent);
                }

                break;
        }
    }

    /// <summary>
    ///     Get patched FMG Entries for the specified category, with TextType Title or TextBody.
    /// </summary>
    /// <param name="category">FMGEntryCategory. If "None", an empty list will be returned.</param>
    /// <returns>List of patched entries if found; empty list otherwise.</returns>
    public static List<FMG.Entry> GetFmgEntriesByCategory(FmgEntryCategory category, bool sort = true)
    {
        if (category == FmgEntryCategory.None)
        {
            return new List<FMG.Entry>();
        }

        foreach (FMGInfo info in FmgInfoBank)
        {
            if (info.EntryCategory == category &&
                info.EntryType is FmgEntryTextType.Title or FmgEntryTextType.TextBody)
            {
                return info.GetPatchedEntries(sort);
            }
        }

        return new List<FMG.Entry>();
    }

    /// <summary>
    ///     Get patched FMG Entries for the specified category and text type.
    /// </summary>
    /// <param name="category">FMGEntryCategory . If "None", an empty list will be returned.</param>
    /// <returns>List of patched entries if found; empty list otherwise.</returns>
    public static List<FMG.Entry> GetFmgEntriesByCategoryAndTextType(FmgEntryCategory category,
        FmgEntryTextType textType, bool sort = true)
    {
        if (category == FmgEntryCategory.None)
        {
            return new List<FMG.Entry>();
        }

        foreach (FMGInfo info in FmgInfoBank)
        {
            if (info.EntryCategory == category && info.EntryType == textType)
            {
                return info.GetPatchedEntries(sort);
            }
        }

        return new List<FMG.Entry>();
    }

    /// <summary>
    ///     Get patched FMG Entries for the specified FmgIDType.
    /// </summary>
    /// <returns>List of patched entries if found; empty list otherwise.</returns>
    public static List<FMG.Entry> GetFmgEntriesByFmgIDType(FmgIDType fmgID, bool sort = true)
    {
        foreach (FMGInfo info in FmgInfoBank)
        {
            if (info.FmgID == fmgID)
            {
                return info.GetPatchedEntries(sort);
            }
        }

        return new List<FMG.Entry>();
    }

    /// <summary>
    ///     Generate a new EntryGroup using a given ID and FMGInfo.
    ///     Data is updated using FMGInfo PatchChildren.
    /// </summary>
    public static EntryGroup GenerateEntryGroup(int id, FMGInfo fmgInfo)
    {
        EntryGroup eGroup = new() { ID = id };

        if (fmgInfo.EntryCategory == FmgEntryCategory.None || CFG.Current.FMG_NoGroupedFmgEntries)
        {
            List<EntryFMGInfoPair> entryPairs = fmgInfo.GetPatchedEntryFMGPairs();
            EntryFMGInfoPair pair = entryPairs.Find(e => e.Entry.ID == id);
            if (pair == null)
            {
                return eGroup;
            }

            eGroup.TextBody = pair.Entry;
            eGroup.TextBodyInfo = pair.FmgInfo;
            return eGroup;
        }

        foreach (FMGInfo info in FmgInfoBank)
        {
            if (info.EntryCategory == fmgInfo.EntryCategory && info.PatchParent == null)
            {
                List<EntryFMGInfoPair> entryPairs = info.GetPatchedEntryFMGPairs();
                EntryFMGInfoPair pair = entryPairs.Find(e => e.Entry.ID == id);
                if (pair != null)
                {
                    switch (info.EntryType)
                    {
                        case FmgEntryTextType.Title:
                            eGroup.Title = pair.Entry;
                            eGroup.TitleInfo = pair.FmgInfo;
                            break;
                        case FmgEntryTextType.Summary:
                            eGroup.Summary = pair.Entry;
                            eGroup.SummaryInfo = pair.FmgInfo;
                            break;
                        case FmgEntryTextType.Description:
                            eGroup.Description = pair.Entry;
                            eGroup.DescriptionInfo = pair.FmgInfo;
                            break;
                        case FmgEntryTextType.ExtraText:
                            eGroup.ExtraText = pair.Entry;
                            eGroup.ExtraTextInfo = pair.FmgInfo;
                            break;
                        case FmgEntryTextType.TextBody:
                            eGroup.TextBody = pair.Entry;
                            eGroup.TextBodyInfo = pair.FmgInfo;
                            break;
                    }
                }
            }
        }

        return eGroup;
    }
    public static void HandleDuplicateEntries()
    {
        var askedAboutDupes = false;
        var ignoreDupes = true;
        foreach (FMGInfo info in FmgInfoBank)
        {
            IEnumerable<FMG.Entry> dupes = info.Fmg.Entries.GroupBy(e => e.ID).SelectMany(g => g.SkipLast(1));
            if (dupes.Any())
            {
                var dupeList = string.Join(", ", dupes.Select(dupe => dupe.ID));
                if (!askedAboutDupes && PlatformUtils.Instance.MessageBox(
                        $"Duplicate text entries have been found in FMG {Path.GetFileNameWithoutExtension(info.FileName)} for the following row IDs:\n\n{dupeList}\n\nRemove all duplicates? (Latest entries are kept)",
                        "Duplicate Text Entries", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ignoreDupes = false;
                }

                askedAboutDupes = true;

                if (!ignoreDupes)
                {
                    foreach (FMG.Entry dupe in dupes)
                    {
                        info.Fmg.Entries.Remove(dupe);
                    }
                }
            }
        }
    }

    public static string FormatJson(string json)
    {
        json = json.Replace("{\"ID\"", "\r\n{\"ID\"");
        json = json.Replace("],", "\r\n],");
        return json;
    }

    private static void SaveFMGsDS2()
    {
        foreach (FMGInfo info in FmgInfoBank)
        {
            Utils.WriteWithBackup(Smithbox.GameRoot, Smithbox.ProjectRoot,
                $@"menu\text\{LanguageFolder}\{info.Name}.fmg", info.Fmg);
        }
    }

    public static void SaveFMGs()
    {
        try
        {
            if (!IsLoaded)
            {
                return;
            }

            if (Smithbox.ProjectType == ProjectType.Undefined)
            {
                return;
            }

            if (Smithbox.ProjectType == ProjectType.DS2S || Smithbox.ProjectType == ProjectType.DS2)
            {
                SaveFMGsDS2();
                TaskLogs.AddLog("Saved FMG text");
                return;
            }

            // Load the fmg bnd, replace fmgs, and save
            IBinder fmgBinderItem;
            IBinder fmgBinderMenu;
            ResourceDescriptor itemMsgPath = ResourceTextLocator.GetItemMsgbnd(LanguageFolder);
            ResourceDescriptor menuMsgPath = ResourceTextLocator.GetMenuMsgbnd(LanguageFolder);
            if (Smithbox.ProjectType == ProjectType.DES || Smithbox.ProjectType == ProjectType.DS1 ||
                Smithbox.ProjectType == ProjectType.DS1R)
            {
                fmgBinderItem = BND3.Read(itemMsgPath.AssetPath);
                fmgBinderMenu = BND3.Read(menuMsgPath.AssetPath);
            }
            else
            {
                fmgBinderItem = BND4.Read(itemMsgPath.AssetPath);
                fmgBinderMenu = BND4.Read(menuMsgPath.AssetPath);
            }

            foreach (BinderFile file in fmgBinderItem.Files)
            {
                FMGInfo info = FmgInfoBank.Find(e => e.FmgID == (FmgIDType)file.ID);
                if (info != null)
                {
                    file.Bytes = info.Fmg.Write();
                }
            }

            foreach (BinderFile file in fmgBinderMenu.Files)
            {
                FMGInfo info = FmgInfoBank.Find(e => e.FmgID == (FmgIDType)file.ID);
                if (info != null)
                {
                    file.Bytes = info.Fmg.Write();
                }
            }

            ResourceDescriptor itemMsgPathDest = ResourceTextLocator.GetItemMsgbnd(LanguageFolder, true);
            ResourceDescriptor menuMsgPathDest = ResourceTextLocator.GetMenuMsgbnd(LanguageFolder, true);
            if (fmgBinderItem is BND3 bnd3)
            {
                Utils.WriteWithBackup(Smithbox.GameRoot,
                    Smithbox.ProjectRoot, itemMsgPathDest.AssetPath, bnd3);
                Utils.WriteWithBackup(Smithbox.GameRoot,
                    Smithbox.ProjectRoot, menuMsgPathDest.AssetPath, (BND3)fmgBinderMenu);

                if (Smithbox.ProjectType is ProjectType.DES)
                {
                    bnd3.Compression = DCX.Type.None;
                    ((BND3)fmgBinderMenu).Compression = DCX.Type.None;
                    Utils.WriteWithBackup(Smithbox.GameRoot,
                        Smithbox.ProjectRoot, itemMsgPathDest.AssetPath[..^4], bnd3);
                    Utils.WriteWithBackup(Smithbox.GameRoot,
                        Smithbox.ProjectRoot, menuMsgPathDest.AssetPath[..^4], (BND3)fmgBinderMenu);
                }
            }
            else if (fmgBinderItem is BND4 bnd4)
            {
                Utils.WriteWithBackup(Smithbox.GameRoot,
                    Smithbox.ProjectRoot, itemMsgPathDest.AssetPath, bnd4);
                Utils.WriteWithBackup(Smithbox.GameRoot,
                    Smithbox.ProjectRoot, menuMsgPathDest.AssetPath, (BND4)fmgBinderMenu);
            }

            fmgBinderItem.Dispose();
            fmgBinderMenu.Dispose();
            TaskLogs.AddLog("Saved FMG text");
        }
        catch (SavingFailedException e)
        {
            TaskLogs.AddLog(e.Wrapped.Message,
                LogLevel.Error, TaskLogs.LogPriority.High, e.Wrapped);
        }
    }

    /// <summary>
    ///     Value pair with an entry and the FMG it belongs to.
    /// </summary>
    public class EntryFMGInfoPair
    {
        public EntryFMGInfoPair(FMGInfo fmgInfo, FMG.Entry entry)
        {
            FmgInfo = fmgInfo;
            Entry = entry;
        }

        public FMGInfo FmgInfo { get; set; }
        public FMG.Entry Entry { get; set; }
    }

    /// <summary>
    ///     Base object that stores an FMG and information regarding it.
    /// </summary>
    public class FMGInfo
    {
        public FmgEntryCategory EntryCategory;
        public FmgEntryTextType EntryType;
        public string FileName;
        public FMG Fmg;
        public FmgIDType FmgID;
        public string Name;

        /// <summary>
        ///     List of associated children to this FMGInfo used to get patch entry data.
        /// </summary>
        public List<FMGInfo> PatchChildren = new();

        public FMGInfo PatchParent;
        public FmgUICategory UICategory;

        private string _patchPrefix = null;
        public string PatchPrefix
        {
            get
            {
                _patchPrefix ??= Name.Replace(RemovePatchStrings(Name), "");
                return _patchPrefix;
            }
        }

        public void AddParent(FMGInfo parent)
        {
            if (CFG.Current.FMG_NoFmgPatching)
            {
                return;
            }

            PatchParent = parent;
            parent.PatchChildren.Add(this);
        }

        /// <summary>
        ///     Returns a patched list of Entry & FMGInfo value pairs from this FMGInfo and its children.
        ///     If a PatchParent exists, it will be checked instead.
        /// </summary>
        public List<EntryFMGInfoPair> GetPatchedEntryFMGPairs(bool sort = true)
        {
            if (PatchParent != null)
            {
                return PatchParent.GetPatchedEntryFMGPairs(sort);
            }

            List<EntryFMGInfoPair> list = new();
            foreach (FMG.Entry entry in Fmg.Entries)
            {
                list.Add(new EntryFMGInfoPair(this, entry));
            }

            // Check and apply patch entries
            foreach (FMGInfo child in PatchChildren.OrderBy(e => (int)e.FmgID))
            {
                foreach (FMG.Entry entry in child.Fmg.Entries)
                {
                    EntryFMGInfoPair match = list.Find(e => e.Entry.ID == entry.ID);
                    if (match != null)
                    {
                        // This is a patch entry
                        // Only non-null text will overrwrite
                        if (entry.Text != null)
                        {
                            match.Entry = entry;
                            match.FmgInfo = child;
                        }
                    }
                    else
                    {
                        list.Add(new EntryFMGInfoPair(child, entry));
                    }
                }
            }

            if (sort)
            {
                list = list.OrderBy(e => e.Entry.ID).ToList();
            }

            return list;
        }

        /// <summary>
        ///     Returns a patched list of entries in this FMGInfo and its children.
        ///     If a PatchParent exists, it will be checked instead.
        /// </summary>
        public List<FMG.Entry> GetPatchedEntries(bool sort = true)
        {
            if (PatchParent != null)
            {
                return PatchParent.GetPatchedEntries(sort);
            }

            List<FMG.Entry> list = new();
            list.AddRange(Fmg.Entries);

            // Check and apply patch entries
            foreach (FMGInfo child in PatchChildren.OrderBy(e => (int)e.FmgID))
            {
                foreach (FMG.Entry entry in child.Fmg.Entries)
                {
                    FMG.Entry match = list.Find(e => e.ID == entry.ID);
                    if (match != null)
                    {
                        // This is a patch entry
                        if (entry.Text != null)
                        {
                            // Text is not null, so it will overwrite non-patch entries.
                            list.Remove(match);
                            list.Add(entry);
                        }
                    }
                    else
                    {
                        list.Add(entry);
                    }
                }
            }

            if (sort)
            {
                list = list.OrderBy(e => e.ID).ToList();
            }

            return list;
        }

        /// <summary>
        ///     Returns title FMGInfo that shares this FMGInfo's EntryCategory.
        ///     If none are found, an exception will be thrown.
        /// </summary>
        public FMGInfo GetTitleFmgInfo()
        {
            foreach (var info in FMGBank.FmgInfoBank)
            {
                if (info.EntryCategory == EntryCategory && info.EntryType == FmgEntryTextType.Title && info.PatchPrefix == PatchPrefix)
                {
                    return info;
                }
            }
            throw new InvalidOperationException($"Couldn't find title FMGInfo for {this.Name}");
        }

        /// <summary>
        ///     Adds an entry to the end of the FMG.
        /// </summary>
        public void AddEntry(FMG.Entry entry)
        {
            Fmg.Entries.Add(entry);
        }

        /// <summary>
        ///     Clones an FMG entry.
        /// </summary>
        /// <returns>Cloned entry</returns>
        public FMG.Entry CloneEntry(FMG.Entry entry)
        {
            FMG.Entry newEntry = new(entry.ID, entry.Text);
            return newEntry;
        }

        /// <summary>
        ///     Removes an entry from FMGInfo's FMG.
        /// </summary>
        public void DeleteEntry(FMG.Entry entry)
        {
            Fmg.Entries.Remove(entry);
        }
    }


    /// <summary>
    ///     A group of entries that may be associated (such as title, summary, description) along with respective FMGs.
    /// </summary>
    public class EntryGroup
    {
        private int _ID = -1;
        public FMG.Entry Description;
        public FMGInfo DescriptionInfo;
        public FMG.Entry ExtraText;
        public FMGInfo ExtraTextInfo;
        public FMG.Entry Summary;
        public FMGInfo SummaryInfo;
        public FMG.Entry TextBody;
        public FMGInfo TextBodyInfo;
        public FMG.Entry Title;
        public FMGInfo TitleInfo;

        public int ID
        {
            set
            {
                _ID = value;
                if (TextBody != null)
                {
                    TextBody.ID = _ID;
                }

                if (Title != null)
                {
                    Title.ID = _ID;
                }

                if (Summary != null)
                {
                    Summary.ID = _ID;
                }

                if (Description != null)
                {
                    Description.ID = _ID;
                }

                if (ExtraText != null)
                {
                    ExtraText.ID = _ID;
                }
            }
            get => _ID;
        }

        /// <summary>
        ///     Gets next unused entry ID.
        /// </summary>
        public int GetNextUnusedID(int idIncrement)
        {
            var id = ID;
            if (TextBody != null)
            {
                List<FMG.Entry> entries = TextBodyInfo.GetPatchedEntries();
                do
                {
                    id = id + idIncrement;
                } while (entries.Find(e => e.ID == id) != null);
            }
            else if (Title != null)
            {
                List<FMG.Entry> entries = TitleInfo.GetPatchedEntries();
                do
                {
                    id = id + idIncrement;
                } while (entries.Find(e => e.ID == id) != null);
            }
            else if (Summary != null)
            {
                List<FMG.Entry> entries = SummaryInfo.GetPatchedEntries();
                do
                {
                    id = id + idIncrement;
                } while (entries.Find(e => e.ID == id) != null);
            }
            else if (Description != null)
            {
                List<FMG.Entry> entries = DescriptionInfo.GetPatchedEntries();
                do
                {
                    id = id + idIncrement;
                } while (entries.Find(e => e.ID == id) != null);
            }
            else if (ExtraText != null)
            {
                List<FMG.Entry> entries = ExtraTextInfo.GetPatchedEntries();
                do
                {
                    id = id + idIncrement;
                } while (entries.Find(e => e.ID == id) != null);
            }

            return id;
        }

        /// <summary>
        ///     Sets ID of all entries to the next unused entry ID.
        /// </summary>
        public void SetNextUnusedID()
        {
            ID = GetNextUnusedID(CFG.Current.FMG_DuplicateIncrement);
        }

        /// <summary>
        ///     Places all entries within this EntryGroup into their assigned FMGs.
        /// </summary>
        public void ImplementEntryGroup()
        {
            if (TextBody != null)
            {
                TextBodyInfo.AddEntry(TextBody);
            }

            if (Title != null)
            {
                TitleInfo.AddEntry(Title);
            }

            if (Summary != null)
            {
                SummaryInfo.AddEntry(Summary);
            }

            if (Description != null)
            {
                DescriptionInfo.AddEntry(Description);
            }

            if (ExtraText != null)
            {
                ExtraTextInfo.AddEntry(ExtraText);
            }
        }

        /// <summary>
        ///     Duplicates all entries within their assigned FMGs.
        ///     New entries are inserted into their assigned FMGs.
        /// </summary>
        /// <returns>New EntryGroup.</returns>
        public EntryGroup DuplicateFMGEntries()
        {
            EntryGroup newGroup = new();
            if (TextBody != null)
            {
                newGroup.TextBodyInfo = TextBodyInfo;
                newGroup.TextBody = TextBodyInfo.CloneEntry(TextBody);
                TextBodyInfo.AddEntry(newGroup.TextBody);
            }

            if (Title != null)
            {
                newGroup.TitleInfo = TitleInfo;
                newGroup.Title = TitleInfo.CloneEntry(Title);
                TitleInfo.AddEntry(newGroup.Title);
            }

            if (Summary != null)
            {
                newGroup.SummaryInfo = SummaryInfo;
                newGroup.Summary = SummaryInfo.CloneEntry(Summary);
                SummaryInfo.AddEntry(newGroup.Summary);
            }

            if (Description != null)
            {
                newGroup.DescriptionInfo = DescriptionInfo;
                newGroup.Description = DescriptionInfo.CloneEntry(Description);
                DescriptionInfo.AddEntry(newGroup.Description);
            }

            if (ExtraText != null)
            {
                newGroup.ExtraTextInfo = ExtraTextInfo;
                newGroup.ExtraText = ExtraTextInfo.CloneEntry(ExtraText);
                ExtraTextInfo.AddEntry(newGroup.ExtraText);
            }

            newGroup.ID = ID;
            return newGroup;
        }

        /// <summary>
        ///     Clones this EntryGroup and returns a duplicate.
        /// </summary>
        /// <returns>Cloned EntryGroup.</returns>
        public EntryGroup CloneEntryGroup()
        {
            EntryGroup newGroup = new();
            if (TextBody != null)
            {
                newGroup.TextBodyInfo = TextBodyInfo;
                newGroup.TextBody = TextBodyInfo.CloneEntry(TextBody);
            }

            if (Title != null)
            {
                newGroup.TitleInfo = TitleInfo;
                newGroup.Title = TitleInfo.CloneEntry(Title);
            }

            if (Summary != null)
            {
                newGroup.SummaryInfo = SummaryInfo;
                newGroup.Summary = SummaryInfo.CloneEntry(Summary);
            }

            if (Description != null)
            {
                newGroup.DescriptionInfo = DescriptionInfo;
                newGroup.Description = DescriptionInfo.CloneEntry(Description);
            }

            if (ExtraText != null)
            {
                newGroup.ExtraTextInfo = ExtraTextInfo;
                newGroup.ExtraText = ExtraTextInfo.CloneEntry(ExtraText);
            }

            return newGroup;
        }

        /// <summary>
        ///     Removes all entries from their assigned FMGs.
        /// </summary>
        public void DeleteEntries()
        {
            if (TextBody != null)
            {
                TextBodyInfo.DeleteEntry(TextBody);
            }

            if (Title != null)
            {
                TitleInfo.DeleteEntry(Title);
            }

            if (Summary != null)
            {
                SummaryInfo.DeleteEntry(Summary);
            }

            if (Description != null)
            {
                DescriptionInfo.DeleteEntry(Description);
            }

            if (ExtraText != null)
            {
                ExtraTextInfo.DeleteEntry(ExtraText);
            }
        }
    }
}
