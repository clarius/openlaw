def to_timestamp:
  if . == null then null
  elif type == "number" then .
  else
    (sub("Z$"; "") | [match("\\d+"; "g").string] | join("") + "00000000000000" | .[0:14] | tonumber)
  end;

.document | {
    id: .metadata.uuid,
    type: .metadata["document-content-type"], 
    alias: (.content["id-infojus"] // .metadata.uuid),
    ref: .content["standard-normativo"],
    name: .content["nombre-coloquial"] | tostring,
    number: (.content["numero-norma"] // .content["numero_norma"] // null), 
    title: (.content["titulo-norma"] // .content["titulo_noticia"] // null),
    summary: (.content.sintesis // ([.content.sumario | .. | select(type == "string")] | join(""))),
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
          to_timestamp
        ) // 
        (.content["timestamp-m"]? | select(. != null) | . + "Z" | to_timestamp) //
        (.content["timestamp"]? | select(. != null) | . + "Z" | to_timestamp) //
        (.content.fecha? | select(. != null) | . + "T00:00:00Z" | to_timestamp) //
        (.metadata.timestamp | to_timestamp)
    ),
    terms: [
        (.content.descriptores?.descriptor?.elegido?.termino?, 
        .content.descriptores?.descriptor?.preferido?.termino?, 
        .content.descriptores?.descriptor?.sinonimos?.termino[]?, 
        .content.descriptores?.suggest?.termino[]?)
    ] | map(select(. != null)) | flatten | unique | map(gsub("^\\s+|\\s+$"; "")),
    pub: first((.content["publicacion-codificada"] | .. | select(.organismo? != null) | { org: .organismo, date: (.fecha // .["fecha-sf"]) | gsub(" "; "-")}) // null)
}