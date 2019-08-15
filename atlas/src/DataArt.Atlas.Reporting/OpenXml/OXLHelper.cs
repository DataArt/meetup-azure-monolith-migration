#region License
// =================================================================================================
// Copyright 2018 DataArt, Inc.
// -------------------------------------------------------------------------------------------------
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this work except in compliance with the License.
// You may obtain a copy of the License in the LICENSE file, or at:
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// =================================================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataArt.Atlas.Infrastructure.Extensions;

namespace DataArt.Atlas.Reporting.OpenXml
{
    public static class OXLHelper
    {
        private const int ReadBufferSize = 5000;

        private static async Task ExportAsync<T>(Stream stream, string sheetName, IList<OXLColumn<T>> columnCollection, Func<int, int, Task<T[]>> getRecordsAsync)
        {
            using (var workBook = new OXLSingleSheetStream<T>(stream, sheetName, columnCollection))
            {
                int rowCount;
                int pageIndex = 0;

                do
                {
                    var rows = await getRecordsAsync(ReadBufferSize, ReadBufferSize * pageIndex);
                    rowCount = rows.Length;

                    foreach (var row in rows)
                    {
                        workBook.AddRow(row);
                    }

                    pageIndex++;
                }
                while (rowCount == ReadBufferSize);
            }
        }

        public static async Task<Stream> ExportToMemoryAsync<T>(string sheetName, IList<OXLColumn<T>> columnCollection, Func<int, int, Task<T[]>> getRecordsAsync)
        {
            var stream = new MemoryStream();

            await ExportAsync(stream, sheetName, columnCollection, getRecordsAsync);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public static async Task<string> ExportToTempFileAsync<T>(string sheetName, IList<OXLColumn<T>> columnCollection, Func<int, int, Task<T[]>> getRecordsAsync)
        {
            var tempFileName = Path.GetTempFileName();

            try
            {
                using (var stream = new FileStream(tempFileName, FileMode.Open, FileAccess.ReadWrite))
                {
                    await ExportAsync(stream, sheetName, columnCollection, getRecordsAsync);
                }
            }
            catch (Exception)
            {
                File.Delete(tempFileName);
                throw;
            }

            return tempFileName;
        }

        public static string GetColumnLetter(uint columnIndex, string prefix = "")
        {
            if (columnIndex < 26)
            {
                return $"{prefix}{(char)(65 + columnIndex)}";
            }

            return GetColumnLetter(columnIndex % 26, GetColumnLetter(((columnIndex - (columnIndex % 26)) / 26) - 1, prefix));
        }

        public static string GetFullReportName(string reportName)
        {
            return $"{DateTimeOffset.UtcNow.ToFilenameFormat()}_{reportName}_Report.xlsx";
        }
    }
}
