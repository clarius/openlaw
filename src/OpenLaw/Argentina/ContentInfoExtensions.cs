namespace Clarius.OpenLaw.Argentina;

public static class ContentInfoExtensions
{
    public static string DataUrl(this IContentInfo info) => $"https://www.saij.gob.ar/view-document?guid={info.Id}";

    public static string WebUrl(this IContentInfo info) => info switch
    {
        DocumentAbstract doc => doc.WebUrl,
        Legislation leg => leg.WebUrl,
        _ => $"https://www.saij.gob.ar/{info.Id}",
    };
}
