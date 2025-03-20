/* using PureHDF;
using PureHDF.Selections;

namespace XXXBlazor.Client.Models
{
    /// <summary>
    /// HDF5 파일 내의 객체 타입을 나타냅니다.
    /// </summary>
    public enum Hdf5NodeType
    {
        Group,
        Dataset,
        Attribute,
        Unknown
    }

    /// <summary>
    /// HDF5 파일의 그룹, 데이터셋 등을 표현하는 기본 클래스입니다.
    /// </summary>
    public class Hdf5TreeNode
    {
        /// <summary>
        /// 노드 이름
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// 전체 경로
        /// </summary>
        public string Path { get; private set; } = string.Empty;

        /// <summary>
        /// 노드 타입 (그룹, 데이터셋 등)
        /// </summary>
        public Hdf5NodeType NodeType { get; private set; }

        /// <summary>
        /// 자식 노드 목록 (그룹인 경우)
        /// </summary>
        public List<Hdf5TreeNode> Children { get; private set; }

        /// <summary>
        /// 속성 목록
        /// </summary>
        public Dictionary<string, object> Attributes { get; private set; }

        /// <summary>
        /// 데이터셋인 경우 차원 정보
        /// </summary>
        public ulong[]? Dimensions { get; private set; }

        /// <summary>
        /// 데이터셋의 데이터 타입
        /// </summary>
        public Type? DataType { get; private set; }

        /// <summary>
        /// 데이터셋 값 (데이터셋인 경우에만 유효)
        /// </summary>
        public object? Data { get; private set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Hdf5TreeNode()
        {
            Children = new List<Hdf5TreeNode>();
            Attributes = new Dictionary<string, object>();
        }

        /// <summary>
        /// HDF5 파일을 로드하여 루트 Hdf5TreeNode를 생성합니다.
        /// </summary>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <returns>루트 노드</returns>
        public static Hdf5TreeNode LoadFromFile(string filePath)
        {
            var rootNode = new Hdf5TreeNode
            {
                Name = "/",
                Path = "/",
                NodeType = Hdf5NodeType.Group,
                Children = new List<Hdf5TreeNode>()
            };

            using (var h5File = H5File.OpenRead(filePath))
            {
                // 루트 그룹에서 시작하여 재귀적으로 모든 항목을 탐색
                var rootGroup = h5File.Group("/");
                ReadGroup(rootGroup, rootNode, "/");
            }

            return rootNode;
        }

        /// <summary>
        /// 메모리 스트림으로부터 HDF5 파일을 로드하여 루트 Hdf5TreeNode를 생성합니다.
        /// </summary>
        /// <param name="stream">HDF5 데이터가 포함된 메모리 스트림</param>
        /// <returns>루트 노드</returns>
        public static Hdf5TreeNode LoadFromStream(MemoryStream stream)
        {
            var rootNode = new Hdf5TreeNode
            {
                Name = "/",
                Path = "/",
                NodeType = Hdf5NodeType.Group,
                Children = new List<Hdf5TreeNode>()
            };

            using (var h5File = H5File.Open(stream))
            {
                // 루트 그룹에서 시작하여 재귀적으로 모든 항목을 탐색
                var rootGroup = h5File.Group("/");
                ReadGroup(rootGroup, rootNode, "/");
            }

            return rootNode;
        }

        /// <summary>
        /// HDF5 그룹을 재귀적으로 읽는 메서드
        /// </summary>
        private static void ReadGroup(IH5Group h5Group, Hdf5TreeNode parentNode, string currentPath)
        {
            // 그룹 속성 읽기
            ReadAttributes(h5Group, parentNode);

            // 그룹 내 모든 자식 항목 탐색
            foreach (var childObj in h5Group.Children())
            {
                string childName = childObj.Name;
                string childPath = currentPath.EndsWith("/") ? $"{currentPath}{childName}" : $"{currentPath}/{childName}";

                if (childObj is IH5Group childGroup)
                {
                    var groupNode = new Hdf5TreeNode
                    {
                        Name = childName,
                        Path = childPath,
                        NodeType = Hdf5NodeType.Group,
                        Children = new List<Hdf5TreeNode>()
                    };

                    parentNode.Children.Add(groupNode);
                    ReadGroup(childGroup, groupNode, childPath);
                }
                else if (childObj is IH5Dataset childDataset)
                {
                    ReadDataset(childDataset, parentNode, childPath);
                }
            }
        }

        /// <summary>
        /// 데이터셋을 읽는 메서드
        /// </summary>
        private static void ReadDataset(IH5Dataset dataset, Hdf5TreeNode parentNode, string datasetPath)
        {
            var datasetName = dataset.Name;
            var datasetNode = new Hdf5TreeNode
            {
                Name = datasetName,
                Path = datasetPath,
                NodeType = Hdf5NodeType.Dataset,
                Dimensions = dataset.Space.Dimensions
            };

            // 데이터 타입 설정
            datasetNode.DataType = GetDataType(dataset);

            // 데이터셋 속성 읽기
            ReadAttributes(dataset, datasetNode);

            // 데이터 읽기
            try
            {
                datasetNode.Data = ReadDatasetValue(dataset);
            }
            catch (Exception ex)
            {
                datasetNode.Data = $"Error reading data: {ex.Message}";
            }

            parentNode.Children.Add(datasetNode);
        }

        /// <summary>
        /// 속성을 읽는 메서드
        /// </summary>
        private static void ReadAttributes(IH5Object obj, Hdf5TreeNode node)
        {
            try
            {
                // IH5Object.Attributes() 메서드를 사용하여 모든 속성 가져오기
                foreach (var attribute in obj.Attributes())
                {
                    try
                    {
                        // 속성 데이터 타입에 따라 적절히
                        if (attribute.Type.Size > 0)
                        {
                            var value = attribute.Read<object>();
                            node.Attributes[attribute.Name] = value;
                        }
                    }
                    catch
                    {
                        // 속성 읽기에 실패하면 오류 메시지 저장
                        node.Attributes[attribute.Name] = "Error reading attribute";
                    }
                }
            }
            catch (Exception ex)
            {
                // 속성 목록 읽기 자체가 실패한 경우
                node.Attributes["_error"] = $"Error reading attributes: {ex.Message}";
            }
        }

        /// <summary>
        /// 데이터셋 타입을 결정하는 메서드
        /// </summary>
        private static Type GetDataType(IH5Dataset dataset)
        {
            // PureHDF의 타입 정보를 활용하여 .NET 타입으로 변환
            var dataType = dataset.Type;

            if (dataType.Class == H5DataTypeClass.FixedPoint)
            {
                if (!dataset.Type.FixedPoint.IsSigned)
                {
                    switch (dataType.Size)
                    {
                        case 1: return typeof(byte);
                        case 2: return typeof(ushort);
                        case 4: return typeof(uint);
                        case 8: return typeof(ulong);
                        default: return typeof(object);
                    }
                }
                else
                {
                    switch (dataType.Size)
                    {
                        case 1: return typeof(sbyte);
                        case 2: return typeof(short);
                        case 4: return typeof(int);
                        case 8: return typeof(long);
                        default: return typeof(object);
                    }
                }
            }
            else if (dataType.Class == H5DataTypeClass.FloatingPoint)
            {
                switch (dataType.Size)
                {
                    case 4: return typeof(float);
                    case 8: return typeof(double);
                    default: return typeof(object);
                }
            }
            else if (dataType.Class == H5DataTypeClass.String)
            {
                return typeof(string);
            }
            else if (dataType.Class == H5DataTypeClass.BitField)
            {
                return typeof(bool);
            }
            else
            {
                return typeof(object);
            }
        }

        /// <summary>
        /// 데이터셋 값을 읽는 메서드
        /// </summary>
        private static object ReadDatasetValue(IH5Dataset dataset)
        {
            // 타입과 크기에 따라 적절한 방법으로 데이터를 읽음
            bool isScalar = dataset.Space.Rank == 0 ||
                (dataset.Space.Dimensions.Length == 1 && dataset.Space.Dimensions[0] == 1);

            if (isScalar)
            {
                // 스칼라 값
                if (dataset.Type.Class == H5DataTypeClass.FixedPoint)
                {
                    if (!dataset.Type.FixedPoint.IsSigned)
                    {
                        switch (dataset.Type.Size)
                        {
                            case 1: return dataset.Read<byte>();
                            case 2: return dataset.Read<ushort>();
                            case 4: return dataset.Read<uint>();
                            case 8: return dataset.Read<ulong>();
                        }
                    }
                    else
                    {
                        switch (dataset.Type.Size)
                        {
                            case 1: return dataset.Read<sbyte>();
                            case 2: return dataset.Read<short>();
                            case 4: return dataset.Read<int>();
                            case 8: return dataset.Read<long>();
                        }
                    }
                }
                else if (dataset.Type.Class == H5DataTypeClass.FloatingPoint)
                {
                    switch (dataset.Type.Size)
                    {
                        case 4: return dataset.Read<float>();
                        case 8: return dataset.Read<double>();
                    }
                }
                else if (dataset.Type.Class == H5DataTypeClass.String)
                {
                    return dataset.Read<string>();
                }
                else if (dataset.Type.Class == H5DataTypeClass.BitField)
                {
                    return dataset.Read<bool>();
                }
            }
            else
            {
                // 배열 또는 다차원 데이터
                try
                {
                    // 작은 데이터셋은 전체를 한 번에 읽기
                    ulong totalSize = dataset.Space.Dimensions.Aggregate((ulong)1, (x, y) => x * y);
                    if (totalSize < 1000000) // 백만 요소 미만
                    {
                        if (dataset.Type.Class == H5DataTypeClass.FixedPoint)
                        {
                            if (!dataset.Type.FixedPoint.IsSigned)
                            {
                                switch (dataset.Type.Size)
                                {
                                    case 1: return dataset.Read<byte[]>();
                                    case 2: return dataset.Read<ushort[]>();
                                    case 4: return dataset.Read<uint[]>();
                                    case 8: return dataset.Read<ulong[]>();
                                }
                            }
                            else
                            {
                                switch (dataset.Type.Size)
                                {
                                    case 1: return dataset.Read<sbyte[]>();
                                    case 2: return dataset.Read<short[]>();
                                    case 4: return dataset.Read<int[]>();
                                    case 8: return dataset.Read<long[]>();
                                }
                            }
                        }
                        else if (dataset.Type.Class == H5DataTypeClass.FloatingPoint)
                        {
                            switch (dataset.Type.Size)
                            {
                                case 4: return dataset.Read<float[]>();
                                case 8: return dataset.Read<double[]>();
                            }
                        }
                        else if (dataset.Type.Class == H5DataTypeClass.String)
                        {
                            return dataset.Read<string[]>();
                        }
                        else if (dataset.Type.Class == H5DataTypeClass.BitField)
                        {
                            return dataset.Read<bool[]>();
                        }
                    }
                    else
                    {
                        // 큰 데이터셋은 메타데이터만 반환하고 데이터는 별도 메서드로 가져오게 함
                        return $"Large dataset: {totalSize} elements";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error reading array data: {ex.Message}";
                }
            }

            return "Unsupported data type";
        }

        /// <summary>
        /// 데이터셋에서 특정 부분만 읽는 메서드 (대용량 데이터셋을 위한 메서드)
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>읽은 데이터 배열</returns>
        public static T[] ReadDatasetRegion<T>(string filePath, string datasetPath, ulong[] start, ulong[] count)
            where T : unmanaged
        {
            using (var h5File = H5File.OpenRead(filePath))
            {
                // 경로가 /로 시작하면 제거 (PureHDF는 루트 경로를 빈 문자열로 처리)
                if (datasetPath.StartsWith("/"))
                    datasetPath = datasetPath.Substring(1);

                var dataset = h5File.Dataset(datasetPath);

                // 하이퍼슬랩 선택 생성
                var hyperslab = new HyperslabSelection(
                    rank: start.Length,
                    starts: start,
                    blocks: count);

                return dataset.Read<T[]>(fileSelection: hyperslab);
            }
        }

        /// <summary>
        /// 데이터셋을 다차원 배열로 반환하는 메서드
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        public static Array ReadDatasetAsMultidimensional<T>(string filePath, string datasetPath)
            where T : unmanaged
        {
            using (var h5File = H5File.OpenRead(filePath))
            {
                // 경로가 /로 시작하면 제거 (PureHDF는 루트 경로를 빈 문자열로 처리)
                if (datasetPath.StartsWith("/"))
                    datasetPath = datasetPath.Substring(1);

                var dataset = h5File.Dataset(datasetPath);
                var dimensions = dataset.Space.Dimensions;

                // 1D 배열로 먼저 데이터 읽기
                T[] rawData = dataset.Read<T[]>();

                // 차원 정보에 따라 다차원 배열로 변환
                return ConvertToMultidimensionalArray<T>(rawData, dimensions);
            }
        }

        /// <summary>
        /// 데이터셋을 다차원 배열로 반환하는 메서드 (부분 선택)
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        public static Array ReadDatasetRegionAsMultidimensional<T>(string filePath, string datasetPath, ulong[] start, ulong[] count)
            where T : unmanaged
        {
            using (var h5File = H5File.OpenRead(filePath))
            {
                // 경로가 /로 시작하면 제거 (PureHDF는 루트 경로를 빈 문자열로 처리)
                if (datasetPath.StartsWith("/"))
                    datasetPath = datasetPath.Substring(1);

                var dataset = h5File.Dataset(datasetPath);

                // 하이퍼슬랩 선택 생성
                var hyperslab = new HyperslabSelection(
                    rank: start.Length,
                    starts: start,
                    blocks: count);

                // 1D 배열로 먼저 데이터 읽기
                T[] rawData = dataset.Read<T[]>(fileSelection: hyperslab);

                // 변환할 때는 count를 차원으로 사용
                return ConvertToMultidimensionalArray<T>(rawData, count);
            }
        }

        /// <summary>
        /// 노드 정보를 문자열로 출력하는 메서드
        /// </summary>
        public override string ToString()
        {
            return $"{NodeType}: {Path}";
        }

        /// <summary>
        /// 데이터셋 노드의 데이터를 다차원 배열로 변환하여 반환
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <returns>다차원 배열</returns>
        public Array? GetDataAsMultidimensional<T>() where T : unmanaged
        {
            if (NodeType != Hdf5NodeType.Dataset)
                throw new InvalidOperationException("이 메서드는 데이터셋 노드에서만 사용할 수 있습니다.");

            if (Data is T[] typedArray && Dimensions != null)
            {
                return ConvertToMultidimensionalArray(typedArray, Dimensions);
            }

            throw new InvalidOperationException($"데이터를 {typeof(T).Name} 타입으로 변환할 수 없습니다.");
        }

        /// <summary>
        /// 전체 트리 구조를 문자열로 출력하는 메서드
        /// </summary>
        public string ToTreeString(int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            var result = $"{indentStr}{ToString()}";

            if (NodeType == Hdf5NodeType.Dataset && Dimensions != null)
            {
                result += $" [{string.Join(", ", Dimensions)}]";
                if (DataType != null)
                {
                    result += $" ({DataType.Name})";
                }
            }

            result += Environment.NewLine;

            if (Attributes.Count > 0)
            {
                result += $"{indentStr}  Attributes: {string.Join(", ", Attributes.Keys)}{Environment.NewLine}";
            }

            foreach (var child in Children)
            {
                result += child.ToTreeString(indent + 1);
            }

            return result;
        }

        /// <summary>
        /// 1차원 배열을 다차원 배열로 변환하는 유틸리티 메서드
        /// </summary>
        /// <typeparam name="T">배열 요소 타입</typeparam>
        /// <param name="source">소스 1차원 배열</param>
        /// <param name="dimensions">차원 정보</param>
        /// <returns>다차원 배열</returns>
        private static Array ConvertToMultidimensionalArray<T>(T[] source, ulong[] dimensions)
        {
            // 차원 정보 변환 (.NET은 int 기반 인덱스 사용)
            int rank = dimensions.Length;
            int[] dims = dimensions.Select(d => (int)d).ToArray();

            // 지원하는 차원에 따라 변환
            switch (rank)
            {
                case 1:
                    // 이미 1차원 배열이므로 그대로 반환
                    return source;

                case 2:
                    // 2차원 배열 생성
                    T[,] array2D = new T[dims[0], dims[1]];
                    for (int i = 0; i < dims[0]; i++)
                    {
                        for (int j = 0; j < dims[1]; j++)
                        {
                            int index = i * dims[1] + j;
                            array2D[i, j] = source[index];
                        }
                    }
                    return array2D;

                case 3:
                    // 3차원 배열 생성
                    T[,,] array3D = new T[dims[0], dims[1], dims[2]];
                    for (int i = 0; i < dims[0]; i++)
                    {
                        for (int j = 0; j < dims[1]; j++)
                        {
                            for (int k = 0; k < dims[2]; k++)
                            {
                                int index = (i * dims[1] * dims[2]) + (j * dims[2]) + k;
                                array3D[i, j, k] = source[index];
                            }
                        }
                    }
                    return array3D;

                case 4:
                    // 4차원 배열 생성
                    T[,,,] array4D = new T[dims[0], dims[1], dims[2], dims[3]];
                    for (int i = 0; i < dims[0]; i++)
                    {
                        for (int j = 0; j < dims[1]; j++)
                        {
                            for (int k = 0; k < dims[2]; k++)
                            {
                                for (int l = 0; l < dims[3]; l++)
                                {
                                    int index = (i * dims[1] * dims[2] * dims[3]) +
                                              (j * dims[2] * dims[3]) +
                                              (k * dims[3]) + l;
                                    array4D[i, j, k, l] = source[index];
                                }
                            }
                        }
                    }
                    return array4D;

                default:
                    // 5차원 이상은 구현 불가, 원본 배열 그대로 반환
                    return source;
            }
        }
    }
}
*/

/*
using PureHDF;

namespace XXXBlazor.Client.Models
{
    /// <summary>
    /// Enum for HDF5 node type
    /// </summary>
    public enum Hdf5NodeType
    {
        NULL_NODE = -1,
        Group,
        Dataset,
        Reserved01,
        Reserved02,
        Reserved03,
        Reserved04,
        Reserved05,
        Other,
    }

    public class Hdf5FileModel
    {
        /// <summary>
        /// MemoryStream for File
        /// </summary>
        //public required MemoryStream Memorystream { get; set; }

        /// <summary>
        /// Name of the HDF5 file
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Root group of the HDF5 file (the top-level group)
        /// </summary>
        public required Hdf5Group RootGroup { get; set; }

        /// <summary>
        /// Test Method for DEBUG : Prtint All Nodes in HDF5 File
        /// </summary>
        /// <param name="node">Hdf5Node</param>
        /// <param name="indent">Indentation</param>
        /// <returns>String</returns>
        public string PrintAllNodes(Hdf5Node node, string indent = "", int depth = 0)
        {
            string result = indent;

            for(int i = 0; i < depth; i++)
            {
                result += "  ";
            }
            result += node.Name + " (" + node.NodeType + ")" + ((node is Hdf5Dataset dataset1) ? (H5DataTypeClass)dataset1.DataType : "") + "\n";

            if (node is Hdf5Group group)
            {
                foreach (var child in group.Children)
                {
                    result += PrintAllNodes(child, indent + "  ", depth + 1);
                }
            }
            else if (node is Hdf5Dataset dataset)
            {
                result += "  " + dataset.GetPreview(depth + 1, false) + "\n";
            }

            return result;
        }
    }

    public abstract class Hdf5Node
    {
        /// <summary>
        /// Name of the node (example: "Group1", "Dataset2" etc.)
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Full path of the node (including all parent names)
        /// </summary>
        public required string Path { get; set; }

        /// <summary>
        /// Type of the node
        /// </summary>
        public Hdf5NodeType NodeType { get; set; }

        /// <summary>
        /// Reference to parent node
        /// </summary>
        public Hdf5Node? Parent { get; set; }

        /// <summary>
        /// Attribute list of the node
        /// </summary>
        public List<Hdf5Attribute> Attributes { get; set; } = new List<Hdf5Attribute>();

        /// <summary>
        /// Add an attribute to the node
        /// </summary>
        public void AddAttribute(string name, object value)
        {
            Attributes.Add(new Hdf5Attribute { Name = name, Value = value });
        }
    }

    // Group Node : Contains other groups or datasets recursively
    public class Hdf5Group : Hdf5Node
    {
        public Hdf5Group()
        {
            NodeType = Hdf5NodeType.Group;
        }

        /// <summary>
        /// Child nodes of the group (can be groups or datasets, etc.)
        /// </summary>
        public List<Hdf5Node> Children { get; set; } = new List<Hdf5Node>();

        /// <summary>
        /// Add a child node to the group (Connects the parent-child relationship)
        /// </summary>
        /// <param name="node">추가할 자식 노드</param>
        public void AddChild(Hdf5Node node)
        {
            node.Parent = this;
            Children.Add(node);
        }
    }

    // DataSet Node : Contains Data. End-point of the recursive path
    public class Hdf5Dataset : Hdf5Node
    {
        public Hdf5Dataset()
        {
            NodeType = Hdf5NodeType.Dataset;
        }

        /// <summary>
        /// Type of Data (ex: "int", "double", "string" , etc.)
        /// </summary>
        public required byte DataType { get; set; }

        /// <summary>
        /// Dimensions of Dataset
        /// </summary>
        public required ulong[] Dimensions { get; set; }

        /// <summary>
        /// Check data already loaded or not
        /// </summary>
        public bool IsDataLoaded { get; private set; }

        /// <summary>
        /// 데이터셋 ID (데이터 로드 시 사용)
        /// </summary>
        public long DatasetId { get; set; }

        /// <summary>
        /// Real Data. Can type-change if need
        /// </summary>
        private object? _data;
        public object? Data
        {
            get => _data;
            set
            {
                _data = value;
                IsDataLoaded = value != null;
            }
        }

        /// <summary>
        /// 데이터를 지연 로딩하는 메서드
        /// </summary>
        public void LoadData(Func<Hdf5Dataset, object> dataLoader)
        {
            if (!IsDataLoaded)
            {
                Data = dataLoader(this);
            }
        }

        /// <summary>
        /// 데이터셋의 미리보기 (요약) 제공
        /// </summary>
        /// <returns>데이터 요약 정보</returns>
        public string GetPreview(int depth, bool isPreview = true)
        {
            string result = "";

            for(int i = 0; i < depth; i++)
            {
                result += "  ";
            }

            if (IsDataLoaded && Data != null)
            {
                // 데이터가 이미 로드된 경우, 간략한 미리보기 반환
                return result + "데이터 로드됨: " + GetDataSummary(isPreview);
            }
            else
            {
                // 데이터가 로드되지 않은 경우, 크기 정보만 반환
                return result + $"Dataset [Name : {Name}] [Path: {Path}] [Size: {string.Join(" x ", Dimensions)}] [Type : {DataType}] [Dimension : {Dimensions.Length}]";
            }
        }

        /// <summary>
        /// 데이터 요약 정보 생성 (타입에 따라 다르게 처리)
        /// </summary>
        private string GetDataSummary(bool isPreview)
        {
            if (Data == null) return "null";

            // 배열 타입인 경우
            if (Data.GetType().IsArray)
            {
                Array array = (Array)Data;
                if (array.Length == 0) return "빈 배열";

                // 1차원 배열인 경우
                if (array.Rank == 1)
                {
                    int previewCount = isPreview ? Math.Min(5, array.Length) : array.Length ;
                    var preview = new object[previewCount];
                    Array.Copy(array, preview, previewCount);

                    string result = $"[{string.Join(", ", preview)}";
                    if ( isPreview && array.Length > previewCount)
                        result += ", ...";
                    result += "]";
                    return result;
                }

                // 다차원 배열인 경우
                return $"{array.Rank}차원 배열, 총 요소 수: {array.Length}";
            }

            // 기타 타입
            return Data.ToString() ?? "알 수 없는 데이터";
        }

        /// <summary>
        /// Get Data Array
        /// </summary>
        /// <returns>Array</returns>
        public Array GetDataArray()
        {
            if (Data == null) return null;

            if (Data.GetType().IsArray)
            {
                return (Array)Data;
            }

            return null;
        }
    }

    // Attribute
    public class Hdf5Attribute
    {
        /// <summary>
        /// Name of Attribute
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Value of Attribute
        /// </summary>
        public required object Value { get; set; }
    }
}
 */
