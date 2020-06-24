using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Animation.Hybrid;

namespace Unity.Roslyn.Tests
{
    public static class PlaymodeTestsEditorSetup
    {
        public static void CreateStreamingAssetsDirectory()
        {
            var streamingAssetsBlobFullPath = Application.streamingAssetsPath + "/BlobAssets";
            if (!Directory.Exists(streamingAssetsBlobFullPath))
                Directory.CreateDirectory(streamingAssetsBlobFullPath);
        }

        private static string GetBlobAssetPath(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            return filename + ".blob";
        }

        public static void BuildRigDefinitionBlobAsset(string path)
        {
            var gameObject = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
            if (gameObject == null)
                throw new NullReferenceException($"Asset '{path}' not found");

            var blobPath = GetBlobAssetPath(path);

            var skeletonNode = RigGenerator.ExtractSkeletonNodesFromGameObject(gameObject);

            var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNode);

            BlobFile.WriteBlobAsset(ref rigDefinition, blobPath);
        }

        public static void BuildClipBlobAsset(string path)
        {
            var animationClip = AssetDatabase.LoadMainAssetAtPath(path) as AnimationClip;
            if (animationClip == null)
                throw new NullReferenceException($"Asset '{path}' not found");

            var blobPath = GetBlobAssetPath(path);

            var clip = animationClip.ToDenseClip();

            BlobFile.WriteBlobAsset(ref clip, blobPath);
        }
    }
}
#endif
