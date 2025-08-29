namespace ModLoader.skill;

/// <summary>
/// a skill that will execute some code after use.
/// </summary>
public abstract class Skill {
    public virtual string name => throw new System.NotImplementedException();
    public virtual string iconPath => throw new System.NotImplementedException();
    public abstract void Execute(ulong playerId);
}