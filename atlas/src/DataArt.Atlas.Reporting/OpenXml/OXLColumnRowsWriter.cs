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
using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataArt.Atlas.Reporting.OpenXml
{
    public class OXLColumnRowsWriter<T> : IDisposable
    {
        private const string DefaultColumnWidth = "15";

        private const int HeaderRowHeight = 30;

        private readonly Row row;

        private readonly Cell cell;

        private readonly OpenXmlWriter writer;

        private readonly OXLDefaultStyle styles;

        private readonly IList<OXLColumn<T>> columnCollection;

        public uint ColumnCount { get; private set; }

        public uint RowCount { get; private set; }

        public OXLColumnRowsWriter(OpenXmlWriter writer, OXLDefaultStyle styles, IList<OXLColumn<T>> columnCollection)
        {
            row = new Row();
            cell = new Cell { CellValue = new CellValue() };

            this.writer = writer;
            this.styles = styles;
            this.columnCollection = columnCollection;

            InitializeSheet();
        }

        public void Dispose()
        {
            FinalizeSheet();
        }

        public void AddRow(T data)
        {
            writer.WriteStartElement(row);

            foreach (var column in columnCollection)
            {
                var value = column.Expression(data);

                cell.StyleIndex = styles.GetCellStyleIndex(column.Type);

                if (value == null)
                {
                    cell.CellValue.Text = string.Empty;
                    cell.DataType = CellValues.String;
                }
                else
                {
                    switch (column.Type)
                    {
                        case OXLColumnType.Integer:
                        case OXLColumnType.Decimal:
                            cell.CellValue.Text = Convert.ToString(value, CultureInfo.InvariantCulture);
                            cell.DataType = CellValues.Number;
                            break;
                        case OXLColumnType.Date:
                        case OXLColumnType.DateTime:
                            GetDateCell(value);
                            break;
                        default:
                            cell.CellValue.Text = Convert.ToString(value);
                            cell.DataType = CellValues.String;
                            break;
                    }
                }

                writer.WriteElement(cell);
            }

            writer.WriteEndElement();
            RowCount++;
        }

        private void GetDateCell(object value)
        {
            var dateTime = (value as DateTimeOffset?)?.UtcDateTime ?? Convert.ToDateTime(value);

            // OverFlow exception condition
            if (dateTime.Year >= 1900)
            {
                cell.CellValue.Text = dateTime.ToOADate().ToString(CultureInfo.InvariantCulture);
                cell.DataType = CellValues.Number;
            }
            else
            {
                cell.StyleIndex = OXLDefaultStyle.GeneralFormatId;

                cell.CellValue.Text = dateTime.ToString(CultureInfo.CurrentCulture);
                cell.DataType = CellValues.String;
            }
        }

        private void InitializeSheet()
        {
            writer.WriteStartElement(new Worksheet());

            SetColumns();

            writer.WriteStartElement(new SheetData());

            AddHeaderRow();
        }

        private void SetColumns()
        {
            writer.WriteStartElement(new Columns());

            for (var i = 1; i <= columnCollection.Count; i++)
            {
                var column = columnCollection[i - 1];
                SetColumn(i, column.Width, column.IsHidden);
            }

            writer.WriteEndElement();
        }

        private void SetColumn(int index, int? width, bool isHidden)
        {
            var columnAttributes = new List<OpenXmlAttribute>();

            var columnIndexString = index.ToString();
            columnAttributes.Add(new OpenXmlAttribute("min", null, columnIndexString));
            columnAttributes.Add(new OpenXmlAttribute("max", null, columnIndexString));

            var widthString = width.HasValue ? width.ToString() : DefaultColumnWidth;
            columnAttributes.Add(new OpenXmlAttribute("width", null, widthString));
            columnAttributes.Add(new OpenXmlAttribute("customWidth", null, "1"));

            if (isHidden)
            {
                columnAttributes.Add(new OpenXmlAttribute("hidden", null, "1"));
            }

            writer.WriteStartElement(new Column(), columnAttributes);
            writer.WriteEndElement();
            ColumnCount++;
        }

        private void AddHeaderRow()
        {
            row.Height = HeaderRowHeight;
            row.CustomHeight = new BooleanValue(true);

            writer.WriteStartElement(row);

            foreach (var column in columnCollection)
            {
                cell.DataType = CellValues.String;
                cell.CellValue.Text = column.Caption;
                cell.StyleIndex = styles.HeaderCellStyleIndex;

                writer.WriteElement(cell);
            }

            writer.WriteEndElement();

            row.CustomHeight = new BooleanValue(false);

            RowCount++;
        }

        private void FinalizeSheet()
        {
            if (writer != null)
            {
                writer.WriteEndElement(); // end of SheetData
                
                writer.WriteElement(new AutoFilter { Reference = $"A1:{OXLHelper.GetColumnLetter(ColumnCount - 1)}{RowCount}" });

                writer.WriteEndElement(); // end of Worksheet
            }
        }
    }
}
