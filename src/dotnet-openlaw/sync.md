```shell
DESCRIPTION:
Sincroniza contenido de SAIJ

USAGE:
    openlaw sync [OPTIONS]

OPTIONS:
                          DEFAULT                                               
    -h, --help                        Prints help information                   
    -t, --tipo            Ley         Tipo de norma a sincronizar               
    -j, --jurisdiccion    Nacional    Jurisdicción a sincronizar                
    -p, --provincia                   Provincia a sincronizar                   
    -f, --filtro                      Filtros avanzados a aplicar (KEY=VALUE)   
        --vigente                     Mostrar solo leyes/decretos vigentes      
        --dir                         Ubicación opcional archivos. Por defecto  
                                      el directorio actual                      
        --changelog                   Escribir un resumen de las operaciones    
                                      efectuadas en el archivo especificado     
        --appendlog                   Agregar al log de cambios si ya existe    
```
