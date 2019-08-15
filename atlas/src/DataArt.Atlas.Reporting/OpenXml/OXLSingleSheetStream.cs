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
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataArt.Atlas.Reporting.OpenXml
{
    public class OXLSingleSheetStream<T> : IDisposable
    {
        private readonly SpreadsheetDocument document;
        private readonly string sheetName;
        private readonly OpenXmlWriter sheetWriter;
        private readonly OXLColumnRowsWriter<T> rowsWriter;

        public OXLSingleSheetStream(Stream stream, string sheetName, IList<OXLColumn<T>> columnCollection)
        {
            document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
            this.sheetName = sheetName;
            sheetWriter = CreateWriter();
            rowsWriter = new OXLColumnRowsWriter<T>(sheetWriter, CreateStylesheet(), columnCollection);
        }

        public void AddRow(T data)
        {
            rowsWriter.AddRow(data);
        }

        public void Dispose()
        {
            rowsWriter?.Dispose();
            CreateAutoFilterDefinedName();
            sheetWriter?.Close();
            document?.Dispose();
        }

        private OpenXmlWriter CreateWriter()
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

            var sheets = new Sheets();

            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = sheetName
            };

            sheets.AppendChild(sheet);

            var workBook = new Workbook();
            workbookPart.Workbook = workBook;
            workBook.AppendChild(sheets);

            return OpenXmlWriter.Create(worksheetPart, Encoding.UTF8);
        }

        private OXLDefaultStyle CreateStylesheet()
        {
            var workbookPart = document.WorkbookPart;

            if (workbookPart.WorkbookStylesPart == null)
            {
                workbookPart.AddNewPart<WorkbookStylesPart>();
            }

            var defaultStyle = new OXLDefaultStyle();
            workbookPart.WorkbookStylesPart.Stylesheet = defaultStyle.Style;
            return defaultStyle;
        }

        private void CreateAutoFilterDefinedName()
        {
            var workbook = document.WorkbookPart.Workbook;

            if (workbook.DefinedNames == null)
            {
                workbook.DefinedNames = new DefinedNames();
            }

            var lastColumnIndex = rowsWriter.ColumnCount - 1;
            var lastColumnLetter = OXLHelper.GetColumnLetter(lastColumnIndex);
            var rowCount = rowsWriter.RowCount;

            var definedName = new DefinedName($"'{sheetName}'!$A$1:${lastColumnLetter}${rowCount}")
            {
                Name = "_xlnm._FilterDatabase",
                LocalSheetId = 0,
                Hidden = true
            };

            workbook.DefinedNames.AppendChild(definedName);
        }
    }
}
