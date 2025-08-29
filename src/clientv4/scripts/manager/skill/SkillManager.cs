using System;
using System.Collections.Generic;
using System.Linq;
using game.scripts.manager.reset;
using Microsoft.Extensions.Logging;
using ModLoader.archive;
using ModLoader.handler;
using ModLoader.logger;
using ModLoader.skill;

namespace game.scripts.manager.skill;

public class SkillManager: IReset, ISkillManager, IDisposable {
    private readonly ILogger _logger = LogManager.GetLogger<SkillManager>();
    public static SkillManager instance { get; private set; } = new();
    private readonly Dictionary<string, Skill> _skills = new();
    
    public void Register<T>() where T : Skill, new() {
        var skill = new T();
        _skills.TryAdd(skill.name, skill);
    }

    public Skill GetSkill(string name) {
        return _skills.GetValueOrDefault(name);
    }
    
    public Skill[] GetSkills() {
        return _skills.Values.ToArray();
    }

    public void ExecuteSkill(ulong playerId, string skillName) {
        if (!_skills.TryGetValue(skillName, out var skill)) {
            _logger.LogWarning("Skill with name '{Name}' not found.", skillName);
            return;
        }
        skill.Execute(playerId);
    }

    public void Reset() {
        instance = new SkillManager();
        Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}