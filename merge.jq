def merge($node):
  if .[$node.name]
  then .[$node.name].results.cpu_release = $node.results.cpu_release
  else .[$node.name] = $node
  end;

def merge2($node):
  if .[$node.name]
  then .[$node.name] = $node
  else .[$node.name] = $node
  end;

{
  "benchmarks": (
    map(.benchmarks)
    | add
    | reduce .[] as $node ({}; merge($node))
    | to_entries | map(.value)
  )
} * (.|.[0]|with_entries(select(.key != "benchmarks"))) * (.|.[1]|with_entries(select(.key != "benchmarks")))
