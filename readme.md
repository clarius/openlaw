![Icon](assets/img/icon.png) OpenLaw
============

[![tool](https://img.shields.io/endpoint?url=https://shields.kzu.app/v/dotnet-openlaw?f=https://clarius.blob.core.windows.net/nuget/index.json&label=dotnet-openlaw&color=blue)](https://clarius.blob.core.windows.net/nuget/index.json)
[![lib](https://img.shields.io/endpoint?url=https://shields.kzu.app/v/clarius.openlaw?f=https://clarius.blob.core.windows.net/nuget/index.json&label=Clarius.OpenLaw&color=purple)](https://clarius.blob.core.windows.net/nuget/index.json)
[![commands](https://img.shields.io/endpoint?url=https://shields.kzu.app/v/clarius.openlaw.commands?f=https://clarius.blob.core.windows.net/nuget/index.json&label=Clarius.OpenLaw.Commands&color=purple)](https://clarius.blob.core.windows.net/nuget/index.json)
[![Build](https://github.com/clarius/openlaw/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/clarius/openlaw/actions)

Plataforma de código abierto para normas de Argentina.

## Instalación

```
dotnet tool install -g dotnet-openlaw --source https://clarius.blob.core.windows.net/nuget/index.json
```

## Uso:

<!-- include src/dotnet-openlaw/help.md -->
```shell
USAGE:
    openlaw [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    convert          Convierte archivos JSON a YAML, Markdown y PDF
    format           Normaliza el formato de archivos JSON         
    sync             Sincroniza contenido de SAIJ                  
    syncitem <ID>    Sincroniza un documento especifico de SAIJ    
```

<!-- src/dotnet-openlaw/help.md -->

<!-- include src/dotnet-openlaw/sync.md -->
```shell
DESCRIPTION:
Sincroniza contenido de SAIJ

USAGE:
    openlaw sync [OPTIONS]

OPTIONS:
                          DEFAULT                                               
    -h, --help                           Prints help information                
    -t, --tipo            Ley            Tipo de norma a sincronizar (ley,      
                                         decreto, resolucion, disposicion,      
                                         decision, acordada)                    
    -j, --jurisdiccion    Nacional       Jurisdicción a sincronizar (nacional,  
                                         internacional, local, federal)         
    -p, --provincia                      Provincia a sincronizar (buenosaires,  
                                         catamarca, chaco, chubut, caba,        
                                         cordoba, corrientes, entrerios,        
                                         formosa, jujuy, lapampa, larioja,      
                                         mendoza, misiones, neuquen, rionegro,  
                                         salta, sanjuan, sanluis, santacruz,    
                                         santafe, santiagodelestero,            
                                         tierradelfuego, tucuman)               
    -c, --content-type    Legislacion    Tipo de contenido a sincronizar        
                                         (legislacion, novedad)                 
    -f, --filtro                         Filtros avanzados a aplicar (KEY=VALUE)
        --vigente                        Mostrar solo leyes/decretos vigentes   
        --dir                            Ubicación opcional archivos. Por       
                                         defecto el directorio actual           
        --changelog                      Escribir un resumen de las operaciones 
                                         efectuadas en el archivo especificado  
        --appendlog                      Agregar al log de cambios si ya existe 
```

<!-- src/dotnet-openlaw/sync.md -->

<!-- include src/dotnet-openlaw/convert.md -->
```shell
DESCRIPTION:
Convierte archivos JSON a YAML, Markdown y PDF

USAGE:
    openlaw convert [file] [OPTIONS]

ARGUMENTS:
    [file]    Archivo a convertir. Opcional

OPTIONS:
                       DEFAULT                                                  
    -h, --help                    Prints help information                       
        --dir                     Ubicación de archivos a convertir. Por defecto
                                  '%AppData%\clarius\openlaw'                   
        --overwrite               Sobreescribir archivos existentes. Por defecto
                                  'false'                                       
        --yaml         True       Generar archivos YAML. Por defecto 'true'     
        --pdf          True       Generar archivos PDF. Por defecto 'true'      
        --md           True       Generar archivos Markdown. Por defecto 'true' 
```

<!-- src/dotnet-openlaw/convert.md -->

<!-- include src/dotnet-openlaw/format.md -->
```shell
DESCRIPTION:
Normaliza el formato de archivos JSON

USAGE:
    openlaw format [OPTIONS]

OPTIONS:
    -h, --help    Prints help information                                       
        --dir     Ubicación de archivos a formatear. Por defecto                
                  '%AppData%\clarius\openlaw'                                   
```

<!-- src/dotnet-openlaw/format.md -->

## WhatsApp API

El proyecto incluye integracion con WhatsApp for Business para consultas de contenido.

Para usar la API de WhatsApp, es necesario tener una cuenta de WhatsApp Business y un 
número de teléfono asociado. Ver [Devlooped.WhatsApp](https://github.com/devlooped/WhatsApp?tab=readme-ov-file#configuration) 
para más detalles de configuración específica de la API de WhatsApp.

### CI/CD

El deployment esta configurado para ocurrir automaticamente en push a main.

Para el deployment, debe configurarse el fork de este repo con las siguientes valores:

* `AZURE_CREDENTIALS` *secret*: credenciales de Azure para el deployment de la API
* `APP_NAME` *variable*: nombre de la app de Azure Functions a deployear, como variable de repo.

