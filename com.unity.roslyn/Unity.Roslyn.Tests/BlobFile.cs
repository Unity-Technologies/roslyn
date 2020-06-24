using System.IO;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

public static class BlobFile
{
    // TODO: Hack we need to access the BlobAssetHeader to get the Length of the blob
    // copy the structure from com.unity.entity
    // unfortunately this class is internal in com.unity.entity and there is no way to query the blob size
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    unsafe struct BlobAssetHeader
    {
        [FieldOffset(0)]  public void* ValidationPtr;
        [FieldOffset(8)]  public int Length;
        [FieldOffset(12)] public Allocator Allocator;
        [FieldOffset(16)] public ulong Hash;
        [FieldOffset(24)] private ulong Padding;
    }

    public static unsafe void WriteBlobAsset<T>(ref BlobAssetReference<T> assetReference, string fileName) where T : struct
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(ConvertFileName(fileName), FileMode.Create)))
        {
            var assetPtr = assetReference.GetUnsafePtr();
            BlobAssetHeader* header = ((BlobAssetHeader*)assetPtr) - 1;
            long dataSize = header->Length;

            var data = new byte[dataSize];
            fixed(byte* ptr = &data[0])
            {
                UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref assetReference.Value), data.Length);
            }
            writer.Write(data);
            writer.Close();
        }
    }

    private static string ConvertFileName(string fileName)
    {
        return Application.streamingAssetsPath + "/BlobAssets/" + fileName;
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(ConvertFileName(fileName));
    }

    public static BlobAssetReference<T> ReadBlobAsset<T>(string fileName) where T : struct
    {
        var fullpath = ConvertFileName(fileName);
        if (!File.Exists(fullpath))
            throw new FileNotFoundException("Cannot find file:" + fullpath);

        return BlobAssetReference<T>.Create(File.ReadAllBytes(fullpath));
    }
}
