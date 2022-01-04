using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Monitel.SCADA.UICommon.Documents
{
    class Icons
    {
		public static object Loaded => TryGetIconByName("MAG_DocumentsUpdate_x16");

		private static object TryGetIconByName(object iconKey) => IconDictionary?[iconKey];

		private static ResourceDictionary IconDictionary { get; }
		static Icons()
		{
			try
			{
				IconDictionary = Application.LoadComponent(new Uri("/Monitel.SCADA.UICommon;Component/Documents/SharedIcons.xaml", UriKind.Relative)) as ResourceDictionary;
			}
			catch
			{
				//Ignore
			}

		}
	}
}
