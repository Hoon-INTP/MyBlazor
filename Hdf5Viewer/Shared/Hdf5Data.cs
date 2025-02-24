namespace Hdf5Viewer.Shared
{
    public class Hdf5TreeNode
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Group", "Dataset", "Attribute" 등
        public List<Hdf5TreeNode> Children { get; set; } = new List<Hdf5TreeNode>();
        public bool HasChildren { get => Children.Count > 0; }
        public bool IsExpanded { get; set; } = false; // TreeView 확장 상태
    }

    public class Hdf5TableData
    {
        public List<string> ColumnNames { get; set; } = new List<string>();
        public List<List<string>> RowData { get; set; } = new List<List<string>>();
    }
    
    namespace DCOPDataStructure
    {
        public enum IOType
        {
            NONE,
            AIO,
            DIO,
            SIO,
            SEQ
        }
    
        public abstract class CIOBase
        {
            protected string _ioName;
            protected IOType _ioType;
    
            public CIOBase(string name, IOType type)
            {
                _ioName = name;
                _ioType = type;
            }
    
            public CIOBase(string name, int type)
            {
                _ioName = name;
                _ioType = (IOType)type;
            }
    
            public string GetName() => _ioName;
            public IOType GetIOType() => _ioType;
            public int GetTypeAsInt() => (int)_ioType;
    
            // Must Override
            public virtual void AddData(string strVal) { }
            public virtual int Size() => 0;
            public virtual bool IsEmpty() => true;
            public virtual void CopyVector(object vecSrc) { }
            public virtual void ClearData() { }
            public virtual void PrintData() { }
        }
    
        // DIO 클래스 (List<int>)
        public class CDIO : CIOBase
        {
            private List<int> _vecData = new List<int>();
    
            public CDIO(string name) : base(name, IOType.DIO) { }
    
            public CDIO(string name, int type) : base(name, type) { }
    
            ~CDIO() { ClearData(); }

            public override void AddData(string strVal)
            {
                if (int.TryParse(strVal, out int val))
                {
                    _vecData.Add(val);
                }
                else
                {
                    Console.WriteLine($"Warning: Could not parse '{strVal}' as int for DIO '{GetName()}'.");
                }
            }
    
            public List<int> GetDataVector() => _vecData;
            public override int Size() => GetDataVector().Count;
            public override bool IsEmpty() => GetDataVector().Count == 0;

            public override void CopyVector(object vecSrc)
            {
                if (vecSrc == null) return;
    
                if (vecSrc is List<int> vecTemp)
                {
                    _vecData = new List<int>(vecTemp);
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid source type for CopyVector in CDIO '{GetName()}'. Expected List<int>.");
                }
            }
    
    
            public override void ClearData()
            {
                _vecData.Clear();
            }
    
            public override void PrintData()
            {
                Console.Write(GetName());
                foreach (var v in _vecData)
                {
                    Console.Write($" {v}");
                }
                Console.WriteLine();
            }
        }
    
        // AIO 클래스 (List<double>)
        public class CAIO : CIOBase
        {
            private List<double> _vecData = new List<double>();
    
            public CAIO(string name) : base(name, IOType.AIO) { }
            public CAIO(string name, int type) : base(name, type) { }
    
    
            ~CAIO() { ClearData(); }
    
            public override void AddData(string strVal)
            {
                if (double.TryParse(strVal, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    _vecData.Add(val);
                }
                else
                {
                    Console.WriteLine($"Warning: Could not parse '{strVal}' as double for AIO '{GetName()}'.");
                }
            }
    
            public List<double> GetDataVector() => _vecData;
    
            public override int Size() => GetDataVector().Count;
            public override bool IsEmpty() => GetDataVector().Count == 0;
    
    
            public override void CopyVector(object vecSrc)
            {
                if (vecSrc == null) return;
    
                if (vecSrc is List<double> vecTemp)
                {
                    _vecData = new List<double>(vecTemp);
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid source type for CopyVector in CAIO '{GetName()}'. Expected List<double>.");
                }
            }
    
    
            public override void ClearData()
            {
                _vecData.Clear();
            }
    
            public override void PrintData()
            {
                Console.Write(GetName());
                foreach (var v in _vecData)
                {
                    Console.Write($" {v:F6}");
                }
                Console.WriteLine();
            }
        }
    
        // SIO 클래스 (List<string>)
        public class CSIO : CIOBase
        {
            private List<string> _vecData = new List<string>();
    
            public CSIO(string name) : base(name, IOType.SIO) { }
            public CSIO(string name, int type) : base(name, type) { }
    
            ~CSIO() { ClearData(); }
    
            public override void AddData(string strVal)
            {
                _vecData.Add(strVal);
            }
    
            public List<string> GetDataVector() => _vecData;
    
            public override int Size() => GetDataVector().Count;
            public override bool IsEmpty() => GetDataVector().Count == 0;
    
    
            public override void CopyVector(object vecSrc)
            {
                if (vecSrc == null) return;
    
                if (vecSrc is List<string> vecTemp)
                {
                    _vecData = new List<string>(vecTemp);
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid source type for CopyVector in CSIO '{GetName()}'. Expected List<string>.");
                }
            }
    
    
            public override void ClearData()
            {
                _vecData.Clear();
            }
    
            public override void PrintData()
            {
                Console.Write(GetName());
                foreach (var v in _vecData)
                {
                    Console.Write($" {v}");
                }
                Console.WriteLine();
            }
        }
    }
}