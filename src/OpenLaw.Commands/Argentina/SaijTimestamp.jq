def to_fecha:
  if . == null then null
  else if type == "number" then tostring | if length == 4 then . + "-01-01" else . end else . end
  end;

def to_timestamp:
  if . == null then null
  elif type == "number" then .
  else
    (sub("Z$"; "") | [match("\\d+"; "g").string] | join("") + "00000000000000" | .[0:14] | tonumber)
  end;

.document | 
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
    (.content.fecha? | to_fecha | select(. != null) | . + "T00:00:00Z" | to_timestamp) //
    (.metadata.timestamp | to_timestamp)