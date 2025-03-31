using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NmkdUtils.Models
{
    public class ConcurrencyMgr
    {
        public int CurrTasks = 0;
        public int CurrTasksRunning = 0;
        public int TasksFinished = 0;
        public ConcurrentDictionary<Action, (string Info, string Log, DateTime? Finished)> _taskLogDict = new(); // Task number, task info (optional), last log msg
        public Dictionary<string, int> _files = []; // Maps filenames to task number, if needed

        List<Action> _actions = [];
        private int _maxThreads = 1;
        private int _staggerDelayMs = 0;

        public ConcurrencyMgr(List<Action> actions, int threads = -1, int staggerDelayMs = 0)
        {
            _actions = actions;
            _maxThreads = threads == -1 ? Environment.ProcessorCount : threads;
        }

        public void Run()
        {
            var opts = new ParallelOptions { MaxDegreeOfParallelism = _maxThreads };

            Parallel.For(0, CurrTasks, opts, i =>
            {
                var action = _actions[i];
                int actNum = i + 1;
                Thread.Sleep(actNum * _staggerDelayMs); // Stagger starting of tasks
                Interlocked.Increment(ref CurrTasksRunning);
                _taskLogDict[action] = ("", "", null);
                action();
                Interlocked.Decrement(ref CurrTasksRunning);
                Interlocked.Increment(ref TasksFinished);
            });
        }

        public void SetFiles(List<string> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                _files[files[i]] = i;
            }
        }

        public void UpdateLogView()
        {
            string msg = $"Running {CurrTasksRunning} task(s), {TasksFinished}/{CurrTasks} finished, max. {_maxThreads} threads\n";

            foreach (var task in _taskLogDict)
            {
                string info = task.Value.Info;
                string log = task.Value.Log;
                DateTime? finished = task.Value.Finished;
                string status = finished.HasValue ? $"Finished {FormatUtils.Time(DateTime.Now - finished.Value)} ago" : "Running";
                string num = _actions.IndexOf(task.Key).ToString().PadLeft(2);
                string infoStr = info != "" ? $" ({info})" : "";
                string logStr = log != "" ? $" {log.Trunc(80).PadRight(83)}" : "";
                msg += $"\n[{num}] {infoStr}{logStr.Trunc(110).PadRight(113)} [{status}]";
            }

            CliUtils.ClearConsoleAnsi();
            Console.WriteLine(msg);
        }
    }
}
