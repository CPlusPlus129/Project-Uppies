using System;
using System.Collections.Generic;

namespace DialogueModule
{
    public class ObservableValue<T>
    {
        private T _value;
        public event Action<T> OnValueChanged;

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }

        public ObservableValue(T initialValue = default)
        {
            _value = initialValue;
        }
    }

}