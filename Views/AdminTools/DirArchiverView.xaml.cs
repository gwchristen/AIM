using AIM.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace AIM.Views.AdminTools
{
    public sealed partial class DirArchiverView : UserControl
    {
        public InventoryArchiveViewModel ViewModel { get; }

        public DirArchiverView()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<InventoryArchiveViewModel>();
            this.DataContext = ViewModel;
            // The Loaded event ensures that the list is populated when the view is shown.
            this.Loaded += (s, e) => ViewModel.LoadArchivedDirectoriesCommand.Execute(null);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string folderName)
            {
                ViewModel.ViewArchivedFolderCommand.Execute(folderName);
            }
        }
    }
}
