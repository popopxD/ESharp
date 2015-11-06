using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;

namespace DuelDamageIndicator
{
    class DrawingData
    {
        public Hero h;
        public bool IsEnoughMana;
        public string NumHitString;
        public bool IsEnoughManaEnemy;
        public string NumHitStringEnemy;

        public DrawingData(Hero hero, bool isEnoughMana, string numHitString, bool isEnoughManaEnemy, string numHitStringEnemy)
        {
            IsEnoughMana = isEnoughMana;
            NumHitString = numHitString;
            IsEnoughManaEnemy = isEnoughManaEnemy;
            NumHitStringEnemy = numHitStringEnemy;
            h = hero;
        }
    }
}
