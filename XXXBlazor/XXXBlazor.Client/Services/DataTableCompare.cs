using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 대용량 DataTable을 효율적으로 비교하기 위한 유틸리티 클래스
/// </summary>
public static class DataTableCompare
{
    // 청크 크기의 기본값 (필요에 따라 조정 가능)
    private const int DefaultChunkSize = 5000;

    // 병렬 처리에 사용할 기본 스레드 수 (환경에 따라 조정 가능)
    private static readonly int DefaultDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1);

    /// <summary>
    /// 두 DataTable이 동일한지 효율적으로 비교합니다.
    /// </summary>
    /// <param name="table1">첫 번째 DataTable</param>
    /// <param name="table2">두 번째 DataTable</param>
    /// <param name="chunkSize">처리할 청크의 크기 (메모리 사용량 조절)</param>
    /// <param name="degreeOfParallelism">사용할 최대 스레드 수 (기본값: CPU 코어 수 - 1)</param>
    /// <returns>두 테이블이 동일하면 true, 그렇지 않으면 false</returns>
    public static bool AreEqual(DataTable? table1, DataTable? table2,
                              int chunkSize = DefaultChunkSize,
                              int? degreeOfParallelism = null)
    {
        // 기본 검사 - 가장 빠른 케이스부터 확인
        if (ReferenceEquals(table1, table2)) return true;  // 동일 객체 참조
        if (table1 == null && table2 == null) return true;  // 둘 다 null
        if (table1 == null || table2 == null) return false;  // 하나만 null

        // 행 및 열 수 비교 - 비용이 저렴한 비교부터 시작
        if (table1.Rows.Count != table2.Rows.Count ||
            table1.Columns.Count != table2.Columns.Count)
            return false;

        // 스키마 비교 (열 이름, 유형, 순서)
        if (!CompareSchema(table1, table2))
            return false;

        // 병렬 처리에 사용할 스레드 수 설정
        int threads;
        if (degreeOfParallelism.HasValue && degreeOfParallelism.Value > 0)
        {
            // 양수 값은 프로세서 수 - 1로 제한
            threads = Math.Min(degreeOfParallelism.Value, Environment.ProcessorCount - 1);
        }
        else
        {
            // null이나 0 이하의 값은 기본값 사용
            threads = DefaultDegreeOfParallelism;
        }

        // 데이터 비교
        return CompareDataChunked(table1, table2, chunkSize, threads);
    }

    /// <summary>
    /// 두 DataTable의 스키마를 비교합니다.
    /// </summary>
    private static bool CompareSchema(DataTable table1, DataTable table2)
    {
        // 열 개수가 다르면 스키마가 다름
        if (table1.Columns.Count != table2.Columns.Count)
            return false;

        // 각 열의 이름, 데이터 타입, 순서 비교
        for (int i = 0; i < table1.Columns.Count; i++)
        {
            DataColumn col1 = table1.Columns[i];
            DataColumn col2 = table2.Columns[i];

            // 열 이름이 다르면 스키마가 다름
            if (!string.Equals(col1.ColumnName, col2.ColumnName, StringComparison.Ordinal))
                return false;

            // 데이터 타입이 다르면 스키마가 다름
            if (col1.DataType != col2.DataType)
                return false;

            // 필요하다면 추가적인 스키마 속성도 비교 가능
            // (Nullable, AutoIncrement, ReadOnly 등)
        }

        return true;
    }

    /// <summary>
    /// 청크 단위로 나누어 데이터를 병렬로 비교합니다.
    /// </summary>
    private static bool CompareDataChunked(DataTable table1, DataTable table2,
                                        int chunkSize, int maxThreads)
    {
        int totalRows = table1.Rows.Count;
        int totalChunks = (totalRows + chunkSize - 1) / chunkSize;  // 올림 계산

        // 취소 토큰 소스 생성 (차이점 발견 시 다른 스레드의 작업을 취소하기 위함)
        using (var cts = new CancellationTokenSource())
        {
            // 결과를 저장할 변수 (원자적 업데이트를 위해 사용)
            var areEqual = new AtomicBoolean(true);

            // 병렬 옵션 설정
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreads,
                CancellationToken = cts.Token
            };

            try
            {
                // 청크 단위로 병렬 처리
                Parallel.For(0, totalChunks, parallelOptions, (chunkIndex) =>
                {
                    // 현재 청크의 시작 행 인덱스
                    int startRow = chunkIndex * chunkSize;
                    // 현재 청크의 종료 행 인덱스 (마지막 청크는 더 작을 수 있음)
                    int endRow = Math.Min(startRow + chunkSize, totalRows);

                    // 이미 차이점이 발견된 경우 더 이상 비교하지 않음
                    if (!areEqual.Value)
                    {
                        // 작업 취소 요청
                        cts.Cancel();
                        return;
                    }

                    // 현재 청크의 모든 행 비교
                    bool chunkEqual = CompareChunk(table1, table2, startRow, endRow);

                    // 청크에서 차이점 발견 시 결과 업데이트 및 나머지 작업 취소
                    if (!chunkEqual)
                    {
                        areEqual.Set(false);
                        cts.Cancel();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // 취소 예외는 예상된 동작이므로 무시
            }

            return areEqual.Value;
        }
    }

    /// <summary>
    /// 지정된 범위의 행을 비교합니다.
    /// </summary>
    private static bool CompareChunk(DataTable table1, DataTable table2, int startRow, int endRow)
    {
        // 각 스레드가 자체 SHA256 인스턴스를 가짐
        using (SHA256 sha = SHA256.Create())
        {
            // 행의 범위를 순회하며 비교
            for (int i = startRow; i < endRow; i++)
            {
                // 행 해시 계산 및 비교
                string hash1 = ComputeRowHash(table1.Rows[i], sha);
                string hash2 = ComputeRowHash(table2.Rows[i], sha);

                // 해시가 다르면 데이터가 다름
                if (hash1 != hash2)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 행의 데이터를 해시화합니다.
    /// </summary>
    private static string ComputeRowHash(DataRow row, SHA256 sha)
    {
        // 스레드별로 StringBuilder 인스턴스 재사용
        ThreadLocal<StringBuilder> threadSb = new ThreadLocal<StringBuilder>(() => new StringBuilder());
        StringBuilder sb = threadSb.Value ?? new StringBuilder();
        sb.Clear();

        // 행의 모든 셀 값을 문자열로 연결
        foreach (var item in row.ItemArray)
        {
            // null 처리 (null은 특별한 문자열로 표현)
            sb.Append(item?.ToString() ?? "::NULL::");
            // 구분자 추가 (값 경계를 명확히 구분하기 위함)
            sb.Append("||");
        }

        // 해시 계산
        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] hash = sha.ComputeHash(bytes);

        // Base64 인코딩된 해시 반환
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 원자적 업데이트를 지원하는 boolean 래퍼 클래스
    /// </summary>
    private class AtomicBoolean
    {
        private int _value;

        public AtomicBoolean(bool initialValue)
        {
            _value = initialValue ? 1 : 0;
        }

        public bool Value => _value == 1;

        public void Set(bool value)
        {
            Interlocked.Exchange(ref _value, value ? 1 : 0);
        }
    }
}
