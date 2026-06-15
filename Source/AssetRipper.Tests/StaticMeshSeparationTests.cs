using AssetRipper.Numerics;
using AssetRipper.Processing.StaticMeshes;
using AssetRipper.SourceGenerated.Enums;
using AssetRipper.SourceGenerated.Extensions;
using System.Numerics;

namespace AssetRipper.Tests;

internal class StaticMeshSeparationTests
{
	/// <summary>
	/// A combined mesh with two independent triangles, each as its own submesh.
	/// </summary>
	private static MeshData CreateCombinedMesh()
	{
		Vector3[] vertices =
		[
			new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), // submesh 0
			new Vector3(5, 0, 0), new Vector3(6, 0, 0), new Vector3(5, 1, 0), // submesh 1
		];
		uint[] indices = [0, 1, 2, 3, 4, 5];
		SubMeshData[] subMeshes =
		[
			new SubMeshData(0, 0, 0, 3, 1, 3, MeshTopology.Triangles, default),
			new SubMeshData(0, 3, 3, 3, 1, 3, MeshTopology.Triangles, default),
		];
		return new MeshData(vertices, null, null, null, null, null, null, null, null, null, null, null, null, null, indices, subMeshes);
	}

	[Test]
	public void ExtractingSecondSubMeshReindexesVertices()
	{
		MeshData combined = CreateCombinedMesh();

		MeshData result = StaticMeshSeparationProcessor.ExtractSubMeshes(combined, [1]);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(result.SubMeshes, Has.Length.EqualTo(1));
			Assert.That(result.Vertices, Has.Length.EqualTo(3));
			// The second submesh's original vertices (indices 3,4,5) become 0,1,2.
			Assert.That(result.ProcessedIndexBuffer, Is.EqualTo(new uint[] { 0, 1, 2 }));
			Assert.That(result.Vertices[0], Is.EqualTo(new Vector3(5, 0, 0)));
			Assert.That(result.Vertices[1], Is.EqualTo(new Vector3(6, 0, 0)));
			Assert.That(result.Vertices[2], Is.EqualTo(new Vector3(5, 1, 0)));
			Assert.That(result.SubMeshes[0].FirstIndex, Is.EqualTo(0));
			Assert.That(result.SubMeshes[0].IndexCount, Is.EqualTo(3));
			Assert.That(result.SubMeshes[0].FirstVertex, Is.EqualTo(0));
			Assert.That(result.SubMeshes[0].VertexCount, Is.EqualTo(3));
		}
	}

	[Test]
	public void ExtractingFirstSubMeshKeepsOriginalVertices()
	{
		MeshData combined = CreateCombinedMesh();

		MeshData result = StaticMeshSeparationProcessor.ExtractSubMeshes(combined, [0]);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(result.Vertices, Has.Length.EqualTo(3));
			Assert.That(result.ProcessedIndexBuffer, Is.EqualTo(new uint[] { 0, 1, 2 }));
			Assert.That(result.Vertices[0], Is.EqualTo(new Vector3(0, 0, 0)));
		}
	}

	[Test]
	public void ExtractingBothSubMeshesPreservesAllGeometry()
	{
		MeshData combined = CreateCombinedMesh();

		MeshData result = StaticMeshSeparationProcessor.ExtractSubMeshes(combined, [0, 1]);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(result.SubMeshes, Has.Length.EqualTo(2));
			Assert.That(result.Vertices, Has.Length.EqualTo(6));
			Assert.That(result.ProcessedIndexBuffer, Is.EqualTo(new uint[] { 0, 1, 2, 3, 4, 5 }));
			Assert.That(result.SubMeshes[1].FirstIndex, Is.EqualTo(3));
			Assert.That(result.SubMeshes[1].FirstVertex, Is.EqualTo(3));
		}
	}
}
