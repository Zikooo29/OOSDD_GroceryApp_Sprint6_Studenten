using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.Generic;

namespace Grocery.App.ViewModels
{

    public partial class NewProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly GlobalViewModel _globalViewModel;

        //Properties voor het formulier
        [ObservableProperty]
        private string productName = "";

        [ObservableProperty]
        private int stock = 0;

        [ObservableProperty]
        private DateOnly shelfLife = DateOnly.FromDateTime(DateTime.Now.AddMonths(1));

        [ObservableProperty]
        private decimal price = 0.00m;

        [ObservableProperty]
        private string message = "";

        public NewProductViewModel(IProductService productService, GlobalViewModel globalViewModel)
        {
            _productService = productService;
            _globalViewModel = globalViewModel;
            Title = "Nieuw Product";
        }


        // Dit wordt aangeroepen als gebruiker op "Product Aanmaken" klikt
        [RelayCommand]
        private async Task CreateProduct()
        {
            // Check of gebruiker admin is
            if (_globalViewModel.Client.Role != Role.Admin)
            {
                Message = "Je hebt geen rechten om producten aan te maken. Alleen admins kunnen dit doen.";
                return;
            }

            // Validatie van invoer
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                Message = "Product naam is verplicht!";
                return;
            }

            if (Stock < 0)
            {
                Message = "Voorraad kan niet negatief zijn!";
                return;
            }

            if (Price < 0)
            {
                Message = "Prijs kan niet negatief zijn!";
                return;
            }

            if (ShelfLife < DateOnly.FromDateTime(DateTime.Now))
            {
                Message = "THT datum kan niet in het verleden liggen!";
                return;
            }

            try
            {
                // Maak nieuw product object
                Product newProduct = new Product(
                    id: 0,
                    name: ProductName,
                    stock: Stock,
                    shelfLife: ShelfLife,
                    price: Price
                );

                // Sla op in database via service
                Product addedProduct = _productService.Add(newProduct);

                //Toon success bericht
                Message = $"Product '{addedProduct.Name}' is succesvol aangemaakt met ID {addedProduct.Id}!";

                //Maak velden leeg voor nieuw product
                ProductName = "";
                Stock = 0;
                ShelfLife = DateOnly.FromDateTime(DateTime.Now.AddMonths(1));
                Price = 0.00m;

                //  Ga terug naar productlijst na 2 seconden
                await Task.Delay(2000);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Message = $"Fout bij aanmaken product: {ex.Message}";
            }
        }


        // Annuleer en ga terug
        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}