using System;
using System.Collections.Generic;

namespace game.scripts.manager.archive;

public interface IArchive {
    public void Archive(Dictionary<string, byte[]> fileList);

    public void Recover(Func<string, byte[]> getDataFunc);
}