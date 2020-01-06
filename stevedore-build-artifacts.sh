#!/bin/sh

version_string="$1"
file_name='builds.7z'
folder='test/'
linux='test/Artifacts/Builds/Binaries/Linux/*'
mac='test/Artifacts/Builds/Binaries/Mac/*'
artifact_path='test/art/*'
linux_archive='test/art/linux.7z'
mac_archive='test/art/mac.7z'

if [ -f "$file_name" ]; then
  echo "it's here"
fi

7z x $file_name -o$folder

7z a $linux_archive $PWD/$linux
7z a $mac_archive $PWD/$mac

for path in $artifact_path;do
  artifact_hash="$(sha256sum "$path" | cut -f1 -d ' ')"
  source_artifact_id="$(basename "$PWD/$path" .7z)/${version_string}_${artifact_hash}.7z"

  echo "artifact hash: $source_artifact_id"
done
