def to_timestamp:
  if . == null then null
  elif type == "number" then .
  else
    (sub("Z$"; "") | [match("\\d+"; "g").string] | join("") + "00000000000000" | .[0:14] | tonumber)
  end;

def articulos($node):
  if $node | type == "object" then
    if $node | has("articulo") then
      if $node.articulo | type == "array" then $node.articulo[] else $node.articulo end
    else
      empty
    end,
    if $node | has("segmento") then $node.segmento[] | articulos(.) else empty end
  elif $node | type == "array" then
    $node[] | articulos(.)
  else
    empty
  end;

def terms($field):
  if $field | type == "object" then
    if $field.termino? | type == "string" then [$field.termino]
    elif $field.termino? | type == "array" then $field.termino
    else empty
    end
  elif $field | type == "string" then [$field]
  else empty
  end;

def refs($context; $name):
  [ articulos($context),
    (if ($context.anexo | type) == "array" then $context.anexo[] else empty end)
  ] | flatten |
  map(.[$name]? | select(. != null) | .["referencia-normativa"]? |
      (if type == "array" then .[] | .ref else .ref end)) |
  flatten | select(. != null);

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
        (if .content.descriptores?.descriptor | type == "array" then
           .content.descriptores?.descriptor[] | terms(.elegido), terms(.preferido), terms(.sinonimos)
         elif .content.descriptores?.descriptor | type == "object" then
           [terms(.content.descriptores?.descriptor.elegido), 
            terms(.content.descriptores?.descriptor.preferido), 
            terms(.content.descriptores?.descriptor.sinonimos)] | add
         else
           empty
         end),
        terms(.content.descriptores?.suggest)
    ] | flatten | map(select(. != null)) | map(gsub("^\\s+|\\s+$"; "")) | map(select(. != "")) | unique,
    pub: first((.content["publicacion-codificada"] | .. | select(.organismo? != null) | { org: .organismo, date: (.fecha // .["fecha-sf"]) | gsub(" "; "-")}) // null),
    refs: {
        ammends: {
            by: refs(.content; "modificado-por") | unique | sort,
            to: refs(.content; "modifica-a") | unique | sort
        }, 
        repeals: {
            by: refs(.content; "derogado-por") | unique | sort,
            to: refs(.content; "deroga-a") | unique | sort
        }, 
        remarks: {
            by: refs(.content; "observado-por") | unique | sort,
            to: refs(.content; "observa-a") | unique | sort
        }
    }
}