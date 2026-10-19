# Lista e apaga containers e volumes nomeados do Docker no WSL.
# Nao remove imagens. Nao mexe no z-server-01.
set -eu
echo "Containers:"
docker ps -a --format '{{.ID}} {{.Names}}'
echo "Volumes nomeados:"
docker volume ls -q
ids=$(docker ps -aq)
if [ -n "$ids" ]; then
  # shellcheck disable=SC2086
  docker rm -f $ids
fi
vols=$(docker volume ls -q)
if [ -n "$vols" ]; then
  # shellcheck disable=SC2086
  docker volume rm $vols
fi
echo "Docker do WSL sem containers e sem volumes nomeados."
