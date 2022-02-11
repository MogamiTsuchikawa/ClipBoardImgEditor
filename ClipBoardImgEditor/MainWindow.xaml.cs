using System;
using System.Collections.Generic;
using System.IO;
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

namespace ClipBoardImgEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        List<DrawData> drawDatas = new List<DrawData>();
        DrawData? editDrawData;
        ImageSource originImage;
        Color drawColor = Colors.Red;
        public CopyFromCBC CopyFromCBC { get; set; }
        public CopyToCBC CopyToCBC { get; set; }
        public RemoveLastDrawDataC RemoveLastDrawDataC { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            CopyToCBC = new();
            CopyFromCBC = new();
            RemoveLastDrawDataC = new();
            DataContext = Instance;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var targetBitmapImg = Clipboard.GetImage();
            if (targetBitmapImg == null) return;
            EditImage.Source = targetBitmapImg;
            using (System.IO.FileStream stream = new System.IO.FileStream("origin.png", FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(targetBitmapImg));
                encoder.Save(stream);
            }
            originImage = new BitmapImage(new Uri("origin.png", UriKind.Relative));
            ChangeDrawColorBtn.Background = new SolidColorBrush(drawColor);
        }

        private void DrawSquareBtn_Click(object sender, RoutedEventArgs e)
        {
            editDrawData = new(Colors.Red, 3);
            DrawSquareBtn.IsEnabled = false;
        }

        private void EditImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(editDrawData == null) return;
            editDrawData.startPos = Mouse.GetPosition(EditImage);

        }

        private void EditImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(editDrawData == null) return;
            editDrawData.endPos = Mouse.GetPosition(EditImage);
            EditImage.Source = DrawRegion(editDrawData);
            drawDatas.Add(editDrawData);
            editDrawData = null;
            DrawSquareBtn.IsEnabled = true;
        }
        private Point GetPixelPointWithPoint(Point mousePoint)
        {
            return new Point(mousePoint.X / EditImage.ActualWidth * originImage.Width, mousePoint.Y / EditImage.ActualWidth * originImage.Width);
        }
        private DrawingImage DrawRegion(DrawData drawData)
        {
            if(drawData == null || drawData.startPos == null || drawData.endPos == null) return null;
            DrawingGroup drawingGroup = new DrawingGroup();
            using (DrawingContext drawContent = drawingGroup.Open())
            {
                // 画像を書いて、その上にテキストを書く
                drawContent.DrawImage(originImage, new System.Windows.Rect(0, 0, originImage.Width, originImage.Height));
            }
            using (DrawingContext drawContent = drawingGroup.Append())
            {
                // 追加でいろんなものを書き込む
                foreach(var dd in drawDatas)
                {
                    drawContent.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), new Pen(new SolidColorBrush(dd.drawColor), dd.width), new Rect(GetPixelPointWithPoint((Point)dd.startPos), GetPixelPointWithPoint((Point)dd.endPos)));
                }
                drawContent.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), new Pen(new SolidColorBrush(drawData.drawColor), drawData.width), new Rect(GetPixelPointWithPoint((Point)drawData.startPos), GetPixelPointWithPoint((Point)drawData.endPos)));   // 四角を描く
            }
            return new DrawingImage(drawingGroup);
        }

        private void EditImage_MouseMove(object sender, MouseEventArgs e)
        {
            if(editDrawData == null || editDrawData.startPos == null) return;
            editDrawData.endPos = Mouse.GetPosition(EditImage);
            EditImage.Source = DrawRegion(editDrawData);
        }

        private void ChangeDrawColorBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        public void CopyFromCB()
        {
            var targetBitmapImg = Clipboard.GetImage();
            if (targetBitmapImg == null) return;
            EditImage.Source = targetBitmapImg;
            using (System.IO.FileStream stream = new System.IO.FileStream("origin.png", FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(targetBitmapImg));
                encoder.Save(stream);
            }
            originImage = new BitmapImage(new Uri("origin.png", UriKind.Relative));
        }
        public void CopyToCB()
        {
            if (originImage == null) return;
            DrawingVisual drawingGroup = new DrawingVisual();
            using (DrawingContext drawContent = drawingGroup.RenderOpen())
            {
                // 画像を書いて、その上にテキストを書く
                drawContent.DrawImage(originImage, new System.Windows.Rect(0, 0, originImage.Width, originImage.Height));
                foreach (var dd in drawDatas)
                {
                    drawContent.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), new Pen(new SolidColorBrush(dd.drawColor), dd.width), new Rect(GetPixelPointWithPoint((Point)dd.startPos), GetPixelPointWithPoint((Point)dd.endPos)));
                }
            }
            var render = new RenderTargetBitmap((int)originImage.Width, (int)originImage.Height, 96, 96, PixelFormats.Pbgra32);
            render.Render(drawingGroup);
            Clipboard.SetImage(render);
        }
        public void RemoveLastDrawData()
        {
            if (drawDatas.Count <= 0) return;
            drawDatas.RemoveAt(drawDatas.Count - 1);
            if (drawDatas.Count == 0) EditImage.Source = originImage;
            else EditImage.Source = DrawRegion(drawDatas.Last());
        }

        private void CopyFromCB_Btn_Click(object sender, RoutedEventArgs e) => CopyFromCB();
        private void CopyToCB_Btn_Click(object sender, RoutedEventArgs e) => CopyToCB();

        private void CopyFromCBMenuItem_Click(object sender, RoutedEventArgs e) => CopyFromCB();
        private void CopyToCBMenuItem_Click(object sender, RoutedEventArgs e) => CopyToCB();
    }
    public class DrawData
    {
        public Point? startPos, endPos;
        public Color drawColor;
        public int width;
        public DrawData(Color color, int width)
        {
            this.drawColor = color;
            this.width = width;
        }
    }
    public class CopyToCBC : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            MainWindow.Instance.CopyToCB();
        }
    }
    public class CopyFromCBC : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            MainWindow.Instance.CopyFromCB();
        }
    }
    public class RemoveLastDrawDataC : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            MainWindow.Instance.RemoveLastDrawData();
        }
    }
}
