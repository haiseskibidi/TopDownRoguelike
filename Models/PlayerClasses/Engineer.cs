using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public class Engineer : PlayerClass
    {
        public override PlayerClassType ClassType => PlayerClassType.Engineer;
        public override string Name => "Инженер";
        public override string Description => "Класс поддержки, способный выживать в бою за счет ремонта и усиливать себя с помощью технологий.";
        public override string SkillName => "Ремонт";
        public override string SkillDescription => "Мгновенно восстанавливает часть прочности и временно увеличивает регенерацию.";
        public override string GunSpriteName => "guns/engineer_t1"; 
        public override double GunWidth => 25;
        public override double GunHeight => 11.25;
        public override double FireRate => 1.2;
        public override double Damage => 8;
        public override double BulletSpeed => 250;
        public override double Spread => 0.05;
        public override double BulletSize => 14; // Gears
        public override string BulletSpriteName => "gear";

        public override bool CanRicochet => true;
        public override int MaxRicochets => 5;

        public override void ApplyTemporarySkill(Player player)
        {
            // TODO: Implement the "Repair" skill
            // This will likely involve giving the player a temporary health regeneration boost.
            System.Console.WriteLine("Applying Engineer skill: Repair!");
        }

        public override void ApplyPassiveBonuses(Player player)
        {
            player.ModifyHealthRegen(0.5); // +0.5 Health Regen / sec
            player.ModifyBulletDamage(-0.10); // -10% Bullet Damage
        }
    }
} 