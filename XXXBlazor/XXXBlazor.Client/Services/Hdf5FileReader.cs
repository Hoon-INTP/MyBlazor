using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PureHDF;
using PureHDF.Selections;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Services
{
    /// <summary>
    /// HDF5 파일 읽기 구현 클래스
    /// </summary>
    public class Hdf5FileReader : IHdf5FileReader
    {
        /// <summary>
        /// 파일 경로에서 HDF5 파일을 로드하여 루트 노드를 반환합니다.
        /// </summary>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <returns>HDF5 파일의 루트 노드</returns>
        public Hdf5TreeNode LoadFromFile(string filePath)
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
        /// 메모리 스트림에서 HDF5 파일을 로드하여 루트 노드를 반환합니다.
        /// </summary>
        /// <param name="stream">HDF5 데이터가 포함된 메모리 스트림</param>
        /// <returns>HDF5 파일의 루트 노드</returns>
        public Hdf5TreeNode LoadFromStream(MemoryStream stream)
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
        /// 데이터셋에서 특정 부분만 읽습니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>읽은 데이터 배열</returns>
        public T[] ReadDatasetRegion<T>(string filePath, string datasetPath, ulong[] start, ulong[] count) where T : unmanaged
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
        /// 데이터셋을 다차원 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        public Array ReadDatasetAsMultidimensional<T>(string filePath, string datasetPath) where T : unmanaged
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
                return Hdf5ArrayHelper.ConvertToMultidimensionalArray(rawData, dimensions);
            }
        }

        /// <summary>
        /// 데이터셋의 부분 영역을 다차원 배열로 반환합니다.
        /// </summary>
        /// <typeparam name="T">데이터 타입</typeparam>
        /// <param name="filePath">HDF5 파일 경로</param>
        /// <param name="datasetPath">데이터셋 경로</param>
        /// <param name="start">시작 인덱스 배열</param>
        /// <param name="count">요소 개수 배열</param>
        /// <returns>차원 정보에 맞게 변환된 다차원 배열</returns>
        public Array ReadDatasetRegionAsMultidimensional<T>(string filePath, string datasetPath, ulong[] start, ulong[] count) where T : unmanaged
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
                return Hdf5ArrayHelper.ConvertToMultidimensionalArray(rawData, count);
            }
        }

        /// <summary>
        /// HDF5 그룹을 재귀적으로 읽는 메서드
        /// </summary>
        private void ReadGroup(IH5Group h5Group, Hdf5TreeNode parentNode, string currentPath)
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
        private void ReadDataset(IH5Dataset dataset, Hdf5TreeNode parentNode, string datasetPath)
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
        private void ReadAttributes(IH5Object obj, Hdf5TreeNode node)
        {
            try
            {
                // IH5Object.Attributes() 메서드를 사용하여 모든 속성 가져오기
                foreach (var attribute in obj.Attributes())
                {
                    try
                    {
                        // 속성 데이터 타입에 따라 적절히 변환
                        if (attribute.Type.Size > 0)
                        {
                            var value = ReadAttributeWithProperType(attribute);
                            node.Attributes[attribute.Name] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 속성 읽기에 실패하면 오류 메시지 저장
                        node.Attributes[attribute.Name] = $"Error reading attribute: {ex.Message}";
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
        /// 적절한 타입으로 속성 값을 읽는 메서드
        /// </summary>
        private object ReadAttributeWithProperType(IH5Attribute attribute)
        {
            // 속성 데이터 타입에 따라 적절한 타입으로 변환
            if (attribute.Type.Class == H5DataTypeClass.FixedPoint)
            {
                if (!attribute.Type.FixedPoint.IsSigned)
                {
                    switch (attribute.Type.Size)
                    {
                        case 1: return attribute.Read<byte>();
                        case 2: return attribute.Read<ushort>();
                        case 4: return attribute.Read<uint>();
                        case 8: return attribute.Read<ulong>();
                        default: return $"Unsupported unsigned integer size: {attribute.Type.Size}";
                    }
                }
                else
                {
                    switch (attribute.Type.Size)
                    {
                        case 1: return attribute.Read<sbyte>();
                        case 2: return attribute.Read<short>();
                        case 4: return attribute.Read<int>();
                        case 8: return attribute.Read<long>();
                        default: return $"Unsupported signed integer size: {attribute.Type.Size}";
                    }
                }
            }
            else if (attribute.Type.Class == H5DataTypeClass.FloatingPoint)
            {
                switch (attribute.Type.Size)
                {
                    case 4: return attribute.Read<float>();
                    case 8: return attribute.Read<double>();
                    default: return $"Unsupported float size: {attribute.Type.Size}";
                }
            }
            else if (attribute.Type.Class == H5DataTypeClass.String ||
                     attribute.Type.Class == H5DataTypeClass.VariableLength)
            {
                try
                {
                    return attribute.Read<string>();
                }
                catch
                {
                    try
                    {
                        // 문자열 배열일 수도 있음
                        return string.Join(", ", attribute.Read<string[]>());
                    }
                    catch
                    {
                        return "Error reading string attribute";
                    }
                }
            }
            else if (attribute.Type.Class == H5DataTypeClass.BitField)
            {
                return attribute.Read<bool>();
            }
            else if (attribute.Type.Class == H5DataTypeClass.Compound)
            {
                return "Compound data type";
            }
            else if (attribute.Type.Class == H5DataTypeClass.Array)
            {
                // 배열 타입 처리
                try
                {
                    // 기본적으로 object[]로 읽고 문자열로 변환
                    var arrayData = attribute.Read<object[]>();
                    return $"Array[{arrayData.Length}]: {string.Join(", ", arrayData.Take(5))}...";
                }
                catch
                {
                    return "Array data (not displayable)";
                }
            }

            // 기타 타입은 일반적인 방식으로 처리
            try
            {
                return attribute.Read<object>();
            }
            catch
            {
                return $"Data of type {attribute.Type.Class} (not displayable)";
            }
        }

        /// <summary>
        /// 데이터셋 타입을 결정하는 메서드
        /// </summary>
        private Type GetDataType(IH5Dataset dataset)
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
        private object ReadDatasetValue(IH5Dataset dataset)
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
    }
}
