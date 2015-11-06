using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ensage;
using Ensage.Common;
using SharpDX;
using Attribute = Ensage.Attribute;

namespace DuelDamageIndicator
{
    class Program
    {
        static void Main(string[] args)
        {
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame) return;
            var player = ObjectMgr.LocalPlayer;
            var me = ObjectMgr.LocalHero;
            if (player == null || player.Team == Team.Observer || me == null) return;

            HeroDamageObj myDamageObj = new HeroDamageObj(me, 0.2);
            
            var enemies = ObjectMgr.GetEntities<Hero>().Where(x => x.Team != me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion).ToList();
            foreach (var enemy in enemies)
            {
                HeroDamageObj enemyDamageObj = new HeroDamageObj(enemy, 0.8);
                int myHitLeft = myDamageObj.CalculateAttackTo(enemyDamageObj);
                string myHitText = myHitLeft < 0 ? "∞" : "" + myHitLeft;

                int enemyHitLeft = enemyDamageObj.CalculateAttackTo(myDamageObj);
                string enemyHitText = enemyHitLeft < 0 ? "∞" : "" + enemyHitLeft;
                
                //begin drawing
                var start = HUDInfo.GetHPbarPosition(enemy) - new Vector2(33, 10);
                var size = new Vector2(28, 20);
                Color backgroundColor = myDamageObj.TotalManaCost <= me.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);

                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(myHitText, start + new Vector2(5, 2), new Color(0, 219, 0, 255), FontFlags.Additive);

                start += new Vector2(0, 22);
                backgroundColor = enemyDamageObj.TotalManaCost <= enemy.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);
                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(enemyHitText, start + new Vector2(5, 2), new Color(219, 0, 0, 255), FontFlags.Additive);
                //Drawing.GetTexture("NyanUI/other/");
            }
        }


    }
}