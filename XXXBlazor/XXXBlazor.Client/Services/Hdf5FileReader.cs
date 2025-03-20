using PureHDF;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Services
{
    public class Hdf5FileReader : IHdf5FileReader
    {
        /// <summary>
        /// Convert MemoryStream of HDF5 File to Hdf5FileModel
        /// </summary>
        /// <param name="stream">MemoryStream of HDF5 File</param>
        /// <param name="fileName">HDF5 File Name</param>
        /// <returns>Hdf5FileModel</returns>
        public Hdf5FileModel ReadHdf5FromStream(MemoryStream stream, string fileName)
        {
            using var root = H5File.Open(stream);

            var rootGroup = ReadGroup(root.Group(root.Name), "/");

            rootGroup.Name = fileName;

            var fileModel = new Hdf5FileModel
            {
                FileName = fileName,
                RootGroup = rootGroup
            };

            return fileModel;
        }

        /// <summary>
        /// Convert MemoryStream of HDF5 File to Hdf5FileModel
        /// </summary>
        /// <param name="path">Path of temporary file</param>
        /// <param name="fileName">HDF5 file name</param>
        /// <returns>Hdf5FileModel</returns>
        public Hdf5FileModel ReadHdf5FromTempFile(string path, string fileName)
        {
            using var root = H5File.OpenRead(path);

            var rootGroup = ReadGroup(root.Group(root.Name), "/");

            rootGroup.Name = fileName;

            var fileModel = new Hdf5FileModel
            {
                FileName = fileName,
                RootGroup = rootGroup
            };

            return fileModel;
        }

        /// <summary>
        /// Convert Group information to Hdf5Group
        /// </summary>
        /// <param name="iH5Group">PureHDF Group interface</param>
        /// <param name="groupPath">Group Path</param>
        /// <returns>Hdf5Group</returns>
        private Hdf5Group ReadGroup(IH5Group iH5Group, string groupPath)
        {

            string groupName = groupPath == "/" ? "/" : Path.GetFileName(groupPath);
            var group = new Hdf5Group
            {
                Name = groupName,
                Path = groupPath
            };

            IEnumerable<IH5Attribute> attributes = iH5Group.Attributes();
            if (!attributes.Any())
            {
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    string attrName = attribute.Name;
                    H5DataTypeClass attrType = attribute.Type.Class;

                    switch(attrType)
                    {
                        case H5DataTypeClass.String: // fixed-Length String
                        {
                            string strValue = attribute.Read<string>();
                            group.AddAttribute(attrName, strValue);
                            break;
                        }

                        case H5DataTypeClass.VariableLength: // variable-Length String
                        {
                            string varValue = attribute.Read<string>();
                            group.AddAttribute(attrName, varValue);
                            break;
                        }

                        case H5DataTypeClass.FixedPoint:
                        {
                            if (attribute.Type.Size == 1)
                            {
                                byte byteValue = attribute.Read<byte>();
                                group.AddAttribute(attrName, byteValue);
                            }
                            else if (attribute.Type.Size == 2)
                            {
                                short shortValue = attribute.Read<short>();
                                group.AddAttribute(attrName, shortValue);
                            }
                            else if (attribute.Type.Size == 8)
                            {
                                long longValue = attribute.Read<long>();
                                group.AddAttribute(attrName, longValue);
                            }
                            else
                            {
                                int intValue = attribute.Read<int>();
                                group.AddAttribute(attrName, intValue);
                            }

                            break;
                        }

                        case H5DataTypeClass.FloatingPoint:
                        {
                            if (attribute.Type.Size == 4)
                            {
                                float floatValue = attribute.Read<float>();
                                group.AddAttribute(attrName, floatValue);
                            }
                            else
                            {
                                double doubleValue = attribute.Read<double>();
                                group.AddAttribute(attrName, doubleValue);
                            }

                            break;
                        }

                        default:
                        {
                            Console.WriteLine($"Not Supported Type: {groupName} - {attrType} for {attrName}");
                            break;
                        }
                    }
                }
            }

            IEnumerable<IH5Object> childrens = iH5Group.Children();

            childrens.ToList().ForEach(child =>{
                string childName = child.Name;
                string memberPath = groupPath == "/" ? $"/{childName}" : $"{groupPath}/{childName}";

                if (child is IH5Group)
                {
                    Hdf5Group subgroup = ReadGroup((IH5Group)child, memberPath);
                    group.AddChild(subgroup);
                }
                else if (child is IH5Dataset)
                {
                    Hdf5Dataset dataset = ReadDataset((IH5Dataset)child, childName, memberPath);
                    group.AddChild(dataset);
                }
                else
                {
                    var otherNode = new Hdf5Group
                    {
                        Name = childName,
                        Path = memberPath,
                        NodeType = Hdf5NodeType.Other
                    };

                    group.AddChild(otherNode);
                }
            });

/*
            // Process all children objects in the group
            foreach (var childName in h5Group.ChildrenNames)
            {
                string memberPath = groupPath == "/" ? $"/{childName}" : $"{groupPath}/{childName}";

                // PureHDF has direct object type checking
                if (h5Group.GroupExists(childName))
                {
                    var childGroup = h5Group.Group(childName);
                    Hdf5Group subgroup = ReadGroup(childGroup, memberPath);
                    group.Children.Add(subgroup);
                }
                else if (h5Group.DatasetExists(childName))
                {
                    var childDataset = h5Group.Dataset(childName);
                    Hdf5Dataset dataset = ReadDataset(childDataset, childName, memberPath);
                    group.Children.Add(dataset);
                }
                else
                {
                    // Handle other types (like named datatypes, etc.)
                    var otherNode = new Hdf5Group
                    {
                        Name = childName,
                        Path = memberPath,
                        NodeType = Models.Hdf5NodeType.Other
                    };

                    // We don't have a direct object to read attributes from for "other" types
                    group.AddChild(otherNode);
                }
            }
*/
            return group;
        }

        /// <summary>
        /// Convert Dataset information to Hdf5Dataset
        /// </summary>
        /// <param name="iH5Dataset">PureHDF Dataset object</param>
        /// <param name="name">Dataset Name</param>
        /// <param name="path">Dataset Path</param>
        /// <returns>Hdf5Dataset</returns>
        private Hdf5Dataset ReadDataset(IH5Dataset iH5Dataset, string name, string path)
        {
            // Get data type information
            H5DataTypeClass type = iH5Dataset.Type.Class;
            byte dataType = (byte)type;

            // Get dimensions
            ulong[] dimensions = iH5Dataset.Space.Dimensions;

            var dataset = new Hdf5Dataset
            {
                Name = name,
                Path = path,
                DataType = dataType,
                Dimensions = dimensions,
            };

            IEnumerable<IH5Attribute> attributes = iH5Dataset.Attributes();
            if (!attributes.Any())
            {
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    string attrName = attribute.Name;
                    H5DataTypeClass attrType = attribute.Type.Class;

                    switch(attrType)
                    {
                        case H5DataTypeClass.String: // fixed-Length String
                        {
                            string strValue = attribute.Read<string>();
                            dataset.AddAttribute(attrName, strValue);
                            break;
                        }

                        case H5DataTypeClass.VariableLength: // variable-Length String
                        {
                            string varValue = attribute.Read<string>();
                            dataset.AddAttribute(attrName, varValue);
                            break;
                        }

                        case H5DataTypeClass.FixedPoint:
                        {
                            if (attribute.Type.Size == 1)
                            {
                                byte byteValue = attribute.Read<byte>();
                                dataset.AddAttribute(attrName, byteValue);
                            }
                            else if (attribute.Type.Size == 2)
                            {
                                short shortValue = attribute.Read<short>();
                                dataset.AddAttribute(attrName, shortValue);
                            }
                            else if (attribute.Type.Size == 8)
                            {
                                long longValue = attribute.Read<long>();
                                dataset.AddAttribute(attrName, longValue);
                            }
                            else
                            {
                                int intValue = attribute.Read<int>();
                                dataset.AddAttribute(attrName, intValue);
                            }

                            break;
                        }

                        case H5DataTypeClass.FloatingPoint:
                        {
                            if (attribute.Type.Size == 4)
                            {
                                float floatValue = attribute.Read<float>();
                                dataset.AddAttribute(attrName, floatValue);
                            }
                            else
                            {
                                double doubleValue = attribute.Read<double>();
                                dataset.AddAttribute(attrName, doubleValue);
                            }

                            break;
                        }

                        default:
                        {
                            Console.WriteLine($"Not Supported Type: {name} - {attrType} for {attrName}");
                            break;
                        }
                    }
                }
            }

            switch(type)
            {
                case H5DataTypeClass.FixedPoint:
                {
                    if (iH5Dataset.Type.Size == 1)
                    {
                        dataset.Data = ReadFixedPointData<byte>(iH5Dataset);
                    }
                    else if (iH5Dataset.Type.Size == 2)
                    {
                        dataset.Data = ReadFixedPointData<short>(iH5Dataset);
                    }
                    else if (iH5Dataset.Type.Size == 8)
                    {
                        dataset.Data = ReadFixedPointData<long>(iH5Dataset);
                    }
                    else
                    {
                        dataset.Data = ReadFixedPointData<int>(iH5Dataset);
                    }

                    break;
                }

                case H5DataTypeClass.FloatingPoint:
                {
                    if (iH5Dataset.Type.Size == 4)
                    {
                        dataset.Data = ReadFloatingPointData<float>(iH5Dataset);
                    }
                    else
                    {
                        dataset.Data = ReadFloatingPointData<double>(iH5Dataset);
                    }

                    break;
                }

                case H5DataTypeClass.String:
                {
                    dataset.Data = ReadStringData(iH5Dataset);
                    break;
                }

                case H5DataTypeClass.VariableLength:
                {
                    dataset.Data = ReadVariableLengthData(iH5Dataset);
                    break;
                }

                case H5DataTypeClass.Compound:
                {
                    dataset.Data = ReadCompoundData(iH5Dataset);
                    break;
                }

                default:
                {
                    Console.WriteLine($"Not Supported Type: {name} - {type}");
                    break;
                }
            }

            return dataset;
        }

        /// <summary>
        /// Read Fixed Point Data
        /// </summary>///
        private Array ReadFixedPointData<T>(IH5Dataset dataset) where T : struct
        {
            ulong[] dimensions = dataset.Space.Dimensions;

            if (dimensions.Length == 1)
            {
                T[] data = dataset.Read<T[]>();
                return data;
            }
            else if (dimensions.Length == 2)
            {
                T[,] data = dataset.Read<T[,]>();
                return data;
            }
            else if (dimensions.Length == 3)
            {
                T[,,] data = dataset.Read<T[,,]>();
                return data;
            }
            else
            {
                Console.WriteLine($"Not Supported Rank : {dimensions.Length}");
                return Array.Empty<T>();
            }
        }

        /// <summary>
        /// Read Floating Point Data
        /// </summary>
        private Array ReadFloatingPointData<T>(IH5Dataset dataset) where T : struct
        {
            ulong[] dimensions = dataset.Space.Dimensions;

            if (dimensions.Length == 1)
            {
                T[] data = dataset.Read<T[]>();
                return data;
            }
            else if (dimensions.Length == 2)
            {
                T[,] data = dataset.Read<T[,]>();
                return data;
            }
            else if (dimensions.Length == 3)
            {
                T[,,] data = dataset.Read<T[,,]>();
                return data;
            }
            else
            {
                Console.WriteLine($"Not Supported Rank : {dimensions.Length}");
                return Array.Empty<T>();
            }
        }

        /// <summary>
        /// Read String Length Data
        /// </summary>
        private Array ReadStringData(IH5Dataset dataset)
        {
            ulong[] dimensions = dataset.Space.Dimensions;

            if (dimensions.Length == 1)
            {
                string[] data = dataset.Read<string[]>();
                return data;
            }
            else if (dimensions.Length == 2)
            {
                try
                {
                    string[,] data = dataset.Read<string[,]>();
                    return data;
                }
                catch
                {
                    string[] flatData = dataset.Read<string[]>();
                    string[,] data = new string[(int)dimensions[0], (int)dimensions[1]];

                    for (int i = 0; i < (int)dimensions[0]; i++)
                    {
                        for (int j = 0; j < (int)dimensions[1]; j++)
                        {
                            data[i, j] = flatData[i * (int)dimensions[1] + j];
                        }
                    }

                    return data;
                }
            }
            else
            {
                throw new NotSupportedException($"문자열 데이터의 차원 수 {dimensions.Length}는 현재 지원되지 않습니다.");
            }
        }

        /// <summary>
        /// Read VariableLength Data
        /// </summary>
        private Array ReadVariableLengthData(IH5Dataset dataset)
        {
            // 가변 길이 데이터는 특별한 처리가 필요할 수 있음
            try
            {
                string[] data = dataset.Read<string[]>();
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"가변 길이 데이터 읽기 오류: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Read Compound Data
        /// </summary>
        private object ReadCompoundData(IH5Dataset dataset)
        {

            //try
            //{
            //    // PureHDF의 복합 데이터 처리 방식에 따라 구현
            //    // 예: 사용자 정의 구조체 또는 동적 객체 배열 반환
            //
            //    // 복합 필드 정보 가져오기
            //    H5CompoundType compoundType = dataset.Type as H5CompoundType;
            //    if (compoundType != null)
            //    {
            //        // 필드 정보 출력 (실제 구현에서는 이 정보를 사용하여 데이터 구조화)
            //        foreach (var field in compoundType.Members)
            //        {
            //            Console.WriteLine($"필드 이름: {field.Name}, 유형: {field.Type.Class}, 오프셋: {field.Offset}");
            //        }
            //    }
            //
            //    // 여기서는 간단히 바이트 배열로 읽어서 반환
            //    // 실제 사용에서는 적절한 구조로 변환 필요
            //    byte[] rawData = dataset.Read<byte[]>();
            //    return rawData;
            //
            //    // 대안: 동적 객체 배열 생성 (실제 구현에서 구체화 필요)
            //    // var records = new List<Dictionary<string, object>>();
            //    // return records.ToArray();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"복합 데이터 읽기 오류: {ex.Message}");
            //    return null;
            //}

            return null;
        }

/*
        /// <summary>
        /// Load Dataset real data (for lazy load)
        /// </summary>
        public void LoadDatasetData(Hdf5Dataset dataset)
        {
            if (dataset.IsDataLoaded)
                return;

            // PureHDF uses disposable objects
            using var h5File = H5File.OpenRead(dataset.FilePath ?? GetFilePathFromDatasetPath(dataset.Path));

            // Get the dataset using its path
            var h5Dataset = h5File.Get<H5Dataset>(dataset.Path);
            if (h5Dataset == null)
                throw new Exception($"데이터셋을 열 수 없습니다: {dataset.Path}");

            LoadDataForDataset(dataset, h5Dataset);
        }

        /// <summary>
        /// Extract File Path from Dataset Path
        /// </summary>
        private string GetFilePathFromDatasetPath(string datasetPath)
        {
            throw new NotImplementedException("파일 경로 관리 로직이 필요합니다.");
        }

        /// <summary>
        /// Load data to Dataset
        /// </summary>
        private void LoadDataForDataset(Hdf5Dataset dataset, IH5Dataset h5Dataset)
        {
            // PureHDF has a type system that maps HDF5 types to .NET types
            var typeClass = h5Dataset.Type.Class;

            object data;

            // PureHDF provides generics-based reading
            switch (typeClass)
            {
                case H5DataTypeClass.Integer:
                    if (h5Dataset.Type.IsUnsigned)
                    {
                        data = ReadUnsignedIntegerData(h5Dataset, dataset.Dimensions);
                    }
                    else
                    {
                        data = ReadIntegerData(h5Dataset, dataset.Dimensions);
                    }
                    break;
                case H5DataTypeClass.Float:
                    data = ReadFloatData(h5Dataset, dataset.Dimensions);
                    break;
                case H5DataTypeClass.String:
                    data = ReadStringData(h5Dataset, dataset.Dimensions);
                    break;
                default:
                    data = $"지원되지 않는 데이터 타입: {typeClass}";
                    break;
            }

            dataset.Data = data;
            dataset.IsDataLoaded = true;
        }

        /// <summary>
        /// Read Integer Data
        /// </summary>
        private object ReadIntegerData(IH5Dataset dataset, int[] dimensions)
        {
            // PureHDF has type-safe read methods
            if (dimensions.Length == 1)
            {
                return dataset.Read<int[]>();
            }
            else if (dimensions.Length == 2)
            {
                var data = dataset.Read<int[]>();
                int[,] array2D = new int[dimensions[0], dimensions[1]];

                for (int i = 0; i < dimensions[0]; i++)
                {
                    for (int j = 0; j < dimensions[1]; j++)
                    {
                        array2D[i, j] = data[i * dimensions[1] + j];
                    }
                }

                return array2D;
            }
            else
            {
                // Higher dimensions - return as flattened array
                return dataset.Read<int[]>();
            }
        }

        /// <summary>
        /// Read Unsigned Integer Data
        /// </summary>
        private object ReadUnsignedIntegerData(IH5Dataset dataset, int[] dimensions)
        {
            // PureHDF has type-safe read methods
            if (dimensions.Length == 1)
            {
                return dataset.Read<uint[]>();
            }
            else if (dimensions.Length == 2)
            {
                var data = dataset.Read<uint[]>();
                uint[,] array2D = new uint[dimensions[0], dimensions[1]];

                for (int i = 0; i < dimensions[0]; i++)
                {
                    for (int j = 0; j < dimensions[1]; j++)
                    {
                        array2D[i, j] = data[i * dimensions[1] + j];
                    }
                }

                return array2D;
            }
            else
            {
                // Higher dimensions - return as flattened array
                return dataset.Read<uint[]>();
            }
        }

        /// <summary>
        /// Read Double Data
        /// </summary>
        private object ReadFloatData(IH5Dataset dataset, int[] dimensions)
        {
            if (dimensions.Length == 1)
            {
                return dataset.Read<double[]>();
            }
            else if (dimensions.Length == 2)
            {
                var data = dataset.Read<double[]>();
                double[,] array2D = new double[dimensions[0], dimensions[1]];

                for (int i = 0; i < dimensions[0]; i++)
                {
                    for (int j = 0; j < dimensions[1]; j++)
                    {
                        array2D[i, j] = data[i * dimensions[1] + j];
                    }
                }

                return array2D;
            }
            else
            {
                return dataset.Read<double[]>();
            }
        }

        /// <summary>
        /// Read String Data
        /// </summary>
        private object ReadStringData(IH5Dataset dataset, int[] dimensions)
        {
            // PureHDF handles both fixed and variable-length strings automatically
            return dataset.Read<string[]>();
        }

        /// <summary>
        /// Add Attributes
        /// </summary>
        /// <param name="h5Object">PureHDF object with attributes</param>
        /// <param name="node">Node for attribute</param>
        private void ReadAttributes(IH5AttributableObject h5Object, Hdf5Node node)
        {
            foreach (var attrName in h5Object.AttributeNames)
            {
                var h5Attribute = h5Object.Attribute(attrName);

                // Read the attribute value based on its type
                object attrValue = ReadAttributeValue(h5Attribute);

                var attribute = new Hdf5Attribute
                {
                    Name = attrName,
                    Value = attrValue
                };

                node.Attributes.Add(attribute);
            }
        }

        /// <summary>
        /// Read Attribute Value
        /// </summary>
        /// <param name="h5Attribute">PureHDF Attribute object</param>
        /// <returns>Attribute Value</returns>
        private object ReadAttributeValue(IH5Attribute h5Attribute)
        {
            // PureHDF handles type conversion automatically through generics
            var typeClass = h5Attribute.Type.Class;

            switch (typeClass)
            {
                case H5DataTypeClass.String:
                    // For string attributes, read as string
                    return h5Attribute.Read<string>();

                case H5DataTypeClass.Integer:
                    // For integer attributes, read as int
                    if (h5Attribute.Type.IsUnsigned)
                        return h5Attribute.Read<uint>();
                    return h5Attribute.Read<int>();

                case H5DataTypeClass.Float:
                    // For float attributes, read as double
                    return h5Attribute.Read<double>();

                case H5DataTypeClass.Array:
                case H5DataTypeClass.VlenArray:
                    // For array attributes, try to read as generic object
                    // Needs refinement based on the base type
                    return "[Array data]";

                default:
                    return $"[지원되지 않는 속성 타입: {typeClass}]";
            }
        }

        /// <summary>
        /// Convert H5DataTypeClass to string description
        /// </summary>
        /// <param name="dataType">PureHDF Type object</param>
        /// <returns>Type String</returns>
        private string GetDataType(H5DataType dataType)
        {
            var typeClass = dataType.Class;

            switch (typeClass)
            {
                case H5DataTypeClass.Integer:
                    return "Integer";
                case H5DataTypeClass.Float:
                    return "Float";
                case H5DataTypeClass.String:
                    return "String";
                case H5DataTypeClass.Compound:
                    return "Compound";
                case H5DataTypeClass.Array:
                    return "Array";
                case H5DataTypeClass.VlenArray:
                    return "VariableLength";
                default:
                    return $"Other ({typeClass})";
            }
        }
*/

    }
}
