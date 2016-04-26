using System;
using Ensage;
using System.Collections.Generic;
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
        public float Range;
        public bool IsDisplayable;
        public bool IsDisplayed;
        private Ability _ability;
        private ParticleEffect _effect;
        private float _lastUsedRange;

        public string TextureName
        {
            get
            {
                if (_ability == null) return "materials/ensage_ui/items/emptyitembg.vmat";
                try
                {
                    if (_ability is Item)
                    {
                        return "materials/ensage_ui/items/" + _ability.TextureName.Substring(5) + ".vmat";
                    }
                    return "materials/ensage_ui/spellicons/" + _ability.TextureName + ".vmat";
                }
                catch (EntityNotFoundException)
                {
                    _ability = null;
                    return "materials/ensage_ui/items/emptyitembg.vmat";
                }
            }
        }

        public RangeObj(Ability ability)
        {
            _ability = ability;
            IsDisplayed = false;
            _effect = null;
            _lastUsedRange = 0;
            UpdateRange();
        }

        private void UpdateRange()
        {
            if (_ability == null)
            {
                _lastUsedRange = Range = 0;
                IsDisplayable = false;
                IsDisplayed = false;
                return;
            }
            Range = _ability.CastRange;
            if (Range < 1)  //known as Range == 0
            {
                var data = _ability.AbilityData.FirstOrDefault(x => x.Name.Contains("radius") || (x.Name.Contains("range") && !x.Name.Contains("ranged")));
                if (data != null)
                {
                    uint level = _ability.Level == 0 ? 0 : _ability.Level - 1;
                    Range = data.Count > 1 ? data.GetValue(level) : data.Value;
                }
            }
            IsDisplayable = Range > 0;
        }

        public void Refresh()
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
                if (_effect == null)
                {
                    _effect = Program.Me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    _effect.SetControlPoint(1, new Vector3(Range, 0, 0));
                    _lastUsedRange = Range;
                }
                else
                {
                    if (Math.Abs(_lastUsedRange - Range) > 1)
                    {
                        _effect.Dispose();
                        _effect = Program.Me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                        _effect.SetControlPoint(1, new Vector3(Range, 0, 0));
                        _lastUsedRange = Range;
                    }
                }
            }
            else if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
        }

        public void Update(Ability ability)
        {
            _ability = ability;
            Refresh();
        }
    }
}