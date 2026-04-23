namespace FinanceiroPessoal.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registra a rota do extrato para navegação
            Routing.RegisterRoute(nameof(Views.ExtratoCartaoPage),
                typeof(Views.ExtratoCartaoPage));

            // Cores da navbar
            this.SetAppThemeColor(Shell.BackgroundColorProperty,
                Color.FromArgb("#185FA5"),
                Color.FromArgb("#185FA5"));
        }
    }
}