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
        public static List<DrawingData> Cache;
        static void Main(string[] args)
        {
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
        }

        public static void Game_OnWndProc(WndEventArgs args)
        {
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame) return;
            if (!Utils.SleepCheck("DDI_GameUpdateSleeper")) return;
            var me = ObjectMgr.LocalHero;
            if (me == null) return;

            Log.WriteSlowDebug = false;
            if (Utils.SleepCheck("DDI_GameDebugWriterSleeper"))
            {
                Log.WriteSlowDebug = true;
                Log.SlowDebug("=======================================");
                Log.SlowDebug("DuelDamageIndicator Debug information");
            }

            Cache = new List<DrawingData>();
            HeroDamageObj myDamageObj = new HeroDamageObj(me, 0.2);

            var enemies = ObjectMgr.GetEntities<Hero>().Where(x => x.Team != me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion).ToList();
            foreach (var enemy in enemies)
            {
                HeroDamageObj enemyDamageObj = new HeroDamageObj(enemy, 0.8);
                int myHitLeft = myDamageObj.CalculateAttackTo(enemyDamageObj);
                string myHitText = myHitLeft < 0 ? "∞" : "" + myHitLeft;

                int enemyHitLeft = enemyDamageObj.CalculateAttackTo(myDamageObj);
                string enemyHitText = enemyHitLeft < 0 ? "∞" : "" + enemyHitLeft;

                //add to cache
                Cache.Add(new DrawingData(enemy, myDamageObj.TotalManaCost <= me.Mana, myHitText, enemyDamageObj.TotalManaCost <= enemy.Mana, enemyHitText));
            }

            Utils.Sleep(50, "DDI_GameUpdateSleeper");
            if (Log.WriteSlowDebug)
            {
                Log.SlowDebug("END. If you found any miscalculated spell, please post it on the forum topic");
                Log.SlowDebug("=======================================");
                Log.WriteSlowDebug = false;
                Utils.Sleep(300000, "DDI_GameDebugWriterSleeper");
            }
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame) return;
            var me = ObjectMgr.LocalHero;
            if (me == null) return;
            if (Cache == null) return;

            foreach (DrawingData cacheEnemy in Cache)
            {
                Hero enemy = cacheEnemy.h;
                if (!enemy.IsAlive || !enemy.IsVisible) continue;

                //begin drawing
                var start = HUDInfo.GetHPbarPosition(enemy) - new Vector2(33, 10);
                var size = new Vector2(28, 20);
                Color backgroundColor = cacheEnemy.IsEnoughMana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);

                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(cacheEnemy.NumHitString, start + new Vector2(5, 2), new Color(0, 219, 0, 255), FontFlags.None);

                start += new Vector2(0, 22);
                backgroundColor = cacheEnemy.IsEnoughManaEnemy ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);
                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(cacheEnemy.NumHitStringEnemy, start + new Vector2(5, 2), new Color(219, 0, 0, 255), FontFlags.None);
            }
        }


    }
}