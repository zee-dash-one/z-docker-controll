#!/usr/bin/env bash
# zlab-stats.sh: leitura do z-server-01 para o Z-Lab. Só lê, não grava nada.
# Saída: uma linha "chave=valor" por métrica. Versão do formato em "v".
set -u
export LC_ALL=C
echo "v=1"
echo "now_epoch=$(date +%s)"
read -r up _ < /proc/uptime
echo "uptime_s=${up%.*}"
echo "users=$(who | wc -l)"
echo "cores=$(nproc)"
read -r _ u n s idle iow irq sirq steal _ < /proc/stat
echo "cpu_total=$((u + n + s + idle + iow + irq + sirq + steal))"
echo "cpu_idle=$((idle + iow))"
awk '/^MemTotal:/{print "mem_total_kb="$2} /^MemAvailable:/{print "mem_available_kb="$2}' /proc/meminfo
read -r l1 l5 l15 _ < /proc/loadavg
echo "load=$l1 $l5 $l15"
disk() { # $1 = chave, $2 = ponto de montagem
  if [ "$2" = "/" ] || mountpoint -q "$2" 2>/dev/null; then
    df -B1 --output=used,size "$2" 2>/dev/null | awk -v k="$1" 'NR==2{print "disk_"k"="$1" "$2}'
  else
    echo "disk_$1=absent"
  fi
}
disk root /
disk docker /var/lib/docker
disk data /data

# --- seção containers (tarefa 04) ---
echo "section=containers"
if command -v docker >/dev/null 2>&1; then
  ids=$(docker ps -aq 2>/dev/null)
  if [ -n "$ids" ]; then
    stats=$(docker stats --no-stream --format '{{.Name}}{{"\t"}}{{.CPUPerc}}{{"\t"}}{{.MemUsage}}' 2>/dev/null)
    # shellcheck disable=SC2086
    docker inspect --format '{{.Name}}{{"\t"}}{{.State.Status}}{{"\t"}}{{.State.StartedAt}}{{"\t"}}{{.RestartCount}}' $ids 2>/dev/null \
    | sed 's#^/##' \
    | while IFS=$'\t' read -r name state started restarts; do
        line=$(printf '%s\n' "$stats" | awk -F'\t' -v n="$name" '$1==n{print $2"\t"$3; exit}')
        if [ -n "$line" ]; then cpu=${line%%$'\t'*}; mem=${line#*$'\t'}; else cpu=-; mem=-; fi
        printf 'c=%s\t%s\t%s\t%s\t%s\t%s\n' "$name" "$state" "$started" "$restarts" "$cpu" "$mem"
      done
  fi
else
  echo "docker=absent"
fi
