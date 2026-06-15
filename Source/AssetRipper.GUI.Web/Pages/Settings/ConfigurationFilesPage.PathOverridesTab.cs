using AssetRipper.GUI.Web.Paths;
using AssetRipper.Processing.PathOverrides;

namespace AssetRipper.GUI.Web.Pages.Settings;

public sealed partial class ConfigurationFilesPage
{
	private sealed class PathOverridesTab : HtmlTab
	{
		public static PathOverridesTab Instance { get; } = new();

		public override string DisplayName => "Asset Path Overrides";

		public override void Write(TextWriter writer)
		{
			PathOverrideData? data = GameFileLoader.Settings.ProcessingSettings.PathOverrides;

			new P(writer).WithClass("p-2").Close("Upload a JSON file to change the export destination of individual assets. " +
				"The file maps asset collection names to a dictionary of path ids and their new output paths (relative to the project root).");

			if (data is null || data.IsEmpty)
			{
				using (new Div(writer).WithTextCenter().End())
				{
					new P(writer).WithClass("p-2").Close("No path overrides have been loaded.");
					using (new Form(writer).WithAction("/ConfigurationFiles/PathOverrides/Set").WithMethod("post").End())
					{
						new Input(writer).WithType("submit").WithClass("btn btn-primary mx-1").WithValue("Load").Close();
					}
				}
			}
			else
			{
				new Pre(writer).WithClass("bg-dark-subtle rounded-3 p-2").Close(data.ToJson().ToHtml());
				using (new Div(writer).WithClass("row text-center").End())
				{
					using (new Div(writer).WithClass("col").End())
					{
						using (new Form(writer).WithAction("/ConfigurationFiles/PathOverrides/Set").WithMethod("post").End())
						{
							new Input(writer).WithType("submit").WithClass("btn btn-primary mx-1").WithValue("Replace").Close();
						}
					}
					using (new Div(writer).WithClass("col").End())
					{
						using (new Form(writer).WithAction("/ConfigurationFiles/PathOverrides/Clear").WithMethod("post").End())
						{
							new Input(writer).WithType("submit").WithClass("btn btn-danger mx-1").WithValue("Remove").Close();
						}
					}
				}
			}
		}
	}
}
