using Grocery.App.ViewModels;

namespace Grocery.App.Views
{
    public partial class ProductView : ContentPage
    {
        public ProductView(ProductViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 NIEUW: OnAppearing override
        // Ververst productlijst als we terugkomen van NewProductView
        // ═══════════════════════════════════════════════════════════
        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is ProductViewModel bindingContext)
            {
                bindingContext.OnAppearing();
            }
        }
    }
}