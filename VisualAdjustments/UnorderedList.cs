using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualAdjustments
{
    public class UnorderedList<TKey, TValue>
    {
        List<TKey> m_Keys = new List<TKey>();
        List<TValue> m_Values = new List<TValue>();
        Dictionary<TKey, int> keyLookup = new Dictionary<TKey, int>();
        public IList<TKey> Keys {
            get { return m_Keys; }
        }
        public IList<TValue> Values
        {
            get { return m_Values; }
        }
        public int Count
        {
            get { return m_Values.Count;  }
        }
        public void Add(TKey key, TValue value)
        {
            if (keyLookup.ContainsKey(key))
            {
                m_Values[keyLookup[key]] = value;
            }
            else
            {
                m_Keys.Add(key);
                m_Values.Add(value);
                keyLookup[key] = m_Keys.Count - 1;
            }
        }
        public TValue this[TKey key]
        {
            get { return m_Values[keyLookup[key]]; }
            set { Add(key, value); }
        }
        public bool ContainsKey(TKey key)
        {
            return keyLookup.ContainsKey(key);
        }
        public int IndexOfKey(TKey key)
        {
            return keyLookup.ContainsKey(key) ? keyLookup[key] : -1;
        }
    }
}
