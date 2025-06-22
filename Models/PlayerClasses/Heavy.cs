using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Heavy : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Heavy;
        public override string Name => "Тяжёлый танк";
        public override string Description => "Живая крепость, способная выдерживать огромный урон и отвечать мощными атаками по площади.";
        public override string SkillName => "Броня";
        public override string SkillDescription => "Временно снижает весь получаемый урон на 50%.";
        public override string GunSpriteName => "guns/heavy_t1"; // Placeholder

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement "Juggernaut" skill
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.UpgradeMaxHealth(50); // +50 Max Health
            player.ModifyMovementSpeed(-0.6); // -0.6 Movement Speed
        }
    }
} 