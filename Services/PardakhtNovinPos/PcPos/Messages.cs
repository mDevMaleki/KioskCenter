using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace KioskCenter.Services.PardakhtNovinPos.PcPos;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Messages
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager resourceManager = new ResourceManager("PcPos.Messages", typeof(Messages).Assembly);
				resourceMan = resourceManager;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static string ResponseMessage00 => ResourceManager.GetString("ResponseMessage00", resourceCulture);

	internal static string ResponseMessage12 => ResourceManager.GetString("ResponseMessage12", resourceCulture);

	internal static string ResponseMessage29 => ResourceManager.GetString("ResponseMessage29", resourceCulture);

	internal static string ResponseMessage50 => ResourceManager.GetString("ResponseMessage50", resourceCulture);

	internal static string ResponseMessage51 => ResourceManager.GetString("ResponseMessage51", resourceCulture);

	internal static string ResponseMessage54 => ResourceManager.GetString("ResponseMessage54", resourceCulture);

	internal static string ResponseMessage55 => ResourceManager.GetString("ResponseMessage55", resourceCulture);

	internal static string ResponseMessage56 => ResourceManager.GetString("ResponseMessage56", resourceCulture);

	internal static string ResponseMessage58 => ResourceManager.GetString("ResponseMessage58", resourceCulture);

	internal static string ResponseMessage61 => ResourceManager.GetString("ResponseMessage61", resourceCulture);

	internal static string ResponseMessage65 => ResourceManager.GetString("ResponseMessage65", resourceCulture);

	internal static string ResponseMessage99 => ResourceManager.GetString("ResponseMessage99", resourceCulture);

	internal Messages()
	{
	}
}
