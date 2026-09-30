using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.MasterData;

public sealed class GetDepartmentsHandler(IMasterDataStore store)
    : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentResponse>>>
{
    public Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken) =>
        store.GetDepartmentsAsync(cancellationToken);
}

public sealed class GetDepartmentByIdHandler(IMasterDataStore store)
    : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentResponse>>
{
    public Task<Result<DepartmentResponse>> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken) =>
        store.GetDepartmentByIdAsync(request.DepartmentId, cancellationToken);
}

public sealed class CreateDepartmentHandler(IMasterDataStore store)
    : IRequestHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    public Task<Result<DepartmentResponse>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken) =>
        store.CreateDepartmentAsync(request.Request, cancellationToken);
}

public sealed class UpdateDepartmentHandler(IMasterDataStore store)
    : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentResponse>>
{
    public Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken) =>
        store.UpdateDepartmentAsync(request.DepartmentId, request.Request, cancellationToken);
}

public sealed class GetCareerLevelsHandler(IMasterDataStore store)
    : IRequestHandler<GetCareerLevelsQuery, Result<IReadOnlyList<CareerLevelResponse>>>
{
    public Task<Result<IReadOnlyList<CareerLevelResponse>>> Handle(GetCareerLevelsQuery request, CancellationToken cancellationToken) =>
        store.GetCareerLevelsAsync(cancellationToken);
}

public sealed class GetCareerLevelByIdHandler(IMasterDataStore store)
    : IRequestHandler<GetCareerLevelByIdQuery, Result<CareerLevelResponse>>
{
    public Task<Result<CareerLevelResponse>> Handle(GetCareerLevelByIdQuery request, CancellationToken cancellationToken) =>
        store.GetCareerLevelByIdAsync(request.CareerLevelId, cancellationToken);
}

public sealed class CreateCareerLevelHandler(IMasterDataStore store)
    : IRequestHandler<CreateCareerLevelCommand, Result<CareerLevelResponse>>
{
    public Task<Result<CareerLevelResponse>> Handle(CreateCareerLevelCommand request, CancellationToken cancellationToken) =>
        store.CreateCareerLevelAsync(request.Request, cancellationToken);
}

public sealed class UpdateCareerLevelHandler(IMasterDataStore store)
    : IRequestHandler<UpdateCareerLevelCommand, Result<CareerLevelResponse>>
{
    public Task<Result<CareerLevelResponse>> Handle(UpdateCareerLevelCommand request, CancellationToken cancellationToken) =>
        store.UpdateCareerLevelAsync(request.CareerLevelId, request.Request, cancellationToken);
}

public sealed class GetProjectsHandler(IMasterDataStore store)
    : IRequestHandler<GetProjectsQuery, Result<IReadOnlyList<ProjectResponse>>>
{
    public Task<Result<IReadOnlyList<ProjectResponse>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken) =>
        store.GetProjectsAsync(cancellationToken);
}

public sealed class GetProjectByIdHandler(IMasterDataStore store)
    : IRequestHandler<GetProjectByIdQuery, Result<ProjectResponse>>
{
    public Task<Result<ProjectResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken) =>
        store.GetProjectByIdAsync(request.ProjectId, cancellationToken);
}

public sealed class CreateProjectHandler(IMasterDataStore store)
    : IRequestHandler<CreateProjectCommand, Result<ProjectResponse>>
{
    public Task<Result<ProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken) =>
        store.CreateProjectAsync(request.Request, cancellationToken);
}

public sealed class UpdateProjectHandler(IMasterDataStore store)
    : IRequestHandler<UpdateProjectCommand, Result<ProjectResponse>>
{
    public Task<Result<ProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken) =>
        store.UpdateProjectAsync(request.ProjectId, request.Request, cancellationToken);
}

public sealed class GetPaymentMethodsHandler(IMasterDataStore store)
    : IRequestHandler<GetPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodResponse>>>
{
    public Task<Result<IReadOnlyList<PaymentMethodResponse>>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken) =>
        store.GetPaymentMethodsAsync(cancellationToken);
}

public sealed class GetPaymentMethodByIdHandler(IMasterDataStore store)
    : IRequestHandler<GetPaymentMethodByIdQuery, Result<PaymentMethodResponse>>
{
    public Task<Result<PaymentMethodResponse>> Handle(GetPaymentMethodByIdQuery request, CancellationToken cancellationToken) =>
        store.GetPaymentMethodByIdAsync(request.PaymentMethodId, cancellationToken);
}

public sealed class CreatePaymentMethodHandler(IMasterDataStore store)
    : IRequestHandler<CreatePaymentMethodCommand, Result<PaymentMethodResponse>>
{
    public Task<Result<PaymentMethodResponse>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken) =>
        store.CreatePaymentMethodAsync(request.Request, cancellationToken);
}

public sealed class UpdatePaymentMethodHandler(IMasterDataStore store)
    : IRequestHandler<UpdatePaymentMethodCommand, Result<PaymentMethodResponse>>
{
    public Task<Result<PaymentMethodResponse>> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken) =>
        store.UpdatePaymentMethodAsync(request.PaymentMethodId, request.Request, cancellationToken);
}

public sealed class DeletePaymentMethodHandler(IMasterDataStore store)
    : IRequestHandler<DeletePaymentMethodCommand, Result>
{
    public Task<Result> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken) =>
        store.DeletePaymentMethodAsync(request.PaymentMethodId, cancellationToken);
}
