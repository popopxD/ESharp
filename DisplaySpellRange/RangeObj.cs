using System;
using Ensage;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using SharpDX.Direct3D9;

namespace DisplaySpellRange
{
    internal class RangeObj
    {
        public static bool UseOldStyle = false;
        public static bool UseColorStyle = false;

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

        private static readonly Vector3[] _rangeColors =
        {
            new Vector3(0  , 0  , 0  ),  //WHITE
            new Vector3(255, 0  , 0  ),  //RED
            new Vector3(0  , 255, 0  ),  //GREEN
            new Vector3(0  , 0  , 255),  //BLUE
            new Vector3(255, 255, 0  ),  //YELLOW
            new Vector3(255, 0  , 255),  //PINK
            new Vector3(0  , 255, 255),  //TEAL
            new Vector3(255, 255, 255),  //BLACK
            new Vector3(128, 0  , 0  ),
            new Vector3(0  , 128, 0  ),
            new Vector3(0  , 0  , 128),
            new Vector3(128, 128, 0  ),
            new Vector3(128, 0  , 128),
            new Vector3(0  , 128, 128),
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
                    int hashcode = Ability.Name.GetHashCode();
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
                _isDisplayed = value;
                if (value)
                {
                    Program.CacheSpellList[Unit.Name + "_" + Ability.Name] = this;
                }
                else
                {
                    Program.CacheSpellList.Remove(Unit.Name + "_" + Ability.Name);
                }
                Refresh();
            }
        }

        public RangeObj(Ability ability, Unit unit = null)
        {
            Ability = ability;
            Unit = unit ?? Program.Me;
            IsDisplayed = false;
            Effect = null;
            EffectColor = null;
            LastUsedRange = 0;
            UpdateRange();
        }

        private void UpdateRange()
        {
            if (Ability == null || Unit == null)
            {
                LastUsedRange = Range = 0;
                IsDisplayable = false;
                IsDisplayed = false;
                return;
            }

            float bonusRange = 0f;
            if ((Ability.AbilityBehavior & AbilityBehavior.NoTarget) == 0)
            {
                var aetherLens = Unit.FindItem("item_aether_lens");
                if (aetherLens != null)
                {
                    bonusRange = aetherLens.GetAbilityData("cast_range_bonus");
                }
            }
            Range = Ability.CastRange;
            if (Range < 1)  //known as Range == 0
            {
                var data = Ability.AbilitySpecialData.FirstOrDefault(x => x.Name.Contains("radius") || (x.Name.Contains("range") && !x.Name.Contains("ranged")));
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
            UpdateRange();

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
                        EffectColor.SetControlPoint(2, new Vector3(Range, 255, 0));
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
            else if (Effect != null)
            {
                Effect.Dispose();
                Effect = null;
            }
            else if (EffectColor != null)
            {
                EffectColor.Dispose();
                EffectColor = null;
            }
            return true;
        }
    }
}