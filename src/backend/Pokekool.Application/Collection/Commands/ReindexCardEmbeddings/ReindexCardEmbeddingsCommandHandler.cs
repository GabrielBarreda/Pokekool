using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Collection.Commands.ReindexCardEmbeddings;

public sealed class ReindexCardEmbeddingsCommandHandler
    : IRequestHandler<ReindexCardEmbeddingsCommand, ReindexCardEmbeddingsResult>
{
    private readonly IPokemonCardRepository _cardRepo;
    private readonly ICardEmbeddingService _embeddingService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReindexCardEmbeddingsCommandHandler> _logger;

    public ReindexCardEmbeddingsCommandHandler(
        IPokemonCardRepository cardRepo,
        ICardEmbeddingService embeddingService,
        IUnitOfWork uow,
        ILogger<ReindexCardEmbeddingsCommandHandler> logger)
    {
        _cardRepo = cardRepo;
        _embeddingService = embeddingService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ReindexCardEmbeddingsResult> Handle(
        ReindexCardEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        var batch = await _cardRepo.GetWithoutEmbeddingAsync(request.BatchSize, cancellationToken);
        if (batch.Count == 0)
            return new ReindexCardEmbeddingsResult(Processed: 0, Failed: 0, Remaining: 0);

        var processed = 0;
        var failed = 0;

        foreach (var card in batch)
        {
            var imageUrl = card.ImageLarge ?? card.ImageSmall;
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                failed++;
                _logger.LogWarning("Carte {CardId} ({Name}) sans image — impossible d'indexer", card.Id, card.Name);
                continue;
            }

            var embedding = await _embeddingService.EmbedFromUrlAsync(imageUrl, cancellationToken);
            if (embedding.Status != CardEmbeddingStatus.Ok || embedding.Embedding is null)
            {
                failed++;
                _logger.LogWarning(
                    "Échec embedding carte {CardId} ({Name}) : {Status} {Diagnostic}",
                    card.Id, card.Name, embedding.Status, embedding.Diagnostic);
                continue;
            }

            await _cardRepo.SetEmbeddingAsync(card.Id, embedding.Embedding, cancellationToken);
            processed++;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var remaining = await _cardRepo.CountWithoutEmbeddingAsync(cancellationToken);
        return new ReindexCardEmbeddingsResult(processed, failed, remaining);
    }
}
