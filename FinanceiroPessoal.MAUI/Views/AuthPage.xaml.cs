using FinanceiroPessoal.MAUI.ViewModels;

namespace FinanceiroPessoal.MAUI.Views  // ← mesmo namespace
{
    public partial class AuthPage : ContentPage  // ← mesmo nome da classe
    {
        public AuthPage(AuthViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}