using System;
using System.Linq;
using game.scripts.manager.blocks;
using game.scripts.manager.item;
using game.scripts.manager.reset;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using ModLoader.logger;
using ModLoader.util;

namespace game.scripts.manager;

public class MaterialManager: IReset, IDisposable {
    private readonly ILogger _logger = LogManager.GetLogger<MaterialManager>();
    public static MaterialManager instance { get; private set; } = new();
    
    private readonly Shader _itemObjShader = ResourceLoader.Load<Shader>("res://shader/item-obj.gdshader");
    private readonly Shader _waterShader = ResourceLoader.Load<Shader>("res://shader/water.gdshader");
    
    private readonly Dictionary<ulong, Dictionary<Direction, Vector2[]>> _uvs = new();
    private readonly Dictionary<ulong, Dictionary<Direction, Vector2[]>> _itemUvs = new();
    private readonly Dictionary<ulong, Texture2D> _itemTextures = new();
    private Material _defaultMaterial;
    private ShaderMaterial _defaultWaterMaterial;
    private Material _defaultItemMaterial;
    private ShaderMaterial _defaultItemObjectMaterial;

    public void GenerateMaterials() {
        _uvs.Clear();
        _defaultMaterial = new StandardMaterial3D();
        GenerateBlockTexture();
        _logger.LogDebug("Default block material generated with texture: {Texture}", ((StandardMaterial3D)_defaultMaterial).AlbedoTexture);
        _defaultWaterMaterial = GenerateWaterShaderMaterial();
        _logger.LogDebug("Default water material generated with shader: {Shader}", _defaultWaterMaterial.Shader);
        _itemUvs.Clear();
        _itemTextures.Clear();
        _defaultItemMaterial = new StandardMaterial3D();
        GenerateItemTexture();
        _defaultItemObjectMaterial = GenerateItemObjectTexture();
        _logger.LogDebug("Default item material generated with texture: {Texture}", ((StandardMaterial3D)_defaultItemMaterial).AlbedoTexture);
    }
    
    private ShaderMaterial GenerateItemObjectTexture() {
        // Create shader material
        var material = new ShaderMaterial();
        // Define shader code with item object rendering
        material.Shader = _itemObjShader;
        material.SetShaderParameter("albedo_texture", ((StandardMaterial3D)_defaultItemMaterial).AlbedoTexture);
        material.SetShaderParameter("alpha_scissor_threshold", 0.9f);
        material.RenderPriority = -10; // 确保透明材质正确渲染
        return material;
    }

    private ShaderMaterial GenerateWaterShaderMaterial() {
        // Create shader material
        var material = new ShaderMaterial();

        // Define shader code with water animation
        material.Shader = _waterShader;
        
        // Create the first noise texture for waves
        var noiseTexture = new NoiseTexture2D();
        noiseTexture.Width = 512;
        noiseTexture.Height = 512;
        noiseTexture.Seamless = true;

        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = 0.04f;
        noise.FractalOctaves = 4;
        noise.FractalLacunarity = 2.0f;
        noise.FractalGain = 0.5f;
        noiseTexture.Noise = noise;

        // Create second noise texture for additional details
        var noiseTexture2 = new NoiseTexture2D();
        noiseTexture2.Width = 512;
        noiseTexture2.Height = 512;
        noiseTexture2.Seamless = true;

        var noise2 = new FastNoiseLite();
        noise2.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise2.Frequency = 0.06f;
        noise2.FractalOctaves = 3;
        noise2.FractalLacunarity = 2.5f;
        noise2.FractalGain = 0.4f;
        noiseTexture2.Noise = noise2;

        // Assign textures and parameters to material
        material.SetShaderParameter("noise_texture", noiseTexture);
        material.SetShaderParameter("noise_texture2", noiseTexture2);
        material.SetShaderParameter("water_color", new Color(1f, 1f, 1f, 0.7f));
        material.SetShaderParameter("deep_water_color", new Color(0.5f, 0.5f, 0.5f, 0.8f));
        material.SetShaderParameter("time_scale", 0.5f);
        material.SetShaderParameter("wave_strength", 0.15f);
        material.SetShaderParameter("wave_speed", 0.5f);
        material.SetShaderParameter("refraction", 0.05f);

        // Configure transparency
        material.RenderPriority = -10;

        return material;
    }

    private static Texture2D LoadFromDisk(string texturePath) {
        var path = ResourcePackManager.instance.GetFileAbsolutePath(texturePath);
        var image = Image.LoadFromFile(path);
        return ImageTexture.CreateFromImage(image);
    }

    private void GenerateBlockTexture() {
        var blockManager = BlockManager.instance;

        // 收集所有方块纹理
        var textures = new Dictionary<ulong, Texture2D>();
        foreach (var blockId in blockManager.GetBlockIds()) {
            var block = blockManager.GetBlock(blockId);
            var texturePath = block.texturePath;
            if (string.IsNullOrEmpty(texturePath)) {
                continue;
            }

            var texture = LoadFromDisk(texturePath);
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

    private void GenerateItemTexture() {
        var itemManager = ItemManager.instance;

        // 收集所有物品纹理
        foreach (var itemId in itemManager.GetItemIds()) {
            var item = itemManager.GetItem(itemId);
            var texturePath = item.iconPath;
            if (string.IsNullOrEmpty(texturePath)) {
                continue;
            }

            var texture = LoadFromDisk(texturePath);
            if (texture == null) {
                GD.PrintErr($"无法加载纹理: {texturePath}");
                continue;
            }

            _itemTextures[itemId] = texture;
        }

        if (_itemTextures.Count == 0) {
            GD.PrintErr("没有找到有效的纹理！");
            return;
        }

        // 创建纹理图集
        var atlasTexture = CreateItemTextureAtlas(_itemTextures, out var uvCoordinates);

        // 设置材质
        ((StandardMaterial3D)_defaultItemMaterial).AlbedoTexture = atlasTexture;

        // 存储各方块各面的UV坐标
        _itemUvs.Clear();
        foreach (var entry in uvCoordinates) {
            _itemUvs[entry.Key] = entry.Value;
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

    private static ImageTexture CreateItemTextureAtlas(Dictionary<ulong, Texture2D> textures, out Dictionary<ulong, Dictionary<Direction, Vector2[]>> uvCoordinates) {
        uvCoordinates = new Dictionary<ulong, Dictionary<Direction, Vector2[]>>();

        // 获取单个纹理的尺寸
        var firstTexture = textures.Values.First();
        var textureImage = firstTexture.GetImage();
        const int blockTextureWidth = 333;
        const int blockTextureHeight = 333;

        // 计算图集大小
        var atlasHeight = textures.Count * blockTextureHeight;

        // 创建图集
        var atlasImage = Image.CreateEmpty(blockTextureWidth, atlasHeight, false, textureImage.GetFormat());

        var faceIndex = 0;
        foreach (var (blockId, texture) in textures) {
            var image = texture.GetImage();
            image.Resize(blockTextureWidth, blockTextureHeight);
            var blockUVs = new Dictionary<Direction, Vector2[]>();

            // 图标无需计算位置
            const int srcX = 0;
            const int srcY = 0;

            // 计算当前面在图集中的位置
            const int destX = 0;
            var destY = faceIndex * blockTextureHeight;

            // 将面复制到图集
            atlasImage.BlitRect(image, new Rect2I(srcX, srcY, blockTextureWidth, blockTextureHeight), new Vector2I(destX, destY));

            // 计算UV坐标
            var uvs = CalculateUVs(destX, destY, blockTextureWidth, blockTextureHeight, blockTextureWidth, atlasHeight);
            blockUVs[Direction.South] = uvs;
            blockUVs[Direction.North] = uvs;
            faceIndex++;
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
    
    public Material GetWaterMaterial() {
        return _defaultWaterMaterial;
    }
    
    public Material GetItemObjectMaterial() {
        return _defaultItemObjectMaterial;
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

    public Material GetItemMaterial() {
        return _defaultItemMaterial;
    }
    
    public Texture2D GetItemTexture(ulong itemId) {
        if (!_itemTextures.TryGetValue(itemId, out var texture)) {
            throw new Exception($"ItemId {itemId} not found");
        }

        return texture;
    }

    public Vector2[] GetItemUVs(ulong itemId, Direction direction) {
        if (!_itemUvs.TryGetValue(itemId, out var uv)) {
            throw new Exception($"BlockId {itemId} not found");
        }

        if (!uv.ContainsKey(direction)) {
            throw new Exception($"Direction {direction} not found for BlockId {itemId}");
        }

        return _itemUvs[itemId][direction];
    }

    public void Reset() {
        instance = new MaterialManager();
        Dispose();
    }
    public void Dispose() {
        _uvs.Clear();
        _itemUvs.Clear();
        _defaultMaterial?.Dispose();
        _defaultWaterMaterial?.Dispose();
        _defaultItemMaterial?.Dispose();
        _defaultItemObjectMaterial?.Dispose();
        _defaultMaterial = null;
        _defaultWaterMaterial = null;
        _defaultItemMaterial = null;
        GC.SuppressFinalize(this);
    }
}