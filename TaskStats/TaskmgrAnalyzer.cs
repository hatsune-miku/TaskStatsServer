using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TaskStatsServer.Extensions;

namespace TaskStatsServer.TaskStats
{
    internal class TaskmgrAnalyzer
    {
        private Application? _taskmgrApp = null;
        private UIA3Automation? _automation = null;
        private Window? _mainWindow = null;
        private List<string> _featuredProcessPatterns = new();
        private IEnumerable<string>? _headers = null;
        private AutomationElement[]? _categories = null;
        private AutomationElement? _columnHeader = null;

        public void StartAnalyzer(List<string> processPatterns)
        {
            var taskmgrProcesses = Process.GetProcessesByName("Taskmgr");
            foreach (var taskmgrProcess in taskmgrProcesses)
            {
                taskmgrProcess.Kill();
            }

            var systemDrive = Environment.GetEnvironmentVariable("SYSTEMDRIVE") ?? "C:";
            var startInfo = new ProcessStartInfo($"Taskmgr.exe");
            _taskmgrApp = FlaUI.Core.Application.Launch(startInfo);
            _automation = new UIA3Automation();
            _mainWindow = _taskmgrApp.GetMainWindow(_automation);
            _featuredProcessPatterns = processPatterns;

            AutomationElement? rootElement = null;
            var elements = _mainWindow.FindAll(TreeScope.Children, TrueCondition.Default);
            foreach (var element in elements)
            {
                if (element.GetCurrentName() == "TaskManagerMain")
                {
                    rootElement = element;
                    break;
                }
            }

            if (rootElement == null)
            {
                throw new InvalidOperationException("Root element not found");
            }

            _columnHeader = rootElement.FindOneBy(el => el.GetCurrentClassName() == "TmColumnHeader");
            if (_columnHeader == null)
            {
                throw new InvalidOperationException("Column header not found");
            }

            _UpdateHeaders();

            var processMasterCategory = rootElement.FindOneBy(el =>
                el.GetCurrentName() == "进程" && el.GetCurrentClassName() == "TmScrollViewer");

            if (processMasterCategory == null)
            {
                throw new InvalidOperationException("Process master category not found");
            }

            var foregroundProcessCategory = processMasterCategory.FindOneBy(el =>
                el.GetCurrentName() == "应用" && el.GetCurrentClassName() == "TmGroupHeader");

            var backgroundProcessCategory = processMasterCategory.FindOneBy(el =>
                el.GetCurrentName() == "后台进程" && el.GetCurrentClassName() == "TmGroupHeader");

            if (foregroundProcessCategory == null || backgroundProcessCategory == null)
            {
                throw new InvalidOperationException("Process category not found");
            }

            _categories = [foregroundProcessCategory, backgroundProcessCategory];

            // Thread.Sleep(3500);
            // TraverseUITree(0, backgroundProcessCategory);
        }

        private void TraverseUITree(int level, AutomationElement rootElement)
        {
            var leftPadding = new string('\t', level);
            Console.WriteLine($"{leftPadding} level={level} ({rootElement.GetCurrentName()} [{rootElement.GetCurrentClassName()}])");
            foreach (var child in rootElement.FindAllChildren())
            {
                TraverseUITree(level + 1, child);
            }
        }

        public List<Dictionary<string, string>> ExtractFeaturedProcessInfo()
        {
            _UpdateHeaders();

            if (_headers == null || _categories == null)
            {
                throw new InvalidOperationException("Analyzer not started");
            }

            var ret = new List<Dictionary<string, string>>();
            foreach (var category in _categories)
            {
                var processes = category.FindAllChildren();
                var targetProcesses = new List<AutomationElement>();

                foreach (var processCandidate in processes)
                {
                    if (processCandidate.GetCurrentClassName() != "TmViewItemSelector")
                    {
                        continue;
                    }
                    if (!_featuredProcessPatterns.Any(pattern => processCandidate.GetCurrentName().Contains(pattern)))
                    {
                        continue;
                    }
                    targetProcesses.Add(processCandidate);
                    var subCandidates = from candidate in processCandidate.FindAllChildren()
                                        where candidate.GetCurrentClassName() == "TmViewItemSelector"
                                        select candidate;
                    targetProcesses.AddRange(subCandidates);
                }
                if (targetProcesses.Count > 0)
                {
                    foreach (var targetProcess in targetProcesses)
                    {
                        var properties = targetProcess.FindAllChildren();
                        var processInfo = new Dictionary<string, string>();

                        _headers.Zip(properties, (header, property) =>
                        {
                            var value = property.GetCurrentName();
                            processInfo[header] = value;
                            return (header, value);
                        }).Count();

                        // 进行简单的后处理~
                        processInfo["名称"] = targetProcess.GetCurrentName();
                        if (processInfo["PID"] == "")
                        {
                            processInfo["PID"] = "0";
                        }
                        ret.Add(processInfo);
                    }
                }
            }
            return ret;
        }

        private void _UpdateHeaders()
        {
            if (_columnHeader == null)
            {
                return;
            }
            _headers = from child in _columnHeader.FindAllChildren()
                       select child.GetCurrentName()
                       .Split('.')
                       .FirstOrDefault("")
                       .Split(" ")
                       .FirstOrDefault("");
        }
    }
}
