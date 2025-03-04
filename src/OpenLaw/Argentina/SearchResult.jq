.document | {
    id: .metadata.uuid,
    contentType: .metadata["document-content-type"], 
    documentType: .content["tipo-norma"] | { code: .codigo, text: .texto },
    date: .content.fecha,
    status: (.content.estado // .content.status),
    timestamp: ((
          .content["fecha-umod"]? | 
          tostring |
          select(. != null and (length >= 14)) |
          .[0:4] + "-" + .[4:6] + "-" + .[6:8] + "T" + 
          .[8:10] + ":" + .[10:12] + ":" + .[12:14] + "Z" |
          fromdateiso8601
        ) // null)
}