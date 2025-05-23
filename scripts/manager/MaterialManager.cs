using System;
using System.Linq;
using game.scripts.utils;
using Godot;
using Godot.Collections;

namespace game.scripts.manager;

public class MaterialManager {
    public static MaterialManager instance { get; private set; } = new();
    private Dictionary<ulong, Dictionary<Direction, Vector2[]>> _uvs = new();
    private Material _defaultMaterial;

    public void GenerateMaterials() {
        _defaultMaterial = new StandardMaterial3D();
        var blockManager = BlockManager.instance;

        // 收集所有方块纹理
        var textures = new Dictionary<ulong, Texture2D>();
        foreach (var blockId in blockManager.GetBlockIds()) {
            var block = blockManager.GetBlock(blockId);
            var texturePath = block.TexturePath;
            if (string.IsNullOrEmpty(texturePath)) {
                continue;
            }

            var image = Image.LoadFromFile(ResourcePackManager.instance.GetFileAbsolutePath(texturePath));
            var texture = ImageTexture.CreateFromImage(image);
            if (texture == null) {
                GD.PrintErr($"无法加载纹理: {texturePath}");
                continue;
            }

            textures[blockId] = texture;
        }

        if (textures.Count == 0) {
            GD.PrintErr("没有找到有效的纹理！");
            return;
        }

        // 创建纹理图集
        var atlasTexture = CreateTextureAtlas(textures, out var uvCoordinates);

        // 设置材质
        ((StandardMaterial3D)_defaultMaterial).AlbedoTexture = atlasTexture;

        // 存储各方块各面的UV坐标
        _uvs.Clear();
        foreach (var entry in uvCoordinates) {
            _uvs[entry.Key] = entry.Value;
        }
        
        // 将atlasTexture保存到硬盘
        // const string atlasPath = "D://atlas_texture.png";
        // var atlasImage = atlasTexture.GetImage();
        // atlasImage.SavePng(atlasPath);
    }

    private static ImageTexture CreateTextureAtlas(Dictionary<ulong, Texture2D> textures, out Dictionary<ulong, Dictionary<Direction, Vector2[]>> uvCoordinates) {
        uvCoordinates = new Dictionary<ulong, Dictionary<Direction, Vector2[]>>();

        // 获取单个纹理的尺寸
        var firstTexture = textures.Values.First();
        var textureImage = firstTexture.GetImage();
        const int blockTextureWidth = 999;
        const int blockTextureHeight = 666;

        // 每个方块纹理是2行3列的布局
        const int faceWidth = blockTextureWidth / 3;
        const int faceHeight = blockTextureHeight / 2;
        
        // 计算图集大小
        const int facesPerRow = 4;
        var range = textures.Count * 6;
        const int atlasWidth = faceWidth * facesPerRow;
        var atlasLine = (int)Math.Ceiling((double)range / facesPerRow);
        if (atlasLine == 0) atlasLine = 1;
        if (atlasLine % 2 > 0) atlasLine++;
        var atlasHeight = atlasLine * faceHeight;

        // 创建图集
        var atlasImage = Image.CreateEmpty(atlasWidth, atlasHeight, false, textureImage.GetFormat());

        var faceIndex = 0;
        foreach (var (blockId, texture) in textures) {
            var image = texture.GetImage();
            image.Resize(blockTextureWidth, blockTextureHeight);

            // 处理当前方块的六个面
            var blockUVs = new Dictionary<Direction, Vector2[]>();

            // 面的顺序：下、西、东、南、上、北
            Direction[] directions = [
                Direction.Down, Direction.West, Direction.East,
                Direction.South, Direction.Up, Direction.North
            ];

            for (var i = 0; i < 6; i++) {
                // 计算当前面在原始纹理中的位置
                var srcX = i % 3 * faceWidth;
                var srcY = i / 3 * faceHeight;

                // 计算当前面在图集中的位置
                var destX = faceIndex % facesPerRow * faceWidth;
                var destY = faceIndex / facesPerRow * faceHeight;

                // 将面复制到图集
                atlasImage.BlitRect(image, new Rect2I(srcX, srcY, faceWidth, faceHeight), new Vector2I(destX, destY));

                // 计算UV坐标
                var uvs = CalculateUVs(destX, destY, faceWidth, faceHeight, atlasWidth, atlasHeight);
                blockUVs[directions[i]] = uvs;

                faceIndex++;
            }

            uvCoordinates[blockId] = blockUVs;
        }

        // 创建图集纹理
        var atlasTexture = ImageTexture.CreateFromImage(atlasImage);
        return atlasTexture;
    }

    private static Vector2[] CalculateUVs(int x, int y, int width, int height, int atlasWidth, int atlasHeight) {
        // 计算UV坐标（归一化到0-1范围）
        var u0 = (float)x / atlasWidth;
        var v0 = (float)y / atlasHeight;
        var u1 = (float)(x + width) / atlasWidth;
        var v1 = (float)(y + height) / atlasHeight;

        // 返回四个顶点的UV坐标（顺时针方向）
        return [
            new Vector2(u0, v0), // 左上
            new Vector2(u1, v0), // 右上
            new Vector2(u1, v1), // 右下
            new Vector2(u0, v1) // 左下
        ];
    }

    public Material GetMaterial() {
        return _defaultMaterial;
    }

    public Vector2[] GetUVs(ulong blockId, Direction direction) {
        if (!_uvs.TryGetValue(blockId, out var uv)) {
            throw new Exception($"BlockId {blockId} not found");
        }

        if (!uv.ContainsKey(direction)) {
            throw new Exception($"Direction {direction} not found for BlockId {blockId}");
        }

        return _uvs[blockId][direction];
    }
}