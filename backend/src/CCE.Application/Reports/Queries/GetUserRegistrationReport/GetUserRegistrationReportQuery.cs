using CCE.Application.Common;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetUserRegistrationReport;

public sealed record GetUserRegistrationReportQuery()
    : IRequest<Response<List<UserRegistrationReportUserDto>>>;
