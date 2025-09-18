using System.Windows.Controls;
using tkkn2025.DataAccess;

namespace tkkn2025.UI.UserControls
{
    /// <summary>
    /// Interaction logic for FirebaseEditorView.xaml
    /// This UserControl uses MVVM pattern with FireBaseEditorVM as its ViewModel
    /// </summary>
    public partial class FirebaseEditorView : UserControl
    {
        public FirebaseEditorView()
        {
            InitializeComponent();
            
            // Handle TreeView selection change since SelectedItem is not bindable
            DatabaseTreeView.SelectedItemChanged += OnTreeViewSelectionChanged;
        }

        private void OnTreeViewSelectionChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is FireBaseEditorVM viewModel && e.NewValue is DatabaseNodeVM selectedNode)
            {
                viewModel.SelectedNode = selectedNode;
            }
        }
    }
}
