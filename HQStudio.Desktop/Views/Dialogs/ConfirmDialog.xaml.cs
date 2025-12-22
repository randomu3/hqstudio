using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        public enum DialogType
        {
            Success,
            Question,
            Warning,
            Error
        }

        public ConfirmDialog(string title, string message, DialogType type = DialogType.Question, 
            string yesText = "Да", string noText = "Нет")
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;
            YesBtn.Content = yesText;
            NoBtn.Content = noText;
            
            // Устанавливаем иконку и цвет в зависимости от типа
            switch (type)
            {
                case DialogType.Success:
                    IconText.Text = "✓";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                    break;
                case DialogType.Question:
                    IconText.Text = "❓";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2196F3"));
                    break;
                case DialogType.Warning:
                    IconText.Text = "⚠️";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFC107"));
                    break;
                case DialogType.Error:
                    IconText.Text = "❌";
                    IconText.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336"));
                    break;
            }
        }

        /// <summary>
        /// Показать диалог подтверждения
        /// </summary>
        public static bool Show(string title, string message, DialogType type = DialogType.Question,
            string yesText = "Да", string noText = "Нет", Window? owner = null)
        {
            var dialog = new ConfirmDialog(title, message, type, yesText, noText)
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            return dialog.ShowDialog() == true;
        }

        /// <summary>
        /// Показать информационное сообщение (только кнопка OK)
        /// </summary>
        public static void ShowInfo(string title, string message, DialogType type = DialogType.Success, Window? owner = null)
        {
            var dialog = new ConfirmDialog(title, message, type, "OK", "")
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            dialog.NoBtn.Visibility = Visibility.Collapsed;
            dialog.YesBtn.SetValue(Grid.ColumnSpanProperty, 2);
            dialog.ShowDialog();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
