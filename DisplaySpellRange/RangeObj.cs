using System;
using Ensage;
using System.Linq;
using Ensage.Common.Extensions;
using SharpDX;

namespace DisplaySpellRange
{
    internal class RangeObj
    {
        public static bool UseOldStyle = false;
        public static bool UseColorStyle = false;

        public bool isRangeOnly;
        public bool isAttackRange;
        public float Range;
        public bool IsDisplayable;
        private bool _isDisplayed;
        public Ability Ability;
        public Unit Unit;
        public ParticleEffect Effect;
        public ParticleEffect EffectColor;
        public float LastUsedRange;
        public int AbilityColorPoint = -1;
        private Vector3 _abilityColor;
        private string _cacheName = null;

        private static readonly Vector3[] _rangeColors =
        {
            //new Vector3(0  , 0  , 0  ),  //BLACK NOT WORKING
            new Vector3(255, 0  , 0  ),  //RED
            new Vector3(0  , 255, 0  ),  //GREEN
            new Vector3(0  , 0  , 255),  //BLUE
            new Vector3(255, 255, 0  ),  //YELLOW
            new Vector3(255, 0  , 255),  //PINK
            new Vector3(0  , 255, 255),  //TEAL
            new Vector3(255, 255, 255),  //WHITE
            new Vector3(192, 0  , 0  ),  //DONT USE 128 ALONE
            new Vector3(0  , 192, 0  ),
            new Vector3(0  , 0  , 192),
            new Vector3(192, 192, 0  ),
            new Vector3(192, 0  , 192),
            new Vector3(0  , 192, 192),
            new Vector3(255, 128, 0  ),
            new Vector3(255, 0  , 128),
            new Vector3(255, 128, 128),
            new Vector3(128, 255, 0  ),
            new Vector3(0  , 255, 128),
            new Vector3(128, 255, 128),
            new Vector3(128, 0  , 255),
            new Vector3(0  , 128, 255),
            new Vector3(128, 128, 255),
            new Vector3(255, 255, 128),
            new Vector3(255, 128, 255),
            new Vector3(128, 255, 255),
            new Vector3(255, 192, 0  ),
            new Vector3(255, 0  , 192),
            new Vector3(255, 192, 192),
            new Vector3(192, 255, 0  ),
            new Vector3(0  , 255, 192),
            new Vector3(192, 255, 192),
            new Vector3(192, 0  , 255),
            new Vector3(0  , 192, 255),
            new Vector3(192, 192, 255),
            new Vector3(255, 255, 192),
            new Vector3(255, 192, 255),
            new Vector3(192, 255, 255),
            new Vector3(128, 255, 192),
            new Vector3(128, 192, 255),
            new Vector3(192, 128, 255),
            new Vector3(255, 128, 192),
            new Vector3(255, 192, 128),
            new Vector3(192, 255, 128),
        };


        public string TextureName
        {
            get
            {
                if (Ability == null) return "materials/ensage_ui/items/emptyitembg.vmat";
                try
                {
                    if (Ability is Item)
                    {
                        return "materials/ensage_ui/items/" + Ability.TextureName.Substring(5) + ".vmat";
                    }
                    return "materials/ensage_ui/spellicons/" + Ability.TextureName + ".vmat";
                }
                catch (EntityNotFoundException)
                {
                    Ability = null;
                    return "materials/ensage_ui/items/emptyitembg.vmat";
                }
            }
        }

        public Vector3 AbilityColor
        {
            get
            {
                if (AbilityColorPoint == -1)
                {
                    int hashcode = Range.GetHashCode();
                    if (Ability != null)
                    {
                        hashcode = Ability.Name.GetHashCode();
                    }
                    if (hashcode < 0) hashcode = -hashcode;
                    AbilityColorPoint = hashcode % _rangeColors.Length;
                    _abilityColor = _rangeColors[AbilityColorPoint];
                }
                return _abilityColor;
            }
        }

        public bool IsDisplayed
        {
            get { return _isDisplayed; }
            set
            {
                if (value == _isDisplayed)
                {
                    return;
                }
                _isDisplayed = value;
                string str = GetCacheKeyName();
                if (value)
                {
                    Program.CacheSpellList[str] = this;
                }
                else
                {
                    Program.CacheSpellList.Remove(str);
                }
                Refresh();
            }
        }

        public RangeObj(Ability ability, Unit unit = null)
        {
            Ability = ability;
            Unit = unit ?? Program.Me;
            IsDisplayed = false;
            IsDisplayable = false;
            Effect = null;
            EffectColor = null;
            LastUsedRange = 0;
            isRangeOnly = false;
            UpdateRange();
        }

        public RangeObj(float num, Unit unit = null)
        {
            Range = LastUsedRange = num;
            Ability = null;
            Unit = unit ?? Program.Me;
            IsDisplayed = false;
            IsDisplayable = true;
            Effect = null;
            EffectColor = null;
            isRangeOnly = true;
            UpdateRange();
        }

        public RangeObj(bool isAttackRange, Unit unit = null)
        {
            this.isAttackRange = isAttackRange;
            Ability = null;
            Unit = unit ?? Program.Me;
            IsDisplayed = false;
            IsDisplayable = true;
            Effect = null;
            EffectColor = null;
            LastUsedRange = 0;
            isRangeOnly = false;
            UpdateRange();
        }

        private void UpdateRange()
        {
            if (isRangeOnly)
            {
                return;
            }

            if (isAttackRange && Unit != null)
            {
                Range = 0;
                var spells = Unit.Spellbook.Spells.Where(x => x.Name.Contains("take_aim") || x.Name.Contains("psi_blades")).ToList();
                foreach (var spell in spells)
                {
                    var data = spell.AbilitySpecialData.FirstOrDefault(x => x.Name.Contains("radius") || (x.Name.Contains("range") && !x.Name.Contains("ranged")));
                    if (data != null)
                    {
                        float value = 0;
                        if (spell.Level > 0)
                        {
                            value = data.Count > 1 ? data.GetValue(spell.Level - 1) : data.Value;
                        }
                        if (value > 0)
                        {
                            Range += value;
                        }
                    }
                }
                if (Unit.HasModifier("modifier_dragon_knight_dragon_form"))
                {
                    Range += 350;
                }
                else if (Unit.HasModifier("modifier_terrorblade_metamorphosis"))
                {
                    Range += 400;
                }
                Range += Unit.AttackRange + Unit.HullRadius;
                if (Unit.IsRanged && Unit.HasInventory)
                {
                    Item item = Unit.Inventory.Items.FirstOrDefault(x => x != null && x.IsValid && (x.Name.Contains("item_dragon_lance") || x.Name.Contains("item_hurricane_pike")));
                    if (item != null)
                    {
                        var spellData = item.AbilitySpecialData.FirstOrDefault(x => x.Name == "base_attack_range");
                        if (spellData != null)
                        {
                            Range += spellData.Value;
                        }
                    }
                }
                return;
            }

            if (Ability == null || Unit == null)
            {
                LastUsedRange = Range = 0;
                IsDisplayable = false;
                IsDisplayed = false;
                return;
            }

            float bonusRange = 0f;
            if (((Ability.AbilityBehavior & AbilityBehavior.NoTarget) == 0) && Unit.HasInventory)
            {
                Item item = Unit.Inventory.Items.FirstOrDefault(x => x != null && x.IsValid && x.Name.Contains("item_aether_lens"));
                if (item != null)
                {
                    var spellData = item.AbilitySpecialData.FirstOrDefault(x => x.Name == "cast_range_bonus");
                    if (spellData != null)
                    {
                        bonusRange += spellData.Value;
                    }
                }
            }
            Range = Ability.CastRange;
            if (Range < 1)  //known as Range == 0
            {
                AbilitySpecialData data = null;
                if (Unit.AghanimState())
                {
                    data = Ability.AbilitySpecialData.FirstOrDefault(x => x.Name.Contains("scepter") && (x.Name.Contains("radius") || (x.Name.Contains("range") && !x.Name.Contains("ranged"))));
                }
                if (data == null)
                {
                    data = Ability.AbilitySpecialData.FirstOrDefault(x => x.Name.Contains("radius") || (x.Name.Contains("range") && !x.Name.Contains("ranged")));
                }
                if (data != null)
                {
                    uint level = Ability.Level == 0 ? 0 : Ability.Level - 1;
                    Range = data.Count > 1 ? data.GetValue(level) : data.Value;
                }
            }
            Range += bonusRange;
            IsDisplayable = Range > 0;
        }

        public bool Refresh()
        {
            try
            {
                UpdateRange();
            }
            catch (EntityNotFoundException)
            {
                IsDisplayable = false;
                IsDisplayed = false;
            }

            //if an displayable ability become undisplayable, then disable it
            if (!IsDisplayable & IsDisplayed)
            {
                IsDisplayed = false;
            }

            //check and display the effect
            if (IsDisplayed)
            {
                bool changed = false;
                if (UseOldStyle)
                {
                    if ((Effect != null) && (Math.Abs(LastUsedRange - Range) > 1))
                    {
                        Effect.Dispose();
                        Effect = null;
                    }

                    if (Effect == null)
                    {
                        Effect = Unit.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                        Effect.SetControlPoint(1, new Vector3(Range, 0, 0));
                        changed = true;
                    }
                }
                else if(Effect != null)
                {
                    Effect.Dispose();
                    Effect = null;
                }

                if (UseColorStyle)
                {
                    if ((EffectColor != null) && (Math.Abs(LastUsedRange - Range) > 1))
                    {
                        EffectColor.Dispose();
                        EffectColor = null;
                    }

                    if (EffectColor == null)
                    {
                        EffectColor = Unit.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                        EffectColor.SetControlPoint(1, AbilityColor);
                        var range = Range + Range / 9;
                        EffectColor.SetControlPoint(2, new Vector3(range, 255, 0));
                        changed = true;
                    }
                }
                else if (EffectColor != null)
                {
                    EffectColor.Dispose();
                    EffectColor = null;
                }

                if (changed)
                {
                    LastUsedRange = Range;
                }
            }
            else
            {
                if (Effect != null)
                {
                    Effect.Dispose();
                    Effect = null;
                }
                if (EffectColor != null)
                {
                    EffectColor.Dispose();
                    EffectColor = null;
                }
            }
            return true;
        }

        public static string GetCacheKeyName(Unit unit, Ability ability = null, float range = 0f, bool isAttackRange = false)
        {
            string postfix;
            if (ability != null)
            {
                postfix = ability.Name;
            }
            else if (isAttackRange)
            {
                postfix = "attack_range";
            }
            else
            {
                postfix = ((int)range).ToString();
            }
            return unit.Handle + "_" + unit.Name + "_" + postfix;
        }

        public string GetCacheKeyName()
        {
            if (_cacheName != null)
            {
                return _cacheName;
            }
            string postfix;
            if (Ability != null)
            {
                postfix = Ability.Name;
            }
            else if (isAttackRange)
            {
                postfix = "attack_range";
            }
            else
            {
                postfix = ((int)Range).ToString();
            }
            _cacheName = Unit.Handle + "_" + Unit.Name + "_" + postfix;
            return _cacheName;
        }
    }
}