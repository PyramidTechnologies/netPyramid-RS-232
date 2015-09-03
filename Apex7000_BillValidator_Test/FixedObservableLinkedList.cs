using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator_Test
{
/// <summary>
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
