namespace Pokekool.Shared.Dtos.Chat;

public sealed record ChatRequestDto(
    string? SessionId,
    string Message,
    string? ImageBase64  // optionnel, pour UPC/produit scellé
);
