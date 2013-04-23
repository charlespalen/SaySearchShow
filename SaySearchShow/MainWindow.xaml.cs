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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace FlickrKinectPhotoFun
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SpeechRecognizer sr;
        public FlickrSearch _fSrch;

        public string latestRecognizedPhrase = "";
        public string latestHypothesizedPhrase = "";

        public MainWindow()
        {
            InitializeComponent();

            dockDude.Opacity = 0;
            levelDock.Opacity = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // Set the main screen width and height
            GlobalConfiguration.mainScreenW = SystemParameters.FullPrimaryScreenWidth;
            GlobalConfiguration.mainScreenH = SystemParameters.FullPrimaryScreenHeight;

            masterParent.Width = GlobalConfiguration.mainScreenW;
            masterParent.Height = GlobalConfiguration.mainScreenH;

            voiceRecText.Width = GlobalConfiguration.mainScreenW;
            srchResultText.Width = GlobalConfiguration.mainScreenW;

            levelDock.Height = GlobalConfiguration.mainScreenH;
            levelDock.Width = GlobalConfiguration.mainScreenW;

            Canvas.SetLeft(splashImage, GlobalConfiguration.mainScreenW / 2 - splashImage.Source.Width / 2);
            Canvas.SetTop(splashImage, GlobalConfiguration.mainScreenH / 2 - splashImage.Source.Height / 2);

            // Hide the serach results
            srchResultText.srch_terms.Text = "";
            srchResultText.srch_terms.Opacity = 0;
            srchResultText.srch_hypothesis.Text = "";
            srchResultText.srch_hypothesis.Opacity = 0;

            // Hide the cursor
            //Mouse.OverrideCursor = Cursors.None;

            _fSrch = new FlickrSearch();
            _fSrch.flikrSearchResultEvent += handleFlikrSearchResults;
            sr = new SpeechRecognizer();
            sr.VoiceRecognized += handlePhraseRecognition;
            sr.VoiceHypothesized += handlePhraseHypothesis;
            sr.VoiceLevelChange += handleInputLevelChange;
            sr.VoiceRejected += handlePhraseRejection;

            // Audio level bar
            if (!sr.usingKinect)
            {
                levlBar.LevelsHidden += handleLevelsHidden;
            }
            else
            {
                levlBar.toggleKinect();
            }



            DoubleAnimation daOpacityHide = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.5)));
            daOpacityHide.Completed += handleSplashHidden;
            daOpacityHide.BeginTime = TimeSpan.FromSeconds(3.5);
            splashImage.BeginAnimation(Image.OpacityProperty, daOpacityHide);

            DoubleAnimation daOpacityShow = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            daOpacityShow.Completed += handleSplashHidden;
            daOpacityShow.BeginTime = TimeSpan.FromSeconds(4);
            dockDude.BeginAnimation(Image.OpacityProperty, daOpacityShow);
            levelDock.BeginAnimation(Image.OpacityProperty, daOpacityShow);
        }

        private void handleSplashHidden(object sender, EventArgs e)
        {
            splashImage.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sr.VoiceRecognized -= handlePhraseRecognition;
            sr.VoiceHypothesized -= handlePhraseHypothesis;
            sr.VoiceLevelChange -= handleInputLevelChange;
            sr.VoiceRejected -= handlePhraseRejection;
            sr.Dispose();
        }
        private void handlePhraseHypothesis(object sender, EventArgs e)
        {
            if (sr.latestHypothesizedSpeech != latestHypothesizedPhrase)
            {
                latestHypothesizedPhrase = sr.latestHypothesizedSpeech;
                voiceRecText.newHypothesisText(latestHypothesizedPhrase);
                sr.VoiceLevelChange += handleInputLevelChange;
                levlBar.showItems();
            }
        }
        private void handlePhraseRejection(object sender, EventArgs e)
        {
                voiceRecText.hypothesisRejected();
        }
        private void handlePhraseRecognition(object sender, EventArgs e)
        {
            if (sr.latestRecognizedSpeech != latestRecognizedPhrase)
            {
                latestRecognizedPhrase = sr.latestRecognizedSpeech;
                voiceRecText.speechRecognized();
                voiceRecText.newSearchText(latestRecognizedPhrase);
                srchResultText.newSearchText("Searching for: " + latestRecognizedPhrase + ". Please wait.");

                if (_fSrch != null)
                {
                    _fSrch.newSearch(latestRecognizedPhrase);
                }
            }
        }
        private void handleInputLevelChange(object sender, EventArgs e)
        {
            levlBar.audLevel.Value = sr.latestAudioLevel;
        }

        private void handleFlikrSearchResults(object sender, flikrCustomCursorEventArgs e)
        {
            if (_fSrch._mostRecentPhotos != null)
            {
                if (_fSrch._mostRecentPhotos.Count > 0)
                {
                    srchResultText.newSearchText(e.resultMessage);
                    cntVwr.injectNewPhotoGuideList(_fSrch._mostRecentPhotos);
                }
                else
                {
                    srchResultText.newSearchText(e.resultMessage + " Please try again.");
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("KeyDown: " + e.Key.ToString() );
            if (e.Key.ToString() == "Escape")
            {
                mainWin.WindowState = System.Windows.WindowState.Normal;
                mainWin.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
            }
        }

        private void mainWin_StateChanged(object sender, EventArgs e)
        {
            if (mainWin.WindowState == System.Windows.WindowState.Maximized)
            {
                mainWin.WindowStyle = System.Windows.WindowStyle.None;
                Hide();
                Show();
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            aboutWindow abt = new aboutWindow();
            Nullable<bool> dialogResult = abt.ShowDialog();
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /**
         * We stop listening to the levels when the bar is hidden to
         * try and save some cycles
         */
        private void handleLevelsHidden(object sender, EventArgs e)
        {
            sr.VoiceLevelChange -= handleInputLevelChange;
        }
    }
}
