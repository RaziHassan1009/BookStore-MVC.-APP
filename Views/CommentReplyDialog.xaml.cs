using System.Windows;

namespace GrapheneSensore.Views
{
    public partial class CommentReplyDialog : Window
    {
        public string ReplyText => ReplyTextBox.Text;

        public CommentReplyDialog()
        {
            InitializeComponent();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReplyTextBox.Text))
            {
                MessageBox.Show("Please enter a reply message.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
