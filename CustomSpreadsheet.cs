using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace excelExport
{
    class CustomSpreadsheet
    {
        private enum Formats
        {
            General = 0,
            Number = 1,
            Decimal = 2,
            Currency = 164,
            Accounting = 44,
            DateShort = 14,
            DateLong = 165,
            Time = 166,
            Percentage = 10,
            Fraction = 12,
            Scientific = 11,
            Text = 49
        }
        public SpreadsheetDocument spreadsheet { get; set; }
        public WorkbookPart workbook { get; set; }
        public SharedStringTablePart sharedStrings { get; set; }
        public CalculationChainPart calcChain { get; set; }        
        public void Save()
        {
            this.spreadsheet.WorkbookPart.Workbook.Save();
            //this.workbook.
        }

        public void Close()
        {
            this.spreadsheet.Close();
        }

        //private WorkbookPart
        public CustomSpreadsheet(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                //this.spreadsheet = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
                try
                {
                    this.spreadsheet = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
                }
                catch(Exception ex)
                {
                    this.spreadsheet = null;
                    return;
                }
                

                WorkbookPart workbookPart = this.spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                this.workbook = workbookPart;

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = this.spreadsheet.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
                Sheet sheet = new Sheet() { Id = this.spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
                sheets.Append(sheet);
                //this.Save();
                this.Save();
                //this.Close();
            }
            else
            {
                OpenSettings openSettings = new OpenSettings();
                openSettings.MarkupCompatibilityProcessSettings =
                    new MarkupCompatibilityProcessSettings(
                        MarkupCompatibilityProcessMode.ProcessAllParts,
                        FileFormatVersions.Office2013
                    );
                try
                {
                    this.spreadsheet = SpreadsheetDocument.Open(path, true, openSettings);
                }
                catch(Exception ex)
                {
                    this.spreadsheet = null;
                }
                if(this.spreadsheet.GetPartsCountOfType<WorkbookPart>() > 0)
                    this.workbook = this.spreadsheet.WorkbookPart;
                else
                {
                    this.workbook = this.spreadsheet.AddWorkbookPart();
                }
                if(this.workbook == null)
                {
                    this.workbook = this.spreadsheet.AddWorkbookPart();
                    this.workbook.Workbook = new Workbook();
                    this.InsertWorksheet();
                }
                if (this.workbook.GetPartsCountOfType<SharedStringTablePart>() > 0)
                {
                    this.sharedStrings = this.workbook.GetPartsOfType<SharedStringTablePart>().First();
                }
                if(this.workbook.GetPartsCountOfType<CalculationChainPart>() >0)
                {
                    this.calcChain = this.workbook.GetPartsOfType<CalculationChainPart>().First();
                }
            }
        }
        public WorksheetPart AddNewSheet(string sheetName)
        {
            if (this.spreadsheet == null)
            {
                return null;
            }
            //Only need get first because a document shall have ony 1 workbook
            WorkbookPart workbookPart = this.spreadsheet.GetPartsOfType<WorkbookPart>().First();
            if (workbookPart == null)
            {
                workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
            }

            
            //workbookPart.WorksheetParts.First();
            //  Add to workbook:
            Sheets sheets = spreadsheet.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            uint newSheetID;

            if(sheets == null)
            {
                sheets = spreadsheet.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
                newSheetID = 1;
            }
            else
            {
                newSheetID = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1; 
                    //uint sID = uint.Parse(sheet.GetAttribute("sheetID", sheet.NamespaceUri).ToString());
            }
            Sheet newSheet;
            int isExist = sheets.Elements<Sheet>().Where(s => s.Name.ToString().Equals(sheetName)).Count();

            //  If not exist any worksheet with the name provided, create a new one;
            if(isExist == 0)
            {
                //  New sheet.xml file: which contains data of a sheet
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                newSheet = new Sheet() { Id = this.spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = newSheetID, Name = sheetName };
                sheets.Append(newSheet);
                return worksheetPart;
            }
            else
            {
                Sheet selectedSheet = sheets.Elements<Sheet>().Where(s => s.Name.ToString().Equals(sheetName)).FirstOrDefault();
                string selectedSheetID = selectedSheet.Id;
                return (WorksheetPart)this.workbook.GetPartById(selectedSheetID);
            }
        }

        public WorksheetPart GetWorksheetPartByName(string sheetName)
        {
            return GetWorksheetPartByName<WorksheetPart>(sheetName);
        }
        public T GetWorksheetPartByName<T>(string sheetName)
        {
            //  Check the type first;
            if(typeof(T) != typeof(WorksheetPart) && typeof(T) != typeof(Sheet))
            {
                Console.WriteLine("Error: Invalid return type!");
                return default(T);
            }

            if(this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return default(T);
            }
            if(this.workbook.Workbook.Descendants<Sheets>().Count() == 0
                || this.workbook.Workbook.Descendants<Sheet>().Count() == 0)
            {
                Console.WriteLine("Error: This spreadsheet has no sheets!");
                return default(T);
            }

            Sheets sheets = this.workbook.Workbook.GetFirstChild<Sheets>();
            if(sheets == null)
            {
                Console.WriteLine("Error: this workbook has no sheets elements");
                return default(T);
            }
            //Sheet selectedSheet = sheets.Elements<Sheet>().First();
            //Console.WriteLine("Name: " + selectedSheet.Name);
            Sheet selectedSheet = null;
            if (sheets.Elements<Sheet>().Where(s => s.Name.ToString().Equals(sheetName)).Count() > 0)
                selectedSheet = sheets.Elements<Sheet>().Where(s => s.Name.ToString().Equals(sheetName)).FirstOrDefault();
            if (selectedSheet == null)
            {
                Console.WriteLine("Error: this workbook has no '{0}'",sheetName);
                return default(T);
            }

            string selectedSheetID = selectedSheet.Id;

            if( typeof(T) == typeof(WorksheetPart))
                return (T)Convert.ChangeType(this.workbook.GetPartById(selectedSheetID), typeof(T));
            else if( typeof(T) == typeof(Sheet))
            {
                return (T)Convert.ChangeType(selectedSheet, typeof(T));
            }
            else
            {
                Console.WriteLine("Error!");
                return default(T);
            }
        }



        public bool InsertValue(string value, string columnName, uint rowIndex, WorksheetPart wsPart, EnumValue<CellValues> type=null, bool force=false)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return false;
            }
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return false;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return false;
                }
            }
            Cell cell = this.InsertCellInWorksheet(columnName, rowIndex, wsPart);
            if(cell == null)
            {
                Console.WriteLine("Error: Cannot get according cell at {0}!", columnName + rowIndex);
                return false;
            }
            bool res;
            switch(type.Value)
            {
                case CellValues.Number:
                    double num;
                    res = double.TryParse(value, out num);
                    if (res == false)
                    {
                        Console.WriteLine("InsertValue: [ERROR] Invalid data type!");
                        return false;
                    }
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.Date:
                    DateTime date;
                    res = DateTime.TryParse(value, out date);
                    if(res == false)
                    {
                        Console.WriteLine("InsertValue: [ERROR] Invalid data type!");
                        return false;
                    }
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.Boolean:
                    bool ret;
                    res = bool.TryParse(value, out ret);
                    if (res == false)
                    {
                        Console.WriteLine("InsertValue: [ERROR] Invalid data type!");
                        return false;
                    }
                    if(ret == false)
                    {
                        value = "FALSE";
                    }
                    else
                    {
                        value = "TRUE";
                    }
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.SharedString:
                    int index = this.InsertSharedStringItem(value);
                    if(index == -1)
                    {
                        Console.WriteLine("InsertValue: [ERROR] Internal error!");
                        return false;
                    }
                    cell.CellValue = new CellValue(index.ToString());
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.Error:
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.String:
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                case CellValues.InlineString:
                    cell.CellValue = new CellValue(value);
                    cell.DataType = new EnumValue<CellValues>(type.Value);
                    return true;
                default:
                    cell.CellValue = new CellValue(value);
                    cell.DataType = null;
                    return true;
            }
        }
        // If Force == true: Get the first sheet to insert, regardless the name;
        public bool InsertText(string text, string columnName, uint rowIndex, WorksheetPart wsPart=null, bool force=false)
        {
            int index = this.InsertSharedStringItem(text);

            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return false;
            }
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return false;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return false;
                }
            }
            Cell cell = InsertCellInWorksheet(columnName, rowIndex, wsPart);
            cell.CellValue = new CellValue(index.ToString());
            cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);

            wsPart.Worksheet.Save();
            return true;
        }
        
        public bool MergeCells(string fromCell, string toCell, WorksheetPart wsPart=null, bool force=false)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return false;
            }
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return false;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return false;
                }
            }

            string fromCol, toCol, tmpFromRow, tmpToRow;
            char fromCol_C, toCol_C;
            uint fromRow, toRow;
            fromCol = this.GetPartFromAddress(fromCell, false);
            if (fromCol == null)
            {
                Console.WriteLine("MergeCells [ERROR]: Invalid fromCell, got {0}", fromCell);
                return false;
            }
            fromCol_C = fromCol[0];
            toCol = this.GetPartFromAddress(toCell, false);
            if (toCol == null)
            {
                Console.WriteLine("MergeCells [ERROR]: Invalid fromCell, got {0}", toCell);
                return false;
            }
            toCol_C = toCol[0];

            tmpFromRow = this.GetPartFromAddress(fromCell, true);


            if (tmpFromRow == null)
            {
                Console.WriteLine("MergeCells [ERROR]: Invalid fromCell, got {0}", fromCell);
                return false;
            }
            tmpToRow = this.GetPartFromAddress(toCell, true);
            if (tmpToRow == null)
            {
                Console.WriteLine("MergeCells [ERROR]: Invalid fromCell, got {0}", toCell);
                return false;
            }

            fromRow = uint.Parse(tmpFromRow);
            toRow = uint.Parse(tmpToRow);

            if(fromCol_C > toCol_C || fromRow > toRow)
            {
                Console.WriteLine("MergeCells [ERROR]: Invalid cell order, got {0} > {1}",fromCell, toCell);
                return false;
            }
            
            for(uint i = (char)fromCol_C; i <= (char)toCol_C; i++)
            {
                for(uint j = fromRow; j <= toRow; j++)
                {
                    Cell cell = this.InsertCellInWorksheet("" + (char)i, j, wsPart);
                    if(cell == null)
                    {
                        Console.WriteLine("MergeCells [WARNING]: Invalid cell create at {0}", (""+(char)i)+j);
                    }
                }
            }

            Worksheet worksheet = wsPart.Worksheet;

            MergeCells mergeCells;
            if (worksheet.Elements<MergeCells>().Count() > 0)
            {
                mergeCells = worksheet.Elements<MergeCells>().First();
            }
            else
            {
                mergeCells = new MergeCells();

                // Insert a MergeCells object into the specified position.
                if (worksheet.Elements<CustomSheetView>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<CustomSheetView>().First());
                }
                else if (worksheet.Elements<DataConsolidate>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<DataConsolidate>().First());
                }
                else if (worksheet.Elements<SortState>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<SortState>().First());
                }
                else if (worksheet.Elements<AutoFilter>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<AutoFilter>().First());
                }
                else if (worksheet.Elements<Scenarios>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<Scenarios>().First());
                }
                else if (worksheet.Elements<ProtectedRanges>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<ProtectedRanges>().First());
                }
                else if (worksheet.Elements<SheetProtection>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetProtection>().First());
                }
                else if (worksheet.Elements<SheetCalculationProperties>().Count() > 0)
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetCalculationProperties>().First());
                }
                else
                {
                    worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetData>().First());
                }
            }

            // Create the merged cell and append it to the MergeCells collection.
            MergeCell mergeCell = new MergeCell() { Reference = new StringValue(fromCell + ":" + toCell) };
            IEnumerable<MergeCell> listCollapse = mergeCells.Elements<MergeCell>()
                                                                .Where(mc => isCollapse(mc.Reference.ToString(), mergeCell.Reference.ToString()));
            if(listCollapse != null)
            {
                foreach (MergeCell mc in listCollapse)
                {
                    Console.WriteLine("MergeCell [WARNING]: merge cell {0} collapsed with {1}! AutoRemoved"
                                        , mc.Reference.ToString(), mergeCell.Reference.ToString());
                    mc.Remove();
                }
            }
            
            mergeCells.Append(mergeCell);
            return true;
        }

        
        public bool DeleteCell(string columnName, uint rowIndex, WorksheetPart wsPart=null, bool force=false)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return false;
            }

            //IEnumerable<Sheet> sheets = this.workbook.Workbook.GetFirstChild<Sheets>()
            //                                .Elements<Sheet>().Where(s => s.Name == )
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return false;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return false;
                }
            }
            Cell cell = GetCellFromWorksheet(columnName, rowIndex, wsPart);
            if(cell == null)
            {
                Console.WriteLine("Error: no such cell at {0}!", columnName + rowIndex);
                return false;
            }
            int sharedStringId;
            if (cell.DataType != null &&
                cell.DataType.Value == CellValues.SharedString)
            {
                sharedStringId = int.Parse(cell.InnerText);
                cell.Remove();
                this.RemoveSharedStringItem(sharedStringId);
            }
            else
            {
                cell.Remove();
            }
            wsPart.Worksheet.Save();
            return true;
            
        }
        // If Force == true: Get the first sheet to insert, regardless the name;
        public string GetCellValue(string addressName, WorksheetPart wsPart=null, bool force=false)
        {
            string value = null;
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return null;
            }
            if(wsPart == null)
            {
                if(force==true)
                {
                    if(this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if(wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return null;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return null;
                }
            }
            Cell theCell = wsPart.Worksheet.Descendants<Cell>()
                            .Where(c => c.CellReference == addressName).FirstOrDefault();
            if(theCell == null)
            {
                //Console.WriteLine("Warning: The cell is null!");
                return "";
            }

            if(theCell.InnerText.Length > 0)
            {
                value = theCell.InnerText;
                // If the cell represents an integer number, you are done. 
                // For dates, this code returns the serialized value that 
                // represents the date. The code handles strings and 
                // Booleans individually. For shared strings, the code 
                // looks up the corresponding value in the shared string 
                // table. For Booleans, the code converts the value into 
                // the words TRUE or FALSE.

                if(theCell.DataType != null)
                {
                    switch(theCell.DataType.Value)
                    {
                        case CellValues.SharedString:
                            SharedStringTablePart stringTable = this.workbook.GetPartsOfType<SharedStringTablePart>()
                                                                    .FirstOrDefault();
                            if(stringTable != null)
                            {
                                value = stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                            }
                            break;
                        case CellValues.Boolean:
                            switch(value)
                            {
                                case "0":
                                    value = "FALSE";
                                    break;
                                default:
                                    value = "TRUE";
                                    break;
                            }
                            break;
                    }
                }
            }
            return value;
        }

        public Cell InsertFormula(string formula, string columnName, uint rowIndex, WorksheetPart wsPart, bool force=false)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return null;
            }
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return null;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return null;
                }
            }

            if(this.calcChain == null)
            {
                Console.WriteLine("InsertFormula [WARNING]: This spreadsheet does not have calcChainPart, auto create new one!");
                this.calcChain = this.workbook.AddNewPart<CalculationChainPart>();
                this.calcChain.CalculationChain = new CalculationChain();
            }

            Cell cell = this.InsertCellInWorksheet(columnName, rowIndex, wsPart);
            if(cell == null)
            {
                Console.WriteLine("InsertFormula [ERROR]: Cannot insert cell at {0}!", columnName + rowIndex);
                return null;
            }

            cell.CellFormula = new CellFormula(formula);
            cell.CellValue = new CellValue("0");

            string wsPartID = this.workbook.GetIdOfPart(wsPart);
            if(wsPartID == null)
            {
                Console.WriteLine("InsertFormula [ERROR]: Internal error!");
                return null;
            }
            Sheets sheets = this.workbook.Workbook.GetFirstChild<Sheets>();

            Sheet correspondingSheet = sheets.Elements<Sheet>()
                                        .Where(e => string.Compare(e.Id, wsPartID, true) == 0).FirstOrDefault();
            if(correspondingSheet == null)
            {
                Console.WriteLine("InsertFormula [ERROR]: Internal error!");
                return null;
            }

            int sheetID; 
            bool res = int.TryParse(correspondingSheet.SheetId.ToString(),out sheetID);
            if(res == false)
            {
                Console.WriteLine("InsertFormula [ERROR]: Internal error!");
                return null;
            }
            CalculationCell calCel;
            //  If there is already cal cell at CellReference in SheetID, no need to add new:
            calCel = this.calcChain.CalculationChain.Elements<CalculationCell>()
                                                    .Where(c => c.CellReference == cell.CellReference && c.SheetId == sheetID)
                                                    .FirstOrDefault();
            if(calCel == null)
            {
                calCel = new CalculationCell() { CellReference = cell.CellReference, NewLevel = true, SheetId = sheetID };
                this.calcChain.CalculationChain.Append(calCel);
            }
            
            this.workbook.Workbook.CalculationProperties.ForceFullCalculation = true;
            this.workbook.Workbook.CalculationProperties.FullCalculationOnLoad = true;
            this.workbook.Workbook.Save();
            return cell;
        }
        public bool InsertFormulaChain(string formula,string fromCell, string toCell, WorksheetPart wsPart=null, bool force=false)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return false;
            }
            if (wsPart == null)
            {
                if (force == true)
                {
                    if (this.workbook.GetPartsOfType<WorksheetPart>().Count() <= 0)
                    {
                        Console.WriteLine("Error: This spreadsheet has no sheets");
                    }
                    else
                    {
                        wsPart = this.workbook.GetPartsOfType<WorksheetPart>().FirstOrDefault();
                        if (wsPart == null)
                        {
                            Console.WriteLine("Error: Internal error while getting sheet!");
                            return false;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error: This speardsheet has no according worksheet.");
                    return false;
                }
            }

            if (this.calcChain == null)
            {
                Console.WriteLine("InsertFormulaChain [WARNING]: This spreadsheet does not have calcChainPart, auto create new one!");
                this.calcChain = this.workbook.AddNewPart<CalculationChainPart>();
                this.calcChain.CalculationChain = new CalculationChain();
            }

            string fromCol, toCol, tmpFromRow, tmpToRow;
            uint fromRow, toRow;
            fromCol = this.GetPartFromAddress(fromCell,false);
            if(fromCol == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Invalid fromCell, got {0}", fromCell);
                return false;
            }
            toCol = this.GetPartFromAddress(toCell, false);
            if(toCol == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Invalid fromCell, got {0}", toCell);
                return false;
            }

            if(!fromCol.Equals(toCol))
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Cannot add calcChain in different column, got {0}, {1}",fromCell, toCell);
                return false;
            }

            tmpFromRow = this.GetPartFromAddress(fromCell, true);
            

            if (tmpFromRow == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Invalid fromCell, got {0}", fromCell);
                return false;
            }
            tmpToRow = this.GetPartFromAddress(toCell, true);
            if (tmpToRow == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Invalid fromCell, got {0}", toCell);
                return false;
            }

            fromRow = uint.Parse(tmpFromRow);
            toRow = uint.Parse(tmpToRow);

            //  Get the corresponding sheet:
            string wsPartID = this.workbook.GetIdOfPart(wsPart);
            if (wsPartID == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Internal error!");
                return false;
            }
            Sheets sheets = this.workbook.Workbook.GetFirstChild<Sheets>();

            Sheet correspondingSheet = sheets.Elements<Sheet>()
                                        .Where(e => string.Compare(e.Id, wsPartID, true) == 0).FirstOrDefault();
            if (correspondingSheet == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Internal error!");
                return false;
            }

            int sheetID;
            bool res = int.TryParse(correspondingSheet.SheetId.ToString(), out sheetID);
            if (res == false)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Internal error!");
                return false;
            }

            Cell baseCell = this.InsertCellInWorksheet(fromCol, fromRow, wsPart);
            if (baseCell == null)
            {
                Console.WriteLine("InsertFormulaChain [ERROR]: Cannot insert cell at {0}!",  fromCell);
                return false;
            }
            string baseCellRef = fromCell + ":" + toCell;
            uint baseCellSi = 0;
            IEnumerable<CellFormula> listFomula = wsPart.Worksheet.Descendants<CellFormula>()
                                                        .Where(f => {
                                                            if (f.FormulaType != null)
                                                                return f.FormulaType == CellFormulaValues.Shared;
                                                            return false;
                                                        });
            if (listFomula.Count() > 0)
            {
                baseCellSi =listFomula.Select(f => uint.Parse(f.SharedIndex.ToString())).Max() + 1;
            }
                                                        
            baseCell.CellFormula = new CellFormula(formula) { FormulaType=new EnumValue<CellFormulaValues>(CellFormulaValues.Shared),
                                                                Reference=baseCellRef, SharedIndex=baseCellSi};
            baseCell.CellValue = new CellValue("0");

            CalculationCell baseCellCal;
            baseCellCal = this.calcChain.CalculationChain.Elements<CalculationCell>()
                                                    .Where(c => c.CellReference == baseCell.CellReference && c.SheetId == sheetID)
                                                    .FirstOrDefault();
            if (baseCellCal == null)
            {
                baseCellCal = new CalculationCell() { CellReference = baseCell.CellReference, SheetId = sheetID };
                this.calcChain.CalculationChain.Append(baseCellCal);
            }

            bool trailAddRet=true;
            for(uint i = fromRow +1; i <= toRow; i++)
            {
                Cell trailCell = this.InsertCellInWorksheet(fromCol, i, wsPart);
                if(trailCell != null)
                {
                    trailCell.CellFormula = new CellFormula() { FormulaType= new EnumValue<CellFormulaValues>(CellFormulaValues.Shared),
                                                                SharedIndex=baseCellSi};
                    trailCell.CellValue = new CellValue("0");
                    CalculationCell trailCellCal;
                    trailCellCal = this.calcChain.CalculationChain.Elements<CalculationCell>()
                                                    .Where(c => c.CellReference == trailCell.CellReference && c.SheetId == sheetID)
                                                    .FirstOrDefault();
                    if(trailCellCal == null)
                    {
                        trailCellCal = new CalculationCell() { CellReference = trailCell.CellReference, SheetId = sheetID };
                        this.calcChain.CalculationChain.Append(trailCellCal);
                    }
                    else
                    {
                        if (trailCellCal.NewLevel != null)
                            trailCellCal.NewLevel = null;
                    }
                }
                else
                {
                    Console.WriteLine("InsertFormularChain [WARNING]: Cannot add formula at trailing cell {0}", fromCol + i);
                    trailAddRet = false;
                }
            }
            this.workbook.Workbook.CalculationProperties.ForceFullCalculation = true;
            this.workbook.Workbook.CalculationProperties.FullCalculationOnLoad = true;
            this.workbook.Workbook.Save();
            return trailAddRet;
        }
        public int InsertSharedStringItem (string text)
        {
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return -1;
            }
            if (this.sharedStrings == null)
            {
                Console.WriteLine("Warning: This spreadsheet has no sharedStrings, auto created new one!");
                if(this.workbook.GetPartsCountOfType<SharedStringTablePart>() > 0)
                {
                    this.sharedStrings = this.workbook.GetPartsOfType<SharedStringTablePart>().First();
                }
                else
                {
                    this.sharedStrings = this.workbook.AddNewPart<SharedStringTablePart>();
                }
                //this.sharedStrings = this.spreadsheet.AddNewPart<SharedStringTablePart>();
            }
            if(this.sharedStrings.SharedStringTable == null)
            {
                this.sharedStrings.SharedStringTable = new SharedStringTable();
            }

            int i = 0;
            foreach (SharedStringItem item in this.sharedStrings.SharedStringTable.Elements<SharedStringItem>())
            {
                if(item.InnerText == text)
                {
                    return i;
                }
                i++;
            }
            this.sharedStrings.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            this.sharedStrings.SharedStringTable.Save();
            return i;
        }
        public int RemoveSharedStringItem(int sharedStringId)
        {
            bool remove = true;
            if (this.workbook == null || this.workbook.Workbook == null)
            {
                Console.WriteLine("Error: This spreadsheet has no workbook!");
                return -1;
            }
            foreach(var part in this.workbook.GetPartsOfType<WorksheetPart>())
            {
                Worksheet worksheet = part.Worksheet;
                foreach(var cell in worksheet.GetFirstChild<SheetData>().Descendants<Cell>())
                {
                    if(cell.DataType != null &&
                        cell.DataType.Value == CellValues.SharedString &&
                        cell.CellValue.Text == sharedStringId.ToString())
                    {
                        remove = false;
                        break;
                    }
                }
                if (!remove)
                    break;
            }
            if(remove)
            {
                if(this.sharedStrings == null)
                {
                    Console.WriteLine("Error: This spreadsheet has no sharedString table!");
                    return -1;
                }
                SharedStringItem item = this.sharedStrings.SharedStringTable
                                        .Elements<SharedStringItem>().ElementAt(sharedStringId);
                if(item != null)
                {
                    item.Remove();

                    //  Refresh all the shared string references.
                    foreach (var part in this.workbook.GetPartsOfType<WorksheetPart>())
                    {
                        Worksheet worksheet = part.Worksheet;
                        foreach(var cell in worksheet.GetFirstChild<SheetData>().Descendants<Cell>())
                        {
                            if(cell.DataType != null &&
                                cell.DataType.Value == CellValues.SharedString)
                            {
                                int itemIndex = int.Parse(cell.CellValue.Text);
                                if(itemIndex > sharedStringId)
                                {
                                    cell.CellValue.Text = (itemIndex - 1).ToString();
                                }
                            }
                        }
                        worksheet.Save();
                    }
                    this.sharedStrings.SharedStringTable.Save();
                }
                else
                {
                    Console.WriteLine("Warning: No item found at {0}!", sharedStringId);
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("Warning: No shared string item deleted!");
                return -1;
            }
            return sharedStringId;
        }
        public WorksheetPart InsertWorksheet()
        {
            if(this.workbook == null)
            {
                Console.WriteLine("Error: this spreadsheet has no workbookPart!");
                return null;
            }
            WorksheetPart newWorksheetPart = this.workbook.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());
            newWorksheetPart.Worksheet.Save();

            Sheets sheets = this.workbook.Workbook.GetFirstChild<Sheets>();
            string relationshipID = this.workbook.GetIdOfPart(newWorksheetPart);

            uint sheetID = 1;
            if(sheets.Elements<Sheet>().Count() > 0)
            {
                sheetID = sheets.Elements<Sheet>().Select(s=> s.SheetId.Value).Max() +1;
            }
            string sheetName = "Sheet" + sheetID;

            Sheet sheet = new Sheet() { Id = relationshipID, SheetId = sheetID, Name = sheetName};
            sheets.Append(sheet);
            this.workbook.Workbook.Save();

            return newWorksheetPart;
        }

        public Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
            }
            else
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
            {
                return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
            }
            else
            {
                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                worksheet.Save();
                return newCell;
            }
        }
        public Cell GetCellFromWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            IEnumerable<Row> rows = worksheetPart.Worksheet.GetFirstChild<SheetData>().Elements<Row>()
                                    .Where(r => r.RowIndex == rowIndex);
            if(rows.Count() == 0)
            {
                Console.WriteLine("Error: No such row at {0} found.", rowIndex);
                return null;
            }
            IEnumerable<Cell> cells = rows.First().Elements<Cell>()
                                        .Where(c => string.Compare(c.CellReference.Value, columnName + rowIndex, true) == 0);
            if(cells.Count() == 0)
            {
                Console.WriteLine("Error: No such cell at {0} found!", columnName + rowIndex);
                return null;
            }
            return cells.First();
        }
        public string GetPartFromAddress(string cellAddress, bool choose)
        {
            string regex;
            if(choose == false)
            {
                regex = @"[A-Z]";
            }
            else
            {
                regex = @"\d";
            }
            MatchCollection matchCollection = Regex.Matches(cellAddress, regex);
            if(matchCollection.Count > 1)
            {
                return null;
            }
            return matchCollection[0].Value;
        }

        private class CustomCellCordinate
        {
            public uint x { get; set; }
            public uint y { get; set; }
            public static string GetPartFromAddress(string cellAddress, bool choose)
            {
                string regex;
                if (choose == false)
                {
                    regex = @"[A-Z]";
                }
                else
                {
                    regex = @"\d";
                }
                MatchCollection matchCollection = Regex.Matches(cellAddress, regex);
                if (matchCollection.Count > 1)
                {
                    return null;
                }
                return matchCollection[0].Value;
            }
            public CustomCellCordinate(string address)
            {
                string strX, strY;
                strX = GetPartFromAddress(address, false);
                strY = GetPartFromAddress(address, true);
                if(strX == null || strY == null)
                {
                    x = 0;
                    y = 0;
                }
                uint tmp;
                strX = (uint)strX[0]+"";
                bool res = uint.TryParse(strX, out tmp);
                
                if(res)
                {
                    x = tmp;
                }
                else
                {
                    x = 0;
                }
                res = uint.TryParse(strY, out tmp);
                if (res)
                {
                    y = tmp;
                }
                else
                {
                    y = 0;
                }
            }
        }
        private bool isCollapse(string ref1, string ref2)
        {
            string ref1Left, ref1Right, ref2Left, ref2Right;

            string[] refSplit = ref1.Split(':');
            ref1Left = refSplit[0]; ref1Right = refSplit[1];
            refSplit = ref2.Split(':');
            ref2Left = refSplit[0]; ref2Right = refSplit[1];

            CustomCellCordinate ref1L, ref1R, ref2L, ref2R;
            ref1L = new CustomCellCordinate(ref1Left);
            ref1R = new CustomCellCordinate(ref1Right);
            ref2L = new CustomCellCordinate(ref2Left);
            ref2R = new CustomCellCordinate(ref2Right);

            if (ref1L.x > ref2R.x || ref2L.x > ref1R.x)
                return false;
            if (ref1L.y > ref2R.y || ref2L.y > ref1R.y)
                return false;
            return true;
        }
    }
}
