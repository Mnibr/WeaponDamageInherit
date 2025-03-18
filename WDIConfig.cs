using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace WeaponDamageInherit
{
    public class WDIConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;
        public enum InheritCheckMode
        {
            All,
            ExtendedFamily,
            Compatible,
            CompatibleNoProj,
            CoreFamily,
            EqualNoProj,
            Equal
        }
        [DefaultValue(InheritCheckMode.ExtendedFamily)]
        [DrawTicks]
        public InheritCheckMode inheritCheck = InheritCheckMode.ExtendedFamily;

        public enum InheritCoefficientMode
        {
            Copy,
            Speed,
            Size,
            Both
        }
        [DefaultValue(InheritCoefficientMode.Speed)]
        [DrawTicks]
        public InheritCoefficientMode inheritCoefficient = InheritCoefficientMode.Speed;

        [DefaultValue(true)]
        public bool sizeInfluenceMeleeCheck = true;

        [DefaultValue(true)]
        public bool DismodifyWhenUnqualified = true;

        [DefaultValue(true)]
        public bool useDamageLimit = false;

        [DefaultValue(5.0f)]
        [Range(1.0f,10.0f)]
        [Increment(0.1f)]
        public float DamageMaxLimit = 5.0f;

        [DefaultValue(0.1f)]
        [Range(0f,1.0f)]
        [Increment(0.1f)]
        public float DamageMinLimit = 0.1f;

        public static WDIConfig Instance => ModContent.GetInstance<WDIConfig>();
    }
}
