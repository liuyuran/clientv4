using Godot;
using System;
using System.Collections.Generic;

namespace game.scripts.start.background;

public partial class SinglePlayerBackground : Panel
{
    public override void _Draw()
    {
        var componentSize = GetTree().Root.GetSize();
        const float margin = 100.0f;
        const float cornerRadius = 15.0f; // 内凹圆角半径
        var color = new Color(0.2f, 0.2f, 0.2f, 1); // 深灰色

        // 1. 左侧矩形
        var leftRectWidth = 250.0f;
        var leftRectHeight = componentSize.Y - 2 * margin;
        if (leftRectHeight > 2 * cornerRadius)
        {
            var leftRect = new Rect2(margin, margin, leftRectWidth, leftRectHeight);
            DrawInvertedRoundRect(leftRect, cornerRadius, color);
        }

        // 右侧区域的起始 X 坐标和可用宽度
        var rightAreaX = margin + leftRectWidth + margin;
        var rightAreaWidth = componentSize.X - rightAreaX - margin;

        if (rightAreaWidth > 2 * cornerRadius)
        {
            // 2. 右下矩形
            var rightBottomRectHeight = 30.0f;
            var rightBottomRectY = componentSize.Y - margin - rightBottomRectHeight;
            if (rightBottomRectY > margin && rightBottomRectHeight >= 2 * cornerRadius)
            {
                var rightBottomRect = new Rect2(rightAreaX, rightBottomRectY, rightAreaWidth, rightBottomRectHeight);
                DrawInvertedRoundRect(rightBottomRect, cornerRadius, color);

                // 3. 右上矩形
                var rightTopRectHeight = rightBottomRectY - margin - margin; // 高度为剩余空间
                if (rightTopRectHeight > 2 * cornerRadius)
                {
                    var rightTopRect = new Rect2(rightAreaX, margin, rightAreaWidth, rightTopRectHeight);
                    DrawInvertedRoundRect(rightTopRect, cornerRadius, color);
                }
            }
        }
    }

    /// <summary>
    /// 绘制一个四角为内凹圆角的矩形。
    /// </summary>
    /// <param name="rect">矩形的位置和大小</param>
    /// <param name="radius">内凹圆角的半径</param>
    /// <param name="color">颜色</param>
    private void DrawInvertedRoundRect(Rect2 rect, float radius, Color color)
    {
        // 确保半径不会过大
        radius = (float)Mathf.Min(radius, Mathf.Min(rect.Size.X / 2, rect.Size.Y / 2));
        if (radius <= 0)
        {
            DrawRect(rect, color);
            return;
        }

        // 定义构成形状的三个主要矩形部分
        var horizontalRect = new Rect2(rect.Position.X, rect.Position.Y + radius, rect.Size.X, rect.Size.Y - 2 * radius);
        var verticalRect = new Rect2(rect.Position.X + radius, rect.Position.Y, rect.Size.X - 2 * radius, rect.Size.Y);

        // 绘制这三个矩形来形成一个中间带孔的十字形状
        DrawRect(horizontalRect, color);
        DrawRect(verticalRect, color);

        // 定义四个角的圆心
        var topLeftCenter = rect.Position + new Vector2(radius, radius);
        var topRightCenter = rect.Position + new Vector2(rect.Size.X - radius, radius);
        var bottomLeftCenter = rect.Position + new Vector2(radius, rect.Size.Y - radius);
        var bottomRightCenter = rect.Position + new Vector2(rect.Size.X - radius, rect.Size.Y - radius);

        // 使用 DrawArc 填充四个角
        const int pointCount = 32; // 弧线的平滑度
        DrawArc(topLeftCenter, radius, Mathf.Pi, Mathf.Pi * 1.5f, pointCount, color, 1, true);
        DrawArc(topRightCenter, radius, Mathf.Pi * 1.5f, Mathf.Pi * 2f, pointCount, color, 1, true);
        DrawArc(bottomLeftCenter, radius, Mathf.Pi * 0.5f, Mathf.Pi, pointCount, color, 1, true);
        DrawArc(bottomRightCenter, radius, 0, Mathf.Pi * 0.5f, pointCount, color, 1, true);
    }
}