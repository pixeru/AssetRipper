using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Extensions.Enums.Shader;
using K4os.Compression.LZ4;

namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Extracts the compiled GPU bytecode programs from a <see cref="IShader"/>'s compressed blob.
/// </summary>
/// <remarks>
/// Unity stores all compiled shader variants for each platform in a single LZ4-compressed blob
/// (<c>CompressedBlob</c>) addressed by per-platform offset/compressed-length/decompressed-length arrays.
/// This decompresses each platform's segment and locates the raw bytecode containers within it.
/// </remarks>
public static class ShaderBlobExtractor
{
	/// <summary>The DXBC container magic, "DXBC".</summary>
	private static ReadOnlySpan<byte> DxbcMagic => [0x44, 0x58, 0x42, 0x43];

	/// <summary>
	/// Decompresses each platform's segment of the shader's compressed blob.
	/// </summary>
	public static IEnumerable<(GPUPlatform Platform, byte[] Data)> GetDecompressedSegments(IShader shader)
	{
		if (!shader.Has_CompressedBlob() || shader.GetPlatforms() is not { } platformsEnumerable)
		{
			yield break;
		}

		byte[] blob = shader.CompressedBlob;
		GPUPlatform[] platforms = platformsEnumerable.ToArray();

		if (shader.Has_Offsets_AssetList_UInt32() && shader.Has_CompressedLengths_AssetList_UInt32() && shader.Has_DecompressedLengths_AssetList_UInt32())
		{
			var offsets = shader.Offsets_AssetList_UInt32;
			var compressedLengths = shader.CompressedLengths_AssetList_UInt32;
			var decompressedLengths = shader.DecompressedLengths_AssetList_UInt32;
			int count = Min(platforms.Length, offsets.Count, compressedLengths.Count, decompressedLengths.Count);
			for (int i = 0; i < count; i++)
			{
				if (TryDecompress(blob, offsets[i], compressedLengths[i], decompressedLengths[i]) is { } data)
				{
					yield return (platforms[i], data);
				}
			}
		}
		else if (shader.Has_Offsets_AssetList_AssetList_UInt32() && shader.Has_CompressedLengths_AssetList_AssetList_UInt32() && shader.Has_DecompressedLengths_AssetList_AssetList_UInt32())
		{
			var offsets = shader.Offsets_AssetList_AssetList_UInt32;
			var compressedLengths = shader.CompressedLengths_AssetList_AssetList_UInt32;
			var decompressedLengths = shader.DecompressedLengths_AssetList_AssetList_UInt32;
			int platformCount = Min(platforms.Length, offsets.Count, compressedLengths.Count, decompressedLengths.Count);
			for (int p = 0; p < platformCount; p++)
			{
				int subCount = Min(offsets[p].Count, compressedLengths[p].Count, decompressedLengths[p].Count);
				for (int s = 0; s < subCount; s++)
				{
					if (TryDecompress(blob, offsets[p][s], compressedLengths[p][s], decompressedLengths[p][s]) is { } data)
					{
						yield return (platforms[p], data);
					}
				}
			}
		}
	}

	/// <summary>
	/// Locates the DXBC bytecode containers within a decompressed blob segment.
	/// </summary>
	public static List<byte[]> ExtractDxbcBlobs(byte[] data)
	{
		List<byte[]> result = new();
		int i = 0;
		while (i + 32 <= data.Length)
		{
			if (data[i] == DxbcMagic[0] && data[i + 1] == DxbcMagic[1] && data[i + 2] == DxbcMagic[2] && data[i + 3] == DxbcMagic[3])
			{
				// The total container size is a little-endian uint at byte offset 24 of the DXBC header.
				uint totalSize = BitConverter.ToUInt32(data, i + 24);
				if (totalSize >= 32 && i + totalSize <= (uint)data.Length)
				{
					result.Add(data[i..(i + (int)totalSize)]);
					i += (int)totalSize;
					continue;
				}
			}
			i++;
		}
		return result;
	}

	private static byte[]? TryDecompress(byte[] blob, uint offset, uint compressedLength, uint decompressedLength)
	{
		if (compressedLength == 0 || decompressedLength == 0 || offset + compressedLength > (uint)blob.Length)
		{
			return null;
		}

		try
		{
			byte[] output = new byte[decompressedLength];
			int written = LZ4Codec.Decode(blob.AsSpan((int)offset, (int)compressedLength), output.AsSpan());
			return written <= 0 ? null : output;
		}
		catch
		{
			return null;
		}
	}

	private static int Min(int a, int b, int c) => Math.Min(a, Math.Min(b, c));

	private static int Min(int a, int b, int c, int d) => Math.Min(Math.Min(a, b), Math.Min(c, d));
}
