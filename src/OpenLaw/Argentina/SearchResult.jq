def to_timestamp:
  if . == null then null
  elif type == "number" then .
  else
    (sub("Z$"; "") | [match("\\d+"; "g").string] | join("") + "00000000000000" | .[0:14] | tonumber)
  end;
  
.document | {
    id: .metadata.uuid,
    contentType: .metadata["document-content-type"], 
    documentType: .content["tipo-norma"] | { code: .codigo, text: .texto },
    date: .content.fecha,
    status: (.content.estado // .content.status),
    timestamp: (
        (
          .content["fecha-umod"]? | 
          tostring |
          select(. != null and (length >= 14)) |
          .[0:4] + "-" + .[4:6] + "-" + .[6:8] + "T" + 
          .[8:10] + ":" + .[10:12] + ":" + .[12:14] + "Z" |
          to_timestamp
        ) // null)
}