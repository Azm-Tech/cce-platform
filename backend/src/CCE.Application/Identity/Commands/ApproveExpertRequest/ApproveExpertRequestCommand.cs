using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.ApproveExpertRequest;

public sealed record ApproveExpertRequestCommand(
    System.Guid Id,
    string AcademicTitleAr,
    string AcademicTitleEn) : IRequest<ExpertProfileDto>;
