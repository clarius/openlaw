namespace Clarius.OpenLaw.Argentina;

public record SyncActionResult(ContentAction Action, Document NewDocument, Document? OldDocument);
