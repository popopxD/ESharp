using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.Common;
using SharpDX;
using Color = SharpDX.Color;
using Ensage.Common.Menu;

namespace DisplaySpellRange
{
    internal class Program
    {
        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static bool _leftMouseIsPress;
        private static List<RangeObj> _customList = new List<RangeObj>();
        private static List<RangeObj> _spellList = new List<RangeObj>();
        private static List<RangeObj> _itemList = new List<RangeObj>();
        public static Dictionary<string, RangeObj> CacheSpellList = new Dictionary<string, RangeObj>();
        private static bool _initialized = false;
        public static Hero Me;
        public static Unit SelectedUnit;
        private static readonly Menu Menu = new Menu("DisplaySpellRange", "DSR", true);

        private static void Main(string[] args)
        {
            //TODO: Custom List
            //TODO: Color range fix
            //TODO: Fix Unit name colision
            Menu.AddItem(new MenuItem("lockMeOnly"   , "Use range indicator for my hero ONLY").SetValue(false));
            Menu.AddItem(new MenuItem("useOldStyle"  , "Use old style range indicator").SetValue(false));
            Menu.AddItem(new MenuItem("useColorStyle", "Use color style range indicator").SetValue(true));
            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                return;
            }

            //loop through the spellList and display them
            int i;
            RangeObj ability;
            Vector2 size = new Vector2(32, 32);
            Vector2 size2 = new Vector2(43, 32);
            Vector2 sizeItem = new Vector2(59, 32);
            Vector2 sizeIcon;
            Vector2 sizeButton;
            int stepSize;
            Vector2 start = new Vector2(100, 52);
            for (i = 0; i < _customList.Count; i++)
            {
                ability = _customList[i];
                Drawing.DrawRect(start, size, Drawing.GetTexture(ability.TextureName));
                DrawButton(start, size, ref ability, new Color(100, 255, 0, 40), new Color(100, 0, 0, 40));
                start.X += 32;
            }

            for (i = 0; i < _spellList.Count; i++)
            {
                ability = _spellList[i];
                Drawing.DrawRect(start, size, Drawing.GetTexture(ability.TextureName));
                DrawButton(start, size, ref ability, new Color(100, 255, 0, 40), new Color(100, 0, 0, 40));
                start.X += 32;
            }

            start = new Vector2(100, 92);
            for (i = 0; i < _itemList.Count; i++)
            {
                ability = _itemList[i];
                Drawing.DrawRect(start, sizeItem, Drawing.GetTexture(ability.TextureName));
                DrawButton(start, size2, ref ability, new Color(100, 255, 0, 45), new Color(100, 0, 0, 45));
                start.X += 43;
            }

            start = new Vector2(100, 132);
            var keys = new List<string>(CacheSpellList.Keys);
            foreach (var key in keys)
            {
                ability = CacheSpellList[key];
                if (ability.Ability is Item)
                {
                    sizeButton = size2;
                    sizeIcon = sizeItem;
                    stepSize = 43;
                }
                else
                {
                    sizeButton = size;
                    sizeIcon = size;
                    stepSize = 32;
                }
                Drawing.DrawRect(start, sizeIcon, Drawing.GetTexture(ability.TextureName));
                DrawButton(start, sizeButton, ref ability, new Color(100, 255, 0, 45), new Color(100, 0, 0, 45));
                start.X += stepSize;
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            Me = ObjectManager.LocalHero;

            if (!Game.IsInGame && _initialized)
            {
                _spellList = new List<RangeObj>();
                _itemList = new List<RangeObj>();
                _customList = new List<RangeObj>();
                CacheSpellList = new Dictionary<string, RangeObj>();
                _initialized = false;
            }
            if (!Game.IsInGame || !Utils.SleepCheck("DSR_GameUpdateSleeper"))
            {
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
            }

            RangeObj.UseOldStyle = Menu.Item("useOldStyle").GetValue<bool>();
            RangeObj.UseColorStyle = Menu.Item("useColorStyle").GetValue<bool>();
            if (Menu.Item("lockMeOnly").GetValue<bool>())
            {
                SelectedUnit = Me;
            }
            else
            {
                SelectedUnit = (Unit)ObjectManager.LocalPlayer.Selection.FirstOrDefault();
            }
            _customList = new List<RangeObj>();
            _spellList = new List<RangeObj>();
            _itemList = new List<RangeObj>();
            if (SelectedUnit == null)
            {
                return;
            }
            RangeObj rangeObj = null;
            foreach (Ability spell in SelectedUnit.Spellbook.Spells)
            {
                if (spell.Name == "attribute_bonus")
                {
                    continue;
                }
                try
                {
                    rangeObj = CacheSpellList[SelectedUnit.Name + "_" + spell.Name];
                }
                catch (KeyNotFoundException)
                {
                    rangeObj = new RangeObj(spell, SelectedUnit);
                }
                _spellList.Add(rangeObj);
            }
            foreach (Item item in SelectedUnit.Inventory.Items)
            {
                try
                {
                    rangeObj = CacheSpellList[SelectedUnit.Name + "_" + item.Name];
                }
                catch (KeyNotFoundException)
                {
                    rangeObj = new RangeObj(item, SelectedUnit);
                }
                _itemList.Add(rangeObj);
            }

            //loop through the spellList and display them
            List<string> deleteList = (from item in CacheSpellList where !item.Value.Refresh() select item.Key).ToList();
            foreach (var key in deleteList)
            {
                CacheSpellList.Remove(key);
            }

            Utils.Sleep(500, "DSR_GameUpdateSleeper");
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.WParam != 1 || Game.IsChatOpen || !Utils.SleepCheck("clicker"))
            {
                _leftMouseIsPress = false;
                return;
            }
            _leftMouseIsPress = true;
        }
        
        #region printer
        private static void DrawButton(Vector2 a, Vector2 b, ref RangeObj rangeObj, Color @on, Color off)
        {
            var isIn = Utils.IsUnderRectangle(Game.MouseScreenPosition,a.X,a.Y, b.X,b.Y);
            if (rangeObj.IsDisplayable)
            {
                if (_leftMouseIsPress && Utils.SleepCheck("DSR_ClickButtonCd") && isIn)
                {
                    rangeObj.IsDisplayed = !rangeObj.IsDisplayed;
                    Utils.Sleep(250, "DSR_ClickButtonCd");
                }
                var newColor = isIn
                    ? new Color((int)(rangeObj.IsDisplayed ? @on.R : off.R), rangeObj.IsDisplayed ? @on.G : off.G, rangeObj.IsDisplayed ? @on.B : off.B, 150)
                    : rangeObj.IsDisplayed ? @on : off;
                Drawing.DrawRect(a, b, newColor);
            }
            else
            {
                Drawing.DrawRect(a, b, new Color(172, 172, 172, 125));
            }
            Drawing.DrawRect(a, b, Color.Black, true);
        }
        #endregion

    }
}



