namespace Clarius.OpenLaw.Argentina;

public enum TipoNorma
{
    Ley = 1,
    Decreto = 2,
    [DisplayValue("Resolución")]
    Resolucion = 3,
    [DisplayValue("Disposición")]
    Disposicion = 4,
    [DisplayValue("Decisión")]
    Decision = 5,
    Acordada = 6,
}
