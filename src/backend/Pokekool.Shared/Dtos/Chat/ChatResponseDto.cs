namespace Pokekool.Shared.Dtos.Chat;

public sealed record ChatResponseDto(
    string SessionId,
    string Message,
    ChatPendingAction? PendingAction,  // non null si une confirmation est attendue
    ChatClientDirective? Directive     // ex: redirect_camera
);

public sealed record ChatPendingAction(
    string ActionId,
    string Summary,         // récap lisible de ce qui sera fait
    string ToolName,        // outil qui sera appelé sur confirmation
    string ArgsJson         // JSON des arguments (sérialisé)
);

public sealed record ChatClientDirective(
    string Type,    // "redirect_camera"
    string? Url     // route Angular cible
);
