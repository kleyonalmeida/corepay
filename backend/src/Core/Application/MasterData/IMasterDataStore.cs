using BuildingBlocks.Results;

namespace Core.Application.MasterData;

public interface IMasterDataStore
{
    Task<Result<IReadOnlyList<DepartmentResponse>>> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<Result<DepartmentResponse>> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<Result<DepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<DepartmentResponse>> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CareerLevelResponse>>> GetCareerLevelsAsync(CancellationToken cancellationToken = default);

    Task<Result<CareerLevelResponse>> GetCareerLevelByIdAsync(Guid careerLevelId, CancellationToken cancellationToken = default);

    Task<Result<CareerLevelResponse>> CreateCareerLevelAsync(CreateCareerLevelRequest request, CancellationToken cancellationToken = default);

    Task<Result<CareerLevelResponse>> UpdateCareerLevelAsync(Guid careerLevelId, UpdateCareerLevelRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProjectResponse>>> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<Result<ProjectResponse>> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<Result<ProjectResponse>> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    Task<Result<ProjectResponse>> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PaymentMethodResponse>>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);

    Task<Result<PaymentMethodResponse>> GetPaymentMethodByIdAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);

    Task<Result<PaymentMethodResponse>> CreatePaymentMethodAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default);

    Task<Result<PaymentMethodResponse>> UpdatePaymentMethodAsync(Guid paymentMethodId, UpdatePaymentMethodRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeletePaymentMethodAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);
}
