namespace FinanceiroPessoal.Web.Models;

public class BillingOptions
{
    public bool EnableTrial { get; set; } = true;
    public int TrialDays { get; set; } = 14;
}
