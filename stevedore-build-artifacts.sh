#!/bin/sh

version_string="$1"
artifact_path='producedbuilds/*'
linux_archive='test/art/linux.7z'

if [ -f "$file_name" ]; then
  echo "it's here"
fi

for path in $artifact_path;do
  artifact_hash="$(sha256sum "$path" | cut -f1 -d ' ')"
  source_artifact_id="$(basename "$PWD/$path" .7z)/${version_string}_${artifact_hash}.7z"

  echo "source artifact id: $source_artifact_id"
done
