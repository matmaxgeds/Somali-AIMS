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
        NameValueCollection newDataLocations;
        NameValueCollection oldDataLocations;
        NameValueCollection oldCustomFields;
        NameValueCollection newCustomFields;

        private DataFormatter dataFormatter;
        private IFormulaEvaluator formulaEvaluator;

        public DataImportService()
        {
            oldDataLocations = new NameValueCollection()
            {
                { "39", "FGS" },
                { "40", "BRA" },
                { "41", "Galmudug" },
                { "42", "Hiirshabelle" },
                { "43", "Jubaland" },
                { "44", "Puntland" },
                { "45", "South West" },
                { "46", "Somaliland" },
                { "47", "Unattributed" }
            };

            newDataLocations = new NameValueCollection()
            {
                { "15", "FGS" },
                { "16", "BRA" },
                { "17", "Galmudug" },
                { "18", "Hiirshabelle" },
                { "19", "Jubaland" },
                { "20", "Puntland" },
                { "21", "South West" },
                { "22", "Somaliland" },
                { "23", "Unattributed" }
            };

            oldCustomFields = new NameValueCollection()
            {
                { "32", "Gender" },
                { "33", "Capacity Building" },
                { "34", "Stabalization" },
                { "35", "Durable Solutions" },
                { "36", "Youth" },
                { "37", "Conflict Sensitivity Analysis" },
            };

            newCustomFields = new NameValueCollection()
            {
                {"36", "Recovery & Resilience" },
                {"37", "Gender" },
                {"38", "Durable Solutions" },
                { "40", "Capacity Development" },
                {"41", "Stabilization" },
                {"42", "PCVE" },
                {"43", "Youth" }
            };
        }

        public List<ImportedAidData> ImportAidDataEighteen(string filePath, IFormFile file)
        {
            List<ImportedAidData> projectsList = new List<ImportedAidData>();
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                int projectTitleIndex = 3, reportingOrgIndex = 2, startDateIndex = 5, endDateIndex = 6,
                    fundersIndex = 7, implementersIndex = 8, yearOneIndex = 11, yearTwoIndex = 12,
                    yearThreeIndex = 13, primarySectorIndex = 26, currencyIndex = 12, exRateIndex = 13,
                    locationLowerIndex = 15, locationUpperIndex = 23, customFieldsLowerIndex = 36,
                    customFieldsUpperIndex = 43, linksIndex = 44;

                file.CopyTo(stream);
                stream.Position = 0;
                
                XSSFWorkbook hssfwb = new XSSFWorkbook(stream);
                this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
                this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(hssfwb);

                ISheet sheet = hssfwb.GetSheetAt(5);
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                
                for (int i = (sheet.FirstRowNum + 1); i < sheet.LastRowNum; i++)
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

                    decimal disbursementValueOne = 0, disbursementValueTwo = 0, disbursementValueThree = 0, exchangeRate = 0;
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearOneIndex)), out disbursementValueOne);
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearTwoIndex)), out disbursementValueTwo);
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(yearThreeIndex)), out disbursementValueThree);
                    decimal.TryParse(this.GetFormattedValue(row.GetCell(exRateIndex)), out exchangeRate);

                    List<ImportedLocation> locationsList = new List<ImportedLocation>();
                    for (int l = locationLowerIndex; l <= locationUpperIndex; l++)
                    {
                        decimal percentage = 0;
                        decimal.TryParse(row.GetCell(l).NumericCellValue.ToString(), out percentage);
                        locationsList.Add(new ImportedLocation()
                        {
                            Location = newDataLocations[l.ToString()],
                            Percentage = (percentage * 100 )
                        });
                    }

                    List<ImportedCustomFields> customFieldsList = new List<ImportedCustomFields>();
                    for (int c = customFieldsLowerIndex; c <= customFieldsUpperIndex; c++)
                    {
                        if (c == 39)
                            continue;

                        customFieldsList.Add(new ImportedCustomFields()
                        {
                            CustomField = newCustomFields[c.ToString()],
                            Value = this.GetFormattedValue(row.GetCell(c))
                        });
                    }

                    projectsList.Add(new ImportedAidData()
                    {
                        ProjectTitle = this.GetFormattedValue(row.GetCell(projectTitleIndex)),
                        ReportingOrganization = this.GetFormattedValue(row.GetCell(reportingOrgIndex)),
                        StartDate = this.GetFormattedValue(row.GetCell(startDateIndex)),
                        EndDate = this.GetFormattedValue(row.GetCell(endDateIndex)),
                        Funders = this.GetFormattedValue(row.GetCell(fundersIndex)),
                        Currency = this.GetFormattedValue(row.GetCell(currencyIndex)),
                        ExchangeRate = exchangeRate,
                        Implementers = this.GetFormattedValue(row.GetCell(implementersIndex)),
                        PreviousYearDisbursements =  disbursementValueOne,
                        CurrentYearDisbursements = disbursementValueTwo,
                        FutureYearDisbursements = disbursementValueThree,
                        PrimarySector = this.GetFormattedValue(row.GetCell(primarySectorIndex)),
                        Links = this.GetFormattedValue(row.GetCell(linksIndex)),
                        Locations = locationsList,
                        CustomFields = customFieldsList,
                    });
                }
            }
            return projectsList;
        }

        public List<ImportedAidData> ImportAidDataSeventeen(string filePath)
        {
            int projectTitleIndex = 0, reportingOrgIndex = 9, startDateIndex = 2, endDateIndex = 3,
                    fundersIndex = 10, implementersIndex = 11, yearOneIndex = 19, yearTwoIndex = 20,
                    yearThreeIndex = 21, primarySectorIndex = 6, currencyIndex = 16, exRateIndex = 17,
                    projectValueIndex = 18, locationLowerIndex = 39, locationUpperIndex = 47, linksIndex = 28,
                    customFieldsLowerIndex = 32, customFieldsUpperIndex = 37;

            List<ImportedAidData> projectsList = new List<ImportedAidData>();
            XSSFWorkbook hssfwb = new XSSFWorkbook(filePath);
            this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
            this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(hssfwb);

            ISheet sheet = hssfwb.GetSheetAt(1);
            IRow headerRow = sheet.GetRow(1);
            int cellCount = headerRow.LastCellNum;


            for (int i = (sheet.FirstRowNum + 1); i < sheet.LastRowNum; i++)
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

                List<ImportedLocation> locationsList = new List<ImportedLocation>();
                for(int l = locationLowerIndex; l <= locationUpperIndex; l++)
                {
                    decimal percentage = 0;
                    decimal.TryParse(row.GetCell(l).NumericCellValue.ToString(), out percentage);
                    locationsList.Add(new ImportedLocation()
                    {
                        Location = oldDataLocations[l.ToString()],
                        Percentage = (percentage * 100)
                    });
                }

                List<ImportedCustomFields> customFieldsList = new List<ImportedCustomFields>();
                for (int c = customFieldsLowerIndex; c <= customFieldsUpperIndex; c++)
                {
                    customFieldsList.Add(new ImportedCustomFields()
                    {
                        CustomField = oldCustomFields[c.ToString()],
                        Value = this.GetFormattedValue(row.GetCell(c))
                    });
                }

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
                    Links = this.GetFormattedValue(row.GetCell(linksIndex)),
                    Locations = locationsList,
                    CustomFields = customFieldsList
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

            XSSFWorkbook newWorkBook = new XSSFWorkbook(newDataFile);
            this.dataFormatter = new DataFormatter(CultureInfo.InvariantCulture);
            this.formulaEvaluator = WorkbookFactory.CreateFormulaEvaluator(newWorkBook);
            ISheet sheetNew = newWorkBook.GetSheetAt(5);
            IRow headerRowNew = sheetNew.GetRow(1);
            int projectTitleIndexNew = 3, endDateIndex = 6, currentYearProjects = 0, futureYearProjects = 0, currentYear = DateTime.Now.Year;
            DateTime endDate = DateTime.Now;

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
                bool isValidDate = DateTime.TryParse(this.GetFormattedValue(row.GetCell(endDateIndex)), out endDate); 
                if (isValidDate)
                {
                    if (endDate.Year == currentYear)
                    {
                        ++currentYearProjects;
                    }
                    else if (endDate.Year > currentYear)
                    {
                        ++futureYearProjects;
                    }
                }
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
                CurrentYearProjectsNew = currentYearProjects,
                FutureYearProjectsNew = futureYearProjects,
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
