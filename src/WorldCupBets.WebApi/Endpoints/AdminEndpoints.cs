using Microsoft.AspNetCore.Authorization;
using Wolverine;
using WorldCupBets.Application.Features.Admin;
using WorldCupBets.Domain.Common;

namespace WorldCupBets.WebApi.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("Admin");

        group.MapPost("/invitations", [Authorize(Policy = "Admin")] async (
            CreateUserInvitationRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<Result<CreateUserInvitationDto>>(
                new CreateUserInvitationCommand(request.Email, request.RoleName),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error?.Code switch
                {
                    "admin.role_not_found" => Results.Problem(
                        title: "Invitation configuration error",
                        detail: result.Error.Message,
                        statusCode: StatusCodes.Status500InternalServerError),
                    _ => Results.BadRequest(new { error = result.Error?.Message ?? "Unable to create invitation." })
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("CreateUserInvitation")
        .WithSummary("Invite a Google account email into the league.")
        .Produces<CreateUserInvitationDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/audit/balances", [Authorize(Policy = "Bettor")] async (
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<AuditBalanceSummaryDto>(
                new GetAuditBalanceSummaryQuery(),
                cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetAuditBalanceSummary")
        .WithSummary("Get the admin derived current-state balance summary for all users.")
        .Produces<AuditBalanceSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/audit/users/{userId:int}", [Authorize(Policy = "Bettor")] async (
            int userId,
            IMessageBus messageBus,
            CancellationToken cancellationToken) =>
        {
            var result = await messageBus.InvokeAsync<AuditUserSubledgerDto?>(
                new GetAuditUserSubledgerQuery(userId),
                cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetAuditUserSubledger")
        .WithSummary("Get the admin derived current-state audit subledger for one user.")
        .Produces<AuditUserSubledgerDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        return group;
    }
}

public sealed record CreateUserInvitationRequest(string Email, string RoleName);
