using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Extensions;
using Attribute = Ensage.Attribute;

namespace DuelDamageIndicator
{
    class HeroDamageObj
    {
        public static readonly string[] ItemMagicDamage = { "item_dagon", "item_shiva" };
        public static readonly string[] ItemPhysicalDamage = { "item_silver_edge", "item_invis_sword" };

        public Hero HeroObj;
        public int TotalManaCost;
        public double AttackDamage;
        public double AttackDamageAmplified;
        public double OutgoingDamageAmplifier;
        public double IncommingDamageAmplifier;
        public double[] TotalDamageArray;

        public bool HasScepter;

        public bool HasQuillSpraySpell;
        public double QuillSprayStackDamage;
        public int QuillSprayStack;

        public bool HasBrislebackSpell;
        public double BristlebackSideBlock;
        public double BristlebackBackBlock;
        public double BristlebackSideAngle;
        public double BristlebackBackAngle;

        public bool HasFurySwipesSpell;
        public double FurySwipesStackDamage;
        public double FurySwipesMultiplier;
        public int FurySwipesStack;

        public bool HasSunderSpell;
        public double SunderMinPercentage;

        public bool HasManaShield;
        public double ManaShieldReduction;
        public double ManaShieldDamageAbsorbed;

        public HeroDamageObj(Hero hero, double damageConfident)
        {
            HeroObj = hero;
            TotalDamageArray = new double[10];
            OutgoingDamageAmplifier = 1.0;
            IncommingDamageAmplifier = 1.0;
            AttackDamage = hero.MaximumDamage * damageConfident + hero.MinimumDamage * (1 - damageConfident) + hero.BonusDamage;
            TotalManaCost = 0;

            HasScepter = false;

            HasBrislebackSpell = false;
            BristlebackSideBlock = 0;
            BristlebackBackBlock = 0;
            BristlebackSideAngle = 0;
            BristlebackBackAngle = 0;

            HasFurySwipesSpell = false;
            FurySwipesStackDamage = 0;
            FurySwipesMultiplier = 1.0;
            FurySwipesStack = 0;

            HasQuillSpraySpell = false;
            QuillSprayStackDamage = 0;
            QuillSprayStack = 0;

            HasSunderSpell = false;
            SunderMinPercentage = 0;

            HasManaShield = false;
            ManaShieldReduction = 0;
            ManaShieldDamageAbsorbed = 0;

            double spellDamage;
            int damageType;

            //calculate total brust damage
            foreach (Ability spell in hero.Spellbook.Spells.Concat(hero.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > hero.Mana || spell.Level == 0) continue;
                CalculateDamage(spell, hero, out spellDamage, out damageType);
                TotalDamageArray[damageType] = TotalDamageArray[damageType] + spellDamage;

                TotalManaCost += (int) spell.ManaCost;
            }

            
            Modifier modifier = null;
            Ability data = null;
            //check for scepter
            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_ultimate_scepter" || x.Name == "modifier_item_ultimate_scepter_consumed");
            if (modifier != null)
            {
                HasScepter = true;
            }

            //calculate stack count based on modifier
            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_bristleback_quill_spray");
            if (modifier != null)
            {
                QuillSprayStack = modifier.StackCount;
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_ursa_fury_swipes_damage_increase");
            if (modifier != null)
            {
                FurySwipesStack = modifier.StackCount;
            }

            //calculate amplified base on modifier
            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_mask_of_madness_berserk");
            if (modifier != null)
            {
                IncommingDamageAmplifier *= 1.0 + SpellDamageLibrary.GetBerserkExtraDamage(hero) / 100;
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_bloodseeker_bloodrage");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "bloodseeker_bloodrage")) != null);
                if (caster != null)
                {
                    double spellAmplifier = data.AbilityData.First(z => z.Name == "damage_increase_pct").GetValue(data.Level - 1);
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier / 100;
                    OutgoingDamageAmplifier *= 1.0 + spellAmplifier / 100;
                }
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_chen_penitence");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "chen_penitence")) != null);
                if (caster != null)
                {
                    double spellAmplifier = data.AbilityData.First(z => z.Name == "bonus_damage_taken").GetValue(data.Level - 1);
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier / 100;
                }
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_shadow_demon_soul_catcher");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "shadow_demon_soul_catcher")) != null);
                if (caster != null)
                {
                    double spellAmplifier = data.AbilityData.First(z => z.Name == "bonus_damage_taken").GetValue(data.Level - 1);
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier / 100;
                }
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_wisp_overcharge");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "wisp_overcharge")) != null);
                if (caster != null)
                {
                    //double spellAmplifier = data.AbilityData.First(z => z.Name == "bonus_damage_pct").GetValue(data.Level - 1);
                    double spellAmplifier = SpellDamageLibrary.GetWispReduction(data.Level - 1);
                    IncommingDamageAmplifier *= 1.0 - spellAmplifier / 100;
                }
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_centaur_stampede");
            if (modifier != null)
            {
                //for centaur: the caster have to has scepter and in the same team
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => ((data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "centaur_stampede")) != null) && (x.Modifiers.FirstOrDefault(y => y.Name == "modifier_item_ultimate_scepter" || y.Name == "modifier_item_ultimate_scepter_consumed") != null) && (x.Team == hero.Team));
                if (caster != null)
                {
                    double spellAmplifier = data.AbilityData.First(z => z.Name == "damage_reduction").Value;
                    IncommingDamageAmplifier *= 1.0 - spellAmplifier / 100;
                }
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_silver_edge_debuff");
            if (modifier != null)
            {
                OutgoingDamageAmplifier *= 1.0 - SpellDamageLibrary.GetSilverEdgeDamageReduction() / 100;
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_ursa_enrage");
            if (modifier != null)
            {
                var spell = hero.Spellbook.Spells.First(x => x.Name == "ursa_enrage");
                FurySwipesMultiplier = spell.AbilityData.First(x => x.Name == "enrage_multiplier").GetValue(spell.Level - 1);
                IncommingDamageAmplifier *= 1.0 - spell.AbilityData.First(x => x.Name == "damage_reduction").Value / 100;
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_nyx_assassin_burrow");
            if (modifier != null)
            {
                var spell = hero.Spellbook.Spells.First(x => x.Name == "nyx_assassin_burrow");
                IncommingDamageAmplifier *= 1.0 - spell.AbilityData.First(x => x.Name == "damage_reduction").Value / 100;
            }

            modifier = hero.Modifiers.FirstOrDefault(x => x.Name == "modifier_medusa_mana_shield");
            if (modifier != null)
            {
                HasManaShield = true;
                var spell = hero.Spellbook.Spells.First(x => x.Name == "medusa_mana_shield");
                ManaShieldReduction = spell.AbilityData.First(x => x.Name == "absorption_tooltip").Value;
                ManaShieldDamageAbsorbed = hero.Mana * spell.AbilityData.First(x => x.Name == "damage_per_mana").GetValue(spell.Level - 1);
            }

            /*foreach (Modifier m in hero.Modifiers)
            {
                Log.Info(m.Name + " " + m.StackCount);
            }*/
        }

        public int CalculateAttackTo(HeroDamageObj enemy)
        {
            double myTotalDamage = TotalDamageArray[(int)DamageType.Pure] +
                                   TotalDamageArray[(int)DamageType.Physical] * (1.0 - enemy.HeroObj.DamageResist) +
                                   TotalDamageArray[(int)DamageType.Magical] * (1.0 - enemy.HeroObj.MagicDamageResist);
            double temporallyDamageAmplifier = 1.0 * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;
            if (HasQuillSpraySpell)
            {
                myTotalDamage += enemy.QuillSprayStack * QuillSprayStackDamage * (1.0 - enemy.HeroObj.DamageResist);
            }

            double enemyShieldLeft = 0;
            if (enemy.HasManaShield)
            {
                myTotalDamage = Math.Max(myTotalDamage - enemy.ManaShieldDamageAbsorbed, 0) + Math.Min(myTotalDamage, enemy.ManaShieldDamageAbsorbed) * (1.0 - enemy.ManaShieldReduction / 100);
                enemyShieldLeft = Math.Max(enemy.ManaShieldDamageAbsorbed - myTotalDamage, 0);
            }

            if (enemy.HasBrislebackSpell)
            {
                double angle2Hero = HeroObj.FindAngleBetween(enemy.HeroObj.Position) + 180.0; //Face: enemy.HeroObj.FindAngleBetween(HeroObj.Position)
                double angleBristleFacing = enemy.HeroObj.Angles.Y + 180.0;
                double angleDifferent = Math.Abs(angle2Hero - angleBristleFacing);
                if (angleDifferent <= enemy.BristlebackBackAngle)
                {
                    temporallyDamageAmplifier *= 1 - enemy.BristlebackBackBlock / 100;
                }
                else if (angleDifferent < enemy.BristlebackSideAngle)
                {
                    temporallyDamageAmplifier *= 1 - enemy.BristlebackSideBlock / 100;
                }
            }

            myTotalDamage *= temporallyDamageAmplifier;
            double myActualAttackDamage = AttackDamage * (1.0 - enemy.HeroObj.DamageResist) * temporallyDamageAmplifier;
            double enemyHealth = enemy.HeroObj.Health;
            if (HasSunderSpell)
            {
                enemyHealth = Math.Min(enemyHealth,  //not exchange
                    Math.Max(((double) HeroObj.Health / HeroObj.MaximumHealth) * enemy.HeroObj.MaximumHealth, SunderMinPercentage * enemy.HeroObj.MaximumHealth));  //if exchange, check between our health and min health
            }
            enemyHealth = enemyHealth - myTotalDamage;

            int shieldHit = 0;
            int rawHit = CalculateHit(enemyHealth, myActualAttackDamage, temporallyDamageAmplifier, enemy);

            //if no hit left or unable to hit, return immediately
            if (rawHit <= 0) return rawHit;

            //otherwise, compute the shield hit
            if (enemyShieldLeft > 0)
            {
                shieldHit = CalculateHit(enemyShieldLeft, myActualAttackDamage, temporallyDamageAmplifier, enemy);
            }
            double shieldCompensationRate = 1.0 / (1 - (ManaShieldReduction / 100));  //compensateRate = 1 / (1 - x);
            rawHit = (int) Math.Ceiling(Math.Max(rawHit - shieldHit, 0) + Math.Min(rawHit, shieldHit) * shieldCompensationRate);  //calculate the real hit. And any hit under shield should be multiplied by compensateRate
            return rawHit;
        }

        private void CalculateDamage(Ability ability, Hero fromHero, out double spell_damage, out int damage_type)
        {
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
                    damage_type = (int)DamageType.Magical;
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
                            damage_type = (int)DamageType.Magical;
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

            if (ability.Name == "terrorblade_sunder")
            {
                HasSunderSpell = true;
                SunderMinPercentage = ability.AbilityData.SingleOrDefault().GetValue(ability.Level - 1) / 100;
                return;
            }
            if (ability.Name == "bristleback_quill_spray")
            {
                HasQuillSpraySpell = true;
                QuillSprayStackDamage = ability.AbilityData.First(x => x.Name == "quill_stack_damage").GetValue(ability.Level - 1);
                spell_damage = ability.AbilityData.First(x => x.Name == "quill_base_damage").GetValue(ability.Level - 1);
                damage_type = (int) ability.DamageType;
                return;
            }
            if (ability.Name == "ursa_fury_swipes")
            {
                HasFurySwipesSpell = true;
                FurySwipesStackDamage = ability.AbilityData.First(x => x.Name == "damage_per_stack").GetValue(ability.Level - 1);
                damage_type = (int)ability.DamageType;
                return;
            }
            if (ability.Name == "bristleback_bristleback")
            {
                HasBrislebackSpell = true;
                BristlebackSideBlock = ability.AbilityData.First(x => x.Name == "side_damage_reduction").GetValue(ability.Level - 1);
                BristlebackBackBlock = ability.AbilityData.First(x => x.Name == "back_damage_reduction").GetValue(ability.Level - 1);
                BristlebackSideAngle = ability.AbilityData.First(x => x.Name == "side_angle").Value;
                BristlebackBackAngle = ability.AbilityData.First(x => x.Name == "back_angle").Value;
                return;
            }
            if (ability.Name == "spectre_dispersion")
            {
                double spellAmplifier = ability.AbilityData.First(x => x.Name == "damage_reflection_pct").GetValue(ability.Level - 1);
                IncommingDamageAmplifier *= 1.0 - spellAmplifier / 100;
                return;
            }

            if (ability.AbilityBehavior == AbilityBehavior.Passive) return;
            if (ability.DamageType == DamageType.Magical || ability.DamageType == DamageType.Physical ||
                ability.DamageType == DamageType.Pure)
            {
                damage_type = (int)ability.DamageType;
            }
            
            //get damage because spell.GetDamage is not working currently
            foreach (AbilityData data in ability.AbilityData)
            {
                //TODO: AbilityDamage
                //TODO: Damage over time
                //TODO: Scepter
                //Log.Info(data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
                if (data.Name.ToLower().Contains("damage"))
                {
                    //Log.Info("Accepted: " + data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
                    spell_damage = data.GetValue(ability.Level - 1);
                    break;
                }
            }

        }

        private int CalculateHit(double rawHealth, double rawDamage, double damageAmplifier, HeroDamageObj enemy)
        {
            int result = -1;
            int compensate = 0;
            //HasFurySwipesSpell need to be calculated differently: formular = (-attackDamage - sqrt(delta)) / furySwipesDamage, delta = attackDamage^2 + 4 *  0.5 * furySwipesDamage * enemyHealth
            //known as enemyHealth = 0.5furySwipesDamage * hit^2 + damage * hit
            if (HasFurySwipesSpell)
            {
                double furySwipesStackDamageAfterAmplifier = FurySwipesStackDamage * (1.0 - enemy.HeroObj.DamageResist) * damageAmplifier * FurySwipesMultiplier;
                rawDamage += enemy.FurySwipesStack * furySwipesStackDamageAfterAmplifier;
                if (rawDamage < 0)
                {
                    compensate = (int) Math.Ceiling((-rawDamage) / furySwipesStackDamageAfterAmplifier);
                    rawDamage = 0;
                }
                result = Math.Max((int)Math.Ceiling((Math.Sqrt(rawDamage * rawDamage + 2.0 * furySwipesStackDamageAfterAmplifier * rawHealth) - rawDamage) / furySwipesStackDamageAfterAmplifier), 0) + compensate;
            }
            else if (rawDamage > 0)
            {
                result = Math.Max((int)Math.Ceiling(rawHealth / rawDamage), 0);
            }
            return result;
        }
    }

    class SpellDamageLibrary
    {
        private static double _berserkExtraDamage = 30;
        private static bool _berserkExtraDamageCalculated = false;
        public static double GetBerserkExtraDamage(Hero hero)
        {
            if (_berserkExtraDamageCalculated || hero == null) return _berserkExtraDamage;

            var spell = hero.Inventory.Items.First(x => x.Name == "item_mask_of_madness");
            if (spell == null) return _berserkExtraDamage;

            _berserkExtraDamage = spell.AbilityData.First(x => x.Name == "berserk_extra_damage").GetValue(spell.Level - 1);
            _berserkExtraDamageCalculated = true;
            return _berserkExtraDamage;
        }

        private static double _silverEdgeReduction = 40;
        private static bool _silverEdgeReductionCalculated = true; //backstab reduction is broken
        public static double GetSilverEdgeDamageReduction()
        {
            if (_silverEdgeReductionCalculated) return _silverEdgeReduction;

            Item item = null;
            var caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (item = x.Inventory.Items.FirstOrDefault(y => y.Name == "item_silver_edge")) != null);
            if (caster == null) return _silverEdgeReduction;
            _silverEdgeReduction = item.AbilityData.First(z => z.Name == "backstab_reduction").GetValue(item.Level - 1);

            _silverEdgeReductionCalculated = true;
            return _silverEdgeReduction;
        }

        private static double[] _wispReduction = {5, 10, 15, 20};
        public static double GetWispReduction(uint level)
        {
            return _wispReduction[level];
        }
    }
}
