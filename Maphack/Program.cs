using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;

namespace Maphack
{
    internal class Program
    {
        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly Menu Menu = new Menu("Maphack", "maphack", true);

        private static readonly Hero[] Heroes = new Hero[40];
        private static readonly int[] HeroesPlayerPosition = new int[40];
        private static int _nHeroes = 0;

        private static readonly Vector2 HeroColorSize = new Vector2(80, 55);
        private static readonly Vector2 HeroColorSizeHalf = new Vector2(40, 27.5f);
        private static readonly Vector2 HeroIconSize = new Vector2(75, 50);
        private static readonly Vector2 HeroIconSizeHalf = new Vector2(37.5f, 25);

        private static Font _text;

        private static readonly Color[] PlayerColor =
        {
            Color.Blue, Color.Teal, Color.Purple, Color.Yellow, Color.Orange,
            Color.Pink, Color.Gray, Color.LightBlue, Color.DarkGreen, Color.Brown,
            Color.White
        };

        private static int _minimapWidth = 0;
        private static int _minimapHeight = 0;
        private static int _minimapCorner = 0;

        private static void Main(string[] args)
        {
            Menu.AddItem(new MenuItem("auto_reload", "Auto Reload").SetValue(false));
            Menu.AddItem(new MenuItem("refresh_hotkey2", "Force Refresh hotkey").SetValue(new KeyBind('M', KeyBindType.Press)).SetTooltip("Force refresh, regardless of the update rate"));
            Menu.AddItem(new MenuItem("repeat_hotkey2", "Repeat hotkey").SetValue(new KeyBind('N', KeyBindType.Press)).SetTooltip("Hold to refresh at refresh rate"));
            Menu.AddItem(new MenuItem("refresh_rate", "Refresh Rate").SetValue(new Slider(5000, 50, 10000)).SetTooltip("tick per refresh"));
            Menu.AddToMainMenu();
            
            _text = new Font(
               Drawing.Direct3DDevice9,
               new FontDescription
               {
                   FaceName = "Calibri",
                   Height = 25,
                   OutputPrecision = FontPrecision.Default,
                   Quality = FontQuality.Default
               });

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
            Game.OnIngameUpdate += Game_OnUpdate;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame) return;

            var me = ObjectMgr.LocalPlayer;
            if (me == null) return;
            for (uint i = 0; i < _nHeroes; ++i)
            {
                var enemy = Heroes[i];
                if (enemy.IsVisible || !enemy.IsAlive) continue;

                int x = (int) Math.Floor((enemy.Position.X + 7500) * _minimapWidth / 15000);
                int y = (int) Math.Floor((enemy.Position.Y + 7000) * _minimapHeight / 14000);

                _text.DrawText(null, (HeroesPlayerPosition[i] + 1).ToString(), x + _minimapCorner, Drawing.Height - y - _minimapCorner - 7, PlayerColor[HeroesPlayerPosition[i]]);
            }
        }

        static void Drawing_OnPostReset(EventArgs args)
        {
            _text.OnResetDevice();
        }

        static void Drawing_OnPreReset(EventArgs args)
        {
            _text.OnLostDevice();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame)
            {
                if (_nHeroes > 0) _nHeroes = 0;
                return;
            }

            var me = ObjectMgr.LocalPlayer;
            if (me == null) return;

            for (uint i = 0; i < _nHeroes; ++i)
            {
                var enemy = Heroes[i];
                if (enemy.IsVisible || !enemy.IsAlive) continue;

                string textureName = enemy.Name.Substring(14);
                Vector2 screenPos;
                Drawing.WorldToScreen(enemy.Position, out screenPos);

                var heroColorScreenPos = screenPos - HeroColorSizeHalf;
                var heroIconScreenPos = screenPos - HeroIconSizeHalf;
                Drawing.DrawRect(heroColorScreenPos, HeroColorSize, PlayerColor[HeroesPlayerPosition[i]]);
                Drawing.DrawRect(heroIconScreenPos, HeroIconSize, Drawing.GetTexture("materials/ensage_ui/heroes_horizontal/" + textureName + ".vmat"));
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            //we only proceed if 1 of the following condition is True
            //+ Forced refresh hotkey is Active and slept for 50ms
            //+ Repeat hotkey is Active and SleepCheck is off CD
            //+ Auto Reload is ON and SleepCheck is off CD
            if (!((Menu.Item("refresh_hotkey2").GetValue<KeyBind>().Active && Utils.SleepCheck("GCMH_GameUpdateMinSleeper")) ||
                ((Menu.Item("repeat_hotkey2").GetValue<KeyBind>().Active || Menu.Item("auto_reload").GetValue<bool>()) && Utils.SleepCheck("GCMH_GameUpdateSleeper")))) return;

            //minimap temp fix
            _minimapHeight = (int) Math.Floor(260.0 * Drawing.Height / 1080);
            _minimapWidth = (int) Math.Floor(270.0 * Drawing.Height / 1080);
            _minimapCorner = (int) Math.Floor(11.0 * Drawing.Height / 1080);

            //backup camera position
            Game.ExecuteCommand("cl_fullupdate");

            //get the game state
            var me = ObjectMgr.LocalPlayer;
            _nHeroes = 0;
            for (uint i = 0; i < 40; ++i)
            {
                var player = ObjectMgr.GetPlayerById(i);
                if (player == null || player.Team == me.Team) continue;
                int playerColor = (player.Team == Team.Radiant ? 0 : 1) * 5 + (int) player.TeamSlot;
                playerColor = playerColor < 0 ? 11 : playerColor > 10 ? 11 : playerColor;
                HeroesPlayerPosition[_nHeroes] = playerColor;
                Heroes[_nHeroes++] = player.Hero;
            }

            //sleepers
            Utils.Sleep(Menu.Item("refresh_rate").GetValue<Slider>().Value, "GCMH_GameUpdateSleeper");
            Utils.Sleep(50, "GCMH_GameUpdateMinSleeper");  //sleep at least 50 tick
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
        }
    }
}



