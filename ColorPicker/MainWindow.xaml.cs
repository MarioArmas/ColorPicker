using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace ColorPicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += MainWindow_KeyDown;
            this.Focusable = true;
            this.Focus(); // importante para que reciba teclas
        }

        private bool firstColorPicked = false;
        private bool secondColorPicked = false;
        private Color firstColor;
        private Color secondColor;
        private Color lastColor;

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private POINT GetMousePosition()
        {
            GetCursorPos(out POINT p);
            return p;
        }

        private Color GetColor()
        {
            var mousePos = GetMousePosition();
            int x = (int)mousePos.X;
            int y = (int)mousePos.Y;

            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);

            byte r = (byte)(pixel & 0x000000FF);
            byte g = (byte)((pixel & 0x0000FF00) >> 8);
            byte b = (byte)((pixel & 0x00FF0000) >> 16);

            var color = Color.FromRgb(r, g, b);
            return color;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                firstColor = GetColor();
                lastColor = firstColor;
                firstColorPicked = true;

                // Mostrar el color
                ColorFont.Fill = new SolidColorBrush(firstColor);
                ColorTextFont.Text = $"RGB: ({firstColor.R}, {firstColor.G}, {firstColor.B})";

            }

            if (e.Key == Key.F3)
            {
                secondColor = GetColor();
                lastColor = secondColor;
                secondColorPicked = true;

                // Mostrar el color
                ColorBack.Fill = new SolidColorBrush(secondColor);
                ColorTextBack.Text = $"RGB: ({secondColor.R}, {secondColor.G}, {secondColor.B})";
            }

            if (firstColorPicked && secondColorPicked)
            {
                ColorRatio.Text = $"Color ratio: {GetContrastRatio(firstColor, secondColor)}";
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                // copy to clipboard
                var hexColor = GetHexColor(lastColor);
                Clipboard.SetText(hexColor);
            }
        }

        private string GetHexColor(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;

            string hex = $"#{r:X2}{g:X2}{b:X2}";
            return hex;
        }

        // Color ratio
        double GetLuminance(byte r, byte g, byte b)
        {
            double RsRGB = r / 255.0;
            double GsRGB = g / 255.0;
            double BsRGB = b / 255.0;

            double Rlin = (RsRGB <= 0.03928) ? RsRGB / 12.92 : Math.Pow((RsRGB + 0.055) / 1.055, 2.4);
            double Glin = (GsRGB <= 0.03928) ? GsRGB / 12.92 : Math.Pow((GsRGB + 0.055) / 1.055, 2.4);
            double Blin = (BsRGB <= 0.03928) ? BsRGB / 12.92 : Math.Pow((BsRGB + 0.055) / 1.055, 2.4);

            return 0.2126 * Rlin + 0.7152 * Glin + 0.0722 * Blin;
        }

        double GetContrastRatio(Color color1, Color color2)
        {
            double L1 = GetLuminance(color1.R, color1.G, color1.B);
            double L2 = GetLuminance(color2.R, color2.G, color2.B);

            // asegurarse de que L1 sea el más brillante
            if (L1 < L2)
            {
                double temp = L1;
                L1 = L2;
                L2 = temp;
            }

            return (L1 + 0.05) / (L2 + 0.05);
        }
    }
}
