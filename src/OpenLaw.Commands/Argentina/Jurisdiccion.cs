namespace Clarius.OpenLaw.Argentina;

public enum Jurisdiccion
{
    Nacional,
    /// <summary>
    /// Aplica solo a <see cref="TipoNorma.Ley"/> y <see cref="TipoNorma.Decreto"/>.
    /// </summary>
    Internacional,
    [DisplayValue("Local")]
    Provincial,
    /// <summary>
    /// Aplica exclusivamente a <see cref="TipoNorma.Acordada"/>.
    /// </summary>
    Federal,
}