
namespace XXXBlazor.Client.Models
{
    /// <summary>
    /// HDF5 배열 변환 헬퍼 클래스
    /// </summary>
    public static class Hdf5ArrayHelper
    {
        /// <summary>
        /// 1차원 배열을 다차원 배열로 변환하는 유틸리티 메서드
        /// </summary>
        /// <typeparam name="T">배열 요소 타입</typeparam>
        /// <param name="source">소스 1차원 배열</param>
        /// <param name="dimensions">차원 정보</param>
        /// <returns>다차원 배열</returns>
        public static Array ConvertToMultidimensionalArray<T>(T[] source, ulong[] dimensions)
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
