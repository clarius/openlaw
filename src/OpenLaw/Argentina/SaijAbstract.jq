﻿.document | {
    id: .metadata.uuid,
    number: (.content["numero-norma"] // .content["numero_norma"] // null), 
    title: (.content["titulo-norma"] // .content["titulo_noticia"] // null),
    summary: (.content.sintesis // ([.content.sumario | .. | select(type == "string")] | join(""))),
    type: .metadata["document-content-type"], 
    kind: .content["tipo-norma"] | { code: .codigo, text: .texto },
    status: (.content.estado // .content.status),
    date: .content.fecha,
    modified: (
        (.content["fecha-umod"]? | select(. != null) | tostring | .[0:4] + "-" + .[4:6] + "-" + .[6:8]) // 
        .content.fecha
    ),
    timestamp: (
        (
          .content["fecha-umod"]? | 
          tostring |
          select(. != null and (length >= 14)) |
          .[0:4] + "-" + .[4:6] + "-" + .[6:8] + "T" + 
          .[8:10] + ":" + .[10:12] + ":" + .[12:14] + "Z" |
          fromdateiso8601
        ) // 
        (
          .content["timestamp-m"]? | 
          select(. != null) | 
          . + "Z" | 
          fromdateiso8601
        ) //
        (
          .content["timestamp"]? | 
          select(. != null) | 
          . + "Z" | 
          fromdateiso8601
        ) //
        (
          .content.fecha? | 
          select(. != null) | 
          . + "T00:00:00Z" |
          fromdateiso8601
        ) //
        .metadata.timestamp
    )
}