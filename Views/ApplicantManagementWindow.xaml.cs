using GrapheneSensore.ViewModels;
using System;
using System.Windows;

namespace GrapheneSensore.Views
{
    /// <summary>
    /// Interaction logic for ApplicantManagementWindow.xaml
    /// </summary>
    public partial class ApplicantManagementWindow : Window
    {
        public ApplicantManagementWindow(Guid userId)
        {
            InitializeComponent();
            DataContext = new ApplicantManagementViewModel(userId);
        }
    }
}
