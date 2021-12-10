// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TplEtwProvider.cs
//
// <OWNER>Microsoft</OWNER>
//
// EventSource for TPL.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Security;

namespace System.Threading.Tasks
{
    using System.Diagnostics.Tracing;

    /// <summary>Provides an event source for tracing TPL information.</summary>
    [EventSource(
        Name = "System.Threading.Tasks.TplEventSource",
        Guid = "2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5",
        LocalizationResources = "mscorlib")]
    internal sealed class TplEtwProvider : EventSource
    {
        /// Used to determine if tasks should generate Activity IDs for themselves
        internal bool TasksSetActivityIds;        // This keyword is set
        internal bool Debug;
        private bool DebugActivityId;

        /// <summary>
        /// Get callbacks when the ETW sends us commands`
        /// </summary>
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            // To get the AsyncCausality events, we need to inform the AsyncCausalityTracer
            if (command.Command == EventCommand.Enable)
                AsyncCausalityTracer.EnableToETW(true);
            else if (command.Command == EventCommand.Disable)
                AsyncCausalityTracer.EnableToETW(false);

            if (IsEnabled(EventLevel.Informational, Keywords.TasksFlowActivityIds))
                ActivityTracker.Instance.Enable();
            else
                TasksSetActivityIds = IsEnabled(EventLevel.Informational, Keywords.TasksSetActivityIds);

            Debug = IsEnabled(EventLevel.Informational, Keywords.Debug);
            DebugActivityId = IsEnabled(EventLevel.Informational, Keywords.DebugActivityId);
        }

        /// <summary>
        /// Defines the singleton instance for the TPL ETW provider.
        /// The TPL Event provider GUID is {2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5}.
        /// </summary>
        public static TplEtwProvider Log = new TplEtwProvider();
        /// <summary>Prevent external instantiation.  All logging should go through the Log instance.</summary>
        private TplEtwProvider() { }

        /// <summary>Type of a fork/join operation.</summary>
        public enum ForkJoinOperationType
        {
            /// <summary>Parallel.Invoke.</summary>
            ParallelInvoke = 1,
            /// <summary>Parallel.For.</summary>
            ParallelFor = 2,
            /// <summary>Parallel.ForEach.</summary>
            ParallelForEach = 3
        }

        /// <summary>Configured behavior of a task wait operation.</summary>
        public enum TaskWaitBehavior : int
        {
            /// <summary>A synchronous wait.</summary>
            Synchronous = 1,
            /// <summary>An asynchronous await.</summary>
            Asynchronous = 2
        }

        /// <summary>ETW tasks that have start/stop events.</summary>
        public class Tasks // this name is important for EventSource
        {
            /// <summary>A parallel loop.</summary>
            public const EventTask Loop = (EventTask)1;
            /// <summary>A parallel invoke.</summary>
            public const EventTask Invoke = (EventTask)2;
            /// <summary>Executing a Task.</summary>
            public const EventTask TaskExecute = (EventTask)3;
            /// <summary>Waiting on a Task.</summary>
            public const EventTask TaskWait = (EventTask)4;
            /// <summary>A fork/join task within a loop or invoke.</summary>
            public const EventTask ForkJoin = (EventTask)5;
            /// <summary>A task is scheduled to execute.</summary>
            public const EventTask TaskScheduled = (EventTask)6;
            /// <summary>An await task continuation is scheduled to execute.</summary>
            public const EventTask AwaitTaskContinuationScheduled = (EventTask)7;

            /// <summary>AsyncCausalityFunctionality.</summary>
            public const EventTask TraceOperation = (EventTask)8;
            public const EventTask TraceSynchronousWork = (EventTask)9;
        }

        public class Keywords // thisname is important for EventSource
        {
            /// <summary>
            /// Only the most basic information about the workings of the task library
            /// This sets activity IDS and logs when tasks are schedules (or waits begin)
            /// But are otherwise silent
            /// </summary>
            public const EventKeywords TaskTransfer = (EventKeywords)1;
            /// <summary>
            /// TaskTranser events plus events when tasks start and stop 
            /// </summary>
            public const EventKeywords Tasks = (EventKeywords)2;
            /// <summary>
            /// Events associted with the higher level parallel APIs
            /// </summary>
            public const EventKeywords Parallel = (EventKeywords)4;
            /// <summary>
            /// These are relatively verbose events that effectively just redirect
            /// the windows AsyncCausalityTracer to ETW
            /// </summary>
            public const EventKeywords AsyncCausalityOperation = (EventKeywords)8;
            public const EventKeywords AsyncCausalityRelation = (EventKeywords)0x10;
            public const EventKeywords AsyncCausalitySynchronousWork = (EventKeywords)0x20;

            /// <summary>
            /// Emit the stops as well as the schedule/start events
            /// </summary>
            public const EventKeywords TaskStops = (EventKeywords)0x40;

            /// <summary>
            /// TasksFlowActivityIds indicate that activity ID flow from one task
            /// to any task created by it. 
            /// </summary>
            public const EventKeywords TasksFlowActivityIds = (EventKeywords)0x80;

            /// <summary>
            /// TasksSetActivityIds will cause the task operations to set Activity Ids 
            /// This option is incompatible with TasksFlowActivityIds flow is ignored
            /// if that keyword is set.   This option is likley to be removed in the future
            /// </summary>
            public const EventKeywords TasksSetActivityIds = (EventKeywords)0x10000;

            /// <summary>
            /// Relatively Verbose logging meant for debugging the Task library itself. Will probably be removed in the future
            /// </summary>
            public const EventKeywords Debug = (EventKeywords)0x20000;
            /// <summary>
            /// Relatively Verbose logging meant for debugging the Task library itself.  Will probably be removed in the future
            /// </summary>
            public const EventKeywords DebugActivityId = (EventKeywords)0x40000;
        }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        //-----------------------------------------------------------------------------------
        //        
        // TPL Event IDs (must be unique)
        //

        /// <summary>The beginning of a parallel loop.</summary>
        private const int PARALLELLOOPBEGIN_ID = 1;
        /// <summary>The ending of a parallel loop.</summary>
        private const int PARALLELLOOPEND_ID = 2;
        /// <summary>The beginning of a parallel invoke.</summary>
        private const int PARALLELINVOKEBEGIN_ID = 3;
        /// <summary>The ending of a parallel invoke.</summary>
        private const int PARALLELINVOKEEND_ID = 4;
        /// <summary>A task entering a fork/join construct.</summary>
        private const int PARALLELFORK_ID = 5;
        /// <summary>A task leaving a fork/join construct.</summary>
        private const int PARALLELJOIN_ID = 6;

        /// <summary>A task is scheduled to a task scheduler.</summary>
        private const int TASKSCHEDULED_ID = 7;
        /// <summary>A task is about to execute.</summary>
        private const int TASKSTARTED_ID = 8;
        /// <summary>A task has finished executing.</summary>
        private const int TASKCOMPLETED_ID = 9;
        /// <summary>A wait on a task is beginning.</summary>
        private const int TASKWAITBEGIN_ID = 10;
        /// <summary>A wait on a task is ending.</summary>
        private const int TASKWAITEND_ID = 11;
        /// <summary>A continuation of a task is scheduled.</summary>
        private const int AWAITTASKCONTINUATIONSCHEDULED_ID = 12;
        /// <summary>A continuation of a taskWaitEnd is complete </summary>
        private const int TASKWAITCONTINUATIONCOMPLETE_ID = 13;
        /// <summary>A continuation of a taskWaitEnd is complete </summary>
        private const int TASKWAITCONTINUATIONSTARTED_ID = 19;

        private const int TRACEOPERATIONSTART_ID       = 14;
        private const int TRACEOPERATIONSTOP_ID        = 15;
        private const int TRACEOPERATIONRELATION_ID    = 16;
        private const int TRACESYNCHRONOUSWORKSTART_ID = 17;
        private const int TRACESYNCHRONOUSWORKSTOP_ID  = 18;


        //-----------------------------------------------------------------------------------
        //        
        // Parallel Events
        //

        #region ParallelLoopBegin
        /// <summary>
        /// Denotes the entry point for a Parallel.For or Parallel.ForEach loop
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The loop ID.</param>
        /// <param name="operationType">The kind of fork/join operation.</param>
        /// <param name="inclusiveFrom">The lower bound of the loop.</param>
        /// <param name="exclusiveTo">The upper bound of the loop.</param>
        [SecuritySafeCritical]
        [Event(PARALLELLOOPBEGIN_ID, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive,
         Task = TplEtwProvider.Tasks.Loop, Opcode = EventOpcode.Start)]
        public void ParallelLoopBegin(
            int originatingTaskSchedulerID, int originatingTaskID,      // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID, ForkJoinOperationType operationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
            long inclusiveFrom, long exclusiveTo)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                // There is no explicit WriteEvent() overload matching this event's fields. Therefore calling
                // WriteEvent() would hit the "params" overload, which leads to an object allocation every time 
                // this event is fired. To prevent that problem we will call WriteEventCore(), which works with 
                // a stack based EventData array populated with the event fields.
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[6];

                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&forkJoinContextID));
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&operationType));
                    eventPayload[4].Size = sizeof(long);
                    eventPayload[4].DataPointer = ((IntPtr)(&inclusiveFrom));
                    eventPayload[5].Size = sizeof(long);
                    eventPayload[5].DataPointer = ((IntPtr)(&exclusiveTo));

                    WriteEventCore(PARALLELLOOPBEGIN_ID, 6, eventPayload);
                }
            }
        }
        #endregion

        #region ParallelLoopEnd
        /// <summary>
        /// Denotes the end of a Parallel.For or Parallel.ForEach loop.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The loop ID.</param>
        /// <param name="totalIterations">the total number of iterations processed.</param>
        [SecuritySafeCritical]
        [Event(PARALLELLOOPEND_ID, Level = EventLevel.Informational, Task = TplEtwProvider.Tasks.Loop, Opcode = EventOpcode.Stop)]
        public void ParallelLoopEnd(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID, long totalIterations)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                // There is no explicit WriteEvent() overload matching this event's fields.
                // Therefore calling WriteEvent() would hit the "params" overload, which leads to an object allocation every time this event is fired.
                // To prevent that problem we will call WriteEventCore(), which works with a stack based EventData array populated with the event fields
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[4];

                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&forkJoinContextID));
                    eventPayload[3].Size = sizeof(long);
                    eventPayload[3].DataPointer = ((IntPtr)(&totalIterations));

                    WriteEventCore(PARALLELLOOPEND_ID, 4, eventPayload);
                }
            }
        }
        #endregion

        #region ParallelInvokeBegin
        /// <summary>Denotes the entry point for a Parallel.Invoke call.</summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The invoke ID.</param>
        /// <param name="operationType">The kind of fork/join operation.</param>
        /// <param name="actionCount">The number of actions being invoked.</param>
        [SecuritySafeCritical]
        [Event(PARALLELINVOKEBEGIN_ID, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive,
         Task = TplEtwProvider.Tasks.Invoke, Opcode = EventOpcode.Start)]
        public void ParallelInvokeBegin(
            int originatingTaskSchedulerID, int originatingTaskID,      // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID, ForkJoinOperationType operationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
            int actionCount)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                // There is no explicit WriteEvent() overload matching this event's fields.
                // Therefore calling WriteEvent() would hit the "params" overload, which leads to an object allocation every time this event is fired.
                // To prevent that problem we will call WriteEventCore(), which works with a stack based EventData array populated with the event fields
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[5];

                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&forkJoinContextID));
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&operationType));
                    eventPayload[4].Size = sizeof(int);
                    eventPayload[4].DataPointer = ((IntPtr)(&actionCount));

                    WriteEventCore(PARALLELINVOKEBEGIN_ID, 5, eventPayload);
                }
            }
        }
        #endregion

        #region ParallelInvokeEnd
        /// <summary>
        /// Denotes the exit point for a Parallel.Invoke call. 
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The invoke ID.</param>
        [Event(PARALLELINVOKEEND_ID, Level = EventLevel.Informational, Task = TplEtwProvider.Tasks.Invoke, Opcode = EventOpcode.Stop)]
        public void ParallelInvokeEnd(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                WriteEvent(PARALLELINVOKEEND_ID, originatingTaskSchedulerID, originatingTaskID, forkJoinContextID);
            }
        }
        #endregion

        #region ParallelFork
        /// <summary>
        /// Denotes the start of an individual task that's part of a fork/join context. 
        /// Before this event is fired, the start of the new fork/join context will be marked 
        /// with another event that declares a unique context ID. 
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The invoke ID.</param>
        [Event(PARALLELFORK_ID, Level = EventLevel.Verbose, ActivityOptions = EventActivityOptions.Recursive,
         Task = TplEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Start)]
        public void ParallelFork(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Parallel))
            {
                WriteEvent(PARALLELFORK_ID, originatingTaskSchedulerID, originatingTaskID, forkJoinContextID);
            }
        }
        #endregion

        #region ParallelJoin
        /// <summary>
        /// Denotes the end of an individual task that's part of a fork/join context. 
        /// This should match a previous ParallelFork event with a matching "OriginatingTaskID"
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="forkJoinContextID">The invoke ID.</param>
        [Event(PARALLELJOIN_ID, Level = EventLevel.Verbose, Task = TplEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Stop)]
        public void ParallelJoin(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int forkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Parallel))
            {
                WriteEvent(PARALLELJOIN_ID, originatingTaskSchedulerID, originatingTaskID, forkJoinContextID);
            }
        }
        #endregion

        //-----------------------------------------------------------------------------------
        //        
        // Task Events
        //

        // These are all verbose events, so we need to call IsEnabled(EventLevel.Verbose, ALL_KEYWORDS) 
        // call. However since the IsEnabled(l,k) call is more expensive than IsEnabled(), we only want 
        // to incur this cost when instrumentation is enabled. So the Task codepaths that call these
        // event functions still do the check for IsEnabled()

        #region TaskScheduled
        /// <summary>
        /// Fired when a task is queued to a TaskScheduler.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        /// <param name="creatingTaskID">The task ID</param>
        /// <param name="taskCreationOptions">The options used to create the task.</param>
        [SecuritySafeCritical]
        [Event(TASKSCHEDULED_ID, Task = Tasks.TaskScheduled, Version = 1, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks)]
        public void TaskScheduled(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int taskID, int creatingTaskID, int taskCreationOptions, int appDomain)
        {
            // IsEnabled() call is an inlined quick check that makes this very fast when provider is off 
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[5];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&taskID));
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&creatingTaskID));
                    eventPayload[4].Size = sizeof(int);
                    eventPayload[4].DataPointer = ((IntPtr)(&taskCreationOptions));
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(taskID);
                        WriteEventWithRelatedActivityIdCore(TASKSCHEDULED_ID, &childActivityId, 5, eventPayload);
                    }
                    else
                        WriteEventCore(TASKSCHEDULED_ID, 5, eventPayload);
                }
            }
        }
        #endregion

        #region TaskStarted
        /// <summary>
        /// Fired just before a task actually starts executing.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        [Event(TASKSTARTED_ID,
         Level = EventLevel.Informational, Keywords = Keywords.Tasks)]
        public void TaskStarted(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int taskID)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Tasks))
                WriteEvent(TASKSTARTED_ID, originatingTaskSchedulerID, originatingTaskID, taskID);
        }
        #endregion

        #region TaskCompleted
        /// <summary>
        /// Fired right after a task finished executing.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        /// <param name="isExceptional">Whether the task completed due to an error.</param>
        [SecuritySafeCritical]
        [Event(TASKCOMPLETED_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.TaskStops)]
        public void TaskCompleted(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int taskID, bool isExceptional)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[4];
                    Int32 isExceptionalInt = isExceptional ? 1 : 0;
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&taskID));
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&isExceptionalInt));
                    WriteEventCore(TASKCOMPLETED_ID, 4, eventPayload);
                }
            }
        }
        #endregion

        #region TaskWaitBegin
        /// <summary>
        /// Fired when starting to wait for a taks's completion explicitly or implicitly.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        /// <param name="behavior">Configured behavior for the wait.</param>
        /// <param name="continueWithTaskID">If known, if 'TaskID' has a 'continueWith' task, mention give its ID here.  
        ///      0 means unknown.   This allows better visualization of the common sequential chaining case.</param>
        /// </summary>
        [SecuritySafeCritical]
        [Event(TASKWAITBEGIN_ID, Version = 3, Task = TplEtwProvider.Tasks.TaskWait, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks)]
        public void TaskWaitBegin(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int taskID, TaskWaitBehavior behavior, int continueWithTaskID, int appDomain)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[5];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&taskID));
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&behavior));
                    eventPayload[4].Size = sizeof(int);
                    eventPayload[4].DataPointer = ((IntPtr)(&continueWithTaskID));
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(taskID);
                        WriteEventWithRelatedActivityIdCore(TASKWAITBEGIN_ID, &childActivityId, 5, eventPayload);
                    }
                    else
                        WriteEventCore(TASKWAITBEGIN_ID, 5, eventPayload);
                }
            }
        }
        #endregion

        /// <summary>
        /// Fired when the wait for a tasks completion returns.
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        [Event(TASKWAITEND_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.Tasks)]
        public void TaskWaitEnd(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int taskID)
        {
            // Log an event if indicated.  
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITEND_ID, originatingTaskSchedulerID, originatingTaskID, taskID);
        }

        /// <summary>
        /// Fired when the the work (method) associated with a TaskWaitEnd completes
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        [Event(TASKWAITCONTINUATIONCOMPLETE_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.TaskStops)]
        public void TaskWaitContinuationComplete(int taskID)
        {
            // Log an event if indicated.  
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITCONTINUATIONCOMPLETE_ID, taskID);
        }

        /// <summary>
        /// Fired when the the work (method) associated with a TaskWaitEnd completes
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="taskID">The task ID.</param>
        [Event(TASKWAITCONTINUATIONSTARTED_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.TaskStops)]
        public void TaskWaitContinuationStarted(int taskID)
        {
            // Log an event if indicated.  
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITCONTINUATIONSTARTED_ID, taskID);
        }

        /// <summary>
        /// Fired when the an asynchronous continuation for a task is scheduled
        /// </summary>
        /// <param name="originatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="originatingTaskID">The task ID.</param>
        /// <param name="TaskID">The activityId for the continuation.</param>
        [SecuritySafeCritical]
        [Event(AWAITTASKCONTINUATIONSCHEDULED_ID, Task = Tasks.AwaitTaskContinuationScheduled, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks)]
        public void AwaitTaskContinuationScheduled(
            int originatingTaskSchedulerID, int originatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int continuwWithTaskId)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[3];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&originatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&originatingTaskID));
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&continuwWithTaskId));
                    if (TasksSetActivityIds)
                    {
                        Guid continuationActivityId = CreateGuidForTaskID(continuwWithTaskId);
                        WriteEventWithRelatedActivityIdCore(AWAITTASKCONTINUATIONSCHEDULED_ID, &continuationActivityId, 3, eventPayload);
                    }
                    else
                        WriteEventCore(AWAITTASKCONTINUATIONSCHEDULED_ID, 3, eventPayload);
                }
            }
        }

        [SecuritySafeCritical]
        [Event(TRACEOPERATIONSTART_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityOperation)]
        public void TraceOperationBegin(int taskID, string operationName, long relatedContext)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
            {
                unsafe
                {
                    fixed (char* operationNamePtr = operationName)
                    {
                        EventData* eventPayload = stackalloc EventData[3];
                        eventPayload[0].Size = sizeof(int);
                        eventPayload[0].DataPointer = ((IntPtr)(&taskID));

                        eventPayload[1].Size = ((operationName.Length + 1) * 2);
                        eventPayload[1].DataPointer = ((IntPtr)operationNamePtr);

                        eventPayload[2].Size = sizeof(long);
                        eventPayload[2].DataPointer = ((IntPtr)(&relatedContext));
                        WriteEventCore(TRACEOPERATIONSTART_ID, 3, eventPayload);
                    }
                }
            }
        }

        [SecuritySafeCritical]
        [Event(TRACEOPERATIONRELATION_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityRelation)]
        public void TraceOperationRelation(int taskID, CausalityRelation relation)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityRelation))
                WriteEvent(TRACEOPERATIONRELATION_ID, taskID, (int)relation);                // optmized overload for this exists
        }

        [SecuritySafeCritical]
        [Event(TRACEOPERATIONSTOP_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityOperation)]
        public void TraceOperationEnd(int taskID, AsyncCausalityStatus status)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
                WriteEvent(TRACEOPERATIONSTOP_ID, taskID, (int)status);                     // optmized overload for this exists
        }

        [SecuritySafeCritical]
        [Event(TRACESYNCHRONOUSWORKSTART_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalitySynchronousWork)]
        public void TraceSynchronousWorkBegin(int taskID, CausalitySynchronousWork work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
                WriteEvent(TRACESYNCHRONOUSWORKSTART_ID, taskID, (int)work);               // optmized overload for this exists
        }

        [SecuritySafeCritical]
        [Event(TRACESYNCHRONOUSWORKSTOP_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalitySynchronousWork)]
        public void TraceSynchronousWorkEnd(CausalitySynchronousWork work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[1];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&work));

                    WriteEventCore(TRACESYNCHRONOUSWORKSTOP_ID, 1, eventPayload);
                }
            }
        }

        [NonEvent, System.Security.SecuritySafeCritical]
        unsafe public void RunningContinuation(int taskID, object obj)
        {
            RunningContinuation(taskID, (long)*((void**)JitHelpers.UnsafeCastToStackPointer(ref obj)));
        }

        [Event(20, Keywords = Keywords.Debug)]
        private void RunningContinuation(int taskID, long obj)
        {
            if (Debug)
                WriteEvent(20, taskID, obj);
        }

        [NonEvent, System.Security.SecuritySafeCritical]
        unsafe public void RunningContinuationList(int taskID, int index, object obj)
        {
            RunningContinuationList(taskID, index, (long)*((void**)JitHelpers.UnsafeCastToStackPointer(ref obj)));
        }

        [Event(21, Keywords = Keywords.Debug)]
        public void RunningContinuationList(int taskID, int index, long obj)
        {
            if (Debug)
                WriteEvent(21, taskID, index, obj);
        }

        [Event(22, Keywords = Keywords.Debug)]
        public void DebugMessage(string message) { WriteEvent(22, message); }

        [Event(23, Keywords = Keywords.Debug)]
        public void DebugFacilityMessage(string facility, string message) { WriteEvent(23, facility, message); }

        [Event(24, Keywords = Keywords.Debug)]
        public void DebugFacilityMessage1(string facility, string message, string value1) { WriteEvent(24, facility, message, value1); }

        [Event(25, Keywords = Keywords.DebugActivityId)]
        public void SetActivityId(Guid newId)
        {
            if (DebugActivityId)
                WriteEvent(25, newId);
        }

        [Event(26, Keywords = Keywords.Debug)]
        public void NewID(int taskID)
        {
            if (Debug)
                WriteEvent(26, taskID);
        }

        /// <summary>
        /// Activity IDs are GUIDS but task IDS are integers (and are not unique across appdomains
        /// This routine creates a process wide unique GUID given a task ID
        /// </summary>
        internal static Guid CreateGuidForTaskID(int taskID)
        {
            // The thread pool generated a process wide unique GUID from a task GUID by
            // using the taskGuid, the appdomain ID, and 8 bytes of 'randomization' chosen by
            // using the last 8 bytes  as the provider GUID for this provider.  
            // These were generated by CreateGuid, and are reasonably random (and thus unlikley to collide
            uint pid = EventSource.s_currentPid;
            int appDomainID = System.Threading.Thread.GetDomainID();
            return new Guid(taskID,
                            (short)appDomainID, (short)(appDomainID >> 16),
                            (byte)pid, (byte)(pid >> 8), (byte)(pid >> 16), (byte)(pid >> 24),
                            0xff, 0xdc, 0xd7, 0xb5);
        }
    }
}
