﻿using AIMS.Models;
using Microsoft.AspNetCore.Hosting;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AIMS.Services
{
    public interface IExcelGeneratorService
    {
        /// <summary>
        /// Generates excel for sector projects report
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        ActionResponse GenerateSectorProjectsReport(ProjectProfileReportBySector report);

        /// <summary>
        /// Generates excel for location projects report
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        ActionResponse GenerateLocationProjectsReport(ProjectProfileReportByLocation report);

        /// <summary>
        /// Generates excel for yearly projects report
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        ActionResponse GenerateYearlyProjectsReport(TimeSeriesReportByYear report);

        /// <summary>
        /// Generates excel for projects budget
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        ActionResponse GenerateProjectBudgetReportSummary(ProjectsBudgetReportSummary report, List<int> yearsList);
    }

    public class ExcelGeneratorService : IExcelGeneratorService
    {
        private IHostingEnvironment hostingEnvironment;
        string sWebRootFolder = "";
        public ExcelGeneratorService(IHostingEnvironment _hostingEnvironment)
        {
            hostingEnvironment = _hostingEnvironment;
            sWebRootFolder = hostingEnvironment.WebRootPath + "/ExcelFiles/";
            Directory.CreateDirectory(sWebRootFolder);
        }

        public ActionResponse GenerateSectorProjectsReport(ProjectProfileReportBySector report)
        {
            ActionResponse response = new ActionResponse();
            try
            {
                string sFileName = @"SectorProjects-" + DateTime.UtcNow.Ticks.ToString() + ".xlsx";
                FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
                var memory = new MemoryStream();
                using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
                {
                    int rowCounter = 0, totalColumns = 5, groupHeaderColumns = 2;
                    decimal grandTotalFunding = 0, grandTotalDisbursement = 0;
                    
                    IWorkbook workbook;
                    workbook = new XSSFWorkbook();
                    ISheet excelSheet = workbook.CreateSheet("Report");

                    //Setting styles for different cell types
                    ICellStyle titleStyle = workbook.CreateCellStyle();
                    IFont fontTitle = workbook.CreateFont();
                    fontTitle.Color = IndexedColors.DarkBlue.Index;
                    fontTitle.IsBold = true;
                    fontTitle.FontHeightInPoints = 16;
                    titleStyle.SetFont(fontTitle);
                    titleStyle.Alignment = HorizontalAlignment.CenterSelection;
                    titleStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle linkStyle = workbook.CreateCellStyle();
                    IFont linkFont = workbook.CreateFont();
                    linkFont.Color = IndexedColors.DarkBlue.Index;
                    linkFont.IsBold = true;
                    linkFont.FontHeightInPoints = 14;
                    linkStyle.SetFont(linkFont);
                    linkStyle.Alignment = HorizontalAlignment.CenterSelection;
                    linkStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont fontHeader = workbook.CreateFont();
                    fontHeader.Color = IndexedColors.Black.Index;
                    fontHeader.IsBold = true;
                    fontHeader.FontHeightInPoints = 12;
                    headerStyle.SetFont(fontHeader);
                    headerStyle.Alignment = HorizontalAlignment.Center;
                    headerStyle.VerticalAlignment = VerticalAlignment.Center;


                    ICellStyle groupHeaderStyle = workbook.CreateCellStyle();
                    IFont groupFontHeader = workbook.CreateFont();
                    groupFontHeader.Color = IndexedColors.DarkYellow.Index;
                    groupFontHeader.FontHeightInPoints = 12;
                    groupFontHeader.IsBold = true;
                    groupHeaderStyle.SetFont(groupFontHeader);
                    groupHeaderStyle.Alignment = HorizontalAlignment.Center;
                    groupHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle dataCellStyle = workbook.CreateCellStyle();
                    dataCellStyle.WrapText = true;
                    dataCellStyle.Alignment = HorizontalAlignment.Center;
                    dataCellStyle.VerticalAlignment = VerticalAlignment.Center;

                    IRow row = excelSheet.CreateRow(rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        1, 1, 0, totalColumns
                        ));

                    row = excelSheet.CreateRow(++rowCounter);
                    var titleCell = row.CreateCell(0, CellType.String);
                    titleCell.SetCellValue("SomaliAIMS sector report - generated on " + DateTime.Now.ToLongDateString());
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));
                    titleCell.CellStyle = titleStyle;

                    //Adding extra line
                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    //Header columns row
                    row = excelSheet.CreateRow(++rowCounter);
                    var projectCol = row.CreateCell(0);
                    projectCol.SetCellValue("Project");
                    projectCol.CellStyle = headerStyle;

                    var funderCol = row.CreateCell(1);
                    funderCol.SetCellValue("Funders");
                    funderCol.CellStyle = headerStyle;

                    var implementerCol = row.CreateCell(2);
                    implementerCol.SetCellValue("Implementers");
                    implementerCol.CellStyle = headerStyle;

                    var projectCostCol = row.CreateCell(3);
                    projectCostCol.SetCellValue("Project value");
                    projectCostCol.CellStyle = headerStyle;

                    var actualDisbursementsCol = row.CreateCell(4);
                    actualDisbursementsCol.SetCellValue("Actual disbursements");
                    actualDisbursementsCol.CellStyle = headerStyle;

                    var plannedDisbursementsCol = row.CreateCell(5);
                    plannedDisbursementsCol.SetCellValue("Planned disbursements");
                    plannedDisbursementsCol.CellStyle = headerStyle;

                    foreach (var sector in report.SectorProjectsList)
                    {
                        decimal totalFunding = 0, totalDisbursements = 0;
                        totalFunding = Math.Round(sector.TotalFunding, MidpointRounding.AwayFromZero);
                        totalDisbursements = Math.Round(sector.TotalDisbursements, MidpointRounding.AwayFromZero);
                        row = excelSheet.CreateRow(++rowCounter);
                        grandTotalFunding += Math.Round(sector.TotalFunding, MidpointRounding.AwayFromZero);
                        grandTotalDisbursement += Math.Round(sector.TotalDisbursements, MidpointRounding.AwayFromZero);

                        var groupTitleCell = row.CreateCell(0, CellType.String);
                        excelSheet.AddMergedRegion(new CellRangeAddress(
                            rowCounter, rowCounter, 0, groupHeaderColumns));
                        groupTitleCell.SetCellValue(sector.SectorName);
                        groupTitleCell.CellStyle = groupHeaderStyle;

                        var groupFundTotalCell = row.CreateCell(3, CellType.Numeric);
                        groupFundTotalCell.SetCellValue(ApplyThousandFormat(sector.TotalFunding));
                        groupFundTotalCell.CellStyle = groupHeaderStyle;

                        var groupDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                        groupDisbursementTotalCell.SetCellValue(ApplyThousandFormat(sector.TotalDisbursements));
                        groupDisbursementTotalCell.CellStyle = groupHeaderStyle;

                        var groupPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Numeric);
                        groupPlannedDisbursementTotalCell.SetCellValue("");

                        foreach (var project in sector.Projects)
                        {
                            row = excelSheet.CreateRow(++rowCounter);
                            var titleDataCell = row.CreateCell(0);
                            titleDataCell.SetCellValue(project.Title);
                            titleDataCell.CellStyle = dataCellStyle;

                            var funderDataCell = row.CreateCell(1);
                            funderDataCell.SetCellValue(project.Funders);
                            funderDataCell.CellStyle = dataCellStyle;

                            var implementerDataCell = row.CreateCell(2);
                            implementerDataCell.SetCellValue(project.Implementers);
                            implementerDataCell.CellStyle = dataCellStyle;

                            var projectCostDataCell = row.CreateCell(3);
                            projectCostDataCell.SetCellValue(ApplyThousandFormat(project.ProjectCost));
                            projectCostDataCell.CellStyle = dataCellStyle;

                            var actualDisbursementDataCell = row.CreateCell(4);
                            actualDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.ActualDisbursements));
                            actualDisbursementDataCell.CellStyle = dataCellStyle;

                            var plannedDisbursementDataCell = row.CreateCell(5);
                            plannedDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.PlannedDisbursements));
                            plannedDisbursementDataCell.CellStyle = dataCellStyle;
                        }
                    }

                    row = excelSheet.CreateRow(++rowCounter);
                    var footerCell = row.CreateCell(0, CellType.String);
                    footerCell.SetCellValue("Grand totals: ");
                    footerCell.CellStyle = headerStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, groupHeaderColumns));
                    

                    var grandFundTotalCell = row.CreateCell(3, CellType.Numeric);
                    grandFundTotalCell.SetCellValue(ApplyThousandFormat(grandTotalFunding));
                    grandFundTotalCell.CellStyle = headerStyle;

                    var grandDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                    grandDisbursementTotalCell.SetCellValue(ApplyThousandFormat(grandTotalDisbursement));
                    grandDisbursementTotalCell.CellStyle = headerStyle;

                    var grandPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Blank);
                    grandPlannedDisbursementTotalCell.CellStyle = headerStyle;

                    row = excelSheet.CreateRow(++rowCounter);
                    var emptyCell = row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    row = excelSheet.CreateRow(++rowCounter);
                    var linkCell = row.CreateCell(0, CellType.String);
                    XSSFHyperlink urlLink = new XSSFHyperlink(HyperlinkType.Url)
                    {
                        Address = report.ReportSettings.ReportUrl
                    };
                    linkCell.SetCellValue("Click to view latest report");
                    linkCell.Hyperlink = (urlLink);
                    linkCell.CellStyle = linkStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    workbook.Write(fs);
                }
                response.Message = sFileName;
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            return response;
        }

        public ActionResponse GenerateLocationProjectsReport(ProjectProfileReportByLocation report)
        {
            ActionResponse response = new ActionResponse();
            try
            {
                string sFileName = @"LocationProjects-" + DateTime.UtcNow.Ticks.ToString() + ".xlsx";
                FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
                var memory = new MemoryStream();
                using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
                {
                    int rowCounter = 0, totalColumns = 5, groupHeaderColumns = 2;
                    decimal grandTotalFunding = 0, grandTotalDisbursement = 0;

                    
                    IWorkbook workbook;
                    workbook = new XSSFWorkbook();
                    ISheet excelSheet = workbook.CreateSheet("Report");

                    //Setting styles for different cell types
                    ICellStyle titleStyle = workbook.CreateCellStyle();
                    IFont fontTitle = workbook.CreateFont();
                    fontTitle.Color = IndexedColors.DarkBlue.Index;
                    fontTitle.IsBold = true;
                    fontTitle.FontHeightInPoints = 16;
                    titleStyle.SetFont(fontTitle);
                    titleStyle.Alignment = HorizontalAlignment.CenterSelection;
                    titleStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle linkStyle = workbook.CreateCellStyle();
                    IFont linkFont = workbook.CreateFont();
                    linkFont.Color = IndexedColors.DarkBlue.Index;
                    linkFont.IsBold = true;
                    linkFont.FontHeightInPoints = 14;
                    linkStyle.SetFont(linkFont);
                    linkStyle.Alignment = HorizontalAlignment.CenterSelection;
                    linkStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont fontHeader = workbook.CreateFont();
                    fontHeader.Color = IndexedColors.Black.Index;
                    fontHeader.IsBold = true;
                    fontHeader.FontHeightInPoints = 12;
                    headerStyle.SetFont(fontHeader);
                    headerStyle.Alignment = HorizontalAlignment.Center;
                    headerStyle.VerticalAlignment = VerticalAlignment.Center;


                    ICellStyle groupHeaderStyle = workbook.CreateCellStyle();
                    IFont groupFontHeader = workbook.CreateFont();
                    groupFontHeader.Color = IndexedColors.DarkYellow.Index;
                    groupFontHeader.FontHeightInPoints = 12;
                    groupFontHeader.IsBold = true;
                    groupHeaderStyle.SetFont(groupFontHeader);
                    groupHeaderStyle.Alignment = HorizontalAlignment.Center;
                    groupHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle dataCellStyle = workbook.CreateCellStyle();
                    dataCellStyle.WrapText = true;
                    dataCellStyle.Alignment = HorizontalAlignment.Center;
                    dataCellStyle.VerticalAlignment = VerticalAlignment.Center;

                    IRow row = excelSheet.CreateRow(rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    row = excelSheet.CreateRow(++rowCounter);
                    var titleCell = row.CreateCell(0, CellType.String);
                    titleCell.SetCellValue("SomaliAIMS location report - generated on " + DateTime.Now.ToLongDateString());
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));
                    titleCell.CellStyle = titleStyle;

                    //Inserting extra row
                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    //Header columns row
                    row = excelSheet.CreateRow(++rowCounter);
                    var projectCol = row.CreateCell(0);
                    projectCol.SetCellValue("Projects");
                    projectCol.CellStyle = headerStyle;

                    var funderCol = row.CreateCell(1);
                    funderCol.SetCellValue("Funders");
                    funderCol.CellStyle = headerStyle;

                    var implementerCol = row.CreateCell(2);
                    implementerCol.SetCellValue("Implementers");
                    implementerCol.CellStyle = headerStyle;

                    var projectCostCol = row.CreateCell(3);
                    projectCostCol.SetCellValue("Project value");
                    projectCostCol.CellStyle = headerStyle;

                    var actualDisbursementsCol = row.CreateCell(4);
                    actualDisbursementsCol.SetCellValue("Actual disbursements");
                    actualDisbursementsCol.CellStyle = headerStyle;

                    var plannedDisbursementsCol = row.CreateCell(5);
                    plannedDisbursementsCol.SetCellValue("Planned disbursements");
                    plannedDisbursementsCol.CellStyle = headerStyle;

                    foreach (var location in report.LocationProjectsList)
                    {
                        row = excelSheet.CreateRow(++rowCounter);
                        grandTotalFunding += Math.Round(location.TotalFunding, MidpointRounding.AwayFromZero);
                        grandTotalDisbursement += Math.Round(location.TotalDisbursements, MidpointRounding.AwayFromZero);

                        var groupTitleCell = row.CreateCell(0, CellType.String);
                        excelSheet.AddMergedRegion(new CellRangeAddress(
                            rowCounter, rowCounter, 0, groupHeaderColumns));
                        groupTitleCell.SetCellValue(location.LocationName);
                        groupTitleCell.CellStyle = groupHeaderStyle;

                        var groupFundTotalCell = row.CreateCell(3, CellType.Numeric);
                        groupFundTotalCell.SetCellValue(ApplyThousandFormat(location.TotalFunding));
                        groupFundTotalCell.CellStyle = groupHeaderStyle;

                        var groupDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                        groupDisbursementTotalCell.SetCellValue(ApplyThousandFormat(location.TotalDisbursements));
                        groupDisbursementTotalCell.CellStyle = groupHeaderStyle;

                        var groupPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Numeric);
                        groupPlannedDisbursementTotalCell.SetCellValue("");

                        foreach (var project in location.Projects)
                        {
                            row = excelSheet.CreateRow(++rowCounter);
                            var titleDataCell = row.CreateCell(0);
                            titleDataCell.SetCellValue(project.Title);
                            titleDataCell.CellStyle = dataCellStyle;

                            var funderDataCell = row.CreateCell(1);
                            funderDataCell.SetCellValue(project.Funders);
                            funderDataCell.CellStyle = dataCellStyle;

                            var implementerDataCell = row.CreateCell(2);
                            implementerDataCell.SetCellValue(project.Implementers);
                            implementerDataCell.CellStyle = dataCellStyle;

                            var projectCostDataCell = row.CreateCell(3);
                            projectCostDataCell.SetCellValue(ApplyThousandFormat(project.ProjectCost));
                            projectCostDataCell.CellStyle = dataCellStyle;

                            var actualDisbursementDataCell = row.CreateCell(4);
                            actualDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.ActualDisbursements));
                            actualDisbursementDataCell.CellStyle = dataCellStyle;

                            var plannedDisbursementDataCell = row.CreateCell(5);
                            plannedDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.PlannedDisbursements));
                            plannedDisbursementDataCell.CellStyle = dataCellStyle;
                        }
                    }

                    row = excelSheet.CreateRow(++rowCounter);
                    var footerCell = row.CreateCell(0, CellType.String);
                    footerCell.SetCellValue("Grand totals: ");
                    footerCell.CellStyle = headerStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, groupHeaderColumns));
                    

                    var grandFundTotalCell = row.CreateCell(3, CellType.Numeric);
                    grandFundTotalCell.SetCellValue(ApplyThousandFormat(grandTotalFunding));
                    grandFundTotalCell.CellStyle = headerStyle;

                    var grandDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                    grandDisbursementTotalCell.SetCellValue(ApplyThousandFormat(grandTotalDisbursement));
                    grandDisbursementTotalCell.CellStyle = headerStyle;

                    var grandPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Blank);
                    grandPlannedDisbursementTotalCell.CellStyle = headerStyle;

                    row = excelSheet.CreateRow(++rowCounter);
                    var emptyCell = row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    row = excelSheet.CreateRow(++rowCounter);
                    var linkCell = row.CreateCell(0, CellType.String);
                    XSSFHyperlink urlLink = new XSSFHyperlink(HyperlinkType.Url)
                    {
                        Address = report.ReportSettings.ReportUrl
                    };
                    linkCell.SetCellValue("Click to view latest report");
                    linkCell.Hyperlink = (urlLink);
                    linkCell.CellStyle = linkStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    workbook.Write(fs);
                }
                response.Message = sFileName;
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            return response;
        }

        public ActionResponse GenerateYearlyProjectsReport(TimeSeriesReportByYear report)
        {
            ActionResponse response = new ActionResponse();
            try
            {
                string sFileName = @"YearProjects-" + DateTime.UtcNow.Ticks.ToString() + ".xlsx";
                FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
                var memory = new MemoryStream();
                using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
                {
                    int rowCounter = 0, totalColumns = 5, groupHeaderColumns = 2;
                    decimal grandTotalFunding = 0, grandTotalDisbursement = 0;


                    IWorkbook workbook;
                    workbook = new XSSFWorkbook();
                    ISheet excelSheet = workbook.CreateSheet("Report");

                    //Setting styles for different cell types
                    ICellStyle titleStyle = workbook.CreateCellStyle();
                    IFont fontTitle = workbook.CreateFont();
                    fontTitle.Color = IndexedColors.DarkBlue.Index;
                    fontTitle.IsBold = true;
                    fontTitle.FontHeightInPoints = 16;
                    titleStyle.SetFont(fontTitle);
                    titleStyle.Alignment = HorizontalAlignment.CenterSelection;
                    titleStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle linkStyle = workbook.CreateCellStyle();
                    IFont linkFont = workbook.CreateFont();
                    linkFont.Color = IndexedColors.DarkBlue.Index;
                    linkFont.IsBold = true;
                    linkFont.FontHeightInPoints = 14;
                    linkStyle.SetFont(linkFont);
                    linkStyle.Alignment = HorizontalAlignment.CenterSelection;
                    linkStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont fontHeader = workbook.CreateFont();
                    fontHeader.Color = IndexedColors.Black.Index;
                    fontHeader.IsBold = true;
                    fontHeader.FontHeightInPoints = 12;
                    headerStyle.SetFont(fontHeader);
                    headerStyle.Alignment = HorizontalAlignment.Center;
                    headerStyle.VerticalAlignment = VerticalAlignment.Center;


                    ICellStyle groupHeaderStyle = workbook.CreateCellStyle();
                    IFont groupFontHeader = workbook.CreateFont();
                    groupFontHeader.Color = IndexedColors.DarkYellow.Index;
                    groupFontHeader.FontHeightInPoints = 12;
                    groupFontHeader.IsBold = true;
                    groupHeaderStyle.SetFont(groupFontHeader);
                    groupHeaderStyle.Alignment = HorizontalAlignment.Center;
                    groupHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle dataCellStyle = workbook.CreateCellStyle();
                    dataCellStyle.WrapText = true;
                    dataCellStyle.Alignment = HorizontalAlignment.Center;
                    dataCellStyle.VerticalAlignment = VerticalAlignment.Center;

                    IRow row = excelSheet.CreateRow(rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    row = excelSheet.CreateRow(++rowCounter);
                    var titleCell = row.CreateCell(0, CellType.String);
                    titleCell.SetCellValue("SomaliAIMS year-wise projects report - generated on " + DateTime.Now.ToLongDateString());
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));
                    titleCell.CellStyle = titleStyle;

                    //Inserting extra row
                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    //Header columns row
                    row = excelSheet.CreateRow(++rowCounter);
                    var projectCol = row.CreateCell(0);
                    projectCol.SetCellValue("Projects");
                    projectCol.CellStyle = headerStyle;

                    var funderCol = row.CreateCell(1);
                    funderCol.SetCellValue("Funders");
                    funderCol.CellStyle = headerStyle;

                    var implementerCol = row.CreateCell(2);
                    implementerCol.SetCellValue("Implementers");
                    implementerCol.CellStyle = headerStyle;

                    var projectCostCol = row.CreateCell(3);
                    projectCostCol.SetCellValue("Project value");
                    projectCostCol.CellStyle = headerStyle;

                    var actualDisbursementsCol = row.CreateCell(4);
                    actualDisbursementsCol.SetCellValue("Actual disbursements");
                    actualDisbursementsCol.CellStyle = headerStyle;

                    var plannedDisbursementsCol = row.CreateCell(5);
                    plannedDisbursementsCol.SetCellValue("Planned disbursements");
                    plannedDisbursementsCol.CellStyle = headerStyle;

                    foreach (var year in report.YearlyProjectsList)
                    {
                        row = excelSheet.CreateRow(++rowCounter);
                        grandTotalFunding += Math.Round(year.TotalFunding, MidpointRounding.AwayFromZero);
                        grandTotalDisbursement += Math.Round(year.TotalDisbursements, MidpointRounding.AwayFromZero);

                        var groupTitleCell = row.CreateCell(0, CellType.String);
                        excelSheet.AddMergedRegion(new CellRangeAddress(
                            rowCounter, rowCounter, 0, groupHeaderColumns));
                        groupTitleCell.SetCellValue(year.Year);
                        groupTitleCell.CellStyle = groupHeaderStyle;

                        var groupFundTotalCell = row.CreateCell(3, CellType.Numeric);
                        groupFundTotalCell.SetCellValue(ApplyThousandFormat(year.TotalFunding));
                        groupFundTotalCell.CellStyle = groupHeaderStyle;

                        var groupDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                        groupDisbursementTotalCell.SetCellValue(ApplyThousandFormat(year.TotalDisbursements));
                        groupDisbursementTotalCell.CellStyle = groupHeaderStyle;

                        var groupPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Numeric);
                        groupPlannedDisbursementTotalCell.SetCellValue("");

                        foreach (var project in year.Projects)
                        {
                            row = excelSheet.CreateRow(++rowCounter);
                            var titleDataCell = row.CreateCell(0);
                            titleDataCell.SetCellValue(project.Title);
                            titleDataCell.CellStyle = dataCellStyle;

                            var funderDataCell = row.CreateCell(1);
                            funderDataCell.SetCellValue(project.Funders);
                            funderDataCell.CellStyle = dataCellStyle;

                            var implementerDataCell = row.CreateCell(2);
                            implementerDataCell.SetCellValue(project.Implementers);
                            implementerDataCell.CellStyle = dataCellStyle;

                            var projectCostDataCell = row.CreateCell(3);
                            projectCostDataCell.SetCellValue(ApplyThousandFormat(project.ProjectCost));
                            projectCostDataCell.CellStyle = dataCellStyle;

                            var actualDisbursementDataCell = row.CreateCell(4);
                            actualDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.ActualDisbursements));
                            actualDisbursementDataCell.CellStyle = dataCellStyle;

                            var plannedDisbursementDataCell = row.CreateCell(5);
                            plannedDisbursementDataCell.SetCellValue(ApplyThousandFormat(project.PlannedDisbursements));
                            plannedDisbursementDataCell.CellStyle = dataCellStyle;
                        }
                    }

                    row = excelSheet.CreateRow(++rowCounter);
                    var footerCell = row.CreateCell(0, CellType.String);
                    footerCell.SetCellValue("Grand totals: ");
                    footerCell.CellStyle = headerStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, groupHeaderColumns));


                    var grandFundTotalCell = row.CreateCell(3, CellType.Numeric);
                    grandFundTotalCell.SetCellValue(ApplyThousandFormat(grandTotalFunding));
                    grandFundTotalCell.CellStyle = headerStyle;

                    var grandDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                    grandDisbursementTotalCell.SetCellValue(ApplyThousandFormat(grandTotalDisbursement));
                    grandDisbursementTotalCell.CellStyle = headerStyle;

                    var grandPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Blank);
                    grandPlannedDisbursementTotalCell.CellStyle = headerStyle;

                    row = excelSheet.CreateRow(++rowCounter);
                    var emptyCell = row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    row = excelSheet.CreateRow(++rowCounter);
                    var linkCell = row.CreateCell(0, CellType.String);
                    XSSFHyperlink urlLink = new XSSFHyperlink(HyperlinkType.Url)
                    {
                        Address = report.ReportSettings.ReportUrl
                    };
                    linkCell.SetCellValue("Click to view latest report");
                    linkCell.Hyperlink = (urlLink);
                    linkCell.CellStyle = linkStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    workbook.Write(fs);
                }
                response.Message = sFileName;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            return response;
        }

        public ActionResponse GenerateProjectBudgetReportSummary(ProjectsBudgetReportSummary report, List<int> yearsList)
        {
            ActionResponse response = new ActionResponse();
            try
            {
                string sFileName = @"ProjectBudget-" + DateTime.UtcNow.Ticks.ToString() + ".xlsx";
                FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
                var memory = new MemoryStream();
                using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
                {
                    int rowCounter = 0, totalColumns = 6;
                    decimal subTotalDisbursement = 0;

                    IWorkbook workbook;
                    workbook = new XSSFWorkbook();
                    ISheet excelSheet = workbook.CreateSheet("Report");

                    //Setting styles for different cell types
                    ICellStyle titleStyle = workbook.CreateCellStyle();
                    IFont fontTitle = workbook.CreateFont();
                    fontTitle.Color = IndexedColors.DarkBlue.Index;
                    fontTitle.IsBold = true;
                    fontTitle.FontHeightInPoints = 16;
                    titleStyle.SetFont(fontTitle);
                    titleStyle.Alignment = HorizontalAlignment.CenterSelection;
                    titleStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle linkStyle = workbook.CreateCellStyle();
                    IFont linkFont = workbook.CreateFont();
                    linkFont.Color = IndexedColors.DarkBlue.Index;
                    linkFont.IsBold = true;
                    linkFont.FontHeightInPoints = 14;
                    linkStyle.SetFont(linkFont);
                    linkStyle.Alignment = HorizontalAlignment.CenterSelection;
                    linkStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont fontHeader = workbook.CreateFont();
                    fontHeader.Color = IndexedColors.Black.Index;
                    fontHeader.IsBold = true;
                    fontHeader.FontHeightInPoints = 12;
                    headerStyle.SetFont(fontHeader);
                    headerStyle.Alignment = HorizontalAlignment.Center;
                    headerStyle.VerticalAlignment = VerticalAlignment.Center;


                    ICellStyle groupHeaderStyle = workbook.CreateCellStyle();
                    IFont groupFontHeader = workbook.CreateFont();
                    groupFontHeader.Color = IndexedColors.DarkYellow.Index;
                    groupFontHeader.FontHeightInPoints = 12;
                    groupFontHeader.IsBold = true;
                    groupHeaderStyle.SetFont(groupFontHeader);
                    groupHeaderStyle.Alignment = HorizontalAlignment.Center;
                    groupHeaderStyle.VerticalAlignment = VerticalAlignment.Center;

                    ICellStyle dataCellStyle = workbook.CreateCellStyle();
                    dataCellStyle.WrapText = true;
                    dataCellStyle.Alignment = HorizontalAlignment.Center;
                    dataCellStyle.VerticalAlignment = VerticalAlignment.Center;

                    IRow row = excelSheet.CreateRow(rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    row = excelSheet.CreateRow(++rowCounter);
                    var titleCell = row.CreateCell(0, CellType.String);
                    titleCell.SetCellValue("SomaliAIMS projects budget report - generated on " + DateTime.Now.ToLongDateString());
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));
                    titleCell.CellStyle = titleStyle;

                    //Inserting extra row
                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns
                        ));

                    //Header columns row
                    row = excelSheet.CreateRow(++rowCounter);

                    var projectCol = row.CreateCell(0);
                    projectCol.SetCellValue("Years");
                    projectCol.CellStyle = headerStyle;

                    int index = 0;
                    foreach(var year in yearsList)
                    {
                        var funderCol = row.CreateCell(++index);
                        funderCol.SetCellValue(year.ToString());
                        funderCol.CellStyle = headerStyle;
                    }

                    
                    foreach (var project in report.Projects)
                    {
                        index = 0;
                        row = excelSheet.CreateRow(++rowCounter);
                        var yearBlankCell = row.CreateCell(0, CellType.Blank);
                        foreach(var disbursement in project.YearlyDisbursements)
                        {
                            var disbursementCol = row.CreateCell(++index);
                            disbursementCol.SetCellValue(disbursement.Year.ToString());
                            disbursementCol.CellStyle = headerStyle;
                        }

                        index = 0;
                        row = excelSheet.CreateRow(++rowCounter);
                        var projectTitleCell = row.CreateCell(0, CellType.String);
                        projectTitleCell.SetCellValue(project.Title);
                        projectTitleCell.CellStyle = groupHeaderStyle;
                        foreach (var disbursement in project.YearlyDisbursements)
                        {
                            var disbursementCol = row.CreateCell(++index);
                            disbursementCol.SetCellValue(disbursement.Disbursements.ToString());
                            disbursementCol.CellStyle = dataCellStyle;
                        }

                        index = 0;
                        row = excelSheet.CreateRow(++rowCounter);
                        var rowOneBlankCell = row.CreateCell(0, CellType.Blank);
                        foreach (var disbursement in project.YearlyDisbursements)
                        {
                            var disbursementCol = row.CreateCell(++index);
                            disbursementCol.SetCellValue(disbursement.ActualDisbursements.ToString());
                            disbursementCol.CellStyle = dataCellStyle;
                        }

                        index = 0;
                        row = excelSheet.CreateRow(++rowCounter);
                        var rowTwoBlankCell = row.CreateCell(0, CellType.Blank);
                        foreach (var disbursement in project.YearlyDisbursements)
                        {
                            var disbursementCol = row.CreateCell(++index);
                            disbursementCol.SetCellValue(disbursement.ExpectedDisbursements.ToString());
                            disbursementCol.CellStyle = dataCellStyle;
                        }
                    }

                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    row = excelSheet.CreateRow(++rowCounter);
                    var footerYearsBlankOne = row.CreateCell(0, CellType.Blank);
                    index = 1;
                    foreach (var year in yearsList)
                    {

                    }

                    var totalTitleCell = row.CreateCell(3, CellType.Blank);
                    var grandDisbursementTotalCell = row.CreateCell(4, CellType.Numeric);
                    grandDisbursementTotalCell.SetCellValue(ApplyThousandFormat(5));
                    grandDisbursementTotalCell.CellStyle = headerStyle;

                    var grandPlannedDisbursementTotalCell = row.CreateCell(5, CellType.Blank);
                    grandPlannedDisbursementTotalCell.CellStyle = headerStyle;

                    row = excelSheet.CreateRow(++rowCounter);
                    row.CreateCell(0, CellType.Blank);
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    row = excelSheet.CreateRow(++rowCounter);
                    var linkCell = row.CreateCell(0, CellType.String);
                    XSSFHyperlink urlLink = new XSSFHyperlink(HyperlinkType.Url)
                    {
                        Address = report.ReportSettings.ReportUrl
                    };
                    linkCell.SetCellValue("Click to view latest report");
                    linkCell.Hyperlink = (urlLink);
                    linkCell.CellStyle = linkStyle;
                    excelSheet.AddMergedRegion(new CellRangeAddress(
                        rowCounter, rowCounter, 0, totalColumns));

                    workbook.Write(fs);
                }
                response.Message = sFileName;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Success = false;
            }
            return response;

        }

        private string ApplyThousandFormat(decimal number)
        {
            return (Math.Round(number).ToString("#,##0.00"));
        }
    }
}
