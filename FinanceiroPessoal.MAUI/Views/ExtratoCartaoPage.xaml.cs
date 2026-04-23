using FinanceiroPessoal.MAUI.ViewModels;

namespace FinanceiroPessoal.MAUI.Views
{
    public partial class ExtratoCartaoPage : ContentPage
    {
        public ExtratoCartaoPage(ExtratoCartaoViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((ExtratoCartaoViewModel)BindingContext).Carregar();
        }
    }
}