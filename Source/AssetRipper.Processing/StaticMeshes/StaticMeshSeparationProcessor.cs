using AssetRipper.Assets.Collections;
using AssetRipper.Import.Logging;
using AssetRipper.Numerics;
using AssetRipper.SourceGenerated.Classes.ClassID_25;
using AssetRipper.SourceGenerated.Classes.ClassID_33;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Extensions;
using System.Numerics;

namespace AssetRipper.Processing.StaticMeshes;

/// <summary>
/// Reverses Unity's static batching optimization.
/// </summary>
/// <remarks>
/// Objects marked as static in a scene get merged into a single "Combined Mesh" when the game is compiled in order to reduce draw calls.
/// Each <see cref="IRenderer"/> that participated in the batch points at the combined mesh and selects a contiguous range of submeshes
/// (via <see cref="IRenderer.StaticBatchInfo_C25"/> or <see cref="IRenderer.SubsetIndices_C25"/>).<br/>
/// This processor extracts the submeshes belonging to each renderer into a standalone <see cref="IMesh"/> instance, reassigns the renderer's
/// <see cref="IMeshFilter"/> to the new mesh, and clears the static batch metadata so the result behaves like an ordinary (non-batched) object.<br/>
/// Renderers that reference an identical slice of the same combined mesh share a single generated mesh instance.
/// </remarks>
public sealed class StaticMeshSeparationProcessor : IAssetProcessor
{
	public void Process(GameData gameData)
	{
		Logger.Info(LogCategory.Processing, "Static Mesh Separation");

		ProcessedAssetCollection? collection = null;
		Dictionary<IMesh, MeshData> meshDataCache = new();
		Dictionary<(IMesh CombinedMesh, string Subset), IMesh> separatedMeshCache = new();

		int separatedMeshCount = 0;
		int reassignedRendererCount = 0;

		foreach (IRenderer renderer in gameData.GameBundle.FetchAssets().OfType<IRenderer>())
		{
			int[] subsetIndices = GetSubsetIndices(renderer);
			if (subsetIndices.Length == 0)
			{
				continue; // Not part of a static batch.
			}

			IMeshFilter? meshFilter = renderer.GameObject_C25P?.TryGetComponent<IMeshFilter>();
			if (meshFilter is null || !meshFilter.TryGetMesh(out IMesh? combinedMesh))
			{
				continue;
			}

			if (!TryGetMeshData(meshDataCache, combinedMesh, out MeshData combinedData))
			{
				continue;
			}

			// Guard against malformed indices that would otherwise throw.
			bool valid = true;
			foreach (int index in subsetIndices)
			{
				if (index < 0 || index >= combinedData.SubMeshes.Length)
				{
					valid = false;
					break;
				}
			}
			if (!valid)
			{
				continue;
			}

			(IMesh, string) cacheKey = (combinedMesh, string.Join(',', subsetIndices));
			if (!separatedMeshCache.TryGetValue(cacheKey, out IMesh? separatedMesh))
			{
				MeshData separatedData = ExtractSubMeshes(combinedData, subsetIndices);

				collection ??= gameData.AddNewProcessedCollection("Separated Static Meshes");
				separatedMesh = collection.CreateMesh();
				separatedMesh.Name = GetSeparatedMeshName(renderer, combinedMesh, separatedMeshCount);
				separatedMesh.FillWithCompressedMeshData(separatedData);

				separatedMeshCache.Add(cacheKey, separatedMesh);
				separatedMeshCount++;
			}

			meshFilter.MeshP = separatedMesh;
			ClearStaticBatchInfo(renderer);
			reassignedRendererCount++;
		}

		if (separatedMeshCount > 0)
		{
			Logger.Info(LogCategory.Processing, $"Separated {separatedMeshCount} static mesh instance(s) for {reassignedRendererCount} renderer(s).");
		}
	}

	private static bool TryGetMeshData(Dictionary<IMesh, MeshData> cache, IMesh mesh, out MeshData meshData)
	{
		if (cache.TryGetValue(mesh, out meshData))
		{
			return true;
		}
		if (MeshData.TryMakeFromMesh(mesh, out meshData))
		{
			cache.Add(mesh, meshData);
			return true;
		}
		return false;
	}

	private static string GetSeparatedMeshName(IRenderer renderer, IMesh combinedMesh, int index)
	{
		string? gameObjectName = renderer.GameObject_C25P?.Name.String;
		if (!string.IsNullOrEmpty(gameObjectName))
		{
			return gameObjectName;
		}
		string baseName = string.IsNullOrEmpty(combinedMesh.Name) ? "SeparatedMesh" : combinedMesh.Name;
		return $"{baseName}_{index}";
	}

	private static void ClearStaticBatchInfo(IRenderer renderer)
	{
		if (renderer.Has_StaticBatchInfo_C25())
		{
			renderer.StaticBatchInfo_C25.FirstSubMesh = 0;
			renderer.StaticBatchInfo_C25.SubMeshCount = 0;
		}
		if (renderer.Has_SubsetIndices_C25())
		{
			renderer.SubsetIndices_C25.Clear();
		}
	}

	private static int[] GetSubsetIndices(IRenderer renderer)
	{
		if (renderer.Has_SubsetIndices_C25() && renderer.SubsetIndices_C25.Count != 0)
		{
			int[] result = new int[renderer.SubsetIndices_C25.Count];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (int)renderer.SubsetIndices_C25[i];
			}
			return result;
		}
		if (renderer.Has_StaticBatchInfo_C25() && renderer.StaticBatchInfo_C25.SubMeshCount != 0)
		{
			int first = renderer.StaticBatchInfo_C25.FirstSubMesh;
			int count = renderer.StaticBatchInfo_C25.SubMeshCount;
			int[] result = new int[count];
			for (int i = 0; i < count; i++)
			{
				result[i] = first + i;
			}
			return result;
		}
		return [];
	}

	/// <summary>
	/// Builds a new <see cref="MeshData"/> containing only the requested submeshes of <paramref name="source"/>, with the vertex buffer
	/// reduced to the vertices those submeshes actually reference and the index buffer remapped accordingly.
	/// </summary>
	public static MeshData ExtractSubMeshes(MeshData source, IReadOnlyList<int> subMeshIndices)
	{
		List<uint> newIndices = new();
		SubMeshData[] newSubMeshes = new SubMeshData[subMeshIndices.Count];
		Dictionary<uint, uint> vertexRemap = new();
		List<uint> originalVertexOrder = new();

		for (int s = 0; s < subMeshIndices.Count; s++)
		{
			SubMeshData sub = source.SubMeshes[subMeshIndices[s]];
			int firstIndex = newIndices.Count;
			uint minVertex = uint.MaxValue;
			uint maxVertex = 0;
			int end = sub.FirstIndex + sub.IndexCount;
			for (int i = sub.FirstIndex; i < end; i++)
			{
				uint original = source.ProcessedIndexBuffer[i];
				if (!vertexRemap.TryGetValue(original, out uint remapped))
				{
					remapped = (uint)originalVertexOrder.Count;
					vertexRemap.Add(original, remapped);
					originalVertexOrder.Add(original);
				}
				newIndices.Add(remapped);
				if (remapped < minVertex)
				{
					minVertex = remapped;
				}
				if (remapped > maxVertex)
				{
					maxVertex = remapped;
				}
			}

			int vertexCount = sub.IndexCount == 0 ? 0 : (int)(maxVertex - minVertex + 1);
			int firstVertex = sub.IndexCount == 0 ? 0 : (int)minVertex;
			newSubMeshes[s] = new SubMeshData(0, firstIndex, firstVertex, sub.IndexCount, sub.TriangleCount, vertexCount, sub.Topology, sub.LocalBounds);
		}

		return new MeshData(
			Gather(source.Vertices, originalVertexOrder)!,
			Gather(source.Normals, originalVertexOrder),
			Gather(source.Tangents, originalVertexOrder),
			Gather(source.Colors, originalVertexOrder),
			Gather(source.UV0, originalVertexOrder),
			Gather(source.UV1, originalVertexOrder),
			Gather(source.UV2, originalVertexOrder),
			Gather(source.UV3, originalVertexOrder),
			Gather(source.UV4, originalVertexOrder),
			Gather(source.UV5, originalVertexOrder),
			Gather(source.UV6, originalVertexOrder),
			Gather(source.UV7, originalVertexOrder),
			Gather(source.Skin, originalVertexOrder),
			source.BindPose,
			newIndices.ToArray(),
			newSubMeshes);
	}

	private static T[]? Gather<T>(T[]? source, List<uint> order)
	{
		if (source is null || source.Length == 0)
		{
			return source;
		}
		T[] result = new T[order.Count];
		for (int i = 0; i < order.Count; i++)
		{
			result[i] = source[order[i]];
		}
		return result;
	}
}
