using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Extensions;
using System.Text;

namespace AssetRipper.Processing.Prefabs;

/// <summary>
/// A repeated GameObject hierarchy that is a candidate for being outlined into a prefab.
/// </summary>
/// <param name="Signature">The structural signature shared by every instance.</param>
/// <param name="NodeCount">The number of GameObjects in each instance's hierarchy.</param>
/// <param name="Instances">The root GameObjects of each occurrence of the hierarchy.</param>
public sealed record class PrefabOutliningGroup(string Signature, int NodeCount, IReadOnlyList<IGameObject> Instances);

/// <summary>
/// Identifies repeated GameObject hierarchies within scenes.
/// </summary>
/// <remarks>
/// When a game is compiled, prefabs placed in a scene are inlined (instantiated), so the link to the original prefab is lost.
/// This analyzer computes a structural signature for each hierarchy (component layout and child arrangement, ignoring
/// instance-specific data such as transform values and object names) and groups hierarchies that share a signature. Groups
/// with two or more occurrences are the candidates that can be outlined back into a single prefab.
/// </remarks>
public static class PrefabOutliningAnalyzer
{
	/// <summary>
	/// Groups the given root GameObjects by structural signature, returning only the groups that repeat.
	/// </summary>
	/// <param name="roots">The root GameObjects to analyze.</param>
	/// <param name="minNodeCount">The minimum number of nodes a hierarchy must contain to be considered (filters out trivial matches).</param>
	/// <returns>The repeated hierarchy groups, ordered by descending occurrence count.</returns>
	public static List<PrefabOutliningGroup> FindRepeatedHierarchies(IReadOnlyList<IGameObject> roots, int minNodeCount = 2)
	{
		Dictionary<string, List<IGameObject>> bySignature = new();
		Dictionary<string, int> nodeCountBySignature = new();

		foreach (IGameObject root in roots)
		{
			int nodeCount = CountNodes(root);
			if (nodeCount < minNodeCount)
			{
				continue;
			}

			string signature = ComputeSignature(root);
			if (!bySignature.TryGetValue(signature, out List<IGameObject>? list))
			{
				list = new List<IGameObject>();
				bySignature.Add(signature, list);
				nodeCountBySignature.Add(signature, nodeCount);
			}
			list.Add(root);
		}

		List<PrefabOutliningGroup> groups = new();
		foreach ((string signature, List<IGameObject> instances) in bySignature)
		{
			if (instances.Count >= 2)
			{
				groups.Add(new PrefabOutliningGroup(signature, nodeCountBySignature[signature], instances));
			}
		}

		groups.Sort(static (a, b) => b.Instances.Count.CompareTo(a.Instances.Count));
		return groups;
	}

	/// <summary>
	/// Computes a structural signature for the hierarchy rooted at <paramref name="root"/>.
	/// </summary>
	/// <remarks>
	/// Two hierarchies with the same signature have the same component layout and child arrangement.
	/// The signature is independent of child ordering, object names, and transform values.
	/// </remarks>
	public static string ComputeSignature(IGameObject root)
	{
		StringBuilder builder = new();
		AppendSignature(root, builder);
		return builder.ToString();
	}

	private static void AppendSignature(IGameObject gameObject, StringBuilder builder)
	{
		builder.Append('{');

		List<string> componentSignatures = new();
		foreach (IComponent? component in gameObject.GetComponentAccessList())
		{
			if (component is not null)
			{
				componentSignatures.Add(ComponentSignature(component));
			}
		}
		componentSignatures.Sort(StringComparer.Ordinal);
		builder.AppendJoin(',', componentSignatures);

		builder.Append('[');
		List<string> childSignatures = new();
		foreach (IGameObject child in gameObject.GetChildren())
		{
			StringBuilder childBuilder = new();
			AppendSignature(child, childBuilder);
			childSignatures.Add(childBuilder.ToString());
		}
		// Sort so that sibling ordering does not affect the signature.
		childSignatures.Sort(StringComparer.Ordinal);
		builder.AppendJoin(string.Empty, childSignatures);
		builder.Append(']');

		builder.Append('}');
	}

	private static string ComponentSignature(IComponent component)
	{
		// MonoBehaviours of different scripts are structurally distinct, so the script identity is part of the signature.
		if (component is IMonoBehaviour monoBehaviour)
		{
			IMonoScript? script = monoBehaviour.ScriptP;
			string scriptId = script is null ? "?" : $"{script.AssemblyName}|{script.ClassName_R}";
			return $"114:{scriptId}";
		}
		return component.ClassID.ToString();
	}

	/// <summary>
	/// Counts the number of GameObjects in the hierarchy rooted at <paramref name="root"/> (including the root).
	/// </summary>
	public static int CountNodes(IGameObject root)
	{
		int count = 1;
		foreach (IGameObject child in root.GetChildren())
		{
			count += CountNodes(child);
		}
		return count;
	}
}
