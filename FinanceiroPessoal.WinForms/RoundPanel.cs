using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;

namespace FinanceiroPessoal.WinForms
{
    public class RoundPanel : Panel
    {
        public int BorderRadius { get; set; } = 18;
        public int BorderThickness { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.FromArgb(229, 231, 235);

        public RoundPanel()
        {
            DoubleBuffered = true;
            Resize += (s, e) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using var path = GetRoundedPath(rect, BorderRadius);
            using var pen = new Pen(BorderColor, BorderThickness);

            Region = new Region(path);
            e.Graphics.DrawPath(pen, path);
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
