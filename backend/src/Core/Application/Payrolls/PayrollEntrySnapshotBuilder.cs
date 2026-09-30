using Core.Domain;
using Core.Domain.PayrollCalculation;

namespace Core.Application.Payrolls;

public static class PayrollEntrySnapshotBuilder
{
    public static PayrollCollaboratorEntry Build(
        Collaborator collaborator,
        Department department,
        CareerLevel? careerLevel,
        bool isApproved = false)
    {
        ArgumentNullException.ThrowIfNull(collaborator);
        ArgumentNullException.ThrowIfNull(department);

        return new PayrollCollaboratorEntry
        {
            Id = Guid.NewGuid(),
            CollaboratorId = collaborator.Id,
            CollaboratorName = collaborator.Name,
            PixKey = collaborator.PixKey,
            AdmissionDate = collaborator.AdmissionDate,
            CareerLevelName = careerLevel?.Name,
            CalculationProfile = CalculationProfileResolver.Resolve(collaborator, department, careerLevel),
            DepartmentId = collaborator.DepartmentId,
            CareerLevelId = collaborator.CareerLevelId,
            FullBaseSalary = collaborator.BaseSalary ?? careerLevel?.BaseSalary,
            Payload = new PayrollCollaboratorEntryPayload(),
            IsApproved = isApproved,
            IsPaid = false,
            NfSent = false
        };
    }
}
