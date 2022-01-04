using System.Collections.Generic;
using Monitel.Mal.Context.CIM16;

namespace Monitel.SCADA.Tools.DataExplorer.Extensions
{
    public static class IdentifiedObjectExtensions
    {
        public static IEnumerable<IdentifiedObject> GetPathInTree(this IdentifiedObject obj, bool addSelf = false, string stopOn = Names.BaseObjectRoot.ClassName)
        {
            var items = new List<IdentifiedObject>();

            if (addSelf)
                items.Add(obj);

            while (obj.ParentObject != null && obj.MetaType.Name != stopOn)
            {
                items.Add(obj.ParentObject);

                obj = obj.ParentObject;
            }

            return items;
        }
    }
}
