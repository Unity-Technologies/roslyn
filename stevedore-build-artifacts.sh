#!/bin/sh

version_string="$1"
artifact_path='producedbuilds/*'

if [ ! "$version_string" ]; then
    echo "missing arg: version_string"
    exit 1
fi

mkdir -p producedbuilds

7z a producedbuilds/roslyn-csc-linux.7z Artifacts/Builds/Binaries/Linux/*
7z a producedbuilds/roslyn-csc-mac.7z Artifacts/Builds/Binaries/Mac/*
7z a producedbuilds/roslyn-csc-windows.7z Artifacts/Builds/Binaries/Windows/*

for path in $artifact_path;do
  artifact_hash="$(sha256sum "$path" | cut -f1 -d ' ')"
  source_artifact_id="$(basename "$PWD/$path" .7z)/${version_string}_${artifact_hash}.7z"

  echo "source artifact id: $source_artifact_id"
done
