﻿using AIMS.DAL.EF;
using AIMS.DAL.UnitOfWork;
using AIMS.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using AIMS.Services.Helpers;

namespace AIMS.Services
{
    public interface IReportService
    {
        /// <summary>
        /// Get projects report by sectors and title
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        //Task<ProjectProfileReportBySector> GetProjectsBySector(ReportModelForProjectSectors model);

        /// <summary>
        /// Search matching projects by sector wise grouped for the provided criteria
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ProjectProfileReportBySector> GetProjectsBySectors(SearchProjectsBySectorModel model, string reportUrl, string defaultCurrency, decimal exchangeRate);

        /// <summary>
        /// Search matching projects by sector wise grouped for the provided criteria
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ProjectProfileReportByLocation> GetProjectsByLocations(SearchProjectsByLocationModel model, string reportUrl, string defaultCurrency, decimal exchangeRate);

        /// <summary>
        /// Gets projects budget report
        /// </summary>
        /// <param name="reportUrl"></param>
        /// <param name="defaultCurrency"></param>
        /// <param name="exchangeRate"></param>
        /// <returns></returns>
        Task<ProjectsBudgetReport> GetProjectsBudgetReport(string reportUrl, string defaultCurrency, decimal exchangeRate);

        /// <summary>
        /// Internal function for extracting rate of default currency
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="ratesList"></param>
        /// <returns></returns>
        decimal GetExchangeRateForCurrency(string currency, List<CurrencyWithRates> ratesList);
    }

    public class ReportService : IReportService
    {
        AIMSDbContext context;
        IMapper mapper;

        public ReportService(AIMSDbContext cntxt, IMapper autoMapper)
        {
            context = cntxt;
            mapper = autoMapper;
        }


        public async Task<ProjectProfileReportByLocation> GetProjectsByLocations(SearchProjectsByLocationModel model, string reportUrl, string defaultCurrency, decimal exchangeRate)
        {
            using (var unitWork = new UnitOfWork(context))
            {
                ProjectProfileReportByLocation locationProjectsReport = new ProjectProfileReportByLocation();
                try
                {
                    IQueryStringGenerator queryStringGenerator = new QueryStringGenerator();
                    string queryString = queryStringGenerator.GetQueryStringForLocationsReport(model);
                    reportUrl += ReportConstants.LOCATION_REPORT_URL;

                    if (!string.IsNullOrEmpty(queryString))
                    {
                        reportUrl += queryString;
                    }
                    locationProjectsReport.ReportSettings = new Report()
                    {
                        Title = ReportConstants.PROJECTS_BY_LOCATION_TITLE,
                        SubTitle = ReportConstants.PROJECTS_BY_LOCATION_SUBTITLE,
                        Footer = ReportConstants.PROJECTS_BY_LOCATION_FOOTER,
                        Dated = DateTime.Now.ToLongDateString(),
                        ReportUrl = reportUrl
                    };

                    DateTime dated = new DateTime();
                    int year = dated.Year;
                    int month = dated.Month;
                    IQueryable<EFProject> projectProfileList = null;
                    IQueryable<EFProjectLocations> projectLocations = null;

                    if (!string.IsNullOrEmpty(model.Title))
                    {
                        projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => p.Title.Contains(model.Title, StringComparison.OrdinalIgnoreCase),
                            new string[] { "Locations", "Locations.Location", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                    }

                    if (model.StartingYear >= 1970 && model.EndingYear >= 1970)
                    {
                        if (projectProfileList == null)
                        {
                            projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => ((p.EndDate.Year >= model.StartingYear && p.EndDate.Year <= model.EndingYear)),
                            new string[] { "Locations", "Locations.Location", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                        }
                        else
                        {
                            projectProfileList = from project in projectProfileList
                                                 where project.StartDate.Year >= model.StartingYear
                                                 && project.EndDate.Year <= model.EndingYear
                                                 select project;
                        }
                    }

                    if (model.OrganizationIds.Count > 0)
                    {
                        var projectFunders = unitWork.ProjectFundersRepository.GetMany(f => model.OrganizationIds.Contains(f.FunderId));
                        var projectIdsFunders = (from pFunder in projectFunders
                                                 select pFunder.ProjectId).ToList<int>().Distinct();

                        var projectImplementers = unitWork.ProjectImplementersRepository.GetMany(f => model.OrganizationIds.Contains(f.ImplementerId));
                        var projectIdsImplementers = (from pImplementer in projectImplementers
                                                      select pImplementer.ProjectId).ToList<int>().Distinct();


                        var projectIdsList = projectIdsFunders.Union(projectIdsImplementers);

                        if (projectProfileList == null)
                        {
                            projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => projectIdsList.Contains(p.Id)
                            , new string[] { "Locations", "Locations.Location", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                        }
                        else
                        {
                            projectProfileList = from project in projectProfileList
                                                 where projectIdsList.Contains(project.Id)
                                                 select project;
                        }
                    }

                    if (model.LocationIds.Count > 0)
                    {
                        projectLocations = unitWork.ProjectLocationsRepository.GetWithInclude(l => model.LocationIds.Contains(l.LocationId), new string[] { "Location" });
                    }
                    else
                    {
                        projectLocations = unitWork.ProjectLocationsRepository.GetWithInclude(p => p.ProjectId != 0, new string[] { "Location" });
                    }

                    projectLocations = from pLocation in projectLocations
                                       orderby pLocation.Location.Location
                                       select pLocation;

                    if (projectProfileList == null)
                    {
                        projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => (p.EndDate.Year >= year && p.EndDate.Month >= month),
                            new string[] { "Locations", "Locations.Location", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                    }

                    List<ProjectProfileView> projectsList = new List<ProjectProfileView>();
                    foreach (var project in projectProfileList)
                    {
                        ProjectProfileView profileView = new ProjectProfileView();
                        profileView.Id = project.Id;
                        profileView.Title = project.Title;
                        profileView.Description = project.Description;
                        profileView.StartDate = project.StartDate.ToLongDateString();
                        profileView.EndDate = project.EndDate.ToLongDateString();
                        profileView.Locations = mapper.Map<List<ProjectLocationDetailView>>(project.Locations);
                        profileView.Funders = mapper.Map<List<ProjectFunderView>>(project.Funders);
                        profileView.Implementers = mapper.Map<List<ProjectImplementerView>>(project.Implementers);
                        profileView.Disbursements = mapper.Map<List<ProjectDisbursementView>>(project.Disbursements);
                        projectsList.Add(profileView);
                    }

                    string currentLocation = null;
                    List<int> projectIds = new List<int>();
                    List<ProjectsByLocation> locationProjectsList = new List<ProjectsByLocation>();
                    ProjectsByLocation projectsByLocation = null;

                    int totalLocations = projectLocations.Count();
                    int counter = 0;
                    List<ProjectViewForLocation> projectsListForLocation = null;
                    foreach (var location in projectLocations)
                    {
                        if (location.Location.Location != currentLocation)
                        {
                            if (currentLocation != null)
                            {
                                projectsListForLocation = new List<ProjectViewForLocation>();
                                var locationProjects = (from project in projectsList
                                                        where projectIds.Contains(project.Id)
                                                        select project).ToList<ProjectProfileView>();

                                decimal totalFunding = 0, totalDisbursements = 0, totalFundingPercentage = 0, totalDisbursementsPercentage = 0;
                                foreach (var project in locationProjects)
                                {
                                    if (project.Funders.Count() > 0)
                                    {
                                        var fundingTotal = project.Funders.Select(f => (f.Amount * f.ExchangeRate)).Sum();
                                        fundingTotal = Math.Round(fundingTotal, 2, MidpointRounding.AwayFromZero);
                                        project.ProjectCost = ((fundingTotal / 100) * location.FundsPercentage);
                                        totalFunding += fundingTotal;
                                    }
                                }

                                if (totalFunding > 0)
                                {
                                    totalFundingPercentage += ((totalFunding / 100) * location.FundsPercentage);
                                }

                                foreach (var project in locationProjects)
                                {
                                    if (project.Disbursements.Count() > 0)
                                    {
                                        if (model.StartingYear >= 1970 && model.EndingYear >= 1970)
                                        {
                                            project.Disbursements = (from d in project.Disbursements
                                                                     where Convert.ToDateTime(d.Dated).Year >= model.StartingYear &&
                                                                     Convert.ToDateTime(d.Dated).Year <= model.EndingYear
                                                                     select d).ToList();
                                        }
                                        
                                        decimal projectDisbursements = project.Disbursements.Select(d => (d.Amount * d.ExchangeRate)).Sum();
                                        totalDisbursements = Math.Round(totalDisbursements, 2, MidpointRounding.AwayFromZero);
                                        totalDisbursements += projectDisbursements;
                                        UtilityHelper helper = new UtilityHelper();
                                        var endDate = Convert.ToDateTime(project.EndDate);
                                        var startDate = DateTime.Now;
                                        int months = helper.GetMonthDifference(startDate, endDate);

                                        project.ActualDisbursements = Math.Round(((projectDisbursements / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero);
                                        if (months > 0)
                                        {
                                            project.PlannedDisbursements = Math.Round((project.ProjectCost - projectDisbursements) / months);
                                            if (project.PlannedDisbursements < 0)
                                            {
                                                project.PlannedDisbursements = 0;
                                            }
                                        }
                                    }
                                }

                                if (totalDisbursements > 0)
                                {
                                    totalDisbursementsPercentage = ((totalDisbursements / 100) * location.FundsPercentage);
                                }

                                foreach (var project in locationProjects)
                                {
                                    projectsListForLocation.Add(new ProjectViewForLocation()
                                    {
                                        Title = project.Title,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        Funders = string.Join(", ", project.Funders.Select(f => f.Funder)),
                                        Implementers = string.Join(", ", project.Implementers.Select(i => i.Implementer)),
                                        ProjectCost = project.ProjectCost,
                                        ActualDisbursements = project.ActualDisbursements,
                                        PlannedDisbursements = project.PlannedDisbursements,
                                    });
                                }

                                projectsByLocation.TotalFunding = totalFundingPercentage;
                                projectsByLocation.TotalDisbursements = totalDisbursementsPercentage;
                                projectsByLocation.Projects = projectsListForLocation;
                                locationProjectsList.Add(projectsByLocation);
                                projectIds.Clear();
                            }
                            projectsByLocation = new ProjectsByLocation();
                            projectsByLocation.LocationName = location.Location.Location;
                        }
                        currentLocation = location.Location.Location;
                        projectIds.Add(location.ProjectId);
                        ++counter;

                        if (totalLocations == counter)
                        {
                            projectsListForLocation = new List<ProjectViewForLocation>();
                            var locationProjects = (from project in projectsList
                                                    where projectIds.Contains(project.Id)
                                                    select project).ToList<ProjectProfileView>();

                            decimal totalFunding = 0, totalFundingPercentage = 0, totalDisbursements = 0, totalDisbursementsPercentage = 0, locationPercentage = 0;
                            totalFunding = Math.Round(totalFunding, MidpointRounding.AwayFromZero);
                            foreach (var project in locationProjects)
                            {
                                if (project.Locations != null)
                                {
                                    locationPercentage = (from l in project.Locations
                                                          where l.Id == location.LocationId
                                                          select l.FundsPercentage).FirstOrDefault();

                                    if (project.Funders.Count() > 0)
                                    {
                                        var fundingTotal = project.Funders.Select(f => (f.Amount * f.ExchangeRate)).Sum();
                                        fundingTotal = Math.Round(fundingTotal, MidpointRounding.AwayFromZero);
                                        project.ProjectCost = Math.Round(((fundingTotal / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero);
                                        totalFunding += fundingTotal;
                                    }
                                }
                            }

                            if (totalFunding > 0)
                            {
                                totalFundingPercentage = Math.Round(((totalFunding / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero);
                            }

                            foreach (var project in locationProjects)
                            {
                                if (project.Locations != null)
                                {
                                    locationPercentage = (from l in project.Locations
                                                          where l.Id == location.LocationId
                                                          select l.FundsPercentage).FirstOrDefault();
                                    if (project.Disbursements.Count() > 0)
                                    {
                                        decimal projectDisbursements = project.Disbursements.Select(d => (d.Amount * d.ExchangeRate)).Sum();
                                        totalDisbursements += projectDisbursements;

                                        UtilityHelper helper = new UtilityHelper();
                                        var endDate = Convert.ToDateTime(project.EndDate);
                                        var startDate = DateTime.Now;
                                        int months = helper.GetMonthDifference(startDate, endDate);

                                        project.ActualDisbursements = Math.Round(((projectDisbursements / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero );
                                        if (months > 0)
                                        {
                                            project.PlannedDisbursements = Math.Round(((project.ProjectCost - project.ActualDisbursements) / months), MidpointRounding.AwayFromZero);
                                            if (project.PlannedDisbursements < 0)
                                            {
                                                project.PlannedDisbursements = 0;
                                            }
                                        }
                                    }
                                }
                            }

                            if (totalDisbursements > 0)
                            {
                                totalDisbursementsPercentage = Math.Round(((totalDisbursements / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero);
                            }

                            foreach (var project in locationProjects)
                            {
                                projectsListForLocation.Add(new ProjectViewForLocation()
                                {
                                    Title = project.Title,
                                    StartDate = project.StartDate,
                                    EndDate = project.EndDate,
                                    Funders = string.Join(",", project.Funders.Select(f => f.Funder)),
                                    Implementers = string.Join(", ", project.Implementers.Select(i => i.Implementer)),
                                    ProjectCost = project.ProjectCost,
                                    ActualDisbursements = project.ActualDisbursements,
                                    PlannedDisbursements = project.PlannedDisbursements,
                                });
                            }

                            projectsByLocation.TotalFunding = Math.Round(totalFundingPercentage, MidpointRounding.AwayFromZero);
                            projectsByLocation.TotalDisbursements = Math.Round(totalDisbursementsPercentage, MidpointRounding.AwayFromZero);
                            projectsByLocation.Projects = projectsListForLocation;
                            locationProjectsList.Add(projectsByLocation);
                            projectIds.Clear();
                        }
                    }
                    locationProjectsReport.LocationProjectsList = locationProjectsList;
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                }
                return await Task<ProjectProfileReportByLocation>.Run(() => locationProjectsReport).ConfigureAwait(false);
            }
        }

        public async Task<ProjectsBudgetReport> GetProjectsBudgetReport(string reportUrl, string defaultCurrency, decimal exchangeRate)
        {
            using (var unitWork = new UnitOfWork(context))
            {
                ProjectsBudgetReport budgetReport = new ProjectsBudgetReport();
                IQueryable<EFProject> projectProfileList = null;
                try
                {
                    budgetReport.ReportSettings = new Report()
                    {
                        Title = ReportConstants.PROJECTS_BUDGET_REPORT_TITLE,
                        SubTitle = ReportConstants.PROJECTS_BUDGET_REPORT_SUBTITLE,
                        Footer = ReportConstants.PROJECTS_BUDGET_REPORT_FOOTER,
                        Dated = DateTime.Now.ToLongDateString(),
                        ReportUrl = reportUrl + ReportConstants.BUDGET_REPORT_URL + "?load=true"
                    };

                    int currentYear = DateTime.Now.Year;
                    int previousYear = (currentYear - 1);
                    int futureYearsLimit = (currentYear + 3);
                    List<ProjectBudgetView> projectBudgetsList = new List<ProjectBudgetView>();

                    projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => p.EndDate.Year >= currentYear,
                            new string[] { "Sectors", "Sectors.Sector", "Locations", "Locations.Location", "Disbursements", "Funders", "Funders.FundingType" });


                    foreach (var project in projectProfileList)
                    {
                        ProjectBudgetView projectBudget = new ProjectBudgetView();
                        int upperYearLimit = project.EndDate.Year;
                        int yearsLeft = upperYearLimit - currentYear;

                        projectBudget.Id = project.Id;
                        projectBudget.Title = project.Title;
                        projectBudget.PreviousYear = (currentYear - 1);
                        if (project.Funders.Count > 0)
                        {
                            List<ProjectFunding> projectFunding = new List<ProjectFunding>();
                            projectBudget.Funding = (from f in project.Funders
                                           group (f.Amount * f.ExchangeRate) by f.FundingType.FundingType into g
                                           select new ProjectFunding()
                                           {
                                               FundType = g.Key.ToString(),
                                               Amount = g.Sum()
                                           }).ToList();

                            projectBudget.ProjectValue = Math.Round(projectBudget.Funding.Sum(f => f.Amount), MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            projectBudget.ProjectValue = 0;
                            projectBudget.Funding = new List<ProjectFunding>();
                        }
                        
                        if (project.Disbursements.Count > 0)
                        {
                            projectBudget.PreviousYearDisbursements = Math.Round((from d in project.Disbursements
                                                                       where d.Dated.Year == previousYear
                                                                       select (d.Amount * d.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);

                            projectBudget.ActualDisbursements = Math.Round((from d in project.Disbursements
                                                                 where d.Dated.Year == currentYear
                                                                 select (d.Amount * d.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);

                            List<ProjectExpectedDisbursements> expectedDisbursements = new List<ProjectExpectedDisbursements>();
                            for (int year = currentYear; year <= upperYearLimit; year++)
                            {
                                decimal yearDisbursements = yearsLeft > 0 ? (Math.Round((projectBudget.ProjectValue - projectBudget.ActualDisbursements) / yearsLeft)) : projectBudget.ActualDisbursements;
                                List<SectorDisbursements> sectorDisbursements = new List<SectorDisbursements>();
                                foreach (var sector in project.Sectors)
                                {
                                    sectorDisbursements.Add(new SectorDisbursements()
                                    {
                                        Sector = sector.Sector.SectorName,
                                        Disbursements = Math.Round(((yearDisbursements / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero)
                                    });
                                }
                                List<LocationDisbursements> locationDisbursements = new List<LocationDisbursements>();
                                foreach (var location in project.Locations)
                                {
                                    locationDisbursements.Add(new LocationDisbursements()
                                    {
                                        Location = location.Location.Location,
                                        Disbursements = Math.Round(((yearDisbursements / 100) * location.FundsPercentage), MidpointRounding.AwayFromZero)
                                    });
                                }

                                expectedDisbursements.Add(new ProjectExpectedDisbursements()
                                {
                                    Year = year,
                                    Disbursements = yearDisbursements,
                                    SectorPercentages = sectorDisbursements,
                                    LocationPercentages = locationDisbursements
                                });
                            }

                            for (int year = (upperYearLimit + 1); year <= futureYearsLimit; year++)
                            {
                                expectedDisbursements.Add(new ProjectExpectedDisbursements()
                                {
                                    Year = year,
                                    Disbursements = 0,
                                    SectorPercentages = new List<SectorDisbursements>(),
                                    LocationPercentages = new List<LocationDisbursements>()
                                });
                            }
                            projectBudget.ExpectedDisbursements = expectedDisbursements;
                        }
                        else
                        {
                            projectBudget.ActualDisbursements = 0;
                            projectBudget.ExpectedDisbursements = new List<ProjectExpectedDisbursements>();
                        }
                        projectBudgetsList.Add(projectBudget);
                    }
                    budgetReport.Projects = projectBudgetsList;
                }
                catch(Exception ex)
                {
                    string message = ex.Message;
                }
                return await Task<ProjectsBudgetReport>.Run(() => budgetReport).ConfigureAwait(false);
            }
        }

        public async Task<ProjectProfileReportBySector> GetProjectsBySectors(SearchProjectsBySectorModel model, string reportUrl, string defaultCurrency, decimal exchangeRate)
        {
            using (var unitWork = new UnitOfWork(context))
            {
                ProjectProfileReportBySector sectorProjectsReport = new ProjectProfileReportBySector();
                try
                {
                    IQueryStringGenerator queryStringGenerator = new QueryStringGenerator();
                    string queryString = queryStringGenerator.GetQueryStringForSectorsReport(model);
                    reportUrl += ReportConstants.SECTOR_REPORT_URL;

                    if (!string.IsNullOrEmpty(queryString))
                    {
                        reportUrl += queryString;
                    }
                    sectorProjectsReport.ReportSettings = new Report()
                    {
                        Title = ReportConstants.PROJECTS_BY_SECTOR_TITLE,
                        SubTitle = ReportConstants.PROJECTS_BY_SECTOR_SUBTITLE,
                        Footer = ReportConstants.PROJECTS_BY_SECTOR_FOOTER,
                        Dated = DateTime.Now.ToLongDateString(),
                        ReportUrl = reportUrl
                    };

                    DateTime dated = new DateTime();
                    int year = dated.Year;
                    int month = dated.Month;
                    IQueryable<EFProject> projectProfileList = null;
                    IQueryable<EFProjectSectors> projectSectors = null;

                    if (!string.IsNullOrEmpty(model.Title))
                    {
                        projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => p.Title.Contains(model.Title, StringComparison.OrdinalIgnoreCase),
                            new string[] { "Sectors", "Sectors.Sector", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                    }

                    if (model.StartingYear >= 2000 && model.EndingYear >= 2000)
                    {
                        if (projectProfileList == null)
                        {
                            projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => ((p.EndDate.Year >= model.StartingYear && p.EndDate.Year <= model.EndingYear)),
                            new string[] { "Sectors", "Sectors.Sector", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                        }
                        else
                        {
                            projectProfileList = from project in projectProfileList
                                                 where project.StartDate.Year >= model.StartingYear
                                                 && project.EndDate.Year <= model.EndingYear
                                                 select project;
                        }
                    }

                    if (model.OrganizationIds.Count > 0)
                    {
                        var projectFunders = unitWork.ProjectFundersRepository.GetMany(f => model.OrganizationIds.Contains(f.FunderId));
                        var projectIdsFunders = (from pFunder in projectFunders
                                                 select pFunder.ProjectId).ToList<int>().Distinct();

                        var projectImplementers = unitWork.ProjectImplementersRepository.GetMany(f => model.OrganizationIds.Contains(f.ImplementerId));
                        var projectIdsImplementers = (from pImplementer in projectImplementers
                                                      select pImplementer.ProjectId).ToList<int>().Distinct();


                        var projectIdsList = projectIdsFunders.Union(projectIdsImplementers);

                        if (projectProfileList == null)
                        {
                            projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => projectIdsList.Contains(p.Id)
                            , new string[] { "Sectors", "Sectors.Sector", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                        }
                        else
                        {
                            projectProfileList = from project in projectProfileList
                                                 where projectIdsList.Contains(project.Id)
                                                 select project;
                        }
                    }

                    if (projectProfileList == null)
                    {
                        projectProfileList = await unitWork.ProjectRepository.GetWithIncludeAsync(p => (p.EndDate.Year >= year && p.EndDate.Month >= month),
                            new string[] { "Sectors", "Sectors.Sector", "Disbursements", "Funders", "Funders.Funder", "Implementers", "Implementers.Implementer" });
                    }

                    if (model.SectorIds.Count > 0)
                    {
                        projectSectors = unitWork.ProjectSectorsRepository.GetWithInclude(p => model.SectorIds.Contains(p.SectorId), new string[] { "Sector" });
                    }
                    else
                    {
                        projectSectors = unitWork.ProjectSectorsRepository.GetWithInclude(p => p.ProjectId != 0, new string[] { "Sector" });
                    }

                    projectSectors = from pSector in projectSectors
                                     orderby pSector.Sector.SectorName
                                     select pSector;

                    List<ProjectProfileView> projectsList = new List<ProjectProfileView>();
                    foreach (var project in projectProfileList)
                    {
                        ProjectProfileView profileView = new ProjectProfileView();
                        profileView.Id = project.Id;
                        profileView.Title = project.Title;
                        profileView.Description = project.Description;
                        profileView.StartDate = project.StartDate.ToLongDateString();
                        profileView.EndDate = project.EndDate.ToLongDateString();
                        profileView.Sectors = mapper.Map<List<ProjectSectorView>>(project.Sectors);
                        profileView.Funders = mapper.Map<List<ProjectFunderView>>(project.Funders);
                        profileView.Implementers = mapper.Map<List<ProjectImplementerView>>(project.Implementers);
                        profileView.Disbursements = mapper.Map<List<ProjectDisbursementView>>(project.Disbursements);
                        projectsList.Add(profileView);
                    }

                    string currentSector = null;
                    List<int> projectIds = new List<int>();
                    List<ProjectsBySector> sectorProjectsList = new List<ProjectsBySector>();
                    ProjectsBySector projectsBySector = null;

                    int totalSectors = projectSectors.Count();
                    int counter = 0;
                    List<ProjectViewForSector> projectsListForSector = null;
                    foreach (var sector in projectSectors)
                    {
                        if (sector.Sector.SectorName != currentSector)
                        {
                            if (currentSector != null)
                            {
                                projectsListForSector = new List<ProjectViewForSector>();
                                var sectorProjects = (from project in projectsList
                                                      where projectIds.Contains(project.Id)
                                                      select project).ToList<ProjectProfileView>();

                                decimal totalFunding = 0, totalDisbursements = 0, totalFundingPercentage = 0, totalDisbursementsPercentage = 0;
                                foreach (var project in sectorProjects)
                                {
                                    if (project.Funders.Count() > 0)
                                    {
                                        var fundingTotal = Math.Round(project.Funders.Select(f => (f.Amount * f.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);
                                        fundingTotal = Math.Round(fundingTotal, MidpointRounding.AwayFromZero);
                                        project.ProjectCost = Math.Round(((fundingTotal / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                        totalFunding += fundingTotal;
                                    }
                                }

                                if (totalFunding > 0)
                                {
                                    totalFundingPercentage += Math.Round(((totalFunding / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                }

                                foreach (var project in sectorProjects)
                                {
                                    if (project.Disbursements.Count() > 0)
                                    {
                                        if (model.StartingYear >= 1970 && model.EndingYear >= 1970)
                                        {
                                            project.Disbursements = (from d in project.Disbursements
                                                                     where Convert.ToDateTime(d.Dated).Year >= model.StartingYear &&
                                                                     Convert.ToDateTime(d.Dated).Year <= model.EndingYear
                                                                     select d).ToList();
                                        }
                                        decimal projectDisbursements = Math.Round(project.Disbursements.Select(d=> (d.Amount * d.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);
                                        totalDisbursements += projectDisbursements;
                                        totalDisbursements = Math.Round(totalDisbursements, MidpointRounding.AwayFromZero);
                                        UtilityHelper helper = new UtilityHelper();
                                        var endDate = Convert.ToDateTime(project.EndDate);
                                        var startDate = DateTime.Now;
                                        int months = helper.GetMonthDifference(startDate, endDate);

                                        project.ActualDisbursements = Math.Round(((projectDisbursements / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                        if (months > 0)
                                        {
                                            project.PlannedDisbursements = Math.Round(((project.ProjectCost - projectDisbursements) / months), MidpointRounding.AwayFromZero);
                                            if (project.PlannedDisbursements < 0)
                                            {
                                                project.PlannedDisbursements = 0;
                                            }
                                        }
                                    }
                                }

                                if (totalDisbursements > 0)
                                {
                                    totalDisbursementsPercentage = Math.Round(((totalDisbursements / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                }

                                foreach (var project in sectorProjects)
                                {
                                    projectsListForSector.Add(new ProjectViewForSector()
                                    {
                                        Title = project.Title,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        Funders = string.Join(", ", project.Funders.Select(f => f.Funder)),
                                        Implementers = string.Join(", ", project.Implementers.Select(i => i.Implementer)),
                                        ProjectCost = project.ProjectCost,
                                        ActualDisbursements = project.ActualDisbursements,
                                        PlannedDisbursements = project.PlannedDisbursements,
                                    });
                                }

                                projectsBySector.TotalFunding = totalFundingPercentage;
                                projectsBySector.TotalDisbursements = totalDisbursementsPercentage;
                                projectsBySector.Projects = projectsListForSector;
                                sectorProjectsList.Add(projectsBySector);
                                projectIds.Clear();
                            }
                            projectsBySector = new ProjectsBySector();
                            projectsBySector.SectorName = sector.Sector.SectorName;
                        }
                        currentSector = sector.Sector.SectorName;
                        projectIds.Add(sector.ProjectId);
                        ++counter;

                        if (totalSectors == counter)
                        {
                            projectsListForSector = new List<ProjectViewForSector>();
                            var sectorProjects = (from project in projectsList
                                                  where projectIds.Contains(project.Id)
                                                  select project).ToList<ProjectProfileView>();

                            decimal totalFunding = 0, totalFundingPercentage = 0, totalDisbursements = 0, totalDisbursementsPercentage = 0, sectorPercentage = 0;
                            foreach (var project in sectorProjects)
                            {
                                if (project.Sectors != null)
                                {
                                    sectorPercentage = (from s in project.Sectors
                                                        where s.SectorId == sector.SectorId
                                                        select s.FundsPercentage).FirstOrDefault();

                                    if (project.Funders.Count() > 0)
                                    {
                                        var fundingTotal = Math.Round(project.Funders.Select(f => (f.Amount * f.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);
                                        fundingTotal = Math.Round(fundingTotal, MidpointRounding.AwayFromZero);
                                        project.ProjectCost = Math.Round(((fundingTotal / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                        totalFunding += fundingTotal;
                                    }
                                }
                            }

                            if (totalFunding > 0)
                            {
                                totalFundingPercentage = Math.Round(((totalFunding / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                            }

                            foreach (var project in sectorProjects)
                            {
                                if (project.Sectors != null)
                                {
                                    sectorPercentage = (from s in project.Sectors
                                                        where s.SectorId == sector.SectorId
                                                        select s.FundsPercentage).FirstOrDefault();
                                    if (project.Disbursements.Count() > 0)
                                    {
                                        decimal projectDisbursements = Math.Round(project.Disbursements.Select(d => (d.Amount * d.ExchangeRate)).Sum(), MidpointRounding.AwayFromZero);
                                        totalDisbursements += projectDisbursements;
                                        totalDisbursements = Math.Round(totalDisbursements, MidpointRounding.AwayFromZero);
                                        UtilityHelper helper = new UtilityHelper();
                                        var endDate = Convert.ToDateTime(project.EndDate);
                                        var startDate = DateTime.Now;
                                        int months = helper.GetMonthDifference(startDate, endDate);

                                        project.ActualDisbursements = Math.Round(((projectDisbursements / 100) * sector.FundsPercentage), MidpointRounding.AwayFromZero);
                                        if (months > 0)
                                        {
                                            project.PlannedDisbursements = Math.Round(((project.ProjectCost - project.ActualDisbursements) / months), MidpointRounding.AwayFromZero);
                                            if (project.PlannedDisbursements < 0)
                                            {
                                                project.PlannedDisbursements = 0;
                                            }
                                        }
                                    }
                                }
                            }

                            if (totalDisbursements > 0)
                            {
                                totalDisbursementsPercentage = Math.Round(((totalDisbursements / 100) *  sector.FundsPercentage), MidpointRounding.AwayFromZero);
                            }

                            foreach(var project in sectorProjects)
                            {
                                projectsListForSector.Add(new ProjectViewForSector()
                                {
                                    Title = project.Title,
                                    StartDate = project.StartDate,
                                    EndDate = project.EndDate,
                                    Funders = string.Join(",", project.Funders.Select(f => f.Funder)),
                                    Implementers = string.Join(", ", project.Implementers.Select(i => i.Implementer)),
                                    ProjectCost = project.ProjectCost,
                                    ActualDisbursements = project.ActualDisbursements,
                                    PlannedDisbursements = project.PlannedDisbursements,
                                }) ;
                            }

                            projectsBySector.TotalFunding = totalFundingPercentage;
                            projectsBySector.TotalDisbursements = totalDisbursementsPercentage;
                            projectsBySector.Projects = projectsListForSector;
                            sectorProjectsList.Add(projectsBySector);
                            projectIds.Clear();
                        }
                    }
                    sectorProjectsReport.SectorProjectsList = sectorProjectsList;
                }
                catch(Exception ex)
                {
                    string error = ex.Message;
                }
                return await Task<ProjectProfileReportBySector>.Run(() => sectorProjectsReport).ConfigureAwait(false);
            }
        }

        public decimal GetExchangeRateForCurrency(string currency, List<CurrencyWithRates> ratesList)
        {
            return (from rate in ratesList
                                    where rate.Currency.Equals(currency)
                                    select rate.Rate).FirstOrDefault();
        }

    }
}
