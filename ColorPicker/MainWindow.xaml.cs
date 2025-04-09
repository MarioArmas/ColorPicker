using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;

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

            // Asignar segundo color
            var color = Color.FromRgb(0, 0, 0);
            
            // Mostrar el color
            ColorBack.Fill = new SolidColorBrush(color);
            ColorBackName.Foreground = new SolidColorBrush(FixFontColor(secondColor));
            ColorBackNameHex.Foreground = new SolidColorBrush(FixFontColor(secondColor));
            ColorTextBack.Foreground = new SolidColorBrush(FixFontColor(secondColor));
        }

        private bool firstColorPicked = false;
        private bool secondColorPicked = false;
        private Color firstColor;
        private Color secondColor;

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
                firstColorPicked = true;

                // Mostrar el color
                ColorFont.Fill = new SolidColorBrush(firstColor);
                ColorTextFont.Text = $"RGB: ({firstColor.R}, {firstColor.G}, {firstColor.B})";
                ColorFontNameHex.Text = $"{GetHexColor(firstColor)}";
                ColorFontName.Foreground = new SolidColorBrush(FixFontColor(firstColor));
                ColorFontNameHex.Foreground = new SolidColorBrush(FixFontColor(firstColor));
                ColorTextFont.Foreground = new SolidColorBrush(FixFontColor(firstColor));
            }

            if (e.Key == Key.F3)
            {
                secondColor = GetColor();
                secondColorPicked = true;

                // Mostrar el color
                ColorBack.Fill = new SolidColorBrush(secondColor);
                ColorTextBack.Text = $"RGB: ({secondColor.R}, {secondColor.G}, {secondColor.B})";
                ColorBackNameHex.Text = $"{GetHexColor(secondColor)}";
                ColorBackName.Foreground = new SolidColorBrush(FixFontColor(secondColor));
                ColorBackNameHex.Foreground = new SolidColorBrush(FixFontColor(secondColor));
                ColorTextBack.Foreground = new SolidColorBrush(FixFontColor(secondColor));
            }

            if (firstColorPicked && secondColorPicked)
            {
                var ratio = GetContrastRatio(firstColor, secondColor);
                var level = GetContrastLevel(ratio);
                ColorRatio.Text = $"Contrast ratio: {ratio:0.00} {level}";
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F2)
                {
                    // copy to clipboard
                    var hexColor = GetHexColor(firstColor);
                    Clipboard.SetText(hexColor);
                    CopyColorToClipboardFont();
                }

                if (e.Key == Key.F3)
                {
                    // copy to clipboard
                    var hexColor = GetHexColor(secondColor);
                    Clipboard.SetText(hexColor);
                    CopyColorToClipboardBack();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void CopyColorToClipboardFont()
        {
            Color color = FixFontColor(firstColor);
            ClipboardFont.Foreground = new SolidColorBrush(color);
            ClipboardFont.Visibility = Visibility.Visible;

            await Task.Delay(2200);
            ClipboardFont.Visibility = Visibility.Collapsed;
        }

        private async void CopyColorToClipboardBack()
        {
            Color color = FixFontColor(secondColor);
            ClipboardBack.Foreground = new SolidColorBrush(color);
            ClipboardBack.Visibility = Visibility.Visible;

            await Task.Delay(2200);
            ClipboardBack.Visibility = Visibility.Collapsed;
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

        string GetContrastLevel(double ratio)
        {
            if (ratio > 7) return "Excellent AAA";
            if (ratio > 4.5) return "Acceptable AA";
            return "⚠️ Low";
        }

        // Font color
        private Color FixFontColor(Color color)
        {
            Color black = Color.FromRgb(0, 0, 0);
            Color white = Color.FromRgb(255, 255, 255);

            double contrastWhite = GetContrastRatio(color, white);
            double contrastBlack = GetContrastRatio(color, black);

            if (contrastWhite >= contrastBlack)
            {
                return white;
            }

            return black;
        }
    }
}
