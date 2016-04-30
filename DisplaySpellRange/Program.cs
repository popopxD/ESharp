using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
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
        private static HashSet<float> _customRange = new HashSet<float>();
        private static List<RangeObj> _customList = new List<RangeObj>();
        private static List<RangeObj> _spellList = new List<RangeObj>();
        private static List<RangeObj> _itemList = new List<RangeObj>();
        public static Dictionary<string, RangeObj> CacheSpellList = new Dictionary<string, RangeObj>();
        private static bool _initialized = false;
        public static Hero Me;
        public static Unit SelectedUnit;

        private static readonly Menu Menu = new Menu("DisplaySpellRange", "DSR", true);
        private static readonly Menu RangeMenu = new Menu("Custom Range", "ranges");
        private static string _filePath;
        private static string _fileFullPath;
        private static bool _customRangeToggler = false;
        private static bool _showCachedRange = false;
        private static bool _showRangeSelector = false;

        private static void Main(string[] args)
        {
            _filePath = Path.Combine(MenuConfig.AppDataDirectory, "GaConConfig");
            _fileFullPath = Path.Combine(_filePath, "DisplaySpellRange.txt");
            Log.Success("DisplaySpellRange data location: " + _fileFullPath);
            LoadData();
            RangeMenu.AddItem(new MenuItem("rangeAddNum"    , "Range x10").SetValue(new Slider(120, 0, 320)));
            RangeMenu.AddItem(new MenuItem("addRangeToggler", "Add (Toggle to add)").SetValue(_customRangeToggler).SetTooltip("Need a menu for this feature :("));
            foreach (var num in _customRange)
            {
                int numInt = (int) num;
                RangeMenu.AddItem(new MenuItem("range" + numInt, "Range " + numInt).SetValue(true).SetTooltip("Toggle to delete").DontSave());
            }
            Menu.AddSubMenu(RangeMenu);

            Menu.AddItem(new MenuItem("showRangeSelector" , "Show").SetValue(true));
            Menu.AddItem(new MenuItem("showCachedRange"   , "Show activated spells").SetValue(true));
            Menu.AddItem(new MenuItem("useAttackRange"    , "Attack Range").SetValue(true));
            Menu.AddItem(new MenuItem("lockMeOnly"        , "Use range indicator for my units ONLY").SetValue(false));
            Menu.AddItem(new MenuItem("useOldStyle"       , "Use old style range indicator").SetValue(false));
            Menu.AddItem(new MenuItem("useColorStyle"     , "Use color style range indicator").SetValue(true));
            Menu.AddItem(new MenuItem("refreshRate"       , "Refresh rate").SetValue(new Slider(500, 0, 1000)));
            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string data = File.ReadAllText(_fileFullPath);
                    var numList = data.Split(',');
                    foreach (var numStr in numList)
                    {
                        double num;
                        if (Double.TryParse(numStr, out num))
                        {
                            _customRange.Add((float)num);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        private static void SaveData()
        {
            try
            {
                string data = string.Join(",", _customRange.Select(x => x.ToString()).ToArray());
                if (!Directory.Exists(_filePath))
                {
                    Directory.CreateDirectory(_filePath);
                }
                File.WriteAllText(_fileFullPath, data);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

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
            Vector2 sizeEmpty = new Vector2(47, 32);
            Vector2 sizeIcon;
            Vector2 sizeButton;
            int stepSize;
            Vector2 start = new Vector2(100, 52);

            if (!_showRangeSelector)
            {
                return;
            }
            for (i = 0; i < _customList.Count; i++)
            {
                ability = _customList[i];
                Drawing.DrawRect(start, sizeEmpty, Drawing.GetTexture(ability.TextureName));
                string text;
                if (ability.isAttackRange)
                {
                    text = "Attack";
                    Drawing.DrawText(text, start + new Vector2(1, 3), new Color(0, 219, 0, 255), FontFlags.None);
                    text = "" + ability.Range;
                    Drawing.DrawText(text, start + new Vector2(1, 15), new Color(0, 219, 0, 255), FontFlags.None);
                }
                else
                {
                    text = ((int) ability.Range).ToString();
                    Drawing.DrawText(text, start + new Vector2(1, 8), new Color(0, 219, 0, 255), FontFlags.None);
                }
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

            if (!_showCachedRange)
            {
                return;
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
                if (ability.isRangeOnly || ability.isAttackRange)
                {
                    string text;
                    Drawing.DrawRect(start, sizeEmpty, Drawing.GetTexture(ability.TextureName));
                    if (ability.isAttackRange)
                    {
                        text = "Attack";
                        Drawing.DrawText(text, start + new Vector2(1, 3), new Color(0, 219, 0, 255), FontFlags.None);
                        text = "" + ability.Range;
                        Drawing.DrawText(text, start + new Vector2(1, 15), new Color(0, 219, 0, 255), FontFlags.None);
                    }
                    else
                    {
                        text = ((int)ability.Range).ToString();
                        Drawing.DrawText(text, start + new Vector2(1, 8), new Color(0, 219, 0, 255), FontFlags.None);
                    }
                }
                else
                {
                    Drawing.DrawRect(start, sizeIcon, Drawing.GetTexture(ability.TextureName));
                }
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

            if (!Game.IsInGame)
            {
                return;
            }

            if (_leftMouseIsPress && Game.IsInGame)
            {
                var selectedUnitTemp = ObjectManager.GetEntities<Unit>().Where(x => Vector3.Distance(x.Position, Game.MousePosition) < 300).OrderByDescending(x => -Vector3.Distance(x.Position, Game.MousePosition)).FirstOrDefault();
                if (selectedUnitTemp != null)
                {
                    SelectedUnit = selectedUnitTemp;
                }
            }

            if (!Utils.SleepCheck("DSR_GameUpdateSleeper"))
            {
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
            }

            List<float> deleteSetList = (from item in _customRange where !RangeMenu.Item("range" + (int)item).GetValue<bool>() select item).ToList();
            bool changedSetList = false;
            foreach (var key in deleteSetList)
            {
                _customRange.Remove(key);
                for (int i = 0; i < RangeMenu.Items.Count; ++i)
                {
                    var menu = RangeMenu.Items[i];
                    if (menu.Name == ("range" + (int) key))
                    {
                        menu.Parent = null;
                        RangeMenu.Items.RemoveAt(i);
                        changedSetList = true;
                        break;
                    }
                }
            }
            if (RangeMenu.Item("addRangeToggler").GetValue<bool>() != _customRangeToggler)
            {
                _customRangeToggler = !_customRangeToggler;
                var num = RangeMenu.Item("rangeAddNum").GetValue<Slider>().Value * 10;
                RangeMenu.AddItem(new MenuItem("range" + num, "Range " + num).SetValue(true).SetTooltip("Toggle to delete").DontSave());
                _customRange.Add(num);
                changedSetList = true;
            }
            if (changedSetList)
            {
                SaveData();
            }

            RangeObj.UseOldStyle = Menu.Item("useOldStyle").GetValue<bool>();
            RangeObj.UseColorStyle = Menu.Item("useColorStyle").GetValue<bool>();
            _showCachedRange = Menu.Item("showCachedRange").GetValue<bool>();
            _showRangeSelector = Menu.Item("showRangeSelector").GetValue<bool>();

            _customList = new List<RangeObj>();
            _spellList = new List<RangeObj>();
            _itemList = new List<RangeObj>();
            if (Menu.Item("lockMeOnly").GetValue<bool>())
            {
                SelectedUnit = (Unit)ObjectManager.LocalPlayer.Selection.FirstOrDefault();
            }
            if (SelectedUnit != null && !SelectedUnit.IsValid)
            {
                SelectedUnit = null;
            }
            if (SelectedUnit == null)
            {
                SelectedUnit = Me;
            }
            if (SelectedUnit != null)
            {
                RangeObj rangeObj = null;
                if (SelectedUnit.AttackCapability != AttackCapability.None)
                {
                    if (Menu.Item("useAttackRange").GetValue<bool>())
                    {
                        try
                        {
                            rangeObj = CacheSpellList[RangeObj.GetCacheKeyName(SelectedUnit, null, 0f, true)];
                        }
                        catch (KeyNotFoundException)
                        {
                            rangeObj = new RangeObj(true, SelectedUnit);
                        }
                        _customList.Add(rangeObj);
                    }
                }
                foreach (var rangeNum in _customRange)
                {
                    try
                    {
                        rangeObj = CacheSpellList[RangeObj.GetCacheKeyName(SelectedUnit, null, rangeNum)];
                    }
                    catch (KeyNotFoundException)
                    {
                        rangeObj = new RangeObj(rangeNum, SelectedUnit);
                    }
                    _customList.Add(rangeObj);
                }
                foreach (var spell in SelectedUnit.Spellbook.Spells)
                {
                    if (spell.Name == "attribute_bonus")
                    {
                        continue;
                    }
                    try
                    {
                        rangeObj = CacheSpellList[RangeObj.GetCacheKeyName(SelectedUnit, spell)];
                    }
                    catch (KeyNotFoundException)
                    {
                        rangeObj = new RangeObj(spell, SelectedUnit);
                    }
                    _spellList.Add(rangeObj);
                }
                if (SelectedUnit.HasInventory)
                {
                    foreach (var item in SelectedUnit.Inventory.Items)
                    {
                        try
                        {
                            rangeObj = CacheSpellList[RangeObj.GetCacheKeyName(SelectedUnit, item)];
                        }
                        catch (KeyNotFoundException)
                        {
                            rangeObj = new RangeObj(item, SelectedUnit);
                        }
                        _itemList.Add(rangeObj);
                    }
                }
            }

            //loop through the spellList and display them
            bool retry = true;
            while (retry)
            {
                try
                {
                    List<string> deleteList = new List<string>();
                    foreach (var item in CacheSpellList)
                    {
                        try
                        {
                            if (!item.Value.Refresh()) deleteList.Add(item.Key);
                        }
                        catch (EntityNotFoundException)
                        {
                        }
                    }
                    foreach (var key in deleteList)
                    {
                        CacheSpellList.Remove(key);
                    }
                    retry = false;
                }
                catch (InvalidOperationException)
                {
                }
            }

            Utils.Sleep(Menu.Item("refreshRate").GetValue<Slider>().Value, "DSR_GameUpdateSleeper");
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



