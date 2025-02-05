.document | {
    id: .metadata.uuid,
    ref: .content["standard-normativo"],
    name: .content["nombre-coloquial"],
    number: .content["numero-norma"], 
    title: .content["titulo-norma"],
    summary: .content.sumario,
    type: .metadata["document-content-type"], 
    kind: .content["tipo-norma"].codigo,
    mecano: .content.mecanografico,
    status: .content.estado,
    date: .content.fecha,
    pub: { 
        org: .content["publicacion-codificada"]["publicacion-decodificada"].organismo,
        date: .content["publicacion-codificada"]["publicacion-decodificada"].fecha
    },
    terms: (
        [.content.descriptores.descriptor[].elegido.termino] + 
        .content.descriptores.suggest.termino + 
        ([.content.descriptores.descriptor[].sinonimos.termino] | map(select(. != null)) | .[])
    ) | unique
}