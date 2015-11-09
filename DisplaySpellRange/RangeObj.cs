using System;
using Ensage;
using System.Collections.Generic;
using Ensage.Common;
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
                if (_ability is Item) return _ability.TextureName.Substring(5);
                return _ability.TextureName;
            }
        }

        public string TextureDirectoryName
        {
            get
            {
                if (_ability is Item) return "items";
                return "spellicons";
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
                foreach (AbilityData data in _ability.AbilityData)
                {
                    if (data.Name.Contains("radius") || (data.Name.Contains("range") && !data.Name.Contains("ranged")))
                    {
                        Range = data.GetValue(_ability.Level - 1);
                        if (Range < 1) Range = data.Value;
                        break;
                    }
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