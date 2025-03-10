def to_timestamp:
  if . == null then null
  elif type == "number" then .
  else
    # Remove trailing "Z" if present
    (sub("Z$"; "") | 
    # Split date and time, ignoring milliseconds
    split("T") | 
    (.[0] | split("-") | map(tonumber)) as $date | 
    (if length > 1 then .[1] | split(":") | map(split(".") | .[0] | tonumber) else [0, 0, 0] end) as $time |
    # Extract year, month, day, hour, minute, second
    ($date[0]) as $year | ($date[1]) as $month | ($date[2]) as $day |
    ($time[0]) as $hour | ($time[1]) as $minute | ($time[2]) as $second |
    # Calculate days since 0001-01-01 (simplified, no leap year adjustment)
    (($year - 1) * 365.25 + ($month - 1) * 30.42 + $day) as $total_days |
    # Convert to seconds
    ($total_days * 86400 + $hour * 3600 + $minute * 60 + $second | floor))
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