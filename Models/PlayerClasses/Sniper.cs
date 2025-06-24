using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Sniper : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Sniper;
        public override string Name => "Снайпер";
        public override string Description => "Специалист по дальнему бою, наносящий огромный урон одиночными точными выстрелами.";
        public override string SkillName => "Прицельный выстрел";
        public override string SkillDescription => "Следующий выстрел нанесет гарантированный критический урон.";
        public override string GunSpriteName => "guns/sniper_t1"; 
        public override double GunWidth => 37.5;
        public override double GunHeight => 8.75;
        public override double FireRate => 0.9;
        public override double Damage => 75;
        public override double BulletSpeed => 800;
        public override double Spread => 0.01;
        public override double BulletSize => 8; 
        public override string BulletSpriteName => "";

        public override bool CanRicochet => false;
        public override int MaxRicochets => 0;

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement Aimed Shot skill
            System.Console.WriteLine("Applying Sniper skill: Aimed Shot!");
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.ModifyBulletDamage(0.20); // +20% Bullet Damage
            player.ReduceMaxHealth(25); // -25 Max Health
        }
    }
} 