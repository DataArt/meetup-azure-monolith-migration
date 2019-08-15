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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataArt.Atlas.Reporting.OpenXml
{
    public class OXLDefaultStyle
    {
        public const uint GeneralFormatId = 0;

        private const ushort DefaultVisibleDecimalPlaces = 2;

        private const string HeaderRowBackgroundColor = "FFFFD6";

        private const uint LiteralFormatId = 49;

        public Stylesheet Style { get; }

        public uint HeaderCellStyleIndex { get; }

        private readonly IDictionary<OXLColumnType, uint> mapping = new Dictionary<OXLColumnType, uint>();

        public OXLDefaultStyle()
        {
            Style = CreateBaseStylesheet();

            AddCellFormat(GeneralFormatId); // reserved (format with zero index cannot be changed - it always 'General')
            MapCellFormats();

            InitializeDefaultFills();
            HeaderCellStyleIndex = AddCellFormat(LiteralFormatId, HorizontalAlignmentValues.Center, VerticalAlignmentValues.Center, true, AddFill(HeaderRowBackgroundColor));
        }

        public uint GetCellStyleIndex(OXLColumnType columnType)
        {
            return mapping[columnType];
        }

        private static Stylesheet CreateBaseStylesheet()
        {
            var st = new Stylesheet
            {
                NumberingFormats = new NumberingFormats { Count = 0 },
                Fonts = new Fonts { Count = 1 },
                Fills = new Fills { Count = 1 },
                Borders = new Borders { Count = 1 },
                CellFormats = new CellFormats { Count = 0 }
            };

            st.Fonts.AppendChild(new Font());
            st.Fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.None } });
            st.Borders.AppendChild(new Border());

            return st;
        }

        private void MapCellFormats()
        {
            // https://msdn.microsoft.com/en-us/library/documentformat.openxml.spreadsheet.numberingformat(v=office.14).aspx

            // custom
            uint integerFormatId = 5;
            AddNumberingFormat(integerFormatId, 0);
            mapping.Add(OXLColumnType.Integer, AddCellFormat(integerFormatId));

            uint decimalFormatId = 6;
            AddNumberingFormat(decimalFormatId, DefaultVisibleDecimalPlaces);
            mapping.Add(OXLColumnType.Decimal, AddCellFormat(decimalFormatId));

            // mm-dd-yy
            mapping.Add(OXLColumnType.Date, AddCellFormat(14, HorizontalAlignmentValues.Center));

            // m/d/yy h:mm
            mapping.Add(OXLColumnType.DateTime, AddCellFormat(22, HorizontalAlignmentValues.Center));

            // @
            mapping.Add(OXLColumnType.String, AddCellFormat(LiteralFormatId));

            // @
            mapping.Add(OXLColumnType.StringCentered, AddCellFormat(LiteralFormatId, HorizontalAlignmentValues.Center));
        }

        private void AddNumberingFormat(uint formatId, ushort visibleDecimalPlaces)
        {
            var numberingFormats = Style.NumberingFormats;

            var numberFormat = new NumberingFormat
            {
                NumberFormatId = formatId,
                FormatCode = visibleDecimalPlaces == 0 ? "0" : "0." + new string('0', visibleDecimalPlaces)
            };

            numberingFormats.AppendChild(numberFormat);
            numberingFormats.Count = Convert.ToUInt32(numberingFormats.ChildElements.Count);
        }

        private uint AddCellFormat(uint dataFormatId, HorizontalAlignmentValues? horizontalAlignment = null, VerticalAlignmentValues? verticalAlignment = null, bool wrapText = false, uint fillId = 0)
        {
            var cellFormats = Style.CellFormats;

            var cellFormat = new CellFormat
            {
                FillId = fillId,
                FontId = 0,
                BorderId = 0,
                FormatId = 0,
                NumberFormatId = dataFormatId
            };

            if (horizontalAlignment.HasValue)
            {
                cellFormat.Alignment = new Alignment
                {
                    Horizontal = new EnumValue<HorizontalAlignmentValues>(horizontalAlignment),
                    WrapText = wrapText
                };
            }

            if (verticalAlignment.HasValue)
            {
                if (cellFormat.Alignment == null)
                {
                    cellFormat.Alignment = new Alignment();
                }

                cellFormat.Alignment.Vertical = new EnumValue<VerticalAlignmentValues>(verticalAlignment);
            }

            cellFormats.AppendChild(cellFormat);

            var cellFormatIndex = Convert.ToUInt32(cellFormats.ChildElements.Count) - 1;
            cellFormats.Count = cellFormatIndex + 1;

            return cellFormatIndex;
        }

        private void InitializeDefaultFills()
        {
            var fills = Style.Fills;
            fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.None } });
            fills.AppendChild(new Fill { PatternFill = new PatternFill { PatternType = PatternValues.Gray125 } });
        }

        private uint AddFill(string hexColor)
        {
            var fills = Style.Fills;

            var pattern = new PatternFill
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = new ForegroundColor { Rgb = HexBinaryValue.FromString($"FF{hexColor}") }
            };

            fills.AppendChild(new Fill { PatternFill = pattern });

            var fillIndex = Convert.ToUInt32(fills.ChildElements.Count) - 1;
            fills.Count = fillIndex + 1;

            return fillIndex;
        }
    }
}
