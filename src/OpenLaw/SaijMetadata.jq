.document | {
    "metadata-timestamp": .metadata.timestamp,
    "metadata-html": "https://www.saij.gob.ar/\(.metadata["friendly-url"].description)/\(.metadata.uuid)",
    "metadata-type": .metadata["document-content-type"],
    "norma-ref": .content["standard-normativo"],
    "norma-numero": .content["numero-norma"],
    "norma-tipo": .content["tipo-norma"].codigo,
    "norma-fecha": .content["fecha"],
    "norma-jurisdiccion": .content.jurisdiccion.codigo,
    "norma-titulo": .content["titulo-norma"],
    "norma-coloquial": "\(.content["identificacion-coloquial"]["identificacion-coloquial"][0]), \(.content["identificacion-coloquial"]["identificacion-coloquial"][1])",
    "pub-org": .content["publicacion-codificada"]["publicacion-decodificada"].organismo,
    "pub-fecha": (.content["publicacion-codificada"]["publicacion-decodificada"].fecha | gsub(" "; "-")),
    "estado": .content.estado,
    "keywords": (
        [.content.descriptores.descriptor[].elegido.termino] + 
        .content.descriptores.suggest.termino + 
        ([.content.descriptores.descriptor[].sinonimos.termino] | map(select(. != null)))
    ) | unique
}