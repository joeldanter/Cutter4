using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public class FullyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        public delegate void ElementsPropertyChangedEventHandler(object sender, ElementPropertyChangedEventArgs e);
        public event ElementsPropertyChangedEventHandler ElementsPropertyChanged;

        public FullyObservableCollection() : base() { }
        public FullyObservableCollection(IEnumerable<T> collection) : base(collection)
        {
            Subscribe(this);
        }
        public FullyObservableCollection(List<T> list) : base(list)
        {
            Subscribe(this);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            Unsubscribe(e.OldItems);
            Subscribe(e.NewItems);
            base.OnCollectionChanged(e);
        }

        private void Subscribe(IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                    element.PropertyChanged += OnElementPropertyChanged;
            }
        }

        private void Unsubscribe(IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                    element.PropertyChanged -= OnElementPropertyChanged;
            }
        }

        protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ElementsPropertyChanged?.Invoke(this, new ElementPropertyChangedEventArgs(sender, e.PropertyName));
        }
    }

    public class ElementPropertyChangedEventArgs : EventArgs
    {
        public object SenderElement { get; }
        public string ElementsPropertyName { get; }

        public ElementPropertyChangedEventArgs(object elementSender, string ElementsPropertyName)
        {
            this.ElementsPropertyName = ElementsPropertyName;
            SenderElement = elementSender;
        }
    }
}
