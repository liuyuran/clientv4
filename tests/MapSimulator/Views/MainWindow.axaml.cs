using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DotnetNoise;
using MapSimulator.ViewModels;
using Vector = Avalonia.Vector;
using Window = Avalonia.Controls.Window;

namespace MapSimulator.Views;

public partial class MainWindow : Window {
    private readonly MainWindowViewModel _model = new();
    private readonly FastNoise _noise = new(123456789);

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
        return (int)(Math.Abs(_noise.GetValue(position.X, position.Y, position.Z)) * 100);
    }
    
    private void GenerateMap(int width, int height) {
        var heightMap = new int[width * height];
        for (var i = 0; i < heightMap.Length; i++) {
            var x = i / width;
            var y = i % width;
            var position = new Vector3(x, 0, y);
            heightMap[i] = (int)((float) GetHeightByPosition(position) / 100 * 255);
        }
        _model.cover = GenerateImageByHeightMap(heightMap, width, height);
    }
}