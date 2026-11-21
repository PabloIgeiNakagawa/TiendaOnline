namespace TiendaOnline.Extensions
{
    public static class FeatureFolderExtensions
    {
        public static IMvcBuilder AddFeatureFolders(this IMvcBuilder builder)
        {
            return builder.AddRazorOptions(options =>
            {
                options.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
            });
        }
    }
}