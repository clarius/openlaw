.document | {
    id: .metadata.uuid,
    number: (.content["numero-norma"] // .content["numero_norma"] // null), 
    title: (.content["titulo-norma"] // .content["titulo_noticia"] // null),
    summary: (.content.sintesis // ([.content.sumario | .. | select(type == "string")] | join(""))),
    type: .metadata["document-content-type"], 
    kind: "$$kind$$",
    status: (.content.estado // .content.status),
    date: .content.fecha,
    modified: (.content["fecha-umod"] // (.content.fecha | gsub("-"; ""))),
    timestamp: (.metadata.timestamp // .content["fecha-umod"] // (.content.fecha | gsub("-"; "")))
}