﻿using AIMS.Models;
using Microsoft.AspNetCore.Http;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AIMS.Services
{
    public interface IDataImportService
    {
        /// <summary>
        /// Imports past data from 2018 file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        List<ImportedAidData> ImportAidDataEighteen(string filePath, IFormFile file);

        /// <summary>
        /// Imports past data from 2017 file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        List<ImportedAidData> ImportAidDataSeventeen(string filePath);

        /// <summary>
        /// Gets data matches for both old and new data
        /// </summary>
        /// <returns></returns>
        ImportedDataMatch GetMatchForOldNewData(string fileFolder);
    }

    public class DataImportService : IDataImportService
    {
        List<string> locationsList;
        private DataFormatter dataFormatter;
        private IFormulaEvaluator formulaEvaluator;

        public DataImportService()
        {
            locationsList = new List<string>()
            {
                "FGS",
                "BRA",
                "Galmudug",
                "Hiirshabelle",
                "Jubaland",
                "Puntland",
                "South West",
                "Somaliland",
                "Unattributed"
            };
        }

        public List<ImportedAidData> ImportAidDataEighteen(string filePath, IFormFile file)
        {
            List<ImportedAidData> projectsList = new List<ImportedAidData>();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                int projectTitleIndex = 3, reportingOrgIndex = 2, startDateIndex = 5, endDateIndex = 6,
                    fundersIndex = 7, implementersIndex = 8, yearOneIndex = 11, yearTwoIndex = 12,
                    yearThreeIndex = 13, primarySectorIndex = 26, rrfMarkerIndex = 28;

                file.CopyTo(stream);
                stream.Position = 0;
                
                XSSFWorkbook hssfwb = new XSSFWorkbook(stream);
                this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
                this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(hssfwb);

                ISheet sheet = hssfwb.GetSheetAt(5);
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                
                for (int i = (sheet.FirstRowNum + 1); i <= 5; i++)
                    {
                    IRow row = sheet.GetRow(i);
                    if (row == null)
                    {
                        continue;
                    }
                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                    {
                        continue;
                    }

                    decimal disbursementValueOne = 0, disbursementValueTwo = 0, disbursementValueThree = 0;
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearOneIndex)), out disbursementValueOne);
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearTwoIndex)), out disbursementValueTwo);
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearThreeIndex)), out disbursementValueThree);

                    projectsList.Add(new ImportedAidData()
                    {
                        ProjectTitle = this.GetFormattedValue(row.GetCell(projectTitleIndex)),
                        ReportingOrganization = this.GetFormattedValue(row.GetCell(reportingOrgIndex)),
                        StartDate = this.GetFormattedValue(row.GetCell(startDateIndex)),
                        EndDate = this.GetFormattedValue(row.GetCell(endDateIndex)),
                        Funders = this.GetFormattedValue(row.GetCell(fundersIndex)),
                        Implementers = this.GetFormattedValue(row.GetCell(implementersIndex)),
                        PreviousYearDisbursements =  disbursementValueOne,
                        CurrentYearDisbursements = disbursementValueTwo,
                        FutureYearDisbursements = disbursementValueThree,
                        PrimarySector = this.GetFormattedValue(row.GetCell(primarySectorIndex)),
                        RRFMarker = this.GetFormattedValue(row.GetCell(rrfMarkerIndex))
                    });
                }
            }
            return projectsList;
        }

        public List<ImportedAidData> ImportAidDataSeventeen(string filePath)
        {
            int projectTitleIndex = 0, reportingOrgIndex = 9, startDateIndex = 2, endDateIndex = 3,
                    fundersIndex = 10, implementersIndex = 11, yearOneIndex = 19, yearTwoIndex = 20,
                    yearThreeIndex = 21, primarySectorIndex = 6, rrfMarkerIndex = 28, currencyIndex = 16, exRateIndex = 17,
                    projectValueIndex = 18;

            List<ImportedAidData> projectsList = new List<ImportedAidData>();
            XSSFWorkbook hssfwb = new XSSFWorkbook(filePath);
            this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
            this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(hssfwb);

            ISheet sheet = hssfwb.GetSheetAt(1);
            IRow headerRow = sheet.GetRow(1);
            int cellCount = headerRow.LastCellNum;


            for (int i = (sheet.FirstRowNum + 1); i <= 5; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row == null)
                {
                    continue;
                }
                if (row.Cells.All(d => d.CellType == CellType.Blank))
                {
                    continue;
                }

                decimal projectValue = 0, exchangeRate = 0, disbursementValueOne = 0, disbursementValueTwo = 0, disbursementValueThree = 0;
                decimal.TryParse(this.GetFormattedValue(row.GetCell(projectValueIndex)), out projectValue);
                decimal.TryParse(this.GetFormattedValue(row.GetCell(exRateIndex)), out exchangeRate);
                decimal.TryParse(this.GetFormattedValue(row.GetCell(yearOneIndex)), out disbursementValueOne);
                decimal.TryParse(this.GetFormattedValue(row.GetCell(yearTwoIndex)), out disbursementValueTwo);
                decimal.TryParse(this.GetFormattedValue(row.GetCell(yearThreeIndex)), out disbursementValueThree);
                

                projectsList.Add(new ImportedAidData()
                {
                    ProjectTitle = this.GetFormattedValue(row.GetCell(projectTitleIndex)),
                    ReportingOrganization = this.GetFormattedValue(row.GetCell(reportingOrgIndex)),
                    StartDate = this.GetFormattedValue(row.GetCell(startDateIndex)),
                    EndDate = this.GetFormattedValue(row.GetCell(endDateIndex)),
                    ProjectValue = projectValue,
                    Currency = this.GetFormattedValue(row.GetCell(currencyIndex)),
                    ExchangeRate = exchangeRate,
                    Funders = this.GetFormattedValue(row.GetCell(fundersIndex)),
                    Implementers = this.GetFormattedValue(row.GetCell(implementersIndex)),
                    PreviousYearDisbursements = disbursementValueOne,
                    CurrentYearDisbursements = disbursementValueTwo,
                    FutureYearDisbursements = disbursementValueThree,
                    PrimarySector = this.GetFormattedValue(row.GetCell(primarySectorIndex)),
                    RRFMarker = this.GetFormattedValue(row.GetCell(rrfMarkerIndex))
                });
            }
            return projectsList;
        }

        public ImportedDataMatch GetMatchForOldNewData(string fileFolder)
        {
            List<string> oldProjectsList = new List<string>();
            List<string> newProjectsList = new List<string>();

            string oldDataFile = fileFolder + "/" + "2017-Somalia-Aid-Mapping.xlsx";
            string newDataFile = fileFolder + "/" + "2018-Somalia-Aid-Mapping.xlsx";

            XSSFWorkbook oldWorkBook = new XSSFWorkbook(oldDataFile);
            this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
            this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(oldWorkBook);
            ISheet sheetOld = oldWorkBook.GetSheetAt(1);
            IRow headerRowOld = sheetOld.GetRow(1);
            int projectTitleIndexOld = 0;

            for (int i = (sheetOld.FirstRowNum + 1); i <= sheetOld.LastRowNum; i++)
            {
                IRow row = sheetOld.GetRow(i);
                if (row == null)
                {
                    continue;
                }
                if (row.Cells.All(d => d.CellType == CellType.Blank))
                {
                    continue;
                }
                oldProjectsList.Add(this.GetFormattedValue(row.GetCell(projectTitleIndexOld)));
            }

            XSSFWorkbook newWorkBook = new XSSFWorkbook(oldDataFile);
            this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
            this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(newWorkBook);
            ISheet sheetNew = newWorkBook.GetSheetAt(1);
            IRow headerRowNew = sheetNew.GetRow(1);
            int projectTitleIndexNew = 0;

            for (int i = (sheetNew.FirstRowNum + 1); i <= sheetNew.LastRowNum; i++)
            {
                IRow row = sheetNew.GetRow(i);
                if (row == null)
                {
                    continue;
                }
                if (row.Cells.All(d => d.CellType == CellType.Blank))
                {
                    continue;
                }
                newProjectsList.Add(this.GetFormattedValue(row.GetCell(projectTitleIndexNew)));
            }

            int matches = 0;
            foreach(string project in newProjectsList)
            {
                var isProjectMatch = (from p in oldProjectsList
                                      where p.Equals(project, StringComparison.OrdinalIgnoreCase)
                                      select p).FirstOrDefault();
                matches += (isProjectMatch != null) ? 1 : 0;
            }

            ImportedDataMatch dataMatch = new ImportedDataMatch()
            {
                TotalProjectsNew = newProjectsList.Count,
                TotalProjectsOld = oldProjectsList.Count,
                TotalMatchedProjects = matches
            };
            return dataMatch;
        }

        private string GetFormattedValue(ICell cell)
        {
            string returnValue = string.Empty;
            if (cell != null)
            {
                try
                {
                    // Get evaluated and formatted cell value
                    returnValue = this.dataFormatter.FormatCellValue(cell, this.formulaEvaluator);
                }
                catch
                {
                    // When failed in evaluating the formula, use stored values instead...
                    // and set cell value for reference from formulae in other cells...
                    if (cell.CellType == CellType.Formula)
                    {
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                returnValue = cell.StringCellValue;
                                cell.SetCellValue(cell.StringCellValue);
                                break;
                            case CellType.Numeric:
                                returnValue = dataFormatter.FormatRawCellContents
                                (cell.NumericCellValue, 0, cell.CellStyle.GetDataFormatString());
                                cell.SetCellValue(cell.NumericCellValue);
                                break;
                            case CellType.Boolean:
                                returnValue = cell.BooleanCellValue.ToString();
                                cell.SetCellValue(cell.BooleanCellValue);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return (returnValue ?? string.Empty).Trim();
        }

        
    }
}
