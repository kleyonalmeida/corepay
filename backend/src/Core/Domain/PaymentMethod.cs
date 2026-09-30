namespace Core.Domain;

public sealed class PaymentMethod
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
