﻿using AIMS.APIs.IATILib.Parsers;
using AIMS.DAL.EF;
using AIMS.Models;
using AIMS.Services;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AIMS.APIs.Scheduler
{
    public class ScheduleTask : ScheduledProcessor
    {
        IConfiguration configuration;
        AIMSDbContext dbContext;
        IMapper mapper;
        public ScheduleTask(IServiceScopeFactory serviceScopeFactory, IConfiguration config, AIMSDbContext context, IMapper mapperObj) : base(serviceScopeFactory)
        {
            configuration = config;
            dbContext = context;
            mapper = mapperObj;
        }

        protected override string Schedule => "*/1 * * * *";

        public override Task ProcessInScope(IServiceProvider serviceProvider)
        {
            try
            {
                string country = configuration.GetValue<string>("IATI:Country");
                string url = "http://datastore.iatistandard.org/api/1/access/activity.xml?recipient-country=" + country;
                XmlReader xReader = XmlReader.Create(url);
                XDocument xDoc = XDocument.Load(xReader);
                var activity = (from el in xDoc.Descendants("iati-activity")
                                select el.FirstAttribute).FirstOrDefault();

                IParser parser;
                ICollection<IATIActivity> activityList = new List<IATIActivity>();
                string version = "";
                version = activity.Value;
                switch (version)
                {
                    case "1.03":
                        parser = new ParserIATIVersion13();
                        activityList = parser.ExtractAcitivities(xDoc);
                        break;

                    case "2.01":
                        parser = new ParserIATIVersion21(configuration);
                        activityList = parser.ExtractAcitivities(xDoc);
                        break;
                }

                IATIService service = new IATIService(dbContext, mapper);
                IATIModel model = new IATIModel()
                {
                    Data = JsonConvert.SerializeObject(activityList),
                };
                service.Add(model);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }
    }
}
