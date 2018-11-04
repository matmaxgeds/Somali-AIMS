﻿using AIMS.DAL.EF;
using AIMS.DAL.UnitOfWork;
using AIMS.Models;
using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIMS.Services
{
    public interface IIATIService
    {
        /// <summary>
        /// Adds IATIData for the specified date
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        ActionResponse Add(IATIModel model);


        /// <summary>
        /// Gets all the activities
        /// </summary>
        /// <returns></returns>
        ICollection<IATIActivity> GetAll();

        /// <summary>
        /// Gets all the organizations
        /// </summary>
        /// <returns></returns>
        ICollection<Organization> GetOrganizations();

        /// <summary>
        /// Deletes all the data less than the specified date
        /// </summary>
        /// <param name="datedLessThan"></param>
        /// <returns></returns>
        ActionResponse Delete(DateTime datedLessThan);
    }

    public class IATIService : IIATIService
    {
        AIMSDbContext context;
        IMapper mapper;

        public IATIService(AIMSDbContext cntxt, IMapper autoMapper)
        {
            this.context = cntxt;
            this.mapper = autoMapper;
        }

        public ICollection<IATIActivity> GetAll()
        {
            using (var unitWork = new UnitOfWork(context))
            {
                List<IATIActivity> activityList = new List<IATIActivity>();
                var iatiData = unitWork.IATIDataRepository.GetFirst(d => d.Dated.Date == d.Dated.Date);
                if (iatiData != null)
                {
                    if (iatiData.Data.Length > 0)
                    {
                        activityList = JsonConvert.DeserializeObject<List<IATIActivity>>(iatiData.Data);
                    }
                }
                return activityList;
            }
        }

        public ICollection<Organization> GetOrganizations()
        {
            using (var unitWork = new UnitOfWork(context))
            {
                List<Organization> organizations = new List<Organization>();
                return organizations;
            }
        }

        public ActionResponse Add(IATIModel model)
        {
            using (var unitWork = new UnitOfWork(context))
            {
                ActionResponse response = new ActionResponse();
                unitWork.IATIDataRepository.Insert(new EFIATIData()
                {
                    Data = model.Data,
                    Dated = DateTime.Now
                });
                unitWork.Save();
                return response;
            }
        }

        public ActionResponse Delete(DateTime dated)
        {
            using (var unitWork = new UnitOfWork(context))
            {
                ActionResponse response = new ActionResponse();
                var iatiData = unitWork.IATIDataRepository.GetMany(d => d.Dated.Date < dated.Date);
                foreach(var data in iatiData)
                {
                    unitWork.IATIDataRepository.Delete(data);
                }
                unitWork.Save();
                return response;
            }
        }
    }
}
