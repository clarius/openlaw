.document | {
    id: .metadata.uuid,
    ref: .content["standard-normativo"],
    timestamp: .metadata.timestamp,
    name: .content["nombre-coloquial"] | tostring,
    number: .content["numero-norma"], 
    title: .content["titulo-norma"],
    summary: [.content.sumario | .. | select(type == "string")] | join(""),
    type: .metadata["document-content-type"], 
    kind: .content["tipo-norma"].codigo,
    status: .content.estado,
    date: .content.fecha,
    pub: first((.content["publicacion-codificada"] | .. | select(.organismo? != null) | { org: .organismo, date: (.fecha // .["fecha-sf"]) | gsub(" "; "-")}) // null),
    modified: (.content["fecha-umod"] // (.content.fecha | gsub("-"; ""))),
    terms: [(.content.descriptores | .. | .termino?)] | map(select(. != null)) | flatten | unique
}