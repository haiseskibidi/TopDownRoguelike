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
        public override string GunSpriteName => "guns/assault_t1";
        public override double GunWidth => 23.75;
        public override double GunHeight => 17.5;
        public override double FireRate => 3;
        public override double Damage => 12;
        public override double BulletSpeed => 150;
        public override double Spread => 0.15;
        public override double BulletSize => 6; // Standard bullets
        public override string BulletSpriteName => "";

        public override bool CanRicochet => false;
        public override int MaxRicochets => 0;

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