.document | {
    id: .metadata.uuid,
    ref: .content["standard-normativo"],
    name: .content["nombre-coloquial"] | tostring,
    number: (.content["numero-norma"] // .content["numero_norma"] // null), 
    title: (.content["titulo-norma"] // .content["titulo_noticia"] // null),
    summary: (.content.sintesis // ([.content.sumario | .. | select(type == "string")] | join(""))),
    type: .metadata["document-content-type"], 
    kind: .content["tipo-norma"].codigo,
    status: (.content.estado // .content.status),
    date: .content.fecha,
    modified: (.content["fecha-umod"] // (.content.fecha | gsub("-"; ""))),
    timestamp: (.metadata.timestamp // .content["fecha-umod"] // (.content.fecha | gsub("-"; ""))),
    terms: [(.content.descriptores | .. | .termino?)] | map(select(. != null)) | flatten | unique,
    pub: first((.content["publicacion-codificada"] | .. | select(.organismo? != null) | { org: .organismo, date: (.fecha // .["fecha-sf"]) | gsub(" "; "-")}) // null)
}