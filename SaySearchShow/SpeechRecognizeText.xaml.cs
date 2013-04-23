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
    /// Interaction logic for SpeechRecognizeText.xaml
    /// </summary>
    public partial class SpeechRecognizeText : UserControl
    {
        private DoubleAnimation outAnimation;
        private DoubleAnimation inAnimation;

        private DoubleAnimation hypothesisOutAnimation;
        private DoubleAnimation hypothesisInAnimation;

        private DoubleAnimation outForGoodAnimation;
        private DoubleAnimation hypothesisOutForGoodAnimation;

        public String _recentText = "";
        public String _recentHypothesis = "";
        private Double _timeToHideText = 5;

        public SpeechRecognizeText()
        {
            InitializeComponent();

            // Set the delay time from config file
            _timeToHideText = Properties.Settings.Default.textDelayTimeinSec;
            // Setup the double animations
            outAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.3)));
            outAnimation.Completed += handleOutAnimDone;
            inAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.3)));
            inAnimation.Completed += handleSearchTermsShowing;

            hypothesisOutAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.3)));
            hypothesisOutAnimation.Completed += handleOutHypAnimDone;
            hypothesisInAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.3)));
            hypothesisInAnimation.Completed += handleHypothesisTextShowing;

            outForGoodAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
            outForGoodAnimation.BeginTime = TimeSpan.FromSeconds(_timeToHideText);
            hypothesisOutForGoodAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
            hypothesisOutForGoodAnimation.BeginTime = TimeSpan.FromSeconds(_timeToHideText);
        }

        public void newSearchText(String _input)
        {
            _recentText = _input;

            // null will stop an animation in progress
            srch_terms.BeginAnimation(TextBlock.OpacityProperty, null);
            srch_terms.Text = _recentText;
            srch_terms.Opacity = 1;
            // Start hiding it
            srch_terms.BeginAnimation(TextBlock.OpacityProperty, outForGoodAnimation);
        }

        public void newHypothesisText(String _input)
        {
            _recentHypothesis = _input;

            // null will stop an animation in progress
            srch_hypothesis.BeginAnimation(TextBlock.OpacityProperty, null);
            srch_hypothesis.Text = _input;
            // ensure the color is white
            srch_hypothesis.Foreground = Brushes.White;
            srch_hypothesis.Opacity = 1;

            srch_hypothesis.BeginAnimation(TextBlock.OpacityProperty, hypothesisOutForGoodAnimation);
        }

        public void speechRecognized()
        {
            // Change the color to green
            srch_hypothesis.Foreground = Brushes.LawnGreen;
        }
        public void hypothesisRejected()
        {
            // Change the color to red
            srch_hypothesis.Foreground = Brushes.IndianRed;
        }

        /// <summary>
        /// Callback from the out animation to signify things are
        /// hidden so the text can be changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleOutAnimDone(object sender, EventArgs e)
        {
            // Change the text
            try
            {
                srch_terms.Text = _recentText;
                // Bring it back in
                srch_terms.BeginAnimation(TextBlock.OpacityProperty, inAnimation);
            }
            catch (Exception)
            {

            }

        }
        /// <summary>
        /// When text is displayed for real, we want to immediateley start hiding it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleSearchTermsShowing(object sender, EventArgs e)
        {
            // Start hiding it
            srch_terms.BeginAnimation(TextBlock.OpacityProperty, outForGoodAnimation);
        }

        private void handleOutHypAnimDone(object sender, EventArgs e)
        {
            // Change the text
            try
            {
                srch_hypothesis.Text = _recentHypothesis;
                // Bring it back in
                srch_hypothesis.BeginAnimation(TextBlock.OpacityProperty, hypothesisInAnimation);
            }
            catch (Exception)
            {

            }

        }
        /// <summary>
        /// When text is displayed for real, we want to immediateley start hiding it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleHypothesisTextShowing(object sender, EventArgs e)
        {
            srch_hypothesis.BeginAnimation(TextBlock.OpacityProperty, hypothesisOutForGoodAnimation);
        }

    }
}
