using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public const int CARD_HEIGHT = 110;

        private static CaptionPage instance;
        public static CaptionPage Instance => instance;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            Loaded += (s, e) =>
            {
                AutoHeight();
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Visible;
                Translator.Caption.PropertyChanged += TranslatedChanged;
            };
            Unloaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Collapsed;
                Translator.Caption.PropertyChanged -= TranslatedChanged;
            };

            CollapseTranslatedCaption(Translator.Setting.MainWindow.CaptionLogEnabled);
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    SnackbarHost.Show("已复制。", textBlock.Text, SnackbarType.Info, 100);
                }
                catch
                {
                    SnackbarHost.Show("复制失败。", string.Empty, SnackbarType.Error, 100);
                }
                await Task.Delay(500);
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            // Keep the latest text visible by auto-scrolling to the end of the caption
            // box. The font size stays fixed; long content scrolls instead of resizing.
            if (e.PropertyName == nameof(Translator.Caption.DisplayTranslatedCaption))
                ScrollCaptionToEnd(TranslatedCaptionScroll);
            else if (e.PropertyName == nameof(Translator.Caption.DisplayOriginalCaption))
                ScrollCaptionToEnd(OriginalCaptionScroll);
        }

        private void ScrollCaptionToEnd(ScrollViewer? scroll)
        {
            if (scroll == null)
                return;
            Dispatcher.BeginInvoke(new Action(() => scroll.ScrollToEnd()), DispatcherPriority.Background);
        }

        public void CollapseTranslatedCaption(bool isCollapsed)
        {
            var converter = new GridLengthConverter();

            if (isCollapsed)
            {
                // Log cards mode: caption rows shrink to fit content, log cards take the rest.
                OriginalCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                LogCards.Visibility = Visibility.Visible;
            }
            else
            {
                // Caption-only mode: both rows split the page equally with fixed font sizes.
                OriginalCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                LogCards.Visibility = Visibility.Collapsed;
            }
        }

        public void AutoHeight()
        {
            if (Translator.Setting.MainWindow.CaptionLogEnabled)
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1),
                    maxHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1));
            else
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: (int)App.Current.MainWindow.MinHeight,
                    maxHeight: (int)App.Current.MainWindow.MinHeight);
        }
    }
}
