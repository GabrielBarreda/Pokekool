using Pokekool.Shared.Dtos.Chat;

namespace Pokekool.Application.Chat;

public interface IChatOrchestrator
{
    Task<ChatResponseDto> ProcessAsync(ChatRequestDto request, CancellationToken ct = default);
}
