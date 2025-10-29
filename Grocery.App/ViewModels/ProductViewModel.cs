using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    public partial class ProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;


        private readonly GlobalViewModel _globalViewModel;

        public ObservableCollection<Product> Products { get; set; }


        //Constructor heeft nu GlobalViewModel parameter
        public ProductViewModel(IProductService productService, GlobalViewModel globalViewModel)
        {
            _productService = productService;
            _globalViewModel = globalViewModel; // UC19 NIEUW

            Products = [];
            foreach (Product p in _productService.GetAll())
            {
                Products.Add(p);
            }
        }

        // Wordt aangeroepen als gebruiker op "+" knop drukt
        [RelayCommand]
        private async Task NavigateToNewProduct()
        {
            // UC19: Check of gebruiker admin is
            if (_globalViewModel.Client.Role != Role.Admin)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Geen toegang",
                    "Alleen beheerders kunnen producten aanmaken.",
                    "OK");
                return;
            }

            // UC19: Navigeer naar NewProductView
            await Shell.Current.GoToAsync(nameof(NewProductView));
        }

        // Ververst productlijst als we terugkomen van NewProductView

        public override void OnAppearing()
        {
            base.OnAppearing();
            RefreshProducts();
        }


        // Herlaadt alle producten uit database
        private void RefreshProducts()
        {
            Products.Clear();
            foreach (Product p in _productService.GetAll())
            {
                Products.Add(p);
            }
        }
    }
}