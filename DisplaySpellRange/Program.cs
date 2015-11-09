using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using Color = SharpDX.Color;

namespace DisplaySpellRange
{
    internal class Program
    {
        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static bool _initialized;
        private static bool _leftMouseIsPress;
        private static List<RangeObj> _spellList;
        private static List<RangeObj> _itemList;
        public static Hero Me;
        private static Dictionary<string, DotaTexture> _textureCache = new Dictionary<string, DotaTexture>();

        private static void Main(string[] args)
        {
            _initialized = false;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame || !_initialized) return;

            //loop through the spellList and display them
            int i;
            Vector2 start = new Vector2(100, 52);
            RangeObj ability;
            Vector2 size = new Vector2(32, 32);
            for (i = 0; i < _spellList.Count; i++)
            {
                ability = _spellList[i];
                Drawing.DrawRect(start, size, GetTexture(ability.TextureName));
                DrawButton(start, size, ref ability.IsDisplayed, ability.IsDisplayable, new Color(100, 255, 0, 40), new Color(100, 0, 0, 40));
                start.X += 32;
            }

            Vector2 itemSize = new Vector2(59, 32);
            size = new Vector2(43, 32);
            start = new Vector2(100, 102);
            for (i = 0; i < _itemList.Count; i++)
            {
                ability = _itemList[i];
                Drawing.DrawRect(start, itemSize, GetTexture(ability.TextureName));
                DrawButton(start, size, ref ability.IsDisplayed, ability.IsDisplayable, new Color(100, 255, 0, 45), new Color(100, 0, 0, 45));
                start.X += 43;
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            Me = ObjectMgr.LocalHero;
            int i;

            //check the game state and initialize if possible
            if (!_initialized)
            {
                if (!Game.IsInGame || Me == null)
                {
                    return;
                }
                _initialized = true;
                Log.Success("> Starting DisplaySpellRange v" + Ver);

                _spellList = new List<RangeObj>();
                _itemList = new List<RangeObj>();
                foreach (Ability spell in Me.Spellbook.Spells)
                {
                    if (spell.Name == "attribute_bonus") continue;
                    _spellList.Add(new RangeObj(spell));
                }
            }
            if (!Game.IsInGame || Me == null)
            {
                _initialized = false;
                _spellList = null;
                _itemList = null;
                Log.Info("> Unloaded DisplaySpellRange");
                return;
            }
            if (!Game.IsInGame || !_initialized || !Utils.SleepCheck("DSR_GameUpdateSleeper")) return;

            //loop through the spellList and display them
            RangeObj ability;
            for (i = 0; i < _spellList.Count; i++)
            {
                ability = _spellList[i];
                ability.Refresh();  //refresh the spell for some reasons: Spell is changed (level up, rupick steal, ...) or state is changed (isDisplayed change)
            }
            
            i = -1;
            foreach (Item item in Me.Inventory.Items)
            {
                ++i;
                if (i > _itemList.Count - 1)
                {
                    _itemList.Add(new RangeObj(item));
                }
                ability = _itemList[i];
                ability.Update(item);
            }
            for (int j = _itemList.Count - 1; j > i; --j)
            {
                _itemList[j].Update(null);
                _itemList.RemoveAt(j);
            }

            Utils.Sleep(100, "DSR_GameUpdateSleeper");
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

        public static DotaTexture GetTexture(string name)
        {
            if (_textureCache.ContainsKey(name)) return _textureCache[name];

            return _textureCache[name] = Drawing.GetTexture(name);
        }

        #region printer
        private static void DrawButton(Vector2 a, Vector2 b,ref bool clicked, bool isActive, Color @on, Color off)
        {
            var isIn = Utils.IsUnderRectangle(Game.MouseScreenPosition,a.X,a.Y, b.X,b.Y);
            if (isActive)
            {
                if (_leftMouseIsPress && Utils.SleepCheck("DSR_ClickButtonCd") && isIn)
                {
                    clicked = !clicked;
                    Utils.Sleep(250, "DSR_ClickButtonCd");
                }
                var newColor = isIn
                    ? new Color((int)(clicked ? @on.R : off.R), clicked ? @on.G : off.G, clicked ? @on.B : off.B, 150)
                    : clicked ? @on : off;
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



