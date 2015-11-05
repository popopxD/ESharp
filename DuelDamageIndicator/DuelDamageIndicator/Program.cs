using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common;
using SharpDX;

namespace DuelDamageIndicator
{
    class Program
    {
        public static Hero Me;
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

            Me = ObjectMgr.LocalHero;
            if (Me == null) return;

            double[] damageArray = new double[10];

            double spellDamage;
            int damageType;
            int totalDamage = 0;
            int myTotalManacost = 0;
            double myAttackDamage = Me.MaximumDamage * 0.2 + Me.MinimumDamage * 0.8;
            double myHealth = Me.Health;

            //calculate total brust damage
            foreach (Ability spell in Me.Spellbook.Spells.Concat(Me.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.Passive || spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > Me.Mana) continue;
                calculateDamage(spell, out spellDamage, out damageType);
                damageArray[damageType] = damageArray[damageType] + spellDamage;

                myTotalManacost += (int)spell.ManaCost;
            }

            var enemies = ObjectMgr.GetEntities<Hero>()
                        .Where(x => x.Team != Me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion)
                        .ToList();

            foreach (var enemy in enemies)
            {
                if (enemy == null) return;
                //calculate damage from me to the enemy
                double enemyHealth = enemy.Health;
                double[] enemyDamageArray = new double[10];
                int enemyTotalManacost = 0;

                enemyHealth = enemyHealth - damageArray[(int)DamageType.Pure] - damageArray[(int)DamageType.Physical] * (1.0 - enemy.DamageResist) - damageArray[(int)DamageType.Magical] * (1.0 - enemy.MagicDamageResist);
                int myHitLeft = Math.Max((int) Math.Ceiling(enemyHealth / (myAttackDamage * (1.0 - enemy.DamageResist))), 0);
                string myHitText = "" + myHitLeft;

                //calculate damage from the enemy to me
                foreach (Ability spell in enemy.Spellbook.Spells.Concat(enemy.Inventory.Items))
                {
                    if (spell.AbilityBehavior == AbilityBehavior.Passive || spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > Me.Mana) continue;
                    calculateDamage(spell, out spellDamage, out damageType);
                    enemyDamageArray[damageType] = enemyDamageArray[damageType] + spellDamage;

                    enemyTotalManacost += (int) spell.ManaCost;
                }
                double enemyAttackDamage = enemy.MaximumDamage * 0.8 + enemy.MinimumDamage * 0.2; //this is different to my_attack_damage because enemy can be more luck than us. 80% confident anyway
                double myHealthLeft = myHealth - enemyDamageArray[(int) DamageType.Pure] - enemyDamageArray[(int)DamageType.Physical] * (1.0 - Me.DamageResist) - enemyDamageArray[(int)DamageType.Magical] * (1.0 - Me.MagicDamageResist);
                int enemyHitLeft = Math.Max((int) Math.Ceiling(myHealthLeft / (enemyAttackDamage * (1.0 - Me.DamageResist))), 0);
                string enemyHitText = "" + enemyHitLeft;

                //begin drawing
                var start = HUDInfo.GetHPbarPosition(enemy) - new Vector2(33, 10);
                var size = new Vector2(28, 20);
                Color backgroundColor = myTotalManacost <= Me.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);

                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(myHitText, start + new Vector2(5, 2), new Color(0, 219, 0, 255), FontFlags.Additive);

                start += new Vector2(0, 22);
                backgroundColor = enemyTotalManacost <= enemy.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);
                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(enemyHitText, start + new Vector2(5, 2), new Color(219, 0, 0, 255), FontFlags.Additive);
            }
        }

        static void calculateDamage(Ability ability, out double spell_damage, out int damage_type)
        {
            //Log.Info("Data----");
            spell_damage = 0;
            damage_type = (int) DamageType.None;

            if (ability is Item)
            {
                //Log.Info("This is an item, return");
                damage_type = (int) DamageType.None;
                return;
            }

            if (ability.DamageType == DamageType.Magical || ability.DamageType == DamageType.Physical ||
                ability.DamageType == DamageType.Pure)
            {
                damage_type = (int) ability.DamageType;
            }

            //get damage because spell.GetDamage is not working currently
            foreach (AbilityData data in ability.AbilityData)
            {
                //~~~test scepter manacost and damage
                //Log.Info(data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
                if (data.Name.ToLower().IndexOf("damage") != -1)
                {
                    spell_damage = data.GetValue(ability.Level - 1);
                    //break;
                }
            }

        }
    }
}