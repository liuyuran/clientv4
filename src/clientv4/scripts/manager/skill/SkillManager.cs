using System;
using System.Collections.Generic;
using game.scripts.manager.reset;
using ModLoader.archive;
using ModLoader.handler;

namespace game.scripts.manager.skill;

public class SkillManager: IReset, ISkillManager, IArchive, IDisposable {
    public static SkillManager instance { get; private set; } = new();
    
    public void Reset() {
        instance = new SkillManager();
        Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        //
    }
    public void Recover(Func<string, byte[]> getDataFunc) {
        //
    }
}