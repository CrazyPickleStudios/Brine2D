using System;
using System.Runtime.CompilerServices;

namespace Brine2D
{
    public class EnumMap<T, U>
        where T : unmanaged, Enum
        where U : unmanaged, Enum
    {
        private struct Value
        {
            public int v;
            public bool set;
        }

        private readonly Value[] valuesT;
        private readonly Value[] valuesU;
        private readonly int peak;

        public struct Entry
        {
            public T t;
            public U u;
            public Entry(T t, U u) { this.t = t; this.u = u; }
        }

        public EnumMap(Entry[] entries, int peak)
        {
            this.peak = peak;
            valuesT = new Value[peak];
            valuesU = new Value[peak];

            foreach (var entry in entries)
            {
                int eT = Unsafe.As<T, int>(ref Unsafe.AsRef(entry.t));
                int eU = Unsafe.As<U, int>(ref Unsafe.AsRef(entry.u));

                if (eT >= 0 && eT < peak)
                {
                    valuesU[eT].v = eU;
                    valuesU[eT].set = true;
                }
                if (eU >= 0 && eU < peak)
                {
                    valuesT[eU].v = eT;
                    valuesT[eU].set = true;
                }
            }
        }

        public bool TryGetValue(T t, out U u)
        {
            int idx = Unsafe.As<T, int>(ref t);
            if (idx >= 0 && idx < peak && valuesU[idx].set)
            {
                u = Unsafe.As<int, U>(ref valuesU[idx].v);
                return true;
            }
            u = default;
            return false;
        }

        public bool TryGetValue(U u, out T t)
        {
            int idx = Unsafe.As<U, int>(ref u);
            if (idx >= 0 && idx < peak && valuesT[idx].set)
            {
                t = Unsafe.As<int, T>(ref valuesT[idx].v);
                return true;
            }
            t = default;
            return false;
        }
    }
}