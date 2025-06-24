using GunVault.Models;

namespace GunVault.Models.PlayerClasses
{
    public abstract class PlayerClass
    {
        public abstract PlayerClassType ClassType { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }
        public abstract string GunSpriteName { get; }
        public abstract double GunWidth { get; }
        public abstract double GunHeight { get; }
        public abstract double FireRate { get; }
        public abstract double Damage { get; }
        public abstract double BulletSpeed { get; }
        public abstract double Spread { get; }
        public abstract double BulletSize { get; }
        public abstract string BulletSpriteName { get; }
        public abstract bool CanRicochet { get; }
        public abstract int MaxRicochets { get; }

        public abstract void ApplyTemporarySkill(Player player);
        
        public abstract void ApplyPassiveBonuses(Player player);
    }
} 