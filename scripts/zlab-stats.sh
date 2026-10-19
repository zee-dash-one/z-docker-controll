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
