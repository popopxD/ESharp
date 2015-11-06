using System;
using System.Collections.Generic;
using System.Drawing;
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
        public static string ShowAbilityName;
        public static int ShowAbilityTickLeft;
        public const int MaxDisplayTick = 400;

        private static void Main(string[] args)
        {
            _initialized = false;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Drawing_OnDraw(EventArgs args)
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

                ShowAbilityTickLeft = 0;
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
            if (!Game.IsInGame || !_initialized) return;

            //loop through the spellList and display them
            Vector2 start;
            RangeObj ability;
            for (i = 0; i < _spellList.Count; i++)
            {
                start = new Vector2(100 + i * 40, 52);
                ability = _spellList[i];
                ability.Refresh();  //refresh the spell for some reasons: Spell is changed (level up, rupick steal, ...) or state is changed (isDisplayed change)
                DrawButton(start, new Vector2(40, 40), ref ability.IsDisplayed, ability.IsDisplayable, new Color(100, 255, 0, 40), new Color(100, 0, 0, 40));
                //Drawing.DrawRect(start, new Vector2(40, 40), Drawing.GetTexture(@"resource\flash3\images\spellicons\abaddon_aphotic_shield.png"));
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
                start = new Vector2(100 + i * 40, 102);
                DrawButton(start, new Vector2(40, 40), ref ability.IsDisplayed, ability.IsDisplayable, new Color(100, 255, 0, 45), new Color(100, 0, 0, 45));
            }
            for (int j = _itemList.Count - 1; j > i; --j)
            {
                _itemList[j].Update(null);
                _itemList.RemoveAt(j);
            }

            if (ShowAbilityTickLeft > 0)
            {
                --ShowAbilityTickLeft;
                Drawing.DrawText(ShowAbilityName, new Vector2(105, 152), new Color(200, 200, 200, 128), FontFlags.None);
                Drawing.DrawRect(new Vector2(100, 150), new Vector2(7 * ShowAbilityName.Length, 20), new Color(0, 0, 0, 70));
            }
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
        private static void DrawButton(Vector2 a, Vector2 b,ref bool clicked, bool isActive, Color @on, Color off)
        {
            var isIn = Utils.IsUnderRectangle(Game.MouseScreenPosition,a.X,a.Y, b.X,b.Y);
            if (isActive)
            {
                if (_leftMouseIsPress && Utils.SleepCheck("ClickButtonCd") && isIn)
                {
                    clicked = !clicked;
                    Utils.Sleep(250, "ClickButtonCd");
                }
                var newColor = isIn
                    ? new Color((int)(clicked ? @on.R : off.R), clicked ? @on.G : off.G, clicked ? @on.B : off.B, 150)
                    : clicked ? @on : off;
                Drawing.DrawRect(a, b, newColor);
            }
            else
            {
                Drawing.DrawRect(a, b, new Color(192, 192, 192, 45));
            }
            Drawing.DrawRect(a, b, Color.Black, true);
        }
        #endregion

    }
}



