using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskStatsServer.Extensions
{
    static class AutomationElementExtension
    {

        public static string GetCurrentName(this AutomationElement el)
        {
            try
            {
                return el.Name;
            }
            catch
            {
                return "";
            }
        }

        public static string GetCurrentClassName(this AutomationElement el)
        {
            try
            {
                return el.ClassName;
            }
            catch
            {
                return "";
            }
        }

        public static AutomationElement? FindOneBy(this AutomationElement el, Func<AutomationElement, bool> predicate)
        {
            return el.FindAllChildren().FirstOrDefault(predicate);
        }
    }
}
