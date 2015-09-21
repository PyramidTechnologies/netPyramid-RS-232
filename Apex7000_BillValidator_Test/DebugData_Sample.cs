using PyramidNETRS232;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PyramidNETRS232_TestApp
{
    /// <summary>
    /// Demonstrates how to make use of the debug data
    /// </summary>
    partial class MainWindow
    {

        private FixedObservableLinkedList<DebugBufferEntry> debugQueueMaster = new FixedObservableLinkedList<DebugBufferEntry>(20);
        private FixedObservableLinkedList<DebugBufferEntry> debugQueueSlave = new FixedObservableLinkedList<DebugBufferEntry>(20);

        /// <summary>
        /// On receipt of a debug entry, add the entry to our UI console
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="entry"></param>
        void config_OnSerialData(object sender, DebugEntryArgs e)
        {
            var entry = e.Entry;
            DoOnUIThread(() =>
            {
                if (entry.Flow == Flows.Master)
                {
                    debugQueueMaster.Add(entry);

                    ConsoleLoggerMaster.ScrollIntoView(entry);
                }
                else
                {
                    debugQueueSlave.Add(entry);

                    ConsoleLoggerSlave.ScrollIntoView(entry);
                }
            });
        }
    }

    /// <summary>
    /// Used for the debug console view, we limit the number of entries that our list contains. This
    /// avoids out of memory exceptions if you were to leave this running overnight at a fast poll rate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FixedObservableLinkedList<T> : LinkedList<T>, INotifyCollectionChanged
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedObservableLinkedList(int size)
        {
            Size = size;
        }

        /// <summary>
        /// Adds elements to the head of the list so when bound to a UI element,
        /// the latest entry is on top
        /// </summary>
        /// <param name="obj"></param>
        public void Add(T obj)
        {
            AddFirst(obj);
            lock (syncObject)
            {
                while (Count > Size)
                {
                    RemoveLast();
                }
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                if (CollectionChanged != null)
                    CollectionChanged(this, args);
            });
        }
    }
}
