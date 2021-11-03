﻿using Ninja_Price.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using Color = SharpDX.Color;
using RectangleF = SharpDX.RectangleF;
using ExileCore.RenderQ;
using ImGuiNET;
using static Ninja_Price.Enums.HaggleTypes.HaggleType;

namespace Ninja_Price.Main
{
    public partial class Main
    {
        // TODO: Clean this shit up later
        private static Vector2 lastProphWindowPos = new Vector2(0, 0);

        private static Vector2 lastProphWindowSize = new Vector2(0, 0);
        public Stopwatch ValueUpdateTimer = Stopwatch.StartNew();
        public double StashTabValue { get; set; }
        public double InventoryTabValue { get; set; }
        public double ExaltedValue { get; set; } = 0;
        public List<NormalInventoryItem> HaggleItemList { get; set; } = new List<NormalInventoryItem>();
        public List<CustomItem> FortmattedHaggleItemList { get; set; } = new List<CustomItem>();
        public List<NormalInventoryItem> ItemList { get; set; } = new List<NormalInventoryItem>();
        public List<CustomItem> FortmattedItemList { get; set; } = new List<CustomItem>();

        public List<NormalInventoryItem> InventoryItemList { get; set; } = new List<NormalInventoryItem>();
        public List<CustomItem> FortmattedInventoryItemList { get; set; } = new List<CustomItem>();

        public List<CustomItem> ItemsToDrawList { get; set; } = new List<CustomItem>();
        public List<CustomItem> InventoryItemsToDrawList { get; set; } = new List<CustomItem>();
        public StashElement StashPanel { get; set; }
        public InventoryElement InventoryPanel { get; set; }
        public Element HagglePanel { get; set; }

        public CustomItem Hovereditem { get; set; }

        // TODO: Get hovered items && items from inventory - Getting hovered itemw ill become useful later on

        public override void Render()
        {
            #region Reset All Data

            CurrentLeague = Settings.LeagueList.Value; //  Update selected league every tick
            StashTabValue = 0;
            InventoryTabValue = 0;
            Hovereditem = null;

            StashPanel = GameController.Game.IngameState.IngameUi.StashElement;
            InventoryPanel = GameController.Game.IngameState.IngameUi.InventoryPanel;
            HagglePanel = GameController.Game.IngameState.IngameUi.HaggleWindow;

            #endregion


            try // Im lazy and just want to surpress all errors produced
            {
                // only update if the time between last update is more than AutoReloadTimer interval
                if (Settings.AutoReload && Settings.LastUpDateTime.AddMinutes(Settings.AutoReloadTimer.Value) < DateTime.Now)
                {
                    LoadJsonData();
                    Settings.LastUpDateTime = DateTime.Now;
                }

                if (Settings.Debug) LogMessage($"{GetCurrentMethod()}.Loop() is Alive", 5, Color.LawnGreen);

                if (Settings.Debug)
                    LogMessage($"{GetCurrentMethod()}: Selected League: {Settings.LeagueList.Value}", 5, Color.White);

                var tabType = StashPanel.VisibleStash?.InvType;

                // Everything is updated, lets check if we should draw
                if (StashPanel.IsVisible)
                {
                    if (ShouldUpdateValues())
                    {
                        ExaltedValue = (double)CollectedData.Currency.Lines.Find(x => x.CurrencyTypeName == "Exalted Orb").ChaosEquivalent;
                        // Format stash items
                        ItemList = new List<NormalInventoryItem>();
                        switch (tabType)
                        {
                            case InventoryType.BlightStash:
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                ItemList.RemoveAt(0);
                                ItemList.RemoveAt(ItemList.Count()-1);
                                break;
                            case InventoryType.MetamorphStash:
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                ItemList.RemoveAt(0);
                                break;
                            default:
                                ItemList = StashPanel.VisibleStash.VisibleInventoryItems.ToList();
                                break;
                        }
                        FortmattedItemList = new List<CustomItem>();
                        FortmattedItemList = FormatItems(ItemList);

                        // Format Inventory Items
                        InventoryItemList = new List<NormalInventoryItem>();
                        InventoryItemList = GetInventoryItems();
                        FortmattedInventoryItemList = new List<CustomItem>();
                        FortmattedInventoryItemList = FormatItems(InventoryItemList);

                        if (Settings.Debug)
                            LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValues())", 5,
                                Color.LawnGreen);

                        foreach (var item in FortmattedItemList)
                            GetValue(item);
                        foreach (var item in FortmattedInventoryItemList)
                            GetValue(item);
                    }

                    // Gather all information needed before rendering as we only want to itterate through the list once

                    ItemsToDrawList = new List<CustomItem>();
                    foreach (var item in FortmattedItemList)
                    {
                        if (item == null || item.Item.Address == 0) continue; // Item is fucked, skip
                        if (!item.Item.IsVisible && item.ItemType != ItemTypes.None)
                            continue; // Disregard non visable items as that usually means they arnt part of what we want to look at

                        StashTabValue += item.PriceData.ChaosValue;
                        ItemsToDrawList.Add(item);
                    }

                    InventoryItemsToDrawList = new List<CustomItem>();
                    foreach (var item in FortmattedInventoryItemList)
                    {

                        if (item == null || item.Item.Address == 0) continue; // Item is fucked, skip
                        if (!item.Item.IsVisible && item.ItemType != ItemTypes.None)
                            continue; // Disregard non visable items as that usually means they arnt part of what we want to look at

                        InventoryTabValue += item.PriceData.ChaosValue;
                        InventoryItemsToDrawList.Add(item);
                    }
                }
                else if (InventoryPanel.IsVisible)
                {
                    if (ShouldUpdateValuesInventory())
                    {
                        // Format Inventory Items
                        InventoryItemList = new List<NormalInventoryItem>();
                        InventoryItemList = GetInventoryItems();
                        FortmattedInventoryItemList = new List<CustomItem>();
                        FortmattedInventoryItemList = FormatItems(InventoryItemList);

                        if (Settings.Debug)
                            LogMessage($"{GetCurrentMethod()}.Render() Looping if (ShouldUpdateValues())", 5,
                                Color.LawnGreen);

                        foreach (var item in FortmattedInventoryItemList)
                            GetValue(item);
                    }

                    // Gather all information needed before rendering as we only want to itterate through the list once
                    InventoryItemsToDrawList = new List<CustomItem>();
                    foreach (var item in FortmattedInventoryItemList)
                    {

                        if (item == null || item.Item.Address == 0) continue; // Item is fucked, skip
                        if (!item.Item.IsVisible && item.ItemType != ItemTypes.None)
                            continue; // Disregard non visable items as that usually means they arnt part of what we want to look at

                        InventoryTabValue += item.PriceData.ChaosValue;
                        InventoryItemsToDrawList.Add(item);
                    }
                }

                if (HagglePanel.IsVisible)
                {

                    var haggleType = None;

                    // Return Haggle Window Type
                    var haggleText = HagglePanel.GetChildAtIndex(6).GetChildAtIndex(2).GetChildAtIndex(0).Text;

                    switch (haggleText)
                    {
                        case "Exchange":
                            haggleType = Exchange;
                            break;
                        case "Gamble":
                            haggleType = Gamble;
                            break;
                        case "Deal":
                            haggleType = Deal;
                            break;
                        case "Haggle":
                            haggleType = Haggle;
                            break;
                    }

                    if (haggleType == Gamble)
                    {
                        HaggleItemList = new List<NormalInventoryItem>();
                        var itemChild = HagglePanel.GetChildAtIndex(8).GetChildAtIndex(1).GetChildAtIndex(0).GetChildAtIndex(0);

                        for (var i = 1; i < itemChild.ChildCount; ++i)
                        {
                            var item = itemChild.Children[i].AsObject<NormalInventoryItem>();
                            HaggleItemList.Add(item);

                            if (Settings.Debug)
                            {
                                LogMessage(
                                    $"Haggle Item[{HaggleItemList.Count}]: {GameController.Files.BaseItemTypes.Translate(item.Item.Path).BaseName}");
                            }
                        }

                        // Format Haggle Items
                        FortmattedHaggleItemList = new List<CustomItem>();
                        FortmattedHaggleItemList = FormatItems(HaggleItemList);

                        foreach (var item in FortmattedHaggleItemList)
                            GetValueHaggle(item);
                    }
                    else
                    {
                        HaggleItemList = new List<NormalInventoryItem>();
                        FortmattedHaggleItemList = new List<CustomItem>();
                    }
                }

                // Expedition Gamble Func
                ExpeditionGamble();

                GetHoveredItem(); // Get information for the hovered item
                DrawGraphics();
            }
            catch (Exception e)
            {
                // ignored
                if (Settings.Debug)
                {

                    LogMessage("Error in: Main Render Loop, restart PoEHUD.", 5, Color.Red);
                    LogMessage(e.ToString(), 5, Color.Orange);
                }
            }

            try
            {
                //PropheccyDisplay();
            }
            catch
            {
                if (Settings.Debug)
                {

                    LogMessage("Error in: PropheccyDisplay(), restart PoEHUD.", 5, Color.Red);
                }
            }
        }

        public void DrawGraphics()
        {
            // Hovered Item
            if (Hovereditem != null && Hovereditem.ItemType != ItemTypes.None && Settings.HoveredItem.Value)
            {
                var text = $"Change in last 7 Days: {Hovereditem.PriceData.ChangeInLast7Days}%%";
                var changeTextLength = text.Length-1;
                text += $"\n\r{String.Concat(Enumerable.Repeat('-', changeTextLength))}";

                switch (Hovereditem.ItemType)
                {
                    case ItemTypes.Currency:
                    case ItemTypes.Essence:
                    case ItemTypes.Fragment:
                    case ItemTypes.Scarab:
                    case ItemTypes.Resonator:
                    case ItemTypes.Fossil:
                    case ItemTypes.Oil:
                    case ItemTypes.Artifact:
                    case ItemTypes.Catalyst:
                    case ItemTypes.DeliriumOrbs:    
                    case ItemTypes.DivinationCard:
                        if (Hovereditem.PriceData.ChaosValue / Hovereditem.PriceData.ExaltedPrice >= 0.1)
                        {
                            text += $"\n\rExalt: {Hovereditem.PriceData.ChaosValue / Hovereditem.PriceData.ExaltedPrice:0.##}ex";
                            text += $"\n\r{String.Concat(Enumerable.Repeat('-', changeTextLength))}";
                        }
                        text += $"\n\rChaos: {Hovereditem.PriceData.ChaosValue / Hovereditem.CurrencyInfo.StackSize}c";
                        text += $"\n\rTotal: {Hovereditem.PriceData.ChaosValue}c";
                        break;
                    case ItemTypes.Prophecy:
                    case ItemTypes.UniqueAccessory:
                    case ItemTypes.UniqueArmour:
                    case ItemTypes.UniqueFlask:
                    case ItemTypes.UniqueJewel:
                    case ItemTypes.UniqueMap:
                    case ItemTypes.UniqueWeapon:
                    case ItemTypes.NormalMap:
                    case ItemTypes.Incubator:
                    case ItemTypes.MavenInvitation:
                        if (Hovereditem.PriceData.ChaosValue / Hovereditem.PriceData.ExaltedPrice >= 0.1)
                        {
                            text += $"\n\rExalt: {Hovereditem.PriceData.ChaosValue / Hovereditem.PriceData.ExaltedPrice:0.##}ex";
                            text += $"\n\r{String.Concat(Enumerable.Repeat('-', changeTextLength))}";
                        }
                        text += $"\n\rChaos: {Hovereditem.PriceData.ChaosValue}c";
                        break;
                }
                
                if (Settings.Debug)
                {
                    text += $"\n\rUniqueName: {Hovereditem.UniqueName}";
                    text += $"\n\rBaseName: {Hovereditem.BaseName}";
                    text += $"\n\rItemType: {Hovereditem.ItemType}";
                    text += $"\n\rMapType: {Hovereditem.MapInfo.MapType}";
                } 
                
                if (Settings.ArtifactChaosPrices)
                {
                    var artifactChaosPrice = TryGetArtifactToChaosPrice(Hovereditem);
                    if (artifactChaosPrice > 0)
                    {
                        var exaltString = ExaltedValue > 0 && artifactChaosPrice >= (0.5 * ExaltedValue)
                            ? $" ({artifactChaosPrice / ExaltedValue:F2} ex)"
                            : string.Empty;
                        text += $"\n\rArtifact price: {artifactChaosPrice:F1}c{exaltString}";
                    }
                }


                var textMeasure = Graphics.MeasureText(text, 15);
                //Graphics.DrawBox(new RectangleF(0, 0, textMeasure.Width, textMeasure.Height), Color.Black);
                //Graphics.DrawText(text, new Vector2(50, 50), Color.White);

                ImGui.BeginTooltip();
                ImGui.SetTooltip(text);
                ImGui.EndTooltip();
            }

            // Inventory Value
            VisibleInventoryValue();

            if (Settings.HelmetEnchantPrices)
                ShowHelmetEnchantPrices();

            if (!StashPanel.IsVisible)
                return;

            // Stash Tab Value
            VisibleStashValue();

            var tabType = StashPanel.VisibleStash.InvType;
            foreach (var customItem in ItemsToDrawList)
            {
                if (customItem.ItemType == ItemTypes.None) continue;

                if (Settings.CurrencyTabSpecifcToggle)
                {
                    switch (tabType)
                    {
                        case InventoryType.CurrencyStash:
                        case InventoryType.FragmentStash:
                        case InventoryType.DelveStash:
                        case InventoryType.DeliriumStash:
                        case InventoryType.MetamorphStash:
                        case InventoryType.BlightStash:
                            PriceBoxOverItem(customItem);
                            break;
                    }
                }

                if (Settings.HighlightUniqueJunk)
                {
                    if (customItem.ItemType == ItemTypes.None || customItem.ItemType == ItemTypes.Currency) continue;
                    HighlightJunkUniques(customItem);
                }
            }

            if (Settings.HighlightUniqueJunk)
                foreach (var customItem in InventoryItemsToDrawList)
                {
                    if (customItem.ItemType == ItemTypes.None || customItem.ItemType == ItemTypes.Currency) continue;

                    HighlightJunkUniques(customItem);
                }
        }

        private void VisibleStashValue()
        {
            try
            {
                var StashType = GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.InvType;
                if (!Settings.VisibleStashValue.Value || !StashPanel.IsVisible) return;
                {
                    var pos = new Vector2(Settings.StashValueX.Value, Settings.StashValueY.Value);
                    var significantDigits = Math.Round((decimal)StashTabValue, Settings.StashValueSignificantDigits.Value);
                    //Graphics.DrawText(
                    //    DrawImage($"{DirectoryFullName}//images//Chaos_Orb_inventory_icon.png",
                    //        new RectangleF(Settings.StashValueX.Value - Settings.StashValueFontSize.Value,
                    //            Settings.StashValueY.Value,
                    //            Settings.StashValueFontSize.Value, Settings.StashValueFontSize.Value))
                    //        ? $"{significantDigits}"
                    //        : $"{significantDigits} Chaos", Settings.StashValueFontSize.Value, pos,
                    //    Settings.UniTextColor);

                    Graphics.DrawText($"Chaos: {significantDigits:#,##0.################}\n\rExalt: {Math.Round((decimal)(StashTabValue / ExaltedValue), Settings.StashValueSignificantDigits.Value):#,##0.################}", pos, Settings.UniTextColor, FontAlign.Left);
                }
            }
            catch (Exception e)
            {
                // ignored
                if (Settings.Debug)
                {

                    LogMessage("Error in: VisibleStashValue, restart PoEHUD.", 5, Color.Red);
                    LogMessage(e.ToString(), 5, Color.Orange);
                }
            }
        }

        private void VisibleInventoryValue()
        {
            try
            {
                var inventory = GameController.Game.IngameState.IngameUi.InventoryPanel;
                if (!Settings.VisibleInventoryValue.Value || !inventory.IsVisible) return;
                {
                    var pos = new Vector2(Settings.InventoryValueX.Value, Settings.InventoryValueY.Value);
                    var significantDigits =
                        Math.Round((decimal)InventoryTabValue, Settings.InventoryValueSignificantDigits.Value);
                    Graphics.DrawText($"Chaos: {significantDigits:#,##0.################}\n\rExalt: {Math.Round((decimal)(InventoryTabValue / ExaltedValue), Settings.StashValueSignificantDigits.Value):#,##0.################}", pos, Settings.UniTextColor, FontAlign.Left);
                }
            }
            catch (Exception e)
            {
                // ignored
                if (Settings.Debug)
                {

                    LogMessage("Error in: VisibleInventoryValue, restart PoEHUD.", 5, Color.Red);
                    LogMessage(e.ToString(), 5, Color.Orange);
                }
            }
        }

        private void PriceBoxOverItem(CustomItem item)
        {
            var box = item.Item.GetClientRect();
            var drawBox = new RectangleF(box.X, box.Y - 2, box.Width, -Settings.CurrencyTabBoxHeight);
            var position = new Vector2(drawBox.Center.X, drawBox.Center.Y - Settings.CurrencyTabFontSize.Value / 2);

            Graphics.DrawText(Math.Round((decimal) item.PriceData.ChaosValue, Settings.CurrenctTabSigDigits.Value).ToString(), position, Settings.CurrencyTabFontColor, FontAlign.Center);
            Graphics.DrawBox(drawBox, Settings.CurrencyTabBackgroundColor);
            //Graphics.DrawFrame(drawBox, 1, Settings.CurrencyTabBorderColor);
        }

        private void PriceBoxOverItemHaggle(CustomItem item)
        {
            var box = item.Item.GetClientRect();
            var drawBox = new RectangleF(box.X, box.Y + 2, box.Width, +Settings.CurrencyTabBoxHeight);
            var position = new Vector2(drawBox.Center.X, drawBox.Center.Y - Settings.CurrencyTabFontSize.Value / 2);

            if (item.PriceData.ItemBasePrices.Count == 0)
                return;

            // Sort base unique price from High -> Low
            item.PriceData.ItemBasePrices.Sort((a, b) => b.CompareTo(a));

            if (Settings.Debug)
                Graphics.DrawText(string.Join(",", item.PriceData.ItemBasePrices), position, Settings.CurrencyTabFontColor, FontAlign.Center);


            Graphics.DrawText(Math.Round((decimal)item.PriceData.ItemBasePrices.FirstOrDefault(), Settings.CurrenctTabSigDigits.Value).ToString(CultureInfo.InvariantCulture), position, Settings.CurrencyTabFontColor, FontAlign.Center);
            Graphics.DrawBox(drawBox, Settings.CurrencyTabBackgroundColor);
            //Graphics.DrawFrame(drawBox, 1, Settings.CurrencyTabBorderColor);
        }

        private void ExpeditionGamble()
        {
            var window = HagglePanel;
            if (!window.IsVisible) return;
            foreach (var customItem in FortmattedHaggleItemList)
            {
                try
                {
                    PriceBoxOverItemHaggle(customItem);
                }
                catch (Exception e)
                {
                    // ignored
                    if (Settings.Debug)
                    {
                        LogMessage("Error in: ExpeditionGamble, restart PoEHUD.", 5, Color.Red);
                        LogMessage(e.ToString(), 5, Color.Orange);
                    }
                }
            }
        }

        /// <summary>
        ///     Displays price for unique items, and highlights the uniques under X value by drawing a border arround them.
        /// </summary>
        /// <param name="item"></param>
        private void HighlightJunkUniques(CustomItem item)
        {
                

            var hoverUi = GameController.Game.IngameState.UIHoverTooltip.Tooltip;
            if (hoverUi != null && (item.Rarity != ItemRarity.Unique || hoverUi.GetClientRect().Intersects(item.Item.GetClientRect()) && hoverUi.IsVisibleLocal)) return;

            var chaosValueSignificanDigits = Math.Round((decimal) item.PriceData.ChaosValue, Settings.HighlightSignificantDigits.Value);
            if (chaosValueSignificanDigits >= Settings.InventoryValueCutOff.Value) return;
            var rec = item.Item.GetClientRect();
            var fontSize = Settings.HighlightFontSize.Value;
            var backgroundBox = Graphics.MeasureText($"{chaosValueSignificanDigits}", fontSize);
            var position = new Vector2(rec.TopRight.X - fontSize, rec.TopRight.Y);

            //Graphics.DrawBox(new RectangleF(position.X - backgroundBox.Width, position.Y, backgroundBox.Width, backgroundBox.Height), Color.Black);
            Graphics.DrawText($"{chaosValueSignificanDigits}", position, Settings.UniTextColor, FontAlign.Center);

            DrawImage($"{DirectoryFullName}//images//Chaos_Orb_inventory_icon.png",
                new RectangleF(rec.TopRight.X - fontSize, rec.TopRight.Y,
                    Settings.HighlightFontSize.Value, Settings.HighlightFontSize.Value)
            );
            //Graphics.DrawFrame(item.Item.GetClientRect(), 2, Settings.HighlightColor.Value);
        }


        private void ShowHelmetEnchantPrices()
        {
            ExileCore.PoEMemory.Element GetElementByString(ExileCore.PoEMemory.Element element, string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    return null;

                if (element.Text != null && element.Text.Contains(str))
                    return element;

                return element.Children.Select(c => GetElementByString(c, str)).FirstOrDefault(e => e != null);
            }
            
            var igui = GameController.Game.IngameState.IngameUi;
            if (!igui.LabyrinthDivineFontPanel.IsVisible) return;
            var triggerEnchantment = GetElementByString(igui.LabyrinthDivineFontPanel, "lvl ");
            var enchantmentContainer = triggerEnchantment?.Parent?.Parent;
            if(enchantmentContainer == null) return;
            var enchants = enchantmentContainer.Children.Select(c => new {Name = c.Children[1].Text, ContainerElement = c}).AsEnumerable();
            if (!enchants.Any()) return;
            foreach (var enchant in enchants)
            {
                var data = GetHelmetEnchantValue(enchant.Name);
                if (data == null) continue;
                var box = enchant.ContainerElement.GetClientRect();
                var drawBox = new RectangleF(box.X + box.Width, box.Y - 2, 65, box.Height);
                var position = new Vector2(drawBox.Center.X, drawBox.Center.Y - 7);

                var textColor = data.PriceData.ExaltedPrice >= 1 ? Color.Black : Color.White;
                var bgColor = data.PriceData.ExaltedPrice >= 1 ? Color.Goldenrod : Color.Black;
                Graphics.DrawText(Math.Round((decimal) data.PriceData.ChaosValue, 2).ToString() + "c", position, textColor, FontAlign.Center);
                Graphics.DrawBox(drawBox, bgColor);
                Graphics.DrawFrame(drawBox, Color.Black, 1);
            }
        }

        private double TryGetArtifactToChaosPrice(CustomItem item)
        {
            if (item == null || item.Item == null)
                return -1;

            ExileCore.PoEMemory.Element GetElementByString(ExileCore.PoEMemory.Element element, string str)
            {
                if (element == null || string.IsNullOrWhiteSpace(str))
                    return null;

                if (element.Text != null && element.Text.Contains(str))
                    return element;

                return element.Children.Select(c => GetElementByString(c, str)).FirstOrDefault(e => e != null);
            }

            var costElement = GetElementByString(item.Item?.AsObject<HoverItemIcon>()?.Tooltip, "Cost");
            if (costElement == null || costElement.Parent == null || costElement.Parent.ChildCount < 2 ||
                costElement.Parent.Children[1].ChildCount < 3)
                return -1;
            var amountText = costElement.Parent.Children[1].Children[0].Text;
            var artifactName = costElement.Parent.Children[1].Children[2].Text;
            if (string.IsNullOrWhiteSpace(amountText) || string.IsNullOrWhiteSpace(artifactName))
                return -1;
            var artifactSearch = CollectedData.Artifacts.Lines.Find(x => x.Name == artifactName);
            if (artifactSearch == null)
                return -1;
            if (costElement.Text.Equals("Cost:")) // Tujen haggling
            {
                var amount = int.Parse(amountText.Substring(0, amountText.Length - 1));
                return artifactSearch.ChaosValue.Value * amount;
            }
            if (costElement.Text.Equals("Cost Per Unit:")) // Artifact stacks (Dannig)
            {
                var costPerUnit = double.Parse(amountText);
                return item.CurrencyInfo.StackSize * costPerUnit * artifactSearch.ChaosValue.Value;
            }
            return -1;
        }

        //private void PropheccyDisplay()
        //{
        //    if (!Settings.ProphecyPrices)
        //        return;

        //    try
        //    {
        //        var UIHover = GameController.Game.IngameState.UIHover;
        //        var newBox = new RectangleF(lastProphWindowPos.X, lastProphWindowPos.Y, lastProphWindowSize.X, lastProphWindowSize.Y);

        //        if (!StashPanel.IsVisible) return;
        //        var refBool = true;

        //        if (!UIHover.Tooltip.GetClientRect().Intersects(newBox))
        //        {
        //            var menuOpacity = ImGui.GetStyle().GetColor(ColorTarget.WindowBg).W;
        //            if (Settings.ProphecyOverrideColors)
        //            {
        //                var tempColor = new SharpDX.Vector4(Settings.ProphecyBackground.Value.R / 255.0f, Settings.ProphecyBackground.Value.G / 255.0f,
        //                    Settings.ProphecyBackground.Value.B / 255.0f, Settings.ProphecyBackground.Value.A / 255.0f);
        //                ImGui.PushStyleColor(ColorTarget.WindowBg, ToImVector4(tempColor));
        //                menuOpacity = ImGui.GetStyle().GetColor(ColorTarget.WindowBg).W;
        //            }

        //            ImGui.BeginWindow("Poe.NinjaProphs", ref refBool, new System.Numerics.Vector2(200, 150), menuOpacity, Settings.ProphecyLocked ? WindowFlags.NoCollapse | WindowFlags.NoScrollbar | WindowFlags.NoMove | WindowFlags.NoResize | WindowFlags.NoInputs | WindowFlags.NoBringToFrontOnFocus | WindowFlags.NoTitleBar | WindowFlags.NoFocusOnAppearing : WindowFlags.Default | WindowFlags.NoTitleBar | WindowFlags.ResizeFromAnySide);

        //            if (Settings.ProphecyOverrideColors)
        //                ImGui.PopStyleColor();


        //            var prophystringlist = new List<string>();
        //            var propicies = GameController.Player.GetComponent<Player>().Prophecies;
        //            foreach (var prophecyDat in propicies)
        //            {
        //                //var text = $"{GetProphecyValues(prophecyDat.Name)}c - {prophecyDat.Name}({prophecyDat.SealCost})";
        //                var text = $"{{{HexConverter(Settings.ProphecyChaosValue)}}}{GetProphecyValues(prophecyDat.Name)}c {{}}- {{{HexConverter(Settings.ProphecyProecyName)}}}{prophecyDat.Name} {{{HexConverter(Settings.ProphecyProecySealColor)}}}({prophecyDat.SealCost}){{}}";
        //                if (prophystringlist.Any(x => Equals(x, text))) continue;
        //                prophystringlist.Add(text);
        //            }

        //            foreach (var proph in prophystringlist)
        //                //ImGui.Text(VARIABLE);
        //                Coloredtext(proph);

        //            lastProphWindowSize = new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y);
        //            lastProphWindowPos = new Vector2(ImGui.GetWindowPosition().X, ImGui.GetWindowPosition().Y);
        //            ImGui.EndWindow();
        //        }
        //    }
        //    catch
        //    {
        //        ImGui.EndWindow();
        //    }
        //}
    }
}