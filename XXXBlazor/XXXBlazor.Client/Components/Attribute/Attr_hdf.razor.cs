using Microsoft.AspNetCore.Components;
using XXXBlazor.Client.Models;

namespace XXXBlazor.Client.Pages
{
    public class Hdf5AttrBase : ComponentBase
    {
        [Parameter]
        public Hdf5TreeNode? currentNode{ get; set; }

        protected bool ShowAttribute = false;

        protected string GetArrayPreview(object array)
        {
            var arrayObj = array as Array;
            if (arrayObj == null) return "잘못된 배열";

            // 작은 배열인 경우 전체 표시
            if (arrayObj.Length <= 10)
            {
                return FormatArray(arrayObj);
            }

            // 큰 배열은 처음 5개와 마지막 5개만 표시
            var preview = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                preview.Add(FormatValue(arrayObj.GetValue(i)));
            }
            preview.Add("...");
            for (int i = Math.Max(5, arrayObj.Length - 5); i < arrayObj.Length; i++)
            {
                preview.Add(FormatValue(arrayObj.GetValue(i)));
            }

            return $"[{string.Join(", ", preview)}]";
        }

        protected string FormatArray(Array array)
        {
            var values = new List<string>();
            for (int i = 0; i < array.Length; i++)
            {
                values.Add(FormatValue(array.GetValue(i)));
            }
            return $"[{string.Join(", ", values)}]";
        }

        protected string FormatValue(object? value)
        {
            if (value == null) return "null";
            if (value is float f) return f.ToString("0.###");
            if (value is double d) return d.ToString("0.###");
            return value.ToString() ?? "";
        }

        protected string FormatAttributeValue(object? value)
        {
            if (value == null) return "null";

            // 에러 메시지인 경우 구분
            if (value is string str && str.StartsWith("Error reading attribute"))
            {
                return str;
            }

            // 숫자 포맷팅
            if (value is float f) return f.ToString("0.###");
            if (value is double d) return d.ToString("0.###");

            // 배열 포맷팅
            if (value.GetType().IsArray)
            {
                return GetArrayPreview(value);
            }

            return value.ToString() ?? "";
        }

        protected void CheckedChanged(bool bMode)
        {
            ShowAttribute = bMode;
        }
    }
}
