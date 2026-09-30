namespace Core.Domain;

public enum PayrollStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4
}
