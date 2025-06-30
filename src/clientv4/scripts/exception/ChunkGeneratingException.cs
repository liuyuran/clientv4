namespace game.scripts.exception;

/// <summary>
/// used to make chunk render wait until chunk is generating.
/// </summary>
public class ChunkGeneratingException() : BaseException("Chunk is generating, please wait until it is finished.");