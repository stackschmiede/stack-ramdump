#!/bin/bash
# Deploy RAM Dump von WSL nach Windows
set -euo pipefail

SRC="/home/stvgu/projects/selfapps/ram-dump"
DEST="/mnt/c/projects/ram-dump"

echo "=== RAM Dump Deploy ==="

# Zielverzeichnis erstellen
mkdir -p "$DEST"

# Dateien synchronisieren (ohne .git, bin, obj)
rsync -av --delete \
  --exclude '.git/' \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude 'WORKNOTES.md' \
  --exclude 'CLAUDE.md' \
  "$SRC/" "$DEST/"

echo ""
echo "=== Deployed nach $DEST ==="
echo "Nächste Schritte (PowerShell als Admin):"
echo "  cd C:\\projects\\ram-dump"
echo "  dotnet restore"
echo "  dotnet build"
echo "  dotnet run"
