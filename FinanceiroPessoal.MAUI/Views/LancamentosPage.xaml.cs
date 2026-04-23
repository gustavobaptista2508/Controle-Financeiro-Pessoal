using FinanceiroPessoal.MAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace FinanceiroPessoal.MAUI.Views
{
    public partial class LancamentosPage : ContentPage
    {
        public LancamentosPage(LancamentosViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((LancamentosViewModel)BindingContext).Carregar();
        }
    }
}