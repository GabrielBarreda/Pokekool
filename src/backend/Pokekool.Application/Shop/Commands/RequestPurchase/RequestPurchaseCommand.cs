using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Shop.Commands.RequestPurchase;

/// <summary>
/// Le client authentifié manifeste son intérêt pour un article du présentoir.
/// Crée une PurchaseRequest (aucune transaction financière). Nom/email pris du compte.
/// </summary>
public sealed record RequestPurchaseCommand(Guid ShopProductId, string? Message) : IRequest<Guid>;

public sealed class RequestPurchaseCommandValidator : AbstractValidator<RequestPurchaseCommand>
{
    public RequestPurchaseCommandValidator()
    {
        RuleFor(c => c.ShopProductId).NotEmpty();
        RuleFor(c => c.Message).MaximumLength(2000);
    }
}

public sealed class RequestPurchaseCommandHandler : IRequestHandler<RequestPurchaseCommand, Guid>
{
    private readonly IShopClientRepository _shop;
    private readonly IPurchaseRequestRepository _requests;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RequestPurchaseCommandHandler> _logger;

    public RequestPurchaseCommandHandler(
        IShopClientRepository shop,
        IPurchaseRequestRepository requests,
        IUserRepository users,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<RequestPurchaseCommandHandler> logger)
    {
        _shop = shop;
        _requests = requests;
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(RequestPurchaseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var product = await _shop.GetProductAvailabilityAsync(request.ShopProductId, cancellationToken)
            ?? throw new DomainException("shop.product_not_found", "Product not found.");

        if (!product.IsAvailable || product.Quantity <= 0)
            throw new DomainException("shop.product_unavailable", "This product is no longer available.");

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("account.not_found", "User not found.");

        var clientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(clientName)) clientName = user.Email.Value;

        var purchaseRequest = PurchaseRequest.Create(
            request.ShopProductId, userId, clientName, user.Email.Value, request.Message);

        await _requests.AddAsync(purchaseRequest, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // V1 : log. V2 : notification email à la boutique.
        _logger.LogInformation(
            "PurchaseRequest {RequestId} created by user {UserId} for product {ProductId}",
            purchaseRequest.Id, userId, request.ShopProductId);

        return purchaseRequest.Id;
    }
}
