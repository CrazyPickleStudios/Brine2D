//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Brine2D
//{
//    public class Type
//    {
//        private static readonly Dictionary<string, Type> types = new();
//        private static uint nextId = 1;

//        private readonly string name;
//        private readonly Type parent;
//        private uint id;
//        private bool inited;
//        private BitArray bits;

//        public Type(string name, Type parent = null)
//        {
//            this.name = name ?? throw new ArgumentNullException(nameof(name));
//            this.parent = parent;
//            this.id = 0;
//            this.inited = false;
//            this.bits = new BitArray(1024, false); // Arbitrary size, can be increased if needed
//        }

//        public void Init()
//        {
//            if (inited)
//                return;

//            // Register type
//            types[name] = this;
//            id = nextId++;
//            EnsureBitsSize((int)id + 1);
//            bits[(int)id] = true;
//            inited = true;

//            if (parent != null)
//            {
//                if (!parent.inited)
//                    parent.Init();
//                bits.Or(parent.bits);
//            }
//        }

//        public uint GetId()
//        {
//            if (!inited)
//                Init();
//            return id;
//        }

//        public string GetName()
//        {
//            return name;
//        }

//        public static Type ByName(string name)
//        {
//            types.TryGetValue(name, out var t);
//            return t;
//        }

//        // Helper to ensure the BitArray is large enough
//        private void EnsureBitsSize(int size)
//        {
//            if (bits.Length < size)
//            {
//                var newBits = new BitArray(size, false);
//                for (int i = 0; i < bits.Length; i++)
//                    newBits[i] = bits[i];
//                bits = newBits;
//            }
//        }

//        // Optional: check if this type is or derives from another type
//        public bool IsA(Type other)
//        {
//            if (!inited)
//                Init();
//            if (!other.inited)
//                other.Init();
//            return bits[(int)other.GetId()];
//        }
//    }
//}
