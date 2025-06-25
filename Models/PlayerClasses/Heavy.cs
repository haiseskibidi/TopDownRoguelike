using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Heavy : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Heavy;
        public override string Name => "Тяжёлый танк";
        public override string Description => "Живая крепость, способная выдерживать огромный урон и отвечать мощными атаками по площади.";
        public override string SkillName => "Несокрушимость";
        public override string SkillDescription => "На короткое время значительно увеличивает броню и сопротивление урону.";
        public override string GunSpriteName => "guns/heavy_t1"; 
        public override double GunWidth => 28.75;
        public override double GunHeight => 15;
        public override double FireRate => 0.5;
        public override double Damage => 50;
        public override double BulletSpeed => 100;
        public override double Spread => 0.1;
        public override double BulletSize => 12; // Big bullets
        public override string BulletSpriteName => "";

        public override bool CanRicochet => false;
        public override int MaxRicochets => 0;

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement Unbreakable skill
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.UpgradeMaxHealth(50); // +50 Max Health
            player.ModifyMovementSpeed(-0.6); // -0.6 Movement Speed
        }
    }
} 