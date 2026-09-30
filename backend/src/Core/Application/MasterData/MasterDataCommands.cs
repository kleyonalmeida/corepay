using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.MasterData;

public sealed record CreateDepartmentCommand(CreateDepartmentRequest Request) : IRequest<Result<DepartmentResponse>>;

public sealed record UpdateDepartmentCommand(Guid DepartmentId, UpdateDepartmentRequest Request) : IRequest<Result<DepartmentResponse>>;

public sealed record CreateCareerLevelCommand(CreateCareerLevelRequest Request) : IRequest<Result<CareerLevelResponse>>;

public sealed record UpdateCareerLevelCommand(Guid CareerLevelId, UpdateCareerLevelRequest Request) : IRequest<Result<CareerLevelResponse>>;

public sealed record CreateProjectCommand(CreateProjectRequest Request) : IRequest<Result<ProjectResponse>>;

public sealed record UpdateProjectCommand(Guid ProjectId, UpdateProjectRequest Request) : IRequest<Result<ProjectResponse>>;

public sealed record CreatePaymentMethodCommand(CreatePaymentMethodRequest Request) : IRequest<Result<PaymentMethodResponse>>;

public sealed record UpdatePaymentMethodCommand(Guid PaymentMethodId, UpdatePaymentMethodRequest Request) : IRequest<Result<PaymentMethodResponse>>;

public sealed record DeletePaymentMethodCommand(Guid PaymentMethodId) : IRequest<Result>;
