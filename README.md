# TaskStatsServer

背景：Windows 10 和以上的任务管理器，其 CPU 占用率读数的获取方法未公开，网上也没有任何公开的方案能够获取到与任务管理器完全一致的数据。

TaskStatsServer (TSS) 通过劫持一个任务管理器的进程实例，利用 Windows Accessibility API 从图形界面层面读取任务管理器的数据，并通过 HTTP 服务提供给外部调用。
