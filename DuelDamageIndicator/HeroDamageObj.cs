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
        public int TotalManaCost = 0;
        public double AttackDamage;
        public double OutgoingDamageAmplifier = 1.0;
        public double IncommingDamageAmplifier = 1.0;
        public double[] TotalDamageArray;

        public bool HasScepter = false;

        public bool HasQuillSpraySpell = false;
        public double QuillSprayStackDamage = 0;
        public int QuillSprayStack = 0;

        public bool HasBrislebackSpell = false;
        public double BristlebackSideBlock = 0;
        public double BristlebackBackBlock = 0;
        public double BristlebackSideAngle = 0;
        public double BristlebackBackAngle = 0;

        public bool HasFurySwipesSpell = false;
        public double FurySwipesStackDamage = 0;
        public double FurySwipesMultiplier = 1.0;
        public int FurySwipesStack = 0;

        public bool HasSunderSpell = false;
        public double SunderMinPercentage = 0;

        public bool HasManaShield = false;
        public double ManaShieldReduction = 0;
        public double ManaShieldDamageAbsorbed = 0;

        public bool HasManaVoid = false;
        public double ManaVoidMultiplier = 0;

        public bool HasLvlDeath = false;
        public double LvlDeathAdditionalDamage = 0;
        public int LvlBonusHeroMultiple = 1;

        public bool HasNecrolyteReapersScythe = false;
        public double NecrolyteReapersDamageMultipler = 0;

        public double SoulAssumption = 0;

        public HeroDamageObj(Hero hero, double damageConfident)
        {
            HeroObj = hero;
            TotalDamageArray = new double[10];
            AttackDamage = hero.MaximumDamage * damageConfident + hero.MinimumDamage * (1 - damageConfident) + hero.BonusDamage;
            
            double spellDamage;
            int damageType;
            
            CalculateCustomModifier();

            //calculate total brust damage
            foreach (Ability spell in hero.Spellbook.Spells.Concat(hero.Inventory.Items))
            {
                if (spell.AbilityBehavior == AbilityBehavior.None || spell.Cooldown > 0.01 || spell.ManaCost > hero.Mana || spell.Level == 0) continue;
                CalculateDamage(spell, out spellDamage, out damageType);
                Log.SlowDebug("Hero: " + hero.Name + " - Spell: " + spell.Name + " - Damage: " + spellDamage + " - Type: " + (DamageType) damageType);
                TotalDamageArray[damageType] = TotalDamageArray[damageType] + spellDamage;
                if (spellDamage > 0)
                {
                    TotalManaCost += (int) spell.ManaCost;
                }
            }
        }

        public void CalculateCustomModifier()
        {
            Modifier modifier = null;
            Ability data = null;

            /*foreach (Modifier m in HeroObj.Modifiers)
            {
                Log.SlowDebug(m.Name + " " + m.StackCount);
            }*/

            //check for scepter
            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_ultimate_scepter" || x.Name == "modifier_item_ultimate_scepter_consumed");
            if (modifier != null)
            {
                HasScepter = true;
            }

            //calculate stack count based on modifier
            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_bristleback_quill_spray");
            if (modifier != null)
            {
                QuillSprayStack = modifier.StackCount;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_ursa_fury_swipes_damage_increase");
            if (modifier != null)
            {
                FurySwipesStack = modifier.StackCount;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_visage_soul_assumption");
            if (modifier != null)
            {
                SoulAssumption = modifier.StackCount;
            }
            
            //calculate amplified base on modifier
            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_mask_of_madness_berserk");
            if (modifier != null)
            {
                IncommingDamageAmplifier *= 1.0 + SpellDamageLibrary.GetBerserkExtraDamage(HeroObj) / 100;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_bloodseeker_bloodrage");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "bloodseeker_bloodrage")) != null);
                if (caster != null)
                {
                    double spellAmplifier = SpellDamageLibrary.GetAbilityValue(data, "damage_increase_pct") / 100;
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier;
                    OutgoingDamageAmplifier *= 1.0 + spellAmplifier;
                }
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_chen_penitence");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "chen_penitence")) != null);
                if (caster != null)
                {
                    double spellAmplifier = SpellDamageLibrary.GetAbilityValue(data, "bonus_damage_taken") / 100;
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier;
                }
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_shadow_demon_soul_catcher");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "shadow_demon_soul_catcher")) != null);
                if (caster != null)
                {
                    double spellAmplifier = SpellDamageLibrary.GetAbilityValue(data, "bonus_damage_taken") / 100;
                    IncommingDamageAmplifier *= 1.0 + spellAmplifier;
                }
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_wisp_overcharge");
            if (modifier != null)
            {
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "wisp_overcharge")) != null);
                if (caster != null)
                {
                    //double spellAmplifier = SpellDamageLibrary.GetAbilityValue(data, "bonus_damage_pct");
                    double spellAmplifier = SpellDamageLibrary.GetWispReduction(data.Level - 1) / 100;
                    IncommingDamageAmplifier *= 1.0 - spellAmplifier;
                }
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_centaur_stampede");
            if (modifier != null)
            {
                //for centaur: the caster have to has scepter and in the same team
                Hero caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => ((data = x.Spellbook.Spells.FirstOrDefault(y => y.Name == "centaur_stampede")) != null) && (x.Modifiers.FirstOrDefault(y => y.Name == "modifier_item_ultimate_scepter" || y.Name == "modifier_item_ultimate_scepter_consumed") != null) && (x.Team == HeroObj.Team));
                if (caster != null)
                {
                    double spellAmplifier = SpellDamageLibrary.GetAbilityValue(data, "damage_reduction") / 100;
                    IncommingDamageAmplifier *= 1.0 - spellAmplifier;
                }
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_silver_edge_debuff");
            if (modifier != null)
            {
                OutgoingDamageAmplifier *= 1.0 - SpellDamageLibrary.GetSilverEdgeDamageReduction() / 100;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_ursa_enrage");
            if (modifier != null)
            {
                data = HeroObj.Spellbook.Spells.First(x => x.Name == "ursa_enrage");
                FurySwipesMultiplier = SpellDamageLibrary.GetAbilityValue(data, "enrage_multiplier");
                IncommingDamageAmplifier *= 1.0 - SpellDamageLibrary.GetAbilityValue(data, "damage_reduction") / 100;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_nyx_assassin_burrow");
            if (modifier != null)
            {
                data = HeroObj.Spellbook.Spells.First(x => x.Name == "nyx_assassin_burrow");
                IncommingDamageAmplifier *= 1.0 - SpellDamageLibrary.GetAbilityValue(data, "damage_reduction") / 100;
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_medusa_mana_shield");
            if (modifier != null)
            {
                HasManaShield = true;
                data = HeroObj.Spellbook.Spells.First(x => x.Name == "medusa_mana_shield");
                ManaShieldReduction = SpellDamageLibrary.GetAbilityValue(data, "absorption_tooltip");
                ManaShieldDamageAbsorbed = HeroObj.Mana * SpellDamageLibrary.GetAbilityValue(data, "damage_per_mana");
            }

            //calculate bonus damage from invi item
            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_invisibility_edge_windwalk");
            if (modifier != null)
            {
                TotalDamageArray[(int) DamageType.Physical] += SpellDamageLibrary.GetInviSwordDamage(HeroObj);
            }

            modifier = HeroObj.Modifiers.FirstOrDefault(x => x.Name == "modifier_item_silver_edge_windwalk");
            if (modifier != null)
            {
                TotalDamageArray[(int)DamageType.Physical] += SpellDamageLibrary.GetSilverEdgeDamage();
            }
             
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
                double angle2Hero = HeroObj.FindAngleBetween(enemy.HeroObj.Position) + 180.0;  //Face: enemy.HeroObj.FindAngleBetween(HeroObj.Position)
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
            enemyHealth = enemyHealth - myTotalDamage - TotalDamageArray[(int) DamageType.HealthRemoval];

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

        private void CalculateDamage(Ability ability, out double spell_damage, out int damage_type)
        {
            foreach (AbilityData data in ability.AbilityData)
            {
                Log.SlowDebug("Ability: " + ability.Name + " - Data: " + data.Name + " : " + data.Value + " : " + data.GetValue(ability.Level - 1));
            }

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
                        double attribute = HeroObj.PrimaryAttribute == Attribute.Strength ? HeroObj.TotalStrength
                                         : HeroObj.PrimaryAttribute == Attribute.Agility ? HeroObj.TotalAgility
                                         : HeroObj.TotalIntelligence;
                        spell_damage += attribute * SpellDamageLibrary.GetAbilityValue(ability, "blast_agility_multiplier") + SpellDamageLibrary.GetAbilityValue(ability, "blast_damage_base");
                    }
                    catch (NullReferenceException)
                    {
                        spell_damage += 0;
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

            switch (ability.Name)
            {
                case "terrorblade_sunder":
                    HasSunderSpell = true;
                    SunderMinPercentage = ability.AbilityData.SingleOrDefault().GetValue(ability.Level - 1) / 100;
                    return;
                case "bristleback_quill_spray":
                    HasQuillSpraySpell = true;
                    QuillSprayStackDamage = SpellDamageLibrary.GetAbilityValue(ability, "quill_stack_damage");
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "quill_base_damage");
                    damage_type = (int)ability.DamageType;
                    return;
                case "ursa_fury_swipes":
                    HasFurySwipesSpell = true;
                    FurySwipesStackDamage = SpellDamageLibrary.GetAbilityValue(ability, "damage_per_stack");
                    damage_type = (int)ability.DamageType;
                    return;
                case "bristleback_bristleback":
                    HasBrislebackSpell = true;
                    BristlebackSideBlock = SpellDamageLibrary.GetAbilityValue(ability, "side_damage_reduction");
                    BristlebackBackBlock = SpellDamageLibrary.GetAbilityValue(ability, "back_damage_reduction");
                    BristlebackSideAngle = SpellDamageLibrary.GetAbilityValue(ability, "side_angle");
                    BristlebackBackAngle = SpellDamageLibrary.GetAbilityValue(ability, "back_angle");
                    return;
                case "antimage_mana_void":
                    HasManaVoid = true;
                    ManaVoidMultiplier = SpellDamageLibrary.GetAbilityValue(ability, "mana_void_damage_per_mana");
                    return;
                case "necrolyte_reapers_scythe":
                    HasNecrolyteReapersScythe = true;
                    NecrolyteReapersDamageMultipler = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "damage_per_health_scepter" : "damage_per_health");
                    break;
                case "doom_bringer_lvl_death":
                    HasLvlDeath = true;
                    LvlDeathAdditionalDamage = SpellDamageLibrary.GetAbilityValue(ability, "lvl_bonus_damage");
                    LvlBonusHeroMultiple = (int)SpellDamageLibrary.GetAbilityValue(ability, "lvl_bonus_multiple");
                    if (LvlBonusHeroMultiple <= 0) LvlBonusHeroMultiple = 1;
                    break;  //not return because lvl death base damage can be calculated
                case "undying_decay":
                    double strSteal = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "str_steal_scepter" : "str_steal");
                    strSteal = strSteal * 19;  //calculate damage on strSteal and return
                    TotalDamageArray[(int) DamageType.HealthRemoval] += strSteal;
                    Log.SlowDebug(ability.Name + " - Extra HP Removal: " + strSteal);
                    break;
                case "nyx_assassin_mana_burn":
                    break;
                case "undying_soul_rip":
                    //TODO: undying_soul_rip
                    break;
                case "huskar_life_break":
                    break;
                case "ancient_apparition_ice_blast":
                    break;
                case "centaur_stampede":
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "strength_damage") * HeroObj.TotalStrength;
                    damage_type = (int) DamageType.Magical;
                    return;  //we can just break, but this spell is DamageType bugged
                case "axe_culling_blade":
                    break;
                case "spectre_dispersion":
                    double spellAmplifier = ability.AbilityData.First(x => x.Name == "damage_reflection_pct").GetValue(ability.Level - 1);
                    IncommingDamageAmplifier *= 1.0 - spellAmplifier / 100;
                    return;
                case "bounty_hunter_jinada":
                case "tusk_walrus_punch":
                    double critMultiplier = SpellDamageLibrary.GetAbilityValue(ability, "crit_multiplier");
                    spell_damage += AttackDamage * critMultiplier / 100;
                    damage_type = (int)DamageType.Physical;
                    return;
                case "visage_soul_assumption":
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "soul_base_damage") + SoulAssumption * SpellDamageLibrary.GetAbilityValue(ability, "soul_charge_damage");
                    damage_type = (int)ability.DamageType;
                    return;
                case "morphling_adaptive_strike":
                    double agiRate = (double) HeroObj.TotalAgility / (HeroObj.TotalStrength + HeroObj.TotalAgility);
                    agiRate = Math.Min(Math.Max(agiRate, 0.4), 0.6);  //rate between 0.4 total and 0.6 total
                    double minMultiplier = SpellDamageLibrary.GetAbilityValue(ability, "damage_min");
                    double maxMultiplier = SpellDamageLibrary.GetAbilityValue(ability, "damage_max");
                    agiRate = ((agiRate - 0.4)/0.2)*(maxMultiplier - minMultiplier) + minMultiplier;  //convert from agiRate to agiDamageRate
                    spell_damage += agiRate * HeroObj.TotalAgility + SpellDamageLibrary.GetAbilityValue(ability, "damage_base");
                    damage_type = (int) ability.DamageType;
                    return;
                default:
                    break;
            }
            
            if (ability.AbilityBehavior == AbilityBehavior.Passive) return;
            if (ability.DamageType == DamageType.Magical || ability.DamageType == DamageType.Physical || ability.DamageType == DamageType.Pure)
            {
                damage_type = (int)ability.DamageType;
            }

            //TODO: custom spells
            //antimage_mana_void
            //doom_bringer_lvl_death
            //necrolyte_reapers_scythe

            //nyx_assassin_mana_burn
            //huskar_life_break
            //ancient_apparition_ice_blast
            //axe_culling_blade
            
            //meepo poof
            //item mana burn
            //invoker_emp

            //get damage because spell.GetDamage is not working currently
            string lastAbilityWord = ability.Name;
            lastAbilityWord = lastAbilityWord.Substring(lastAbilityWord.LastIndexOf("_") + 1) + "_damage";
            //find ability damage
            var spellDamageData = ability.AbilityData.FirstOrDefault(x =>
                x.Name == "target_damage" || x.Name == "#AbilityDamage" || x.Name == "total_damage" || x.Name == "total_damage_tooltip" || x.Name == "hero_damage_tooltip" || x.Name == "bonus_damage" || 
                x.Name == lastAbilityWord
            );
            if (spellDamageData != null)
            {
                spell_damage += spellDamageData.GetValue(ability.Level - 1);
                return;
            }
            
            //for some spell that is ambigious between instant and dot
            double tickInterval = 1.0;
            double duration = 1.0;
            double bonusDamage = 0.0;
            double spellDoT = 0.0;
            if (HasScepter)
            {
                spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == "damage_scepter");
            }
            if (!HasScepter || spellDamageData == null)
            {
                spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == "damage");
            }
            if (spellDamageData == null)
            {
                spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == "damage_per_second" || x.Name == "tick_damage");
            }
            if (spellDamageData != null)
            {
                spellDoT = SpellDamageLibrary.GetAbilityValue(ability, spellDamageData);
            }
            duration = SpellDamageLibrary.GetAbilityValue(ability, "duration");
            tickInterval = SpellDamageLibrary.GetAbilityValue(ability, "tick_interval");
            bonusDamage = SpellDamageLibrary.GetAbilityValue(ability, "strike_damage");
            spell_damage += spellDoT * duration / tickInterval + bonusDamage;
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

            var item = hero.Inventory.Items.FirstOrDefault(x => x.Name == "item_mask_of_madness");
            if (item == null) return _berserkExtraDamage;

            _berserkExtraDamage = GetAbilityValue(item, "berserk_extra_damage");
            _berserkExtraDamageCalculated = true;
            return _berserkExtraDamage;
        }

        private static double _silverEdgeReduction = 40;
        private static double _silverEdgeDamage = 225;
        private static bool _silverEdgeCalculated = true;  //backstab reduction is broken

        public static void GetSilverEdgeInfo()
        {
            Item item = null;
            var caster = ObjectMgr.GetEntities<Hero>().FirstOrDefault(x => (item = x.Inventory.Items.FirstOrDefault(y => y.Name == "item_silver_edge")) != null);
            if (caster == null) return;
            _silverEdgeReduction = GetAbilityValue(item, "backstab_reduction");
            _silverEdgeDamage = GetAbilityValue(item, "windwalk_bonus_damage");
            _silverEdgeCalculated = true;
        }

        public static double GetSilverEdgeDamageReduction()
        {
            if (_silverEdgeCalculated) return _silverEdgeReduction;
            GetSilverEdgeInfo();
            return _silverEdgeReduction;
        }

        public static double GetSilverEdgeDamage()
        {
            if (_silverEdgeCalculated) return _silverEdgeDamage;
            GetSilverEdgeInfo();
            return _silverEdgeDamage;
        }

        private static double _inviSwordDamage = 175;
        private static bool _inviSwordCalculated = false;
        public static double GetInviSwordDamage(Hero hero)
        {
            if (_inviSwordCalculated || hero == null) return _inviSwordDamage;

            var item = hero.Inventory.Items.First(x => x.Name == "item_invis_sword");
            if (item == null) return _inviSwordDamage;

            _inviSwordDamage = GetAbilityValue(item, "windwalk_bonus_damage");
            _inviSwordCalculated = true;
            return _inviSwordDamage;
        }

        private static double[] _wispReduction = {5, 10, 15, 20};  //another broken thing
        public static double GetWispReduction(uint level)
        {
            return _wispReduction[level];
        }

        public static double GetAbilityValue(Ability ability, AbilityData data)
        {
            double value = 0.0;
            value = data.GetValue(ability.Level - 1);
            if (value < 0.1 || value > 1E9) value = data.Value;
            return value;
        }

        public static double GetAbilityValue(Ability ability, string data)
        {
            double value = 0.0;
            AbilityData abilityData = ability.AbilityData.FirstOrDefault(x => x.Name == data);
            if (abilityData == null) return 0;
            value = abilityData.GetValue(ability.Level - 1);
            if (value < 0.1 || value > 1E9) value = abilityData.Value;
            return value;
        }
    }
}
