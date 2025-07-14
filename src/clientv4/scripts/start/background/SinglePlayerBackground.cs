using Godot;

namespace game.scripts.start.background;

public partial class SinglePlayerBackground : Panel {
    public override void _Draw() {
        var componentSize = GetTree().Root.GetSize();
        const float margin = 100.0f;
        const float cornerRadius = 15.0f; // 内凹圆角半径
        var color = new Color(0.2f, 0.2f, 0.2f); // 深灰色

        // 1. 左侧矩形
        var leftRectWidth = 250.0f;
        var leftRectHeight = componentSize.Y - 2 * margin;
        if (leftRectHeight > 2 * cornerRadius) {
            var leftRect = new Rect2(margin, margin, leftRectWidth, leftRectHeight);
            DrawInvertedRoundRect(leftRect, cornerRadius, color);
        }

        // 右侧区域的起始 X 坐标和可用宽度
        var rightAreaX = margin + leftRectWidth + margin;
        var rightAreaWidth = componentSize.X - rightAreaX - margin;

        if (rightAreaWidth > 2 * cornerRadius) {
            // 2. 右下矩形
            const float rightBottomRectHeight = 30.0f;
            var rightBottomRectY = componentSize.Y - margin - rightBottomRectHeight;
            if (rightBottomRectY > margin && rightBottomRectHeight >= 2 * cornerRadius) {
                var rightBottomRect = new Rect2(rightAreaX, rightBottomRectY, rightAreaWidth, rightBottomRectHeight);
                DrawInvertedRoundRect(rightBottomRect, cornerRadius, color);

                // 3. 右上矩形
                var rightTopRectHeight = rightBottomRectY - margin - margin; // 高度为剩余空间
                if (rightTopRectHeight > 2 * cornerRadius) {
                    var rightTopRect = new Rect2(rightAreaX, margin, rightAreaWidth, rightTopRectHeight);
                    DrawInvertedRoundRect(rightTopRect, cornerRadius, color);
                }
            }
        }
    }

    /// <summary>
        /// 绘制一个或多个空心的、四角为内凹圆角的同心矩形。
        /// </summary>
        /// <param name="rect">最外层矩形的位置和大小</param>
        /// <param name="radius">最外层矩形的内凹圆角半径</param>
        /// <param name="color">边框的颜色</param>
        /// <param name="borderCount">要绘制的边框总数。例如，2会绘制一个外边框和一个内边框。</param>
        /// <param name="width">每个边框的宽度</param>
        private void DrawInvertedRoundRect(Rect2 rect, float radius, Color color, int borderCount = 1, float width = 1.0f)
        {
            const float borderSpacing = 10.0f;
    
            for (var i = 0; i < borderCount; i++)
            {
                var offset = i * borderSpacing;
                var currentRect = new Rect2(
                    rect.Position + new Vector2(offset, offset),
                    rect.Size - new Vector2(offset * 2, offset * 2)
                );
    
                // 如果矩形太小无法绘制，则停止
                if (currentRect.Size.X <= 0 || currentRect.Size.Y <= 0)
                {
                    break;
                }
    
                // 按比例缩放半径以保持形状
                var scaleFactor = (float)(rect.Size.X > 0 ? currentRect.Size.X / rect.Size.X : 0);
                var currentRadius = radius * scaleFactor;
    
                // 确保半径不会过大
                currentRadius = (float)Mathf.Min(currentRadius, Mathf.Min(currentRect.Size.X / 2, currentRect.Size.Y / 2));
                if (currentRadius <= 0)
                {
                    DrawRect(currentRect, color, false, width);
                    continue; // 继续绘制下一个可能的矩形（如果它有效）
                }
    
                var p1 = currentRect.Position;
                var p2 = new Vector2(currentRect.Position.X + currentRect.Size.X, currentRect.Position.Y);
                var p3 = new Vector2(currentRect.Position.X + currentRect.Size.X, currentRect.Position.Y + currentRect.Size.Y);
                var p4 = new Vector2(currentRect.Position.X, currentRect.Position.Y + currentRect.Size.Y);
    
                // 绘制四条直线边框
                DrawLine(p1 + new Vector2(currentRadius, 0), p2 - new Vector2(currentRadius, 0), color, width); // Top
                DrawLine(p2 + new Vector2(0, currentRadius), p3 - new Vector2(0, currentRadius), color, width); // Right
                DrawLine(p4 + new Vector2(currentRadius, 0), p3 - new Vector2(currentRadius, 0), color, width); // Bottom
                DrawLine(p1 + new Vector2(0, currentRadius), p4 - new Vector2(0, currentRadius), color, width); // Left

                // 使用 DrawArc 绘制四个内凹圆角
                const int pointCount = 32; // 弧线的平滑度
                DrawArc(p1, currentRadius, 0, Mathf.Pi * 0.5f, pointCount, color, width);
                DrawArc(p2, currentRadius, Mathf.Pi * 0.5f, Mathf.Pi, pointCount, color, width);
                DrawArc(p4, currentRadius, Mathf.Pi * 1.5f, Mathf.Pi * 2f, pointCount, color, width);
                DrawArc(p3, currentRadius, Mathf.Pi, Mathf.Pi * 1.5f, pointCount, color, width);
            }
        }
}