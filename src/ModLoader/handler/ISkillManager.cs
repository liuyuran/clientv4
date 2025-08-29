using ModLoader.skill;

namespace ModLoader.handler;

public interface ISkillManager {
    public void Register<T>() where T : Skill, new();
    public Skill GetSkill(string name);
    public Skill[] GetSkills();
    public void ExecuteSkill(ulong playerId, string skillName);
}