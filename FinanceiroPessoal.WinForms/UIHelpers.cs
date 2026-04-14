using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Text;

namespace FinanceiroPessoal.WinForms
{
    public static class UIHelpers
    {
        public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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

        public static void ApplyRoundedRegion(Control control, int radius)
        {
            using var path = CreateRoundedPath(control.ClientRectangle, radius);
            control.Region = new Region(path);
        }
    }
}
