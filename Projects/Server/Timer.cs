/***************************************************************************
 *                                 Timer.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Server.Diagnostics;

namespace Server
{
  public enum TimerPriority
  {
    EveryTick,
    TenMS,
    TwentyFiveMS,
    FiftyMS,
    TwoFiftyMS,
    OneSecond,
    FiveSeconds,
    OneMinute
  }

  public delegate void TimerCallback();

  public delegate void TimerStateCallback<in T>(T state);

  public class Timer
  {
    private static Queue<Timer> m_Queue = new Queue<Timer>();

    private static int m_QueueCountAtSlice;
    private long m_Delay;
    private int m_Index, m_Count;
    private long m_Interval;
    private List<Timer> m_List;
    private long m_Next;
    private TimerPriority m_Priority;
    private bool m_PrioritySet;

    private bool m_Queued;
    private bool m_Running;

    public Timer(TimeSpan delay) : this(delay, TimeSpan.Zero, 1)
    {
    }

    public Timer(TimeSpan delay, TimeSpan interval, int count = 0)
    {
      m_Delay = (long)delay.TotalMilliseconds;
      m_Interval = (long)interval.TotalMilliseconds;
      m_Count = count;

      if (!m_PrioritySet)
      {
        if (count == 1)
          m_Priority = ComputePriority(delay);
        else
          m_Priority = ComputePriority(interval);
        m_PrioritySet = true;
      }

      if (DefRegCreation)
        RegCreation();
    }

    public TimerPriority Priority
    {
      get => m_Priority;
      set
      {
        if (!m_PrioritySet)
          m_PrioritySet = true;

        if (m_Priority != value)
        {
          m_Priority = value;

          if (m_Running)
            TimerThread.PriorityChange(this, (int)m_Priority);
        }
      }
    }

    public DateTime Next => DateTime.UtcNow + TimeSpan.FromMilliseconds(m_Next - Core.TickCount);

    public TimeSpan Delay
    {
      get => TimeSpan.FromMilliseconds(m_Delay);
      set => m_Delay = (long)value.TotalMilliseconds;
    }

    public TimeSpan Interval
    {
      get => TimeSpan.FromMilliseconds(m_Interval);
      set => m_Interval = (long)value.TotalMilliseconds;
    }

    public bool Running
    {
      get => m_Running;
      set
      {
        if (value)
          Start();
        else
          Stop();
      }
    }

    public static int BreakCount{ get; set; } = 20000;

    public virtual bool DefRegCreation => true;

    private static string FormatDelegate(Delegate callback)
    {
      if (callback == null)
        return "null";

      return $"{callback.Method.DeclaringType.FullName}.{callback.Method.Name}";
    }

    public static void DumpInfo(TextWriter tw)
    {
      TimerThread.DumpInfo(tw);
    }

    public TimerProfile GetProfile()
    {
      if (!Core.Profiling) return null;

      string name = ToString();

      return TimerProfile.Acquire(name);
    }

    public static void Slice()
    {
      lock (m_Queue)
      {
        m_QueueCountAtSlice = m_Queue.Count;

        int index = 0;

        while (index < BreakCount && m_Queue.Count != 0)
        {
          Timer t = m_Queue.Dequeue();
          TimerProfile prof = t.GetProfile();

          prof?.Start();

          t.OnTick();
          t.m_Queued = false;
          ++index;

          prof?.Finish();
        }
      }
    }

    public void RegCreation()
    {
      TimerProfile prof = GetProfile();

      if (prof != null) prof.Created++;
    }

    public override string ToString() => GetType().FullName;

    public static TimerPriority ComputePriority(TimeSpan ts)
    {
      if (ts >= TimeSpan.FromMinutes(1.0))
        return TimerPriority.FiveSeconds;

      if (ts >= TimeSpan.FromSeconds(10.0))
        return TimerPriority.OneSecond;

      if (ts >= TimeSpan.FromSeconds(5.0))
        return TimerPriority.TwoFiftyMS;

      if (ts >= TimeSpan.FromSeconds(2.5))
        return TimerPriority.FiftyMS;

      if (ts >= TimeSpan.FromSeconds(1.0))
        return TimerPriority.TwentyFiveMS;

      if (ts >= TimeSpan.FromSeconds(0.5))
        return TimerPriority.TenMS;

      return TimerPriority.EveryTick;
    }

    public void Start()
    {
      if (!m_Running)
      {
        m_Running = true;
        TimerThread.AddTimer(this);

        TimerProfile prof = GetProfile();

        if (prof != null) prof.Started++;
      }
    }

    public void Stop()
    {
      if (m_Running)
      {
        m_Running = false;
        TimerThread.RemoveTimer(this);

        TimerProfile prof = GetProfile();

        if (prof != null) prof.Stopped++;
      }
    }

    protected virtual void OnTick()
    {
    }

    public class TimerThread
    {
      private static Dictionary<Timer, TimerChangeEntry> m_Changed = new Dictionary<Timer, TimerChangeEntry>();

      private static long[] m_NextPriorities = new long[8];

      private static long[] m_PriorityDelays = {
        0,
        10,
        25,
        50,
        250,
        1000,
        5000,
        60000
      };

      private static List<Timer>[] m_Timers = new List<Timer>[8]
      {
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>(),
        new List<Timer>()
      };

      private static AutoResetEvent m_Signal = new AutoResetEvent(false);

      public static void DumpInfo(TextWriter tw)
      {
        for (int i = 0; i < 8; ++i)
        {
          tw.WriteLine("Priority: {0}", (TimerPriority)i);
          tw.WriteLine();

          Dictionary<string, List<Timer>> hash = new Dictionary<string, List<Timer>>();

          for (int j = 0; j < m_Timers[i].Count; ++j)
          {
            Timer t = m_Timers[i][j];

            string key = t.ToString();

            if (!hash.TryGetValue(key, out List<Timer> list))
              hash[key] = list = new List<Timer>();

            list.Add(t);
          }

          foreach (KeyValuePair<string, List<Timer>> kv in hash)
          {
            string key = kv.Key;
            List<Timer> list = kv.Value;

            tw.WriteLine("Type: {0}; Count: {1}; Percent: {2}%", key, list.Count,
              (int)(100 * (list.Count / (double)m_Timers[i].Count)));
          }

          tw.WriteLine();
          tw.WriteLine();
        }
      }

      public static void Change(Timer t, int newIndex, bool isAdd)
      {
        lock (m_Changed)
        {
          m_Changed[t] = TimerChangeEntry.GetInstance(t, newIndex, isAdd);
        }

        m_Signal.Set();
      }

      public static void AddTimer(Timer t)
      {
        Change(t, (int)t.Priority, true);
      }

      public static void PriorityChange(Timer t, int newPrio)
      {
        Change(t, newPrio, false);
      }

      public static void RemoveTimer(Timer t)
      {
        Change(t, -1, false);
      }

      private static void ProcessChanged()
      {
        lock (m_Changed)
        {
          long curTicks = Core.TickCount;

          foreach (TimerChangeEntry tce in m_Changed.Values)
          {
            Timer timer = tce.m_Timer;
            int newIndex = tce.m_NewIndex;

            timer.m_List?.Remove(timer);

            if (tce.m_IsAdd)
            {
              timer.m_Next = curTicks + timer.m_Delay;
              timer.m_Index = 0;
            }

            if (newIndex >= 0)
            {
              timer.m_List = m_Timers[newIndex];
              timer.m_List.Add(timer);
            }
            else
            {
              timer.m_List = null;
            }

            tce.Free();
          }

          m_Changed.Clear();
        }
      }

      public static void Set()
      {
        m_Signal.Set();
      }

      public void TimerMain()
      {
        while (!Core.Closing)
        {
          if (World.Loading || World.Saving)
          {
            m_Signal.WaitOne(1, false);
            continue;
          }

          ProcessChanged();

          bool loaded = false;

          for (int i = 0; i < m_Timers.Length; i++)
          {
            long now = Core.TickCount;
            if (now < m_NextPriorities[i])
              break;

            m_NextPriorities[i] = now + m_PriorityDelays[i];

            for (int j = 0; j < m_Timers[i].Count; j++)
            {
              Timer t = m_Timers[i][j];

              if (!t.m_Queued && now > t.m_Next)
              {
                t.m_Queued = true;

                lock (m_Queue)
                {
                  m_Queue.Enqueue(t);
                }

                loaded = true;

                if (t.m_Count != 0 && ++t.m_Index >= t.m_Count)
                  t.Stop();
                else
                  t.m_Next = now + t.m_Interval;
              }
            }
          }

          if (loaded)
            Core.Set();

          m_Signal.WaitOne(1, false);
        }
      }

      private class TimerChangeEntry
      {
        private static Queue<TimerChangeEntry> m_InstancePool = new Queue<TimerChangeEntry>();
        public bool m_IsAdd;
        public int m_NewIndex;
        public Timer m_Timer;

        private TimerChangeEntry(Timer t, int newIndex, bool isAdd)
        {
          m_Timer = t;
          m_NewIndex = newIndex;
          m_IsAdd = isAdd;
        }

        public void Free()
        {
          lock (m_InstancePool)
          {
            if (m_InstancePool.Count < 200) // Arbitrary
              m_InstancePool.Enqueue(this);
          }
        }

        public static TimerChangeEntry GetInstance(Timer t, int newIndex, bool isAdd)
        {
          TimerChangeEntry e = null;

          lock (m_InstancePool)
          {
            if (m_InstancePool.Count > 0) e = m_InstancePool.Dequeue();
          }

          if (e != null)
          {
            e.m_Timer = t;
            e.m_NewIndex = newIndex;
            e.m_IsAdd = isAdd;
          }
          else
          {
            e = new TimerChangeEntry(t, newIndex, isAdd);
          }

          return e;
        }
      }
    }

    #region DelayCall(..)

    public static Timer DelayCall(TimerCallback callback) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback);

    public static Timer DelayCall(TimeSpan delay, TimerCallback callback) => DelayCall(delay, TimeSpan.Zero, 1, callback);

    public static Timer DelayCall(TimeSpan delay, TimeSpan interval, TimerCallback callback) => DelayCall(delay, interval, 0, callback);

    public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
    {
      Timer t = new DelayCallTimer(delay, interval, count, callback);

      t.Priority = ComputePriority(count == 1 ? delay : interval);
      t.Start();

      return t;
    }

    #endregion

    #region DelayCall<T>(..)

    public static Timer DelayCall<T>(TimerStateCallback<T> callback, T state) => DelayCall(TimeSpan.Zero, TimeSpan.Zero, 1, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimerStateCallback<T> callback, T state) => DelayCall(delay, TimeSpan.Zero, 1, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, TimerStateCallback<T> callback, T state) => DelayCall(delay, interval, 0, callback, state);

    public static Timer DelayCall<T>(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T> callback,
      T state)
    {
      Timer t = new DelayStateCallTimer<T>(delay, interval, count, callback, state);

      t.Priority = ComputePriority(count == 1 ? delay : interval);

      t.Start();

      return t;
    }

    #endregion

    #region DelayCall Timers

    private class DelayCallTimer : Timer
    {
      public DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback) : base(delay,
        interval, count)
      {
        Callback = callback;
        RegCreation();
      }

      public TimerCallback Callback{ get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke();
      }

      public override string ToString() => $"DelayCallTimer[{FormatDelegate(Callback)}]";
    }

    private class DelayStateCallTimer<T> : Timer
    {
      private T m_State;

      public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback<T> callback, T state)
        : base(delay, interval, count)
      {
        Callback = callback;
        m_State = state;

        RegCreation();
      }

      public TimerStateCallback<T> Callback{ get; }

      public override bool DefRegCreation => false;

      protected override void OnTick()
      {
        Callback?.Invoke(m_State);
      }

      public override string ToString() => $"DelayStateCall[{FormatDelegate(Callback)}]";
    }

    #endregion

    private class DelayTaskTimer : Timer
    {
      private TaskCompletionSource<DelayTaskTimer> m_TaskCompleter;

      public Task Task => m_TaskCompleter.Task;

      public DelayTaskTimer(TimeSpan delay) : base(delay) => m_TaskCompleter = new TaskCompletionSource<DelayTaskTimer>();

      protected override void OnTick()
      {
        m_TaskCompleter.SetResult(this);
      }
    }

    public static Task Pause(int ms) => Pause(TimeSpan.FromMilliseconds(ms));

    public static Task Pause(TimeSpan ms)
    {
      DelayTaskTimer t = new DelayTaskTimer(ms);
      t.Start();
      return t.Task;
    }
  }
}
