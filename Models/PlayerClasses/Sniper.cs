using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Sniper : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Sniper;
        public override string Name => "Снайпер";
        public override string Description => "Специалист по дальнему бою, наносящий огромный урон одиночными точными выстрелами.";
        public override string SkillName => "Точный выстрел";
        public override string SkillDescription => "Следующие 3 выстрела гарантированно наносят критический урон.";
        public override string GunSpriteName => "guns/sniper_t1"; // Placeholder

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement the "Precise Shot" skill
            // This will likely involve giving the player a temporary status that makes the next N bullets critical.
            System.Console.WriteLine("Applying Sniper skill: Precise Shot!");
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.ModifyBulletDamage(0.20); // +20% Bullet Damage
            player.ReduceMaxHealth(25); // -25 Max Health
        }
    }
} 