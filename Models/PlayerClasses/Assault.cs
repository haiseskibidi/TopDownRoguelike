using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Assault : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Assault;
        public override string Name => "Штурмовик";
        public override string Description => "Мастер ближнего боя, специализирующийся на подавляющем огне и высокой скорострельности.";
        public override string SkillName => "Шквал огня";
        public override string SkillDescription => "Резко увеличивает скорострельность и размер магазина на 5-7 секунд.";
        public override string GunSpriteName => "guns/assault_t1"; // Placeholder for new gun sprite

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement Berserk skill
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.ModifyReloadSpeed(0.15); // +15% Reload Speed
            player.ModifyBulletSpread(0.25); // +25% Bullet Spread
        }
    }
} 