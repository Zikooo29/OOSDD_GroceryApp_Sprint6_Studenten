using Grocery.App.ViewModels;

namespace Grocery.App.Views
{
    // ═══════════════════════════════════════════════════════════
    // UC19 NIEUW: Dit hele bestand is nieuw!
    // Code-behind voor NewProductView
    // ═══════════════════════════════════════════════════════════
    public partial class NewProductView : ContentPage
    {
        public NewProductView(NewProductViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}