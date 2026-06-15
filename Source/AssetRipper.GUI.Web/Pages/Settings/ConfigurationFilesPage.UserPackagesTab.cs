using AssetRipper.GUI.Web.Paths;
using AssetRipper.Mining.PredefinedAssets;

namespace AssetRipper.GUI.Web.Pages.Settings;

public sealed partial class ConfigurationFilesPage
{
	private sealed class UserPackagesTab : HtmlTab
	{
		public static UserPackagesTab Instance { get; } = new();

		public override string DisplayName => "User Packages";

		public override void Write(TextWriter writer)
		{
			List<UnityPackageData> packages = GameFileLoader.Settings.UserDefinedPackages;

			new P(writer).WithClass("p-2").Close("Upload mined package data (JSON) so that assets belonging to those packages are exported as " +
				"package references instead of being duplicated. This avoids broken references when the packages are re-added after export.");

			if (packages.Count == 0)
			{
				new P(writer).WithClass("p-2 text-center").Close("No user packages have been loaded.");
			}
			else
			{
				using (new Div(writer).WithClass("list-group mb-2").End())
				{
					foreach (UnityPackageData package in packages)
					{
						string name = string.IsNullOrEmpty(package.Name) ? "(unnamed)" : package.Name;
						string version = string.IsNullOrEmpty(package.Version) ? "" : $" {package.Version}";
						new Div(writer).WithClass("list-group-item").Close($"{name}{version} — {package.Assets.Count} asset reference(s)".ToHtml());
					}
				}
			}

			using (new Div(writer).WithClass("row text-center").End())
			{
				using (new Div(writer).WithClass("col").End())
				{
					using (new Form(writer).WithAction("/ConfigurationFiles/UserPackages/Add").WithMethod("post").End())
					{
						new Input(writer).WithType("submit").WithClass("btn btn-primary mx-1").WithValue("Load").Close();
					}
				}
				if (packages.Count > 0)
				{
					using (new Div(writer).WithClass("col").End())
					{
						using (new Form(writer).WithAction("/ConfigurationFiles/UserPackages/Clear").WithMethod("post").End())
						{
							new Input(writer).WithType("submit").WithClass("btn btn-danger mx-1").WithValue("Remove All").Close();
						}
					}
				}
			}
		}
	}
}
