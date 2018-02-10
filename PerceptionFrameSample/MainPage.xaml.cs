namespace PerceptionFrameSample
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Windows.Devices.Perception;
    using Windows.Foundation;
    using Windows.Graphics.Imaging;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class MainPage : Page
    {
        Rect irDrawRect;
        Rect colorDrawRect;
        CanvasBitmap bitmap;
        PerceptionColorFrameReader colorFrameReader;
        PerceptionInfraredFrameReader irFrameReader;
        SoftwareBitmap colorFrameBitmap;
        SoftwareBitmap ilFrameBitmap;
        SoftwareBitmap nilFrameBitmap;
        bool ilFrame = true;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        async void OnLoaded(object sender, RoutedEventArgs args)
        {
            // Request access to Infrared sources 赤外線ソースにアクセス要求
            var accessir = await PerceptionInfraredFrameSource.RequestAccessAsync();
            // Request access Color sources カラーソースにアクセス要求
            var accesscolor = await PerceptionColorFrameSource.RequestAccessAsync();

            if (accessir == PerceptionFrameSourceAccessStatus.Allowed && accessir == accesscolor)
            {
                startColorReaderAsync();
                startIrReaderAsync();
            }
        }

        private async void startIrReaderAsync()
        {
            // Search all IR sources 全ての赤外線ソースを検索
            var irSources = await PerceptionInfraredFrameSource.FindAllAsync();
            // Select the first IR source 最初の赤外線ソースを選択
            var irSource = irSources.First();

            this.irDrawRect = new Rect(0, 0, irSource.VideoProfile.Width,
                    irSource.VideoProfile.Height);
            // Open the frame reader フレームリーダーを開いて
            this.irFrameReader = irSource.OpenReader();
            // Send each new frame to _irFrameArrived _irFrameArrivedに各新フレームを送る
            this.irFrameReader.FrameArrived += _irFrameArrived;
        }

        private async void startColorReaderAsync()
        {
            // Search all Color sources 全てのカラーソースを検索
            var colorSources = await PerceptionColorFrameSource.FindAllAsync();
            // Select the first Color source 最初のカラーソースを選択
            var colorSource = colorSources.First();

            this.colorDrawRect = new Rect(0, 0, colorSource.VideoProfile.Width,
                colorSource.VideoProfile.Height);
            // Open the frame reader フレームリーダーを開いて
            this.colorFrameReader = colorSource.OpenReader();
            // We pause this stream so that it doesn't interfere with the IR stream
            // 赤外線ストリームと絡まないためこのストリームを一時停止する
            this.colorFrameReader.IsPaused = true;
            // Send each new frame to _colorFrameArrived _colorFrameArrivedに各新フレームを送る
            this.colorFrameReader.FrameArrived += _colorFrameArrived;
        }

        private void _irFrameArrived(PerceptionInfraredFrameReader sender, PerceptionInfraredFrameArrivedEventArgs args)
        {
            // Open the frame フレーム開いて
            using (var frame = args.TryOpenFrame())
            {
                // Confirm that frame exists フレームの存在確認
                if (frame != null)
                {
                    var videoFrame = frame.VideoFrame.SoftwareBitmap;
                    var frameBitmap = SoftwareBitmap.Convert(
                            videoFrame, BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore);
                    // Divide frames by illuminated and non-illuminated
                    // フレームを照明と非照明に分ける
                    if (this.ilFrame)
                    {
                        this.ilFrameBitmap = frameBitmap;
                    }
                    else
                    {
                        this.nilFrameBitmap = frameBitmap;
                    }
                    videoFrame.Dispose();
                }
                this.ilFrame = !this.ilFrame;
            }
        }

        private void _colorFrameArrived(PerceptionColorFrameReader sender, PerceptionColorFrameArrivedEventArgs args)
        {
            // Open the frame フレーム開いて
            using (var frame = args.TryOpenFrame())
            {
                // Confirm that frame exists フレームの存在確認
                if (frame != null)
                {
                    var videoFrame = frame.VideoFrame.SoftwareBitmap;
                    this.colorFrameBitmap = SoftwareBitmap.Convert(
                        videoFrame, BitmapPixelFormat.Rgba16,
                        BitmapAlphaMode.Ignore);
                    videoFrame.Dispose();
                }
            }
        }

        void OnCanvasControlDraw(
        CanvasControl sender,
        CanvasDrawEventArgs args)
        {
            SoftwareBitmap lastFrameBitmap;
            Rect drawRect;

            if (sender.Name == "i")
            {
                lastFrameBitmap = this.ilFrameBitmap;
                drawRect = this.irDrawRect;
            }
            else if (sender.Name == "ni")
            {
                lastFrameBitmap = this.nilFrameBitmap;
                drawRect = this.irDrawRect;
            }
            else
            {
                lastFrameBitmap = this.colorFrameBitmap;
                drawRect = this.colorDrawRect;
            }

            if (lastFrameBitmap != null)
            {
                this.bitmap?.Dispose();

                this.bitmap = CanvasBitmap.CreateFromSoftwareBitmap(
                sender.Device,
                lastFrameBitmap);

                if (this.bitmap != null)
                {
                    args.DrawingSession.DrawImage(this.bitmap, drawRect);
                }
            }
            sender.Invalidate();
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            this.HamburgerView.IsPaneOpen = !this.HamburgerView.IsPaneOpen;
        }

        private void HamburgerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ColorCamera.IsSelected)
            {
                this.irFrameReader.IsPaused = true;
                this.colorFrameReader.IsPaused = false;
                this.i.Visibility = Visibility.Collapsed;
                this.ni.Visibility = Visibility.Collapsed;
                this.c.Visibility = Visibility.Visible;
            }
            else if (this.IRCamera.IsSelected)
            {
                this.colorFrameReader.IsPaused = true;
                this.irFrameReader.IsPaused = false;
                this.i.Visibility = Visibility.Visible;
                this.ni.Visibility = Visibility.Visible;
                this.c.Visibility = Visibility.Collapsed;
            }

            this.HamburgerView.IsPaneOpen = false;
        }
    }
}