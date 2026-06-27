param(
  [Parameter(Mandatory=$true)][uint32]$Start,
  [Parameter(Mandatory=$true)][uint32]$Handle,
  [int]$Max = 300
)
$cmd = 'C:\Users\Public\ddon_cmd.txt'
$logp = 'C:\Users\Public\ddon_mod.log'

function Send-Peek([uint32]$addr, [string]$type) {
  $tag = "WALK_{0:x8}_{1}" -f $addr, (Get-Random)
  # use a comment line as a unique marker, then the real command
  Set-Content -Path $cmd -Value @("# $tag", ("peek 0x{0:x8} {1}" -f $addr, $type)) -Encoding ascii
  for ($i=0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 120
    $lines = Get-Content $logp -Tail 6 -ErrorAction SilentlyContinue
    foreach ($l in $lines) {
      if ($l -match ("peek 0x{0:x8} {1} = 0x([0-9a-fA-F]+)" -f $addr, $type)) {
        return [uint32]("0x" + $matches[1])
      }
    }
  }
  return $null
}

$node = $Start
for ($n=0; $n -lt $Max; $n++) {
  if ($node -eq 0) { Write-Host "NOT FOUND (null next) after $n nodes"; exit 2 }
  $key  = Send-Peek ([uint32]($node + 0x1c)) 'u32'
  if ($key -eq $null) { Write-Host "READ FAIL at node 0x$("{0:x8}" -f $node)"; exit 3 }
  if ($key -eq $Handle) {
    Write-Host ("FOUND node=0x{0:x8} key=0x{1:x8} (after {2} hops)" -f $node, $key, $n)
    exit 0
  }
  $next = Send-Peek ([uint32]($node + 0x14)) 'ptr'
  if ($next -eq $null) { Write-Host "READ FAIL(next) at node 0x$("{0:x8}" -f $node)"; exit 3 }
  $node = $next
}
Write-Host "NOT FOUND after $Max hops"
exit 4
