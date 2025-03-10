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