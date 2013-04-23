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
    /// Interaction logic for audioLevels.xaml
    /// </summary>
    public partial class audioLevels : UserControl
    {
        public event EventHandler LevelsHidden;
        private DoubleAnimation outForGoodAnimation;

        public audioLevels()
        {
            InitializeComponent();

            outForGoodAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(5)));
            outForGoodAnimation.Completed += handleOutAnimDone;
            outForGoodAnimation.BeginTime = TimeSpan.FromSeconds(4);
        }

        public void toggleKinect() {
            snd_devName.Text = "";
            audLevel.Visibility = System.Windows.Visibility.Hidden;
        }

        public void showItems()
        {
            sndContainer.BeginAnimation(StackPanel.OpacityProperty, null);
            sndContainer.Opacity = 1;
            // Start hiding it
            sndContainer.BeginAnimation(StackPanel.OpacityProperty, outForGoodAnimation);
        }

        private void handleOutAnimDone(object sender, EventArgs e)
        {
            // Unregister the listener
            if (LevelsHidden != null)
            {
                EventArgs evt = new EventArgs();
                LevelsHidden(this, evt);
            }
        }
    }
}
