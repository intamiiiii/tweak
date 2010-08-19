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
using Std.Tweak.Streaming;
using Std.Tweak.CredentialProviders;
using Std.Tweak;
using System.Collections.ObjectModel;

namespace TweakLib
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        ObservableCollection<TwitterStatus> status = null;
        ObservableCollection<string> activities = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            (new Warning() { Owner = this }).ShowDialog();
            status = new ObservableCollection<TwitterStatus>();
            activities = new ObservableCollection<string>();
            TimelineList.ItemsSource = status;
            ActivityBox.ItemsSource = activities;
        }

        string id = null;
        string pw = null;
        Basic b = null;
        public void ReceiveThread()
        {
            b = new Basic(id, pw);
            var sctl = StreamingController.BeginStreaming(b, StreamingController.StreamingType.user);
            sctl.OnDisconnected += new Action(StreamingApi_OnDisconnected);
            foreach (var i in sctl.EnumerateStreaming())
            {
                switch (i.Kind)
                {
                    case TwitterStreamingElement.ElementKind.Status:
                        if (((TwitterStatus)i.Status).IsOfficialRetweeted)
                        {
                            this.Dispatcher.Invoke(new Action(() => status.Insert(0, ((TwitterStatus)i.Status))));
                            this.Dispatcher.Invoke(new Action(() => status.Insert(0, ((TwitterStatus)i.Status).RetweetedOriginal)));
                        }
                        else
                        {
                            this.Dispatcher.Invoke(new Action(() => status.Insert(0, (TwitterStatus)i.Status)));
                        }
                        System.Diagnostics.Debug.WriteLine(i.Status.ToString());
                        break;
                    case TwitterStreamingElement.ElementKind.Delete:
                        System.Diagnostics.Debug.WriteLine("×Deleted " + i.RawXElement);
                        break;
                    case TwitterStreamingElement.ElementKind.Favorite:
                        this.Dispatcher.Invoke(new Action(() => activities.Insert(0, "Fav " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + ": " + i.Status.Text)));
                        System.Diagnostics.Debug.WriteLine("●Fav " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + ": " + i.Status.Text);
                        break;
                    case TwitterStreamingElement.ElementKind.Unfavorite:
                        this.Dispatcher.Invoke(new Action(() => activities.Insert(0, "Unfav " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + ": " + i.Status.Text)));
                        System.Diagnostics.Debug.WriteLine("○Unfav " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + ": " + i.Status.Text);
                        break;
                    case TwitterStreamingElement.ElementKind.Follow:
                        this.Dispatcher.Invoke(new Action(() => activities.Insert(0, "Follow " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString())));
                        System.Diagnostics.Debug.WriteLine("★Follow " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString());
                        break;
                    case TwitterStreamingElement.ElementKind.ListUpdated:
                        this.Dispatcher.Invoke(new Action(() => activities.Insert(0, "AddList " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + " to " + i.TargetList.ToString())));
                        System.Diagnostics.Debug.WriteLine("▲AddList " + i.SourceUser.ToString() + " => " + i.TargetUser.ToString() + " to " + i.TargetList.ToString());
                        break;
                    case TwitterStreamingElement.ElementKind.UserEnumerations:
                        break;
                    case TwitterStreamingElement.ElementKind.DirectMessage:
                        var dm = (TwitterDirectMessage)i.Status;
                        MessageBox.Show("Direct message from " + dm.Sender.ScreenName + Environment.NewLine + dm.Text);
                        break;
                    case TwitterStreamingElement.ElementKind.Undefined:
                        this.Dispatcher.Invoke(new Action(() => activities.Insert(0, "★Undefined event:" + i.RawXElement)));
                        System.Diagnostics.Debug.WriteLine("★Undefined event:" + i.RawXElement);
                        break;
                }
            }
        }

        void StreamingApi_OnDisconnected()
        {
            this.Dispatcher.Invoke(new Action(() => Submit.IsEnabled = true), null);
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit.IsEnabled = false;
            id = ID.Text;
            pw = Password.Password;
            var act = new Action(ReceiveThread);
            act.BeginInvoke((iar) => ((Action)iar.AsyncState).EndInvoke(iar), act);
        }

        private void Updater_Click(object sender, RoutedEventArgs e)
        {
            if (b == null)
                return;
            b.UpdateStatus(UpdateText.Text);
            UpdateText.Clear();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            b.EndStreaming();
        }

        private void OpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            var status = TimelineList.SelectedItem as TwitterStatus;
            if (status != null)
            {
                System.Diagnostics.Process.Start("http://twitter.com/" + status.User.ScreenName + "/status/" + status.Id.ToString());
            }
        }

        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            var status = TimelineList.SelectedItem as TwitterStatus;
            if (status != null)
            {
                System.Diagnostics.Process.Start("http://twitter.com/" + status.User.ScreenName);
            }
        }

        private void Retweet_Click(object sender, RoutedEventArgs e)
        {
            var status = TimelineList.SelectedItem as TwitterStatus;
            if (status != null)
            {
                if (MessageBox.Show(
                    "公式リツイートしますか？" + Environment.NewLine +
                    "(すでにリツイートしている場合、この操作は無視されます)", "公式リツイートの確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    b.Retweet(status.Id);
                }
            }
        }

        private void Fav_Click(object sender, RoutedEventArgs e)
        {
            var status = TimelineList.SelectedItem as TwitterStatus;
            if (status != null)
            {
                b.CreateFavorites(status.Id);
            }
        }
    }

    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var uri = value as Uri;
            BitmapImage bm = new BitmapImage();
            bm.BeginInit();
            bm.UriSource = uri;
            bm.EndInit();
            return bm;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
