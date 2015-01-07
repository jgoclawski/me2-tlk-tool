using System.Windows;

namespace Mass_Effect_2_TLK_Tool
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public string ApplicationVersion
        {
            get { return "v. " + App.GetVersion(); }
        }
        
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
