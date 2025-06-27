using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using game.scripts.config;
using game.scripts.manager;
using game.scripts.manager.blocks;
using game.scripts.manager.blocks.util;
using game.scripts.manager.map;
using game.scripts.utils;
using Godot;
using MapSimulator.ViewModels;
using Window = Avalonia.Controls.Window;

namespace MapSimulator.Views;

public partial class MainWindow : Window {
    private readonly MainWindowViewModel _model = new();

    public MainWindow() {
        InitializeComponent();
    }
    
    private void Window_OnOpened(object? sender, EventArgs e) {
        GenerateMap(500, 500);
        this.DataContext = _model;
    }

    private static unsafe void SetPixel(ref WriteableBitmap bmp, int x, int y, byte r, byte g, byte b, byte? a = null) {
        using var lockedBitmap = bmp.Lock();
        // get a pointer to beginning bitmap
        var bmpPtr = (byte*)lockedBitmap.Address;
        // set stride depending on alpha presence 
        var stride = a.HasValue ? 4 : 3;
        // find offset to the beginning of pixel
        var offset = stride * (bmp.PixelSize.Width * y + x);
        // set each channel of the pixel
        *(bmpPtr + offset + 0) = b;
        *(bmpPtr + offset + 1) = g;
        *(bmpPtr + offset + 2) = r;
        if (a.HasValue) {
            *(bmpPtr + offset + 3) = a.Value;
        }
    }

    private static WriteableBitmap GenerateImageByHeightMap(int[] heightMap, int width, int height) {
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        for (var i = 0; i < width; i++) {
            for (var j = 0; j < height; j++) {
                var pixel = (byte)heightMap[i * width + j];
                SetPixel(ref writeableBitmap, i, j, pixel, pixel, pixel, 255);
            }
        }
        return writeableBitmap;
    }

    private int GetHeightByPosition(Vector3 position) {
        var chunkPosition = position.ToChunkPosition();
        var chunkMap = MapManager.instance.GetBlockData(0, chunkPosition);
        if (chunkMap == null) return 0;
        var localPosition = position.ToLocalPosition();
        for (var x = 0; x < Config.ChunkSize; x++) {
            for (var z = 0; z < Config.ChunkSize; z++) {
                for (var y = 0; y < Config.ChunkSize; y++) {
                    if (localPosition.X != x || localPosition.Z != z) continue;
                    var block = chunkMap[x][y][z].BlockId;
                    if (block == 0) {
                        return y;
                    }
                    var blockInfo = BlockManager.instance.GetBlock(block);
                    if (blockInfo.blockType != EBlockType.Solid || y == Config.ChunkSize - 1) {
                        return y;
                    }
                }
            }
        }
        return 0;
    }
    
    private void GenerateMap(int width, int height) {
        var heightMap = new int[width * height];
        for (var i = 0; i < heightMap.Length; i++) {
            var x = i / width;
            var y = i % width;
            var position = new Vector3(x, 0, y);
            heightMap[i] = (int)((float) GetHeightByPosition(position) / Config.ChunkSize * 255);
        }
        _model.cover = GenerateImageByHeightMap(heightMap, width, height);
    }
}