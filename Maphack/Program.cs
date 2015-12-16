using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using Color = SharpDX.Color;

namespace Maphack
{
    internal class Program
    {
        private static readonly string Ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly Menu Menu = new Menu("Maphack", "maphack", true);

        private static readonly Hero[] Heroes = new Hero[40];
        private static int _nHeroes = 0;

        private static void Main(string[] args)
        {
            Menu.AddItem(new MenuItem("auto_reload", "Auto Reload").SetValue(false));
            Menu.AddItem(new MenuItem("refresh_hotkey2", "Force Refresh hotkey").SetValue(new KeyBind('M', KeyBindType.Press)).SetTooltip("Force refresh, regardless of the update rate"));
            Menu.AddItem(new MenuItem("repeat_hotkey2", "Repeat hotkey").SetValue(new KeyBind('N', KeyBindType.Press)).SetTooltip("Hold to refresh at refresh rate"));
            Menu.AddItem(new MenuItem("refresh_rate", "Refresh Rate").SetValue(new Slider(5000, 50, 10000)).SetTooltip("miliseconds"));
            Menu.AddToMainMenu();

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

            var player = ObjectMgr.LocalPlayer;
            if (player == null || player.Team == Team.Observer) return;

            /*if (_enabled)
                _text.DrawText(null, "Snatcher: Enabled, \"P\" for toggle.", 5, 64, Color.White);
            else
                _text.DrawText(null, "Snatcher: Disabled, \"P\" for toggle.", 5, 64, Color.White);*/
        }

        static void Drawing_OnPostReset(EventArgs args)
        {
            //_text.OnResetDevice();
        }

        static void Drawing_OnPreReset(EventArgs args)
        {
            //_text.OnLostDevice();
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

            Vector2 ScreenPos;
            for (uint i = 0; i < _nHeroes; ++i)
            {
                var enemy = Heroes[i];
                string textureName = enemy.Name.Substring(14);
                Drawing.WorldToScreen(enemy.Position, out ScreenPos);
                Drawing.DrawRect(ScreenPos, new Vector2(75, 50), Drawing.GetTexture("materials/ensage_ui/heroes_horizontal/" + textureName + ".vmat"));
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            //we only proceed if 1 of the following condition is True
            //+ Forced refresh hotkey is Active
            //+ Repeat hotkey is Active and SleepCheck is off CD
            //+ Auto Reload is ON and SleepCheck is off CD
            if (!(Menu.Item("refresh_hotkey2").GetValue<KeyBind>().Active ||
                ((Menu.Item("repeat_hotkey2").GetValue<KeyBind>().Active || Menu.Item("auto_reload").GetValue<bool>()) && Utils.SleepCheck("GCMH_GameUpdateSleeper")))) return;

            Game.ExecuteCommand("cl_fullupdate");
            var me = ObjectMgr.LocalPlayer;
            _nHeroes = 0;
            for (uint i = 0; i < 40; ++i)
            {
                var player = ObjectMgr.GetPlayerById(i);
                if (player == null) continue;  // || player.Team == me.Team not working ?
                Heroes[_nHeroes++] = player.Hero;
            }

            Utils.Sleep(Menu.Item("refresh_rate").GetValue<Slider>().Value, "GCMH_GameUpdateSleeper");
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            //TODO: Draw to mini map
            //TODO: Improve
        }
    }
}



