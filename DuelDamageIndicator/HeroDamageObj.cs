using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Extensions;
using Attribute = Ensage.Attribute;

namespace DuelDamageIndicator
{
    class HeroDamageObj
    {
        public static readonly string[] ItemPureDamage = { "item_urn_of_shadows" };
        public static readonly string[] ItemMagicDamage = { "item_dagon", "item_shiva" };
        public static readonly string[] ItemPhysicalDamage = { "item_silver_edge", "item_invis_sword" };

        public static string[] FullDOTSpellName = {
            "bane_nightmare",
            "axe_battle_hunger",
            "bane_fiends_grip",
            "dazzle_poison_touch",
            "doom_bringer_doom",
            "disruptor_thunder_strike",
            "huskar_burning_spear",
            "jakiro_dual_breath",
            "jakiro_liquid_fire",
            "queenofpain_shadow_strike",
            "venomancer_venomous_gale",
            "venomancer_poison_nova",
            "viper_poison_attack",
            "viper_viper_strike",
            "silencer_curse_of_the_silent",
            "enigma_malefice",
            //TODO: Enigma - Black Hole
            //TODO: Winter Wyvern - Arctic Burn
            //ancient_apparition_ice_blast calculated
        };
        public static string[] HalfDOTSpellName = {
            "phoenix_supernova",
            "shredder_chakram",
            "shredder_chakram_2",
            "ember_spirit_flame_guard",
            "gyrocopter_rocket_barrage",
            "dark_seer_ion_shell",
            //"rattletrap_battery_assault",
            //"alchemist_acid_spray",
            //"sniper_shrapnel",
            //"leshrac_diabolic_edict",
            //"leshrac_pulse_nova",
            //"jakiro_macropyre",
            //"pudge_rot",
            //Not calculated: Enigma - Midnight Pulse
            //Not calculated: Luna - Eclipse
        };
        public static string[] OneSecDOTSpellName ={
            "ember_spirit_flame_guard",
            //"pudge_rot",
        };
        public static string[] DOTDamageName = {
            "damage_per_second",
            "tick_damage",
            "duration_damage",
            "burn_damage",
            "dps",
            "damage_per_sec",
            "health_damage",
        };

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
        public int QuillSprayDamageType = 0;

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
        public int ManaVoidDamageType = 0;

        public bool HasLvlDeath = false;
        public double LvlDeathAdditionalDamage = 0;
        public int LvlDeathBonusHeroMultiple = 1;
        public int LvlDeathDamageType = 0;

        public bool HasNecrolyteReapersScythe = false;
        public double NecrolyteReapersDamageMultipler = 0;
        public int NecrolyteReapersDamageType = 0;

        public double SoulAssumption = 0;

        public bool HasNyxManaBurn = false;
        public double NyxManaBurnMultiplier = 0;
        public int NyxManaBurnDamageType = 0;

        public bool HasLifeBreak = false;
        public double LifeBreakMultiplier = 0;
        public int LifeBreakDamageType = 0;

        public bool HasCullingBlade = false;
        public double CullingBladeThreshold = 0;
        public double CullingBladeDamage = 0;
        public int CullingBladeDamageType = 0;

        public bool HasIceBlast = false;
        public double IceBlastThreshold = 0;

        public HeroDamageObj(Hero hero, double damageConfident)
        {
            HeroObj = hero;
            TotalDamageArray = new double[10];
            AttackDamage = hero.MaximumDamage * damageConfident + hero.MinimumDamage * (1 - damageConfident) + hero.BonusDamage;
            
            double spellDamage;
            int damageType;
            IEnumerable<Ability> spellBook;
            
            if (hero.Name == "npc_dota_hero_invoker")
            {
                List<Ability> invokerCurrentSpells = new List<Ability>();
                if (hero.Spellbook.SpellD != null) invokerCurrentSpells.Add(hero.Spellbook.SpellD);
                if (hero.Spellbook.SpellF != null) invokerCurrentSpells.Add(hero.Spellbook.SpellF);
                spellBook = invokerCurrentSpells;
            }
            else
            {
                spellBook = hero.Spellbook.Spells;
            }
            
            CalculateCustomModifier();

            //calculate total brust damage
            foreach (Ability spell in spellBook.Concat(hero.Inventory.Items))
            {
                if (spell.AbilityState != AbilityState.Ready) continue;
                CalculateDamage(spell, out spellDamage, out damageType);
                //Log.SlowDebug("Hero: " + hero.Name + " - Spell: " + spell.Name + " - Damage: " + spellDamage + " - Type: " + (DamageType) damageType + " - State: " + spell.AbilityState);
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
            double[] toEnemyTotalDamage = new double[TotalDamageArray.Length];
            Array.Copy(TotalDamageArray, toEnemyTotalDamage, TotalDamageArray.Length);

            //Some spell need to calculate based on enemy hero
            if (HasQuillSpraySpell)
            {
                toEnemyTotalDamage[QuillSprayDamageType] += enemy.QuillSprayStack * QuillSprayStackDamage;
            }
            if (HasManaVoid)
            {
                toEnemyTotalDamage[ManaVoidDamageType] += (enemy.HeroObj.MaximumMana - enemy.HeroObj.Mana) * ManaVoidMultiplier;
            }
            if (HasLvlDeath && ((enemy.HeroObj.Level == 25) || (enemy.HeroObj.Level % LvlDeathBonusHeroMultiple == 0)))
            {
                toEnemyTotalDamage[LvlDeathDamageType] += enemy.HeroObj.MaximumHealth * 0.2;
            }
            if (HasNecrolyteReapersScythe)
            {
                toEnemyTotalDamage[NecrolyteReapersDamageType] += (enemy.HeroObj.MaximumHealth - enemy.HeroObj.Health) * NecrolyteReapersDamageMultipler;
            }
            if (HasNyxManaBurn)
            {
                toEnemyTotalDamage[NyxManaBurnDamageType] += Math.Max(enemy.HeroObj.TotalIntelligence * NyxManaBurnMultiplier, enemy.HeroObj.Mana);
            }
            if (HasLifeBreak)
            {
                toEnemyTotalDamage[LifeBreakDamageType] += enemy.HeroObj.Health * LifeBreakMultiplier;
            }

            double myTotalDamage = toEnemyTotalDamage[(int)DamageType.Pure] +
                                   toEnemyTotalDamage[(int)DamageType.Physical] * (1.0 - enemy.HeroObj.DamageResist) +
                                   toEnemyTotalDamage[(int)DamageType.Magical] * (1.0 - enemy.HeroObj.MagicDamageResist);
            double temporallyDamageAmplifier = 1.0 * OutgoingDamageAmplifier * enemy.IncommingDamageAmplifier;

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
            enemyHealth = enemyHealth - myTotalDamage - toEnemyTotalDamage[(int) DamageType.HealthRemoval];

            //check any threshold HP removal type
            if (HasCullingBlade)
            {
                if (enemyHealth < CullingBladeThreshold)
                {
                    enemyHealth = 0;
                }
                else
                {
                    double cullingBladeAmplifier = CullingBladeDamageType == (int) DamageType.Physical ? 1.0 - enemy.HeroObj.DamageResist :
                                                   CullingBladeDamageType == (int) DamageType.Magical ? 1.0 - enemy.HeroObj.MagicDamageResist :
                                                   1.0;
                    enemyHealth = enemyHealth - CullingBladeDamage * cullingBladeAmplifier * temporallyDamageAmplifier;
                }
            }
            if (HasIceBlast && (enemyHealth / enemy.HeroObj.MaximumHealth < IceBlastThreshold))
            {
                enemyHealth = 0;
            }

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
            /*foreach (AbilityData data in ability.AbilityData)
            {
                Log.SlowDebug("Ability: " + ability.Name + " - Data: " + data.Name + " : " + SpellDamageLibrary.GetAbilityValue(ability, data));
            }*/

            spell_damage = 0;
            int damage_none = (int)DamageType.None;
            damage_type = damage_none;
            double tickInterval = 1.0;
            double duration = 1.0;
            double bonusDamage = 0.0;
            double spellDoT = 0.0;
            int i;

            if (ability is Item)
            {
                Item item = (Item) ability;
                //process charge based item: if it has no charge left, return
                if (item.IsRequiringCharges && item.CurrentCharges == 0) return;

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

                //process pure item
                if (damage_type == damage_none)
                {
                    for (i = 0; i < ItemPureDamage.Length; ++i)
                    {
                        if (ability.Name.Contains(ItemPureDamage[i]))
                        {
                            damage_type = (int)DamageType.Pure;
                            break;
                        }
                    }
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

                AbilityData data = ability.AbilityData.FirstOrDefault(x => x.Name != "bonus_damage" && x.Name.ToLower().Contains("damage") && !x.Name.ToLower().Contains("duration"));
                if (data != null)
                {
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, data);
                }
                return;
            }

            switch (ability.Name)
            {
                case "terrorblade_sunder":
                    HasSunderSpell = true;
                    SunderMinPercentage = SpellDamageLibrary.GetAbilityValue(ability, "hit_point_minimum_pct") / 100;
                    return;
                case "bristleback_quill_spray":
                    HasQuillSpraySpell = true;
                    QuillSprayStackDamage = SpellDamageLibrary.GetAbilityValue(ability, "quill_stack_damage");
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "quill_base_damage");
                    QuillSprayDamageType = damage_type = (int)ability.DamageType;
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
                    ManaVoidDamageType = (int) ability.DamageType;
                    return;
                case "necrolyte_reapers_scythe":
                    HasNecrolyteReapersScythe = true;
                    NecrolyteReapersDamageMultipler = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "damage_per_health_scepter" : "damage_per_health");
                    NecrolyteReapersDamageType = (int)ability.DamageType;
                    break;
                case "doom_bringer_lvl_death":
                    HasLvlDeath = true;
                    LvlDeathAdditionalDamage = SpellDamageLibrary.GetAbilityValue(ability, "lvl_bonus_damage");
                    LvlDeathBonusHeroMultiple = (int)SpellDamageLibrary.GetAbilityValue(ability, "lvl_bonus_multiple");
                    if (LvlDeathBonusHeroMultiple <= 0) LvlDeathBonusHeroMultiple = 1;
                    LvlDeathDamageType = (int) ability.DamageType;
                    break;  //not return because lvl death base damage can be calculated
                case "undying_decay":
                    double strSteal = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "str_steal_scepter" : "str_steal");
                    strSteal = strSteal * 19;  //calculate damage on strSteal and return
                    TotalDamageArray[(int) DamageType.HealthRemoval] += strSteal;
                    break;
                case "nyx_assassin_mana_burn":
                    HasNyxManaBurn = true;
                    NyxManaBurnMultiplier = SpellDamageLibrary.GetAbilityValue(ability, "float_multiplier");
                    NyxManaBurnDamageType = (int) ability.DamageType;
                    return;
                case "undying_soul_rip":
                    double radius = SpellDamageLibrary.GetAbilityValue(ability, "radius");
                    double damagePerUnit = SpellDamageLibrary.GetAbilityValue(ability, "damage_per_unit");
                    double maxUnits = SpellDamageLibrary.GetAbilityValue(ability, "max_units");
                    int nearUnitsCount = ObjectMgr.GetEntities<Unit>().Count(
                            x => !x.Equals(HeroObj)  //it shouldn't be the caster
                            && x.IsAlive && x.IsVisible && x.Distance2D(HeroObj) < (radius + x.HullRadius)  //it should be any unit that is alive, visible and within range
                            && !(x.Team != HeroObj.Team && x.IsMagicImmune())  //it shouldn't be magic immune on enemy team
                            && (x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Creep
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Creature
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit
                                    || x.ClassID == ClassID.CDOTA_Unit_Undying_Zombie
                                    || x.ClassID == ClassID.CDOTA_BaseNPC_Warlock_Golem
                                    || x is Hero)
                            ) - 1;  //and remove the target
                    spell_damage += Math.Min(nearUnitsCount, maxUnits) * damagePerUnit;
                    break;
                case "invoker_emp":
                    uint wex = HeroObj.FindSpell("invoker_wex").Level - 1;
                    spell_damage = SpellDamageLibrary.GetAbilityValue(ability, "mana_burned", wex) * SpellDamageLibrary.GetAbilityValue(ability, "damage_per_mana_pct", wex) / 100;
                    break;
                case "huskar_life_break":
                    HasLifeBreak = true;
                    LifeBreakMultiplier = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "health_damage_scepter" : "health_damage");
                    LifeBreakDamageType = (int) ability.DamageType;
                    return;
                case "centaur_stampede":
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "strength_damage") * HeroObj.TotalStrength;
                    damage_type = (int) DamageType.Magical;
                    return;  //we can just break, but this spell is DamageType bugged
                case "ancient_apparition_ice_blast":
                    HasIceBlast = true;
                    IceBlastThreshold = SpellDamageLibrary.GetAbilityValue(ability, "kill_pct") / 100;
                    spell_damage += SpellDamageLibrary.GetAbilityValue(ability, "dot_damage") * SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "frostbite_duration_scepter" : "frostbite_duration");
                    break;
                case "axe_culling_blade":
                    HasCullingBlade = true;
                    CullingBladeDamageType = (int) ability.DamageType;
                    CullingBladeDamage = SpellDamageLibrary.GetAbilityValue(ability, "damage");
                    CullingBladeThreshold = SpellDamageLibrary.GetAbilityValue(ability, HasScepter ? "kill_threshold_scepter" : "kill_threshold");
                    return;
                case "spectre_dispersion":
                    double spellAmplifier = SpellDamageLibrary.GetAbilityValue(ability, "damage_reflection_pct");
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
                    agiRate = ((agiRate - 0.4) / 0.2) * (maxMultiplier - minMultiplier) + minMultiplier;  //convert from agiRate to agiDamageRate
                    spell_damage += agiRate * HeroObj.TotalAgility + SpellDamageLibrary.GetAbilityValue(ability, "damage_base");
                    damage_type = (int) ability.DamageType;
                    return;
                default:
                    break;
            }
            
            if (ability.AbilityBehavior == AbilityBehavior.Passive) return;
            if (ability.DamageType == DamageType.Magical || ability.DamageType == DamageType.Physical || ability.DamageType == DamageType.Pure)
            {
                damage_type = (int) ability.DamageType;
            }
            else
            {
                damage_type = (int) DamageType.Magical;
            }

            //TODO: meepo poof

            //get damage because spell.GetDamage is not working currently
            string lastAbilityWord = ability.Name;
            lastAbilityWord = lastAbilityWord.Substring(lastAbilityWord.LastIndexOf("_") + 1) + "_damage";
            //find ability damage
            var spellDamageData = ability.AbilityData.FirstOrDefault(x =>
                x.Name == "target_damage" || x.Name == "#AbilityDamage" || x.Name == "total_damage" || x.Name == "total_damage_tooltip" || x.Name == "hero_damage_tooltip" || x.Name == "bonus_damage" || 
                x.Name == lastAbilityWord
            );

            if (spellDamageData == null)
            {
                if (HasScepter)
                {
                    spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == "damage_scepter");
                }
                if (!HasScepter || spellDamageData == null)
                {
                    spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == "damage");
                }
            }

            double spellDamage = SpellDamageLibrary.GetAbilityValue(ability, spellDamageData);

            //if it's not any DOT white list, just leave
            if (!FullDOTSpellName.Contains(ability.Name) && !HalfDOTSpellName.Contains(ability.Name))
            {
                spell_damage += spellDamage;
                return;
            }

            string customSpellDamageName = "damage";
            string customSpellDurationName = "duration";
            string customSpellIntervalName = "tick_interval";
            string customSpellBonusName = "strike_damage";
            if (ability.Name == "bane_fiends_grip")
            {
                customSpellDamageName = HasScepter ? "fiend_grip_damage_scepter" : "fiend_grip_damage";
                customSpellDurationName = HasScepter ? "fiend_grip_duration_scepter" : "fiend_grip_duration";
                customSpellIntervalName = "fiend_grip_tick_interval";
            }
            else if (ability.Name == "doom_bringer_doom")
            {
                customSpellDurationName = HasScepter ? "duration_scepter" : "duration";
            }
            else if (ability.Name == "disruptor_thunder_strike")
            {
                customSpellDurationName = "strikes";
            }
            else if (ability.Name == "enigma_malefice")
            {
                customSpellDurationName = "tooltip_stuns";
            }
            else if (ability.Name == "bane_nightmare")
            {
                customSpellIntervalName = "nightmare_dot_interval";
            }
            else if (ability.Name == "shredder_chakram" || ability.Name == "shredder_chakram_2")
            {
                customSpellBonusName = "pass_damage";
            }

            if (spellDamageData == null)
            {
                spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == customSpellDamageName || DOTDamageName.Contains(x.Name));
            }
            if (spellDamageData != null)
            {
                spellDoT = SpellDamageLibrary.GetAbilityValue(ability, spellDamageData);
            }

            if (!HalfDOTSpellName.Contains(ability.Name))
            {
                //get duration
                spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == customSpellDurationName || x.Name == "duration_tooltip" || x.Name == "tooltip_duration" || x.Name == "burn_duration");
                duration = SpellDamageLibrary.GetAbilityValue(ability, spellDamageData);
                if (ability.Name == "huskar_burning_spear") duration = 8.0;
                else if (ability.Name == "dazzle_poison_touch")
                {
                    duration -= SpellDamageLibrary.GetAbilityValue(ability, "set_time") + 1;
                }
            }

            //get tick interval
            spellDamageData = ability.AbilityData.FirstOrDefault(x => x.Name == customSpellIntervalName);
            tickInterval = SpellDamageLibrary.GetAbilityValue(ability, spellDamageData);
            if (OneSecDOTSpellName.Contains(ability.Name)) tickInterval = 1.0;
            else if (ability.Name == "gyrocopter_rocket_barrage") tickInterval = 1.0 / SpellDamageLibrary.GetAbilityValue(ability, "rockets_per_second");

            //get bonus damage
            bonusDamage = SpellDamageLibrary.GetAbilityValue(ability, customSpellBonusName);

            //calculate final result
            if (tickInterval < 0.001) tickInterval = 1.0;
            if (duration < 0.001) duration = 1.0;
            if (duration < tickInterval) duration = tickInterval;  //HalfDOT should be getting at least 1 interval damage
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

        public static double GetAbilityValue(Ability ability, AbilityData data, uint level = 0xABADC0DE)
        {
            if (data == null) return 0.0;
            if (level == 0xABADC0DE) level = ability.Level - 1;
            if (level > 0xF0000000) level = 0;
            return data.Count > 1 ? data.GetValue(level) : data.Value;
        }

        public static double GetAbilityValue(Ability ability, string data, uint level = 0xABADC0DE)
        {
            AbilityData abilityData = ability.AbilityData.FirstOrDefault(x => x.Name == data);
            if (abilityData == null) return 0;
            return GetAbilityValue(ability, abilityData, level);
        }
    }
}
