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

        public abstract void ApplyTemporarySkill(Player player);
        
        public abstract void ApplyPassiveBonuses(Player player);
    }
} 