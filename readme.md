![Icon](assets/img/icon.png) OpenLaw
============

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
    convert    Convierte archivos JSON a YAML, Markdown y PDF
    format     Normaliza el formato de archivos JSON         
    ar                                                       
```

<!-- src/dotnet-openlaw/help.md -->

<!-- include src/dotnet-openlaw/ar-download.md -->
```shell
DESCRIPTION:
Descargar normas argentinas del sistema SAIJ.

USAGE:
    openlaw ar download [OPTIONS]

OPTIONS:
                     DEFAULT                                                    
    -h, --help                  Prints help information                         
        --all        True       Descargar todos los documentos, no solamente    
                                Leyes de alcance Nacional                       
        --convert    True       Convertir automaticamente documentos nuevos     
                                descargados a Markdown, PDF y YAML              
        --dir                   Ubicación opcional para descarga de archivos.   
                                Por defecto '%AppData%\clarius\openlaw'         
```

<!-- src/dotnet-openlaw/ar-download.md -->

<!-- include src/dotnet-openlaw/convert.md -->
```shell
DESCRIPTION:
Convierte archivos JSON a YAML, Markdown y PDF.

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
Normaliza el formato de archivos JSON.

USAGE:
    openlaw format [OPTIONS]

OPTIONS:
    -h, --help    Prints help information                                       
        --dir     Ubicación de archivos a formatear. Por defecto                
                  '%AppData%\clarius\openlaw'                                   
```

<!-- src/dotnet-openlaw/format.md -->
