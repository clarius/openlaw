using System.ComponentModel;

namespace Clarius.OpenLaw.Argentina;

public enum Provincia
{
    [DisplayValue("Buenos Aires")]
    BuenosAires,
    Catamarca,
    Chaco,
    Chubut,
    [DisplayValue("Ciudad Autónoma de Buenos Aires")]
    [DisplayValue("Ciudad Autonoma de Buenos Aires")]
    [DisplayValue("CABA")]
    [Description("CABA")]
    CiudadAutonomaDeBuenosAires,
    [DisplayValue("Córdoba")]
    Cordoba,
    Corrientes,
    [DisplayValue("Entre Ríos")]
    [DisplayValue("Entre Rios")]
    EntreRios,
    Formosa,
    Jujuy,
    [DisplayValue("La Pampa")]
    LaPampa,
    [DisplayValue("La Rioja")]
    LaRioja,
    Mendoza,
    Misiones,
    [DisplayValue("Neuquén")]
    Neuquen,
    [DisplayValue("Río Negro")]
    [DisplayValue("Rio Negro")]
    RioNegro,
    Salta,
    [DisplayValue("San Juan")]
    SanJuan,
    [DisplayValue("San Luis")]
    SanLuis,
    [DisplayValue("Santa Cruz")]
    SantaCruz,
    [DisplayValue("Santa Fe")]
    SantaFe,
    [DisplayValue("Santiago del Estero")]
    SantiagoDelEstero,
    [DisplayValue("Tierra del Fuego")]
    TierraDelFuego,
    [DisplayValue("Tucumán")]
    Tucuman,
}