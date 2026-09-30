using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.MasterData;

public sealed record GetDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentResponse>>>;

public sealed record GetDepartmentByIdQuery(Guid DepartmentId) : IRequest<Result<DepartmentResponse>>;

public sealed record GetCareerLevelsQuery : IRequest<Result<IReadOnlyList<CareerLevelResponse>>>;

public sealed record GetCareerLevelByIdQuery(Guid CareerLevelId) : IRequest<Result<CareerLevelResponse>>;

public sealed record GetProjectsQuery : IRequest<Result<IReadOnlyList<ProjectResponse>>>;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<Result<ProjectResponse>>;

public sealed record GetPaymentMethodsQuery : IRequest<Result<IReadOnlyList<PaymentMethodResponse>>>;

public sealed record GetPaymentMethodByIdQuery(Guid PaymentMethodId) : IRequest<Result<PaymentMethodResponse>>;
