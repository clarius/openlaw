namespace Clarius.OpenLaw.Argentina;

public record Location(string Text, string Data);

public record SyncActionResult(ContentAction Action, Document NewDocument, Document? OldDocument, Location Location);
