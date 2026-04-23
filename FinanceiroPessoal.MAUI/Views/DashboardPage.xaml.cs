using FinanceiroPessoal.MAUI.ViewModels;

namespace FinanceiroPessoal.MAUI.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((DashboardViewModel)BindingContext).Carregar();
        }
    }
}