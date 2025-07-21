using System;
using System.Collections.Generic;
using System.Linq;
using game.scripts.manager.blocks;
using Godot;
using JetBrains.Annotations;

namespace game.unittest.manager;

[UsedImplicitly]
public class BlockManagerUnitTest: IUnitTest {
    private class TestBlock : Block {
        public override string name => "TestBlock";
    }
    
    public void RunTest(Node node, Action<string> log) {
        try {
            BlockManager.instance.Register<TestBlock>();
            var blockIds = BlockManager.instance.GetBlockIds().ToList();
            if (blockIds.Count != 4) {
                log($"BlockManagerUnitTest failed: expected 3 blocks, got {blockIds.Count}");
                return;
            }
            if (BlockManager.instance.GetBlockId<TestBlock>() != 4) {
                log("BlockManagerUnitTest failed: TestBlock block ID is not 4");
                return;
            }
            BlockManager.instance.Reset();
            BlockManager.instance.Register<TestBlock>();
            blockIds = BlockManager.instance.GetBlockIds().ToList();
            if (blockIds.Count != 4) {
                log("BlockManagerUnitTest failed: TestBlock block ID is not 4 after reset");
                return;
            }
            var fileBuffer = new Dictionary<string, byte[]>();
            BlockManager.instance.Archive(fileBuffer);
            BlockManager.instance.Reset();
            BlockManager.instance.Recover(s => fileBuffer[s]);
            blockIds = BlockManager.instance.GetBlockIds().ToList();
            if (blockIds.Count != 4) {
                log($"BlockManagerUnitTest failed: expected 4 blocks, got {blockIds.Count}");
                return;
            }
            if (BlockManager.instance.GetBlockId<TestBlock>() != 4) {
                log("BlockManagerUnitTest failed: TestBlock block ID is not 4");
                return;
            }
            log("BlockManagerUnitTest passed");
        } catch (Exception e) {
            log($"BlockManagerUnitTest failed: {e.Message}");
        }
    }
    
    public void Cleanup(Node node, Action<string> log) {
        // no need to clean up for BlockManager
    }
}