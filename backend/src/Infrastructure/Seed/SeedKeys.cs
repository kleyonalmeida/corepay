namespace Infrastructure.Seed;

public static class SeedKeys
{
    public static class Departments
    {
        public const string Tipster = "department:tipster";
        public const string PaidTraffic = "department:paid-traffic";
        public const string ProjectLeaders = "department:project-leaders";
        public const string CommercialAnalysts = "department:commercial-analysts";
        public const string Management = "department:management";
        public const string Affiliates = "department:affiliates";
        public const string Administrative = "department:administrative";
        public const string Automation = "department:automation";
        public const string Contingency = "department:contingency";
        public const string Support = "department:support";
    }

    public static class CareerLevels
    {
        public const string CommercialAnalystJunior = "career-level:commercial-analyst-junior";
        public const string CommercialSupervisor = "career-level:commercial-supervisor";
        public const string PaidTrafficSenior = "career-level:paid-traffic-senior";
    }

    public static class Projects
    {
        public const string LimaKarttos = "project:lima-karttos";
        public const string FeiraX = "project:feira-x";
        public const string ThreeCSports = "project:3c-sports";
        public const string Affiliates = "project:affiliates";
        public const string LastlinkSample = "project:lastlink-sample";
        public const string HublaSample = "project:hubla-sample";
    }

    public static class Collaborators
    {
        public const string CommercialAnalystActive = "collaborator:commercial-analyst-active";
        public const string PaidTrafficInactive = "collaborator:paid-traffic-inactive";
    }

    public static class Demo
    {
        public const string PaymentMethod = "demo:payment-method";
        public const string Revenue = "demo:revenue";
        public const string AnalystMetric = "demo:analyst-metric";
        public const string TrafficInvestment = "demo:traffic-investment";
        public const string TrafficDeposit = "demo:traffic-deposit";
        public const string CashflowIncome = "demo:cashflow-income";
        public const string CashflowExpense = "demo:cashflow-expense";
        public static string Payroll(string status) => $"demo:payroll:{status}";
        public static string PayrollEntry(string status) => $"demo:payroll-entry:{status}";
        public static string Notification(string status) => $"demo:notification:{status}";
    }
}
