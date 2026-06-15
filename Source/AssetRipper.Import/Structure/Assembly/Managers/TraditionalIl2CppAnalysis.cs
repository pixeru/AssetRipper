using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.Model.Contexts;
using Cpp2IL.Core.OutputFormats;
using Cpp2IL.Core.ProcessingLayers;

namespace AssetRipper.Import.Structure.Assembly.Managers;

/// <summary>
/// The "traditional" experimental Il2Cpp analysis used at <see cref="Configuration.ScriptContentLevel.Level3"/>.
/// </summary>
/// <remarks>
/// At <see cref="Configuration.ScriptContentLevel.Level3"/>, method bodies are recovered from the compiled native code
/// using Cpp2IL's IL recovery output format together with the call/native-method analysis processing layers.<br/>
/// For performance, framework assemblies (mscorlib and the System/Unity assemblies) are excluded from analysis: their
/// concrete methods are given a cheap throw-null stub instead of being analyzed, since they don't need to be decompiled.
/// </remarks>
public static class TraditionalIl2CppAnalysis
{
	/// <summary>
	/// The processing layers used to recover Il2Cpp methods.
	/// </summary>
	public static List<Cpp2IlProcessingLayer> CreateRecoveryProcessingLayers() =>
	[
		new AttributeAnalysisProcessingLayer(),
		new MethodOverrideNameFixer(),
		new CallAnalysisProcessingLayer(),
		new NativeMethodDetectionProcessingLayer(),
	];

	/// <summary>
	/// The output format used to recover Il2Cpp method bodies, excluding framework assemblies.
	/// </summary>
	public static AsmResolverDllOutputFormat CreateRecoveryOutputFormat() => new SelectiveIlRecoveryOutputFormat();

	/// <summary>
	/// Determines whether an assembly should be excluded from analysis for performance.
	/// </summary>
	/// <remarks>
	/// mscorlib and the System and Unity assemblies are excluded, as well as other common framework assemblies.
	/// </remarks>
	/// <param name="assemblyName">The assembly name, with or without a <c>.dll</c> extension.</param>
	public static bool IsExcludedAssembly(string? assemblyName)
	{
		if (string.IsNullOrEmpty(assemblyName))
		{
			return false;
		}

		ReadOnlySpan<char> name = assemblyName.AsSpan().Trim();
		if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
		{
			name = name[..^4];
		}

		return Equals(name, "mscorlib")
			|| Equals(name, "netstandard")
			|| Equals(name, "WindowsBase")
			|| Equals(name, "System")
			|| StartsWith(name, "System.")
			|| Equals(name, "Unity")
			|| StartsWith(name, "Unity.")
			|| StartsWith(name, "UnityEngine")
			|| StartsWith(name, "UnityEditor")
			|| StartsWith(name, "Mono.")
			|| StartsWith(name, "Microsoft.");

		static bool Equals(ReadOnlySpan<char> value, string other) => value.Equals(other, StringComparison.OrdinalIgnoreCase);
		static bool StartsWith(ReadOnlySpan<char> value, string prefix) => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// An IL recovery output format that skips analysis of framework assemblies, stubbing their concrete methods instead.
	/// </summary>
	private sealed class SelectiveIlRecoveryOutputFormat : AsmResolverDllOutputFormatIlRecovery
	{
		public override string OutputFormatId => "dll_traditional";

		public override string OutputFormatName => "Traditional Il2Cpp analysis (framework assemblies excluded)";

		protected override void FillMethodBody(MethodDefinition methodDefinition, MethodAnalysisContext methodContext)
		{
			string? assemblyName = methodContext.DeclaringType?.DeclaringAssembly?.Name;
			if (IsExcludedAssembly(assemblyName) && TryStubMethodBody(methodDefinition))
			{
				return;
			}

			base.FillMethodBody(methodDefinition, methodContext);
		}

		/// <summary>
		/// Replaces a concrete method's body with a cheap <c>ldnull; throw</c> stub, skipping analysis.
		/// </summary>
		/// <returns>True if the method was stubbed; false if it has no managed body to stub.</returns>
		private static bool TryStubMethodBody(MethodDefinition methodDefinition)
		{
			if (methodDefinition.IsAbstract
				|| methodDefinition.IsNative
				|| methodDefinition.IsInternalCall
				|| methodDefinition.IsPInvokeImpl
				|| methodDefinition.IsRuntime)
			{
				return false;
			}

			CilMethodBody body = new();
			body.Instructions.Add(CilOpCodes.Ldnull);
			body.Instructions.Add(CilOpCodes.Throw);
			methodDefinition.CilMethodBody = body;
			return true;
		}
	}
}
