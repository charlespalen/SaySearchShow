using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Media.Effects;
using System.ComponentModel;
using System.Net;
using System.Net.Cache;
using System.IO;
using FlickrNet;

namespace FlickrKinectPhotoFun
{
    internal class imageWorkerArgs
    {
        internal string uri { get; set; }
        internal int targetWidth { get; set; }
        internal int targetHeight { get; set; }
    }

    /// <summary>
    /// Interaction logic for contentViewer.xaml
    /// </summary>
    /// 
    public partial class contentViewer : UserControl
    {
        private PhotoCollection _photoGuide;
        private Random _rand = new Random();
        private int _latestPhotoIndex = 0;
        private Timer _imageDisplayDelay;
        /// <summary>
        /// For some reason images will not download no matter what at times.
        /// Manually checking looks like dns problems.
        /// </summary>
        private Timer _maximumDownloadTimeout;
        BitmapImage _currentImage;
        /// <summary>
        /// Medium size is too small nativeley for a display app,
        /// so heres the option to scale them. Most look great scaled a bit.
        /// </summary>
        private double _imageScalePercentage = 1.0;
        private int _currentImgWidth = 640;
        private int _currentImgHeight = 480;

        private string _latestPhotoTitle = "";
        private string _latestPhotoDesc = "";
        private string _latestPhotoAuth = "";

        private BackgroundWorker _backgroundWorker;

        public contentViewer()
        {
            InitializeComponent();

            // Set the allowed image scale
            _imageScalePercentage = Properties.Settings.Default.imageScaleMultiplier;
            // Init the background worker
            initWorkerIfNeeded();

        }

        public void injectNewPhotoGuideList(PhotoCollection _newGuide)
        {
            if (_newGuide.Count > 0)
            {
                _photoGuide = _newGuide;

                if (mainHolder.Children.Count > 0)
                {
                    stopMaximumDownloadTimeout();
                    destroyImageDisplayTimer();
                    hideCurrentImages();
                }
                else
                {
                    selectRandomImage();
                    loadLatestImage();
                }
            }
        }

        private void adjustLayoutToAccomodateRealRes()
        {
            double availableWidth = GlobalConfiguration.mainScreenW;

            // Need to account for bottom of screen 250
            double availableHeight = GlobalConfiguration.mainScreenH;

            imgParent.Width = availableWidth;
            imgParent.Height = availableHeight;

            mainHolder.Width = availableWidth;
            mainHolder.Height = availableHeight;
        }

        private void initWorkerIfNeeded()
        {
            if (_backgroundWorker == null)
            {
                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += BackgroundWorker_DoWork;
                _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                _backgroundWorker.WorkerReportsProgress = false;
                _backgroundWorker.WorkerSupportsCancellation = true;
            } else if(_backgroundWorker.IsBusy) {
                _backgroundWorker.DoWork -= BackgroundWorker_DoWork;
                _backgroundWorker.RunWorkerCompleted -= BackgroundWorker_RunWorkerCompleted;
                _backgroundWorker.CancelAsync();
                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += BackgroundWorker_DoWork;
                _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                _backgroundWorker.WorkerReportsProgress = false;
                _backgroundWorker.WorkerSupportsCancellation = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            adjustLayoutToAccomodateRealRes();
        }

        private void selectRandomImage()
        {
            //Console.WriteLine("select random image");
            // This will skip the photo in slot 0 until it circles around. Oh well.
            _latestPhotoIndex += 1;
            if (_latestPhotoIndex > _photoGuide.Count - 1)
            {
                _latestPhotoIndex = 0;
            }
        }

        private void loadLatestImage()
        {

            Object imgObj = new Object();
            imageWorkerArgs imgParams = new imageWorkerArgs();
            imgParams.uri = _photoGuide[_latestPhotoIndex].MediumUrl;
            imgParams.targetHeight = 0;
            imgParams.targetWidth = 0;

            if (_photoGuide[_latestPhotoIndex].MediumWidth.HasValue)
            {
                imgParams.targetWidth = (int)_photoGuide[_latestPhotoIndex].MediumWidth;
            }
            else
            {
                imgParams.targetWidth = 640;
            }
            if (_photoGuide[_latestPhotoIndex].MediumHeight.HasValue)
            {
                imgParams.targetHeight = (int)_photoGuide[_latestPhotoIndex].MediumHeight;
            }


            initWorkerIfNeeded();
            _backgroundWorker.RunWorkerAsync(imgParams);
            startMaximumDownloadTimeout();
        }

        private void handleImageDownloaded(object sender, EventArgs e)
        {
                stopMaximumDownloadTimeout();

            // Set the metadata info for display
            _latestPhotoAuth = _photoGuide[_latestPhotoIndex].OwnerName;
            _latestPhotoDesc = _photoGuide[_latestPhotoIndex].Description;
            _latestPhotoTitle = _photoGuide[_latestPhotoIndex].Title;

            Image MainImg = new Image();
            MainImg.Stretch = Stretch.Fill;
            MainImg.Opacity = 1;
            MainImg.Source = _currentImage;
            // WPF accounts for dpi in images and it will scale things down. We are going to force
            // the item to the size that flikr reports....and looks like at times some of these values are zero! Super
            //
            // Also we can scale here easily
            if (_currentImage.DecodePixelWidth != 0)
            {
                MainImg.Width = _currentImage.DecodePixelWidth * _imageScalePercentage;
            }
            else if(_photoGuide[_latestPhotoIndex].MediumWidth != null)
            {
                MainImg.Width = (double)_photoGuide[_latestPhotoIndex].MediumWidth * _imageScalePercentage;
            } else {
                MainImg.Width = 640 * _imageScalePercentage;
            }

            _currentImgWidth = (int)MainImg.Width;

            if (_currentImage.DecodePixelHeight != 0)
            {
                MainImg.Height = _currentImage.DecodePixelHeight * _imageScalePercentage;
            }
            else if (_photoGuide[_latestPhotoIndex].MediumHeight != null)
            {
                MainImg.Height = (double)_photoGuide[_latestPhotoIndex].MediumHeight * _imageScalePercentage;
            }
            else
            {
                MainImg.Height = 480 * _imageScalePercentage;
            }
            _currentImgHeight = (int)MainImg.Height;

            Border myBorder = new Border();
            myBorder.BorderThickness = new Thickness(10);
            myBorder.BorderBrush = new SolidColorBrush(Colors.White);
            myBorder.Margin = new Thickness(8.0);
            myBorder.Opacity = 1;
            myBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            myBorder.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            myBorder.Child = MainImg;

            // Figure out a random placement and tilted angle on the display
            // -45 to 45
            //int angle = _rand.Next(-10, 10);

            // We know that the scroller needs 200 px
            // We know the actual bar needs 74
            double availableWidth = GlobalConfiguration.mainScreenW - _currentImgWidth - 50;

            // Need to account for bottom of screen 250
            // Need to account for image height 480
            // Need to account for rotated image height 240

            double availableHeight = GlobalConfiguration.mainScreenH - _currentImgHeight;

            Point newXY = new Point(_rand.Next(50, (int)availableWidth), _rand.Next(0, (int)availableHeight));
            // We want all items all the way to the left or right of the screen
            int lOrR = _rand.Next(0, 100);
            if (lOrR > 50)
            {
                newXY.X = GlobalConfiguration.mainScreenW + 300;
            }
            else
            {
                newXY.X = -1000;
            }

            int tOrB = _rand.Next(0, 100);
            if (tOrB > 50)
            {
                newXY.Y = GlobalConfiguration.mainScreenH + 300;
            }
            else
            {
                newXY.Y = -1000;
            }

            // Set the tag property to the point
            myBorder.SetValue(FrameworkElement.TagProperty, newXY);

            // Transforms for moving and rotating
            TranslateTransform moveTransform = new TranslateTransform(newXY.X, newXY.Y);
            TransformGroup photoTransforms = new TransformGroup();
            photoTransforms.Children.Add(moveTransform);


            myBorder.RenderTransform = photoTransforms;

            mainHolder.Children.Add(myBorder);
            showNewImages();
        }

        private void handleImageDownloadFailure(object sender, EventArgs e)
        {
            Console.WriteLine("handle image download failure");
            stopMaximumDownloadTimeout();
            if (_currentImage != null)
            {
                _currentImage = null;
            }
            selectRandomImage();
            loadLatestImage();
        }

        private void handleImageDecodeFailure(object sender, EventArgs e)
        {
            Console.WriteLine("handle image decode failure");
            stopMaximumDownloadTimeout();
            if (_currentImage != null)
            {
                _currentImage = null;
            }
            selectRandomImage();
            loadLatestImage();
        }
        // Should slide these in from the left and right
        private void showNewImages()
        {
            double delayBuildUp = 0;

            // Set the image metadata
            photoDetails.LayoutUpdated += photoDetails_SizeChanged;
            phot_title.Text = "Title: " + _latestPhotoTitle;
            phot_author.Text = "Author: " + _latestPhotoAuth;
            HTMLphot_description.Text = "Description: " + _latestPhotoDesc;

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                // need to get references to each border
                var curBorder = mainHolder.Children[i] as Border;
                if (curBorder != null)
                {

                    // Figure out a random placement and tilted angle on the display
                    // -45 to 45
                    int angle = _rand.Next(-10, 10);

                    int borderThickness = 10;
                    // We know that the scroller needs 200 px
                    // We know the actual bar needs 74
                    double availableWidth = GlobalConfiguration.mainScreenW - _currentImgWidth - 50 - (borderThickness * 2);
                    if (availableWidth < 1) availableWidth = 0;
                    int finX = _rand.Next(50, (int)availableWidth);
                    double availableHeight = GlobalConfiguration.mainScreenH - _currentImgHeight - 50 - (borderThickness * 2);
                    if (availableHeight < 1) availableHeight = 0;
                    int finY = _rand.Next(0, (int)availableHeight);

                    var previousPoints = (Point)curBorder.GetValue(FrameworkElement.TagProperty);
                    if (previousPoints != null)
                    {
                        // Transforms for moving and rotating - need the first positions
                        TranslateTransform moveTransform = new TranslateTransform(previousPoints.X, previousPoints.Y);
                        RotateTransform rotTransform = new RotateTransform(0);
                        TransformGroup photoTransforms = new TransformGroup();
                        photoTransforms.Children.Add(moveTransform);
                        photoTransforms.Children.Add(rotTransform);

                        curBorder.RenderTransform = photoTransforms;

                        SineEase ease = new SineEase();
                        ease.EasingMode = EasingMode.EaseOut;

                        DoubleAnimation daMoveX = new DoubleAnimation();
                        DoubleAnimation daMoveY = new DoubleAnimation();
                        DoubleAnimation daRotate = new DoubleAnimation();
                        daMoveX.To = finX;
                        daMoveY.To = finY;
                        daRotate.To = angle;
                        daMoveX.EasingFunction = ease;
                        daRotate.EasingFunction = ease;
                        daMoveY.EasingFunction = ease;

                        int moveRandTime = _rand.Next(1, 3);
                        daMoveX.Duration = TimeSpan.FromSeconds(moveRandTime);
                        daRotate.Duration = TimeSpan.FromSeconds(_rand.Next(1, 2));
                        daMoveY.Duration = TimeSpan.FromSeconds(moveRandTime);

                        daMoveX.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        daMoveY.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        daRotate.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        delayBuildUp += .3;

                        // Only add a complete listener to the final guy
                        if (i == mainHolder.Children.Count - 1)
                        {
                            daMoveX.Completed += handleAnimationOnComplete;
                        }

                        moveTransform.BeginAnimation(TranslateTransform.XProperty, daMoveX);
                        moveTransform.BeginAnimation(TranslateTransform.YProperty, daMoveY);
                        rotTransform.BeginAnimation(RotateTransform.AngleProperty, daRotate);
                    }

                }
            }
        }

        private void handleAnimationOnComplete(object sender, EventArgs e)
        {
            Console.WriteLine("handle animation on complete");
            var daRef = sender as AnimationClock;
            if (daRef != null)
            {
                var daTL = daRef.Timeline as DoubleAnimation;
                if (daTL != null)
                {

                }
            }

            // Start the hold timer
            createImageDisplayTimer();
        }

        private void createImageDisplayTimer()
        {
            if (_imageDisplayDelay != null)
            {
                destroyImageDisplayTimer();
            }
            // 10 to 20 seconds
            int _msTime = _rand.Next(Properties.Settings.Default.photoDisplayTimeMinSec * 1000, Properties.Settings.Default.photoDisplayTimeMaxSec * 1000);

            _imageDisplayDelay = new Timer(_msTime);
            _imageDisplayDelay.Elapsed += handleImageDisplayUp;
            _imageDisplayDelay.Start();
        }

        private void destroyImageDisplayTimer()
        {
            if (_imageDisplayDelay != null)
            {
                _imageDisplayDelay.Stop();
                _imageDisplayDelay.Elapsed -= handleImageDisplayUp;
                _imageDisplayDelay = null;
            }
        }

        /// <summary>
        /// This isnt in the UI thread so we need to use the dispatcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleImageDisplayUp(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                destroyImageDisplayTimer();
                hideCurrentImages();
            }));
        }

        /// <summary>
        /// Want to hide everything with a creepy slow dissolve
        /// </summary>
        private void hideCurrentImages()
        {
            int fadeOutSec = _rand.Next(5, 10);

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                var curBrdr = mainHolder.Children[i] as Border;
                if (curBrdr != null)
                {
                    DoubleAnimation outAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(fadeOutSec)));
                    if (i == mainHolder.Children.Count - 1)
                    {
                        outAnimation.Completed += handleImagesHidden;
                    }
                    curBrdr.BeginAnimation(Border.OpacityProperty, outAnimation);
                }
            }

            if (_currentImage != null)
            {
                if (_currentImage.IsFrozen == false)
                {
                    _currentImage.DownloadFailed -= handleImageDownloadFailure;
                    _currentImage.DownloadCompleted -= handleImageDownloaded;
                    _currentImage.DecodeFailed -= handleImageDecodeFailure;
                }
                _currentImage = null;
            }
        }


        private void handleImagesHidden(object sender, EventArgs e)
        {
            var daRef = sender as DoubleAnimation;
            if (daRef != null)
            {
                daRef.Completed -= handleImagesHidden;
            }

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                var curBrdr = mainHolder.Children[i] as Border;
                if (curBrdr != null)
                {
                    curBrdr.Child = null;
                }
            }

            mainHolder.Children.Clear();

            selectRandomImage();
            loadLatestImage();
        }

        private void photoDetails_SizeChanged(object sender, EventArgs e)
        {
            photoDetails.LayoutUpdated -= photoDetails_SizeChanged;
            // Move the photo caption to a random area around the display
            // it has to be stuck to the left or right side of the screen
            int leftOrRight = _rand.Next(0, 100);
            if (leftOrRight < 50)
            {
                leftOrRight = 20;
            }
            else
            {
                leftOrRight = (int)GlobalConfiguration.mainScreenW - 520;
            }

            // Width is locked, but height is variable. Some captions go offscreen so we need to clamp them!
            int aHeight = 0;
            if (photoDetails.ActualHeight < GlobalConfiguration.mainScreenH)
            {
                aHeight = (int)(GlobalConfiguration.mainScreenH - photoDetails.ActualHeight);
                aHeight = _rand.Next(0, aHeight);
            }
            Thickness newBounds = new Thickness(0, 0, 0, 0);
            newBounds.Left = leftOrRight;
            newBounds.Top = aHeight;
            CaptionContent.Margin = newBounds;
        }

        private void startMaximumDownloadTimeout()
        {
            if (_maximumDownloadTimeout != null)
            {
                stopMaximumDownloadTimeout();
            }
            _maximumDownloadTimeout = new Timer(Properties.Settings.Default.maximumDownloadTimeoutSec * 1000);
            _maximumDownloadTimeout.Elapsed += handleMaximumDownloadTimeoutReached;
            _maximumDownloadTimeout.Start();
        }

        private void stopMaximumDownloadTimeout()
        {
            if (_maximumDownloadTimeout != null)
            {
                _maximumDownloadTimeout.Stop();
                _maximumDownloadTimeout.Elapsed -= handleMaximumDownloadTimeoutReached;
                _maximumDownloadTimeout = null;
            }
        }

        private void handleMaximumDownloadTimeoutReached(object sender, ElapsedEventArgs e)
        {
            // Well time to move on!
            Console.WriteLine("handle maximum download timeout reached");

            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                stopMaximumDownloadTimeout();

                if (_currentImage != null)
                {
                    _currentImage = null;
                }
                if (_backgroundWorker.IsBusy == true)
                {
                    _backgroundWorker.CancelAsync();
                }

                selectRandomImage();
                loadLatestImage();
            }));
        }

        #region Background Worker For Photos
        // -----------------------------------------------------------------------------
        // ** BACKGROUND WORKER FUN **/
        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            imageWorkerArgs imgParams = e.Argument as imageWorkerArgs;

            using (WebClient webClient = new WebClient())
            {
                webClient.Proxy = null;  //avoids dynamic proxy discovery delay
                webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                try
                {
                    byte[] imageBytes = null;

                    imageBytes = webClient.DownloadData(imgParams.uri);

                    if (imageBytes == null)
                    {
                        e.Result = null;
                        return;
                    }
                    MemoryStream imageStream = new MemoryStream(imageBytes);
                    BitmapImage image = new BitmapImage();

                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    if (imgParams.targetWidth != 0)
                    {
                        image.DecodePixelWidth = imgParams.targetWidth;
                    }
                    if (imgParams.targetHeight != 0)
                    {
                        image.DecodePixelHeight = imgParams.targetHeight;
                    }
                    image.StreamSource = imageStream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    image.Freeze();
                    imageStream.Close();

                    e.Result = image;
                }
                catch (WebException ex)
                {
                    //do something to report the exception
                    e.Result = ex;
                }
            }


        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            var imgResult = e.Result as BitmapImage;
            if (imgResult != null)
            {
                _currentImage = imgResult;
                handleImageDownloaded(new object(), new EventArgs());
            }
            else
            {
                var imgException = e.Result as WebException;
                if (imgException != null)
                {
                    stopMaximumDownloadTimeout();
                    if (_currentImage != null)
                    {
                        _currentImage = null;
                    }
                    selectRandomImage();
                    loadLatestImage();
                }
            }
        }
        // -----------------------------------------------------------------------------
        #endregion
    }
}
