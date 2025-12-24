using System.Windows;
using System.Windows.Controls;

namespace HQStudio.Controls
{
    public partial class NoApiConnectionView : UserControl
    {
        public static readonly DependencyProperty SectionNameProperty =
            DependencyProperty.Register(nameof(SectionName), typeof(string), typeof(NoApiConnectionView),
                new PropertyMetadata("Этот раздел", OnSectionNameChanged));

        public string SectionName
        {
            get => (string)GetValue(SectionNameProperty);
            set => SetValue(SectionNameProperty, value);
        }

        public NoApiConnectionView()
        {
            InitializeComponent();
            UpdateDescription();
        }

        private static void OnSectionNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NoApiConnectionView view)
            {
                view.UpdateDescription();
            }
        }

        private void UpdateDescription()
        {
            DescriptionText.Text = $"{SectionName} доступен только при подключении к серверу";
        }
    }
}
