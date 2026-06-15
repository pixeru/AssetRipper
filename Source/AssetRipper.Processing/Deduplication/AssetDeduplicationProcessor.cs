using AssetRipper.Assets;
using AssetRipper.Assets.Cloning;
using AssetRipper.Import.Logging;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Classes.ClassID_72;
using AssetRipper.SourceGenerated.Classes.ClassID_83;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Processing.Deduplication;

/// <summary>
/// Reverses Unity's tendency to duplicate the same asset into multiple asset bundles.
/// </summary>
/// <remarks>
/// When a project is built, an asset referenced by several bundles is copied into each one. This processor identifies
/// assets that are deeply equal and consolidates them: one instance is kept as the canonical asset and every reference to
/// the others is redirected to it during export (see <see cref="Assets.Bundles.GameBundle.DeduplicationMap"/>).<br/>
/// Only a conservative set of self-contained asset types is considered: Mono Scripts, Shaders, Compute Shaders,
/// Audio Clips, Text Assets, Meshes, and Textures that are not used by sprites.
/// </remarks>
public sealed class AssetDeduplicationProcessor : IAssetProcessor
{
	public void Process(GameData gameData)
	{
		Logger.Info(LogCategory.Processing, "Asset Deduplication");

		HashSet<ITexture2D> spriteTextures = GetSpriteTextures(gameData);

		Dictionary<IUnityObjectBase, IUnityObjectBase> duplicateToCanonical = BuildDeduplicationMap(gameData, spriteTextures);

		if (duplicateToCanonical.Count > 0)
		{
			gameData.GameBundle.SetDeduplicationMap(duplicateToCanonical);
			Logger.Info(LogCategory.Processing, $"Consolidated {duplicateToCanonical.Count} duplicate asset(s).");
		}
	}

	private static Dictionary<IUnityObjectBase, IUnityObjectBase> BuildDeduplicationMap(GameData gameData, HashSet<ITexture2D> spriteTextures)
	{
		Dictionary<IUnityObjectBase, IUnityObjectBase> duplicateToCanonical = new();

		// Bucket eligible assets so that only assets with the same type and name are ever deeply compared.
		Dictionary<(Type, string), List<IUnityObjectBase>> buckets = new();
		foreach (IUnityObjectBase asset in gameData.GameBundle.FetchAssets())
		{
			if (!IsEligible(asset, spriteTextures))
			{
				continue;
			}

			(Type, string) key = (asset.GetType(), asset.GetBestName());
			if (!buckets.TryGetValue(key, out List<IUnityObjectBase>? bucket))
			{
				bucket = new List<IUnityObjectBase>();
				buckets.Add(key, bucket);
			}
			bucket.Add(asset);
		}

		AssetEqualityComparer comparer = new();
		foreach (List<IUnityObjectBase> bucket in buckets.Values)
		{
			if (bucket.Count < 2)
			{
				continue;
			}

			List<IUnityObjectBase> canonicals = new();
			foreach (IUnityObjectBase asset in bucket)
			{
				IUnityObjectBase? canonical = null;
				foreach (IUnityObjectBase candidate in canonicals)
				{
					if (comparer.Equals(candidate, asset))
					{
						canonical = candidate;
						break;
					}
				}

				if (canonical is null)
				{
					canonicals.Add(asset);
				}
				else
				{
					duplicateToCanonical.Add(asset, canonical);
				}
			}
		}

		return duplicateToCanonical;
	}

	private static bool IsEligible(IUnityObjectBase asset, HashSet<ITexture2D> spriteTextures)
	{
		return asset switch
		{
			IMonoScript => true,
			IShader => true,
			IComputeShader => true,
			IAudioClip => true,
			ITextAsset => true,
			IMesh => true,
			ITexture2D texture => !spriteTextures.Contains(texture),
			_ => false,
		};
	}

	private static HashSet<ITexture2D> GetSpriteTextures(GameData gameData)
	{
		HashSet<ITexture2D> spriteTextures = new();
		foreach (ISprite sprite in gameData.GameBundle.FetchAssets().OfType<ISprite>())
		{
			if (sprite.TryGetTexture() is { } texture)
			{
				spriteTextures.Add(texture);
			}
		}
		return spriteTextures;
	}
}
