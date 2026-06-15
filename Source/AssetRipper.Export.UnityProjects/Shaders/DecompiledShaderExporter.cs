using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_48;

namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Exports shaders as decompiled HLSL/shaderlab using the experimental <see cref="ShaderDecompiler"/>.
/// </summary>
public sealed class DecompiledShaderExporter : ShaderExporterBase
{
	public override bool Export(IExportContainer container, IUnityObjectBase asset, string path, FileSystem fileSystem)
	{
		using Stream fileStream = fileSystem.File.Create(path);
		using InvariantStreamWriter writer = new(fileStream);
		return ShaderDecompiler.Decompile((IShader)asset, writer);
	}
}
