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
        public static readonly string[] ItemMagicDamage = { "item_dagon", "item_shiva"};
        public static readonly string[] ItemPhysicalDamage = { "item_silver_edge", "item_invis_sword" };
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

            double[] damageArray = new double[10];

            double spellDamage;
            int damageType;
            int totalDamage = 0;
            int myTotalManacost = 0;
            double myAttackDamage = me.MaximumDamage * 0.2 + me.MinimumDamage * 0.8 + me.BonusDamage;
            double myHealth = me.Health;
            double myOutgoingAmplification = 1.0;
            double myIncommingAmplification = 1.0;

            //calculate total brust damage
            foreach (Ability spell in me.Spellbook.Spells.Concat(me.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.Passive || spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > me.Mana) continue;
                calculateDamage(spell, me, out spellDamage, out damageType);
                damageArray[damageType] = damageArray[damageType] + spellDamage;

                myTotalManacost += (int)spell.ManaCost;
            }

            var enemies = ObjectMgr.GetEntities<Hero>().Where(x => x.Team != me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion).ToList();
            foreach (var enemy in enemies)
            {
                //calculate damage from me to the enemy
                double enemyHealth = enemy.Health;
                double[] enemyDamageArray = new double[10];
                int enemyTotalManacost = 0;
                double myTotalDamage = damageArray[(int) DamageType.Pure] +
                                       damageArray[(int) DamageType.Physical] * (1.0 - enemy.DamageResist) +
                                       damageArray[(int) DamageType.Magical] * (1.0 - enemy.MagicDamageResist);
                double enemyOutgoingAmplification = 1.0;
                double enemyIncommingAmplification = 1.0;
                double myActualAttackDamage = myAttackDamage * (1.0 - enemy.DamageResist) * myOutgoingAmplification* enemyIncommingAmplification;

                enemyHealth = enemyHealth - myTotalDamage * myOutgoingAmplification * enemyIncommingAmplification;
                string myHitText;
                if (myActualAttackDamage > 0)
                {
                    int myHitLeft = Math.Max((int) Math.Ceiling(enemyHealth / myActualAttackDamage), 0);
                    myHitText = "" + myHitLeft;
                }
                else
                {
                    myHitText = "∞";
                }

                //calculate damage from the enemy to me
                foreach (Ability spell in enemy.Spellbook.Spells.Concat(enemy.Inventory.Items))
                {
                    if (spell.AbilityBehavior == AbilityBehavior.Passive || spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > me.Mana) continue;
                    calculateDamage(spell, enemy, out spellDamage, out damageType);
                    enemyDamageArray[damageType] = enemyDamageArray[damageType] + spellDamage;

                    enemyTotalManacost += (int) spell.ManaCost;
                }
                double enemyAttackDamage = enemy.MaximumDamage * 0.8 + enemy.MinimumDamage * 0.2 + enemy.BonusDamage; //this is different to my_attack_damage because enemy can be more luck than us. 80% confident anyway
                double enemyActualAttackDamage = enemyAttackDamage * enemyOutgoingAmplification * myIncommingAmplification;
                double enemyTotalDamage = enemyDamageArray[(int) DamageType.Pure] +
                                          enemyDamageArray[(int) DamageType.Physical] * (1.0 - me.DamageResist) +
                                          enemyDamageArray[(int) DamageType.Magical] * (1.0 - me.MagicDamageResist);

                double myHealthLeft = myHealth - enemyTotalDamage * enemyOutgoingAmplification * myIncommingAmplification;
                string enemyHitText;
                if (enemyActualAttackDamage > 0)
                {
                    int enemyHitLeft = Math.Max((int)Math.Ceiling(myHealthLeft / enemyActualAttackDamage), 0);
                    enemyHitText = "" + enemyHitLeft;
                }
                else
                {
                    enemyHitText = "∞";
                }
                

                //begin drawing
                var start = HUDInfo.GetHPbarPosition(enemy) - new Vector2(33, 10);
                var size = new Vector2(28, 20);
                Color backgroundColor = myTotalManacost <= me.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);

                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(myHitText, start + new Vector2(5, 2), new Color(0, 219, 0, 255), FontFlags.Additive);

                start += new Vector2(0, 22);
                backgroundColor = enemyTotalManacost <= enemy.Mana ? new Color(0, 0, 0, 128) : new Color(20, 20, 219, 128);
                Drawing.DrawRect(start, size, backgroundColor);
                Drawing.DrawText(enemyHitText, start + new Vector2(5, 2), new Color(219, 0, 0, 255), FontFlags.Additive);
            }
        }

        static void calculateDamage(Ability ability, Hero fromHero, out double spell_damage, out int damage_type)
        {
            //TODO: calculate amplification (magic, bloodrage) bonus
            //TODO: recalculate spell like Ursa passive, bristleback Quill Spray
            //TODO: calculate reduction of under effect
            //TODO: calculate HP removal (TB Sunder, Ice Blast)
            //TODO: check actual spell damage
            //Log.Info("Data----" + ability.Name);
            spell_damage = 0;
            int damage_none = (int)DamageType.None;
            damage_type = damage_none;
            int i;

            if (ability is Item)
            {
                //process ethereal blade, special treatment to ethereal blade and return
                if (ability.Name.Contains("item_ethereal_blade"))
                {
                    damage_type = (int) DamageType.Magical;
                    try
                    {
                        double attribute = fromHero.PrimaryAttribute == Attribute.Strength ? fromHero.TotalStrength
                                         : fromHero.PrimaryAttribute == Attribute.Agility ? fromHero.TotalAgility
                                         : fromHero.TotalIntelligence;
                        spell_damage = attribute * ability.AbilityData.FirstOrDefault(x => x.Name == "blast_agility_multiplier").GetValue(ability.Level - 1)
                                     + ability.AbilityData.FirstOrDefault(x => x.Name == "blast_damage_base").GetValue(ability.Level - 1);
                    }
                    catch (NullReferenceException)
                    {
                        spell_damage = 0;
                    }
                    return;
                }

                //process magical item
                if (damage_type == damage_none)
                {
                    for (i = 0; i < ItemMagicDamage.Length; ++i)
                    {
                        if (ability.Name.Contains(ItemMagicDamage[i]))
                        {
                            damage_type = (int) DamageType.Magical;
                            break;
                        }
                    }
                }

                //process physical item
                if (damage_type == damage_none)
                {
                    for (i = 0; i < ItemMagicDamage.Length; ++i)
                    {
                        if (ability.Name.Contains(ItemPhysicalDamage[i]))
                        {
                            damage_type = (int)DamageType.Physical;
                            break;
                        }
                    }
                }

                //stop calculation if item is not in whitelist
                if (damage_type == damage_none) return;

                AbilityData data = ability.AbilityData.FirstOrDefault(x => x.Name != "bonus_damage" && x.Name.ToLower().Contains("damage"));
                if (data != null)
                {
                    spell_damage += data.GetValue(ability.Level - 1);
                }
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