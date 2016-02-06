using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using SharpDX;
using SharpDX.Direct3D9;

namespace AutoDeward {
    internal class Deward {
        private static Item quellingBlade;
        private static Item tango;

        private static int sleepTime;


        public static void Init() {
            Game.OnUpdate += GameOnUpdate;
        }

        private static void GameOnUpdate(EventArgs eventArgs) {
            var me = ObjectMgr.LocalHero;

            if (!Game.IsInGame || me == null) return;
            if (sleepTime > 0) {
                sleepTime--;
                return;
            }

            quellingBlade =
                me.Inventory.Items.FirstOrDefault(
                    i => i.ClassID == ClassID.CDOTA_Item_QuellingBlade || i.ClassID == ClassID.CDOTA_Item_Battlefury);
            tango =
                me.Inventory.Items.FirstOrDefault(
                    i => i.ClassID == ClassID.CDOTA_Item_Tango || i.ClassID == ClassID.CDOTA_Item_Tango_Single);

            var units = ObjectMgr.GetEntities<Unit>();

            var wards = units
                .Where(
                    u => (u.ClassID == ClassID.CDOTA_NPC_Observer_Ward || u.ClassID == ClassID.CDOTA_NPC_Observer_Ward_TrueSight) && u.Team != me.Team && u.IsAlive && Vector3.Distance(me.Position, u.Position) < 475).ToList();

            var mines = units.Where(u => u.ClassID == ClassID.CDOTA_NPC_TechiesMines && u.Team != me.Team && u.IsAlive && Vector3.Distance(me.NetworkPosition, u.NetworkPosition) < 475).ToList();

            var canDewardWard = ((quellingBlade != null || tango != null) && wards.Count > 0);
            var canDewardMine = ((quellingBlade != null) && mines.Count > 0);

            if (canDewardWard && me.IsAlive) {
                Item dewardItem = quellingBlade;

                // is using a tango worth it?
                // tango heals 230 if used on ward, 115 if used on tree
                if (tango != null && quellingBlade != null && me.Modifiers.All(m => m.Name != "modifier_tango_heal")) {
                    var hpMissing = me.MaximumHealth - me.Health;
                    if (hpMissing > 115) dewardItem = tango;
                }
                else if (quellingBlade == null) dewardItem = tango;


                if (dewardItem.Cooldown == 0) {
                    dewardItem.UseAbility(wards[0]);
                    sleepTime = 10;
                }
            }

            if (canDewardMine) {
                if (quellingBlade.Cooldown == 0) {
                    quellingBlade.UseAbility(mines[0]);
                    sleepTime = 10;
                }
            }
        }
    }
}