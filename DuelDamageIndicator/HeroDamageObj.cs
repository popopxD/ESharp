using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
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
        public bool HasBrislebackSpell;
        public bool HasQuillSpraySpell;
        public bool HasFurySwipesSpell;


        public HeroDamageObj(Hero hero, double damageConfident)
        {
            HeroObj = hero;
            TotalDamageArray = new double[10];
            OutgoingDamageAmplifier = 1.0;
            IncommingDamageAmplifier = 1.0;
            AttackDamage = hero.MaximumDamage * damageConfident + hero.MinimumDamage * (1 - damageConfident) + hero.BonusDamage;
            TotalManaCost = 0;

            double spellDamage;
            int damageType;
            //calculate total brust damage
            foreach (Ability spell in hero.Spellbook.Spells.Concat(hero.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.Passive || spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > hero.Mana) continue;
                calculateDamage(spell, hero, out spellDamage, out damageType);
                TotalDamageArray[damageType] = TotalDamageArray[damageType] + spellDamage;

                TotalManaCost += (int)spell.ManaCost;
            }
        }

        public int CalculateAttackTo(HeroDamageObj enemy)
        {
            double myTotalDamage = TotalDamageArray[(int)DamageType.Pure] +
                                   TotalDamageArray[(int)DamageType.Physical] * (1.0 - enemy.HeroObj.DamageResist) +
                                   TotalDamageArray[(int)DamageType.Magical] * (1.0 - enemy.HeroObj.MagicDamageResist);
            double myActualAttackDamage = AttackDamage * (1.0 - enemy.HeroObj.DamageResist) * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;
            double enemyHealth = enemy.HeroObj.Health - myTotalDamage * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;
            if (myActualAttackDamage > 0)
            {
                return Math.Max((int)Math.Ceiling(enemyHealth / myActualAttackDamage), 0);
            }
            return -1;
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

            if (ability.DamageType == DamageType.Magical || ability.DamageType == DamageType.Physical ||
                ability.DamageType == DamageType.Pure)
            {
                damage_type = (int)ability.DamageType;
            }

            //get damage because spell.GetDamage is not working currently
            foreach (AbilityData data in ability.AbilityData)
            {
                //Log.Info(data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
                if (data.Name.ToLower().Contains("damage"))
                {
                    spell_damage = data.GetValue(ability.Level - 1);
                    //break;
                }
            }

        }
    }
}
