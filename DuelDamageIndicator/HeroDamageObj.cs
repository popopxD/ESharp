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
        public int FurySwipesStack;

        public bool HasSunderSpell;
        public double SunderMinPercentage;

        public HeroDamageObj(Hero hero, double damageConfident)
        {
            HeroObj = hero;
            TotalDamageArray = new double[10];
            OutgoingDamageAmplifier = 1.0;
            IncommingDamageAmplifier = 1.0;
            AttackDamage = hero.MaximumDamage * damageConfident + hero.MinimumDamage * (1 - damageConfident) + hero.BonusDamage;
            TotalManaCost = 0;

            HasBrislebackSpell = false;
            BristlebackSideBlock = 0;
            BristlebackBackBlock = 0;
            BristlebackSideAngle = 0;
            BristlebackBackAngle = 0;

            HasFurySwipesSpell = false;
            FurySwipesStackDamage = 0;
            FurySwipesStack = 0;

            HasQuillSpraySpell = false;
            QuillSprayStackDamage = 0;
            QuillSprayStack = 0;

            HasSunderSpell = false;
            SunderMinPercentage = 0;

            double spellDamage;
            int damageType;

            //calculate total brust damage
            foreach (Ability spell in hero.Spellbook.Spells.Concat(hero.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > hero.Mana) continue;
                CalculateDamage(spell, hero, out spellDamage, out damageType);
                TotalDamageArray[damageType] = TotalDamageArray[damageType] + spellDamage;

                TotalManaCost += (int) spell.ManaCost;
            }

            //calculate amplifier based on modifier
            /*foreach (Modifier modifier in hero.Modifiers)
            {
            
            //TODO: calculate amplification (magic, bloodrage) bonus
            //TODO: calculate reduction of under effect
                if (modifier.Name == "modifier_bristleback_quill_spray")
                {
                    QuillSprayStack = modifier.StackCount;
                }
                else if (modifier.Name == "modifier_ursa_fury_swipes_damage_increase")
                {
                    FurySwipesStack = modifier.StackCount;
                }
                //"modifier_bloodseeker_bloodrage", "modifier_item_mask_of_madness_berserk", 
                //"modifier_ursa_enrage", "modifier_item_silver_edge_windwalk", "modifier_nyx_assassin_burrow", 

                Log.Info(modifier.Name + " " + modifier.StackCount);
                if (modifier.Caster != null)
                {
                    Log.Info(modifier.Caster.Name);
                }
                if (modifier.Owner != null)
                {
                    Log.Info(modifier.Owner.Name);
                }
                if (modifier.Ability != null)
                {
                    Log.Info(modifier.Ability.Name);
                }
                //Chen penitence, Shadow demon soul catcher,
                //spectre dispersion, medusa shield if mana ?, IO overcharge, stampede, bristleback
            }*/

        }

        public int CalculateAttackTo(HeroDamageObj enemy)
        {
            double myTotalDamage = TotalDamageArray[(int)DamageType.Pure] +
                                   TotalDamageArray[(int)DamageType.Physical] * (1.0 - enemy.HeroObj.DamageResist) +
                                   TotalDamageArray[(int)DamageType.Magical] * (1.0 - enemy.HeroObj.MagicDamageResist);
            double myActualAttackDamage = AttackDamage * (1.0 - enemy.HeroObj.DamageResist) * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;
            double enemyHealth = enemy.HeroObj.Health;

            if (HasFurySwipesSpell)
            {
                //~~~~
            }

            if (HasQuillSpraySpell)
            {
                //~~~~
            }

            if (enemy.HasBrislebackSpell)
            {
                //~~~~calculate damage amplifier
            }

            if (HasSunderSpell)
            {
                enemyHealth = Math.Min(enemyHealth,  //not exchange
                    Math.Max(((double) HeroObj.Health / HeroObj.MaximumHealth) * enemy.HeroObj.MaximumHealth, SunderMinPercentage * enemy.HeroObj.MaximumHealth));  //if exchange, check between our health and min health
            }
            enemyHealth = enemyHealth - myTotalDamage * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;
            if (myActualAttackDamage > 0)
            {
                return Math.Max((int)Math.Ceiling(enemyHealth / myActualAttackDamage), 0);
            }
            return -1;
        }

        private void CalculateDamage(Ability ability, Hero fromHero, out double spell_damage, out int damage_type)
        {
            Log.Info("Data----" + ability.Name);
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
                BristlebackSideAngle = ability.AbilityData.First(x => x.Name == "side_angle").GetValue(ability.Level - 1);
                BristlebackBackAngle = ability.AbilityData.First(x => x.Name == "back_angle").GetValue(ability.Level - 1);
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
                //base damage and damage overtime

                Log.Info(data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
                if (data.Name.ToLower().Contains("damage"))
                {
                    spell_damage = data.GetValue(ability.Level - 1);
                    //break;
                }
            }

        }
    }

    class SpellDamageLibrary
    {
        private static double _sunderMinHealth = -1;
        public static double SunderMinHealth
        {
            get
            {
                if (_sunderMinHealth < 0)
                {
                    //ObjectMgr.GetEntities<Hero>()
                }
                return _sunderMinHealth;
            }

            set { _sunderMinHealth = value; }
        }


    }
}
