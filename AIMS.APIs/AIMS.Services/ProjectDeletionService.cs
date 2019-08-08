﻿using AIMS.DAL.EF;
using AIMS.DAL.UnitOfWork;
using AIMS.Models;
using AIMS.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIMS.Services
{
    public interface IProjectDeletionService
    {
        /// <summary>
        /// Adds new request for project deletion
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        ActionResponse AddRequest(ProjectDeletionRequestModel model);

        /// <summary>
        /// Gets list of project deletion requests
        /// </summary>
        /// <returns></returns>
        ICollection<ProjectDeletionRequestView> GetDeletionRequests();
    }

    public class ProjectDeletionService
    {
        AIMSDbContext context;

        public ProjectDeletionService(AIMSDbContext cntxt)
        {
            context = cntxt;
        }

        public ICollection<ProjectDeletionRequestView> GetDeletionRequests()
        {
            using (var unitWork = new UnitOfWork(context))
            {
                List<ProjectDeletionRequestView> requests = new List<ProjectDeletionRequestView>();
                var projectRequests = unitWork.ProjectDeletionRepository.GetWithInclude(p => p.Status == ProjectDeletionStatus.Requested, new string[] { "User", "Project", "User.Organization" });
                return requests;
            }
        }

        public ActionResponse AddRequest(ProjectDeletionRequestModel model)
        {
            using(var unitWork = new UnitOfWork(context))
            {
                ActionResponse response = new ActionResponse();
                IMessageHelper mHelper;
                var project = unitWork.ProjectRepository.GetByID(model.ProjectId);
                if (project == null)
                {
                    mHelper = new MessageHelper();
                    response.Success = false;
                    response.Message = mHelper.GetNotFound("Project");
                    return response;
                }

                var user = unitWork.UserRepository.GetOne(u => u.Email == model.UserEmail);
                if (user == null)
                {
                    mHelper = new MessageHelper();
                    response.Success = false;
                    response.Message = mHelper.GetNotFound("User");
                    return response;
                }

                var isRequestExists = unitWork.ProjectDeletionRepository.GetOne(p => p.ProjectId == project.Id && p.Status == ProjectDeletionStatus.Requested);
                if (isRequestExists != null)
                {
                    mHelper = new MessageHelper();
                    response.Success = false;
                    response.Message = mHelper.GetProjectDeletionExistsMessage();
                    return response;
                }

                unitWork.ProjectDeletionRepository.Insert(new EFProjectDeletionRequests()
                {
                    Project = project,
                    RequestedBy = user,
                    RequestedOn = DateTime.Now,
                    StatusUpdatedOn = DateTime.Now,
                    Status = ProjectDeletionStatus.Requested
                });
                unitWork.Save();

                var adminEmails = unitWork.UserRepository.GetProjection(u => u.UserType == UserTypes.Manager, u => u.Email);
                List<EmailAddress> usersEmailList = new List<EmailAddress>();
                foreach(var email in adminEmails)
                {
                    usersEmailList.Add(new EmailAddress() { Email = email });
                }

                if (usersEmailList.Count > 0)
                {
                    ISMTPSettingsService smtpService = new SMTPSettingsService(context);
                    var smtpSettings = smtpService.GetPrivate();
                    SMTPSettingsModel smtpSettingsModel = new SMTPSettingsModel();
                    if (smtpSettings != null)
                    {
                        smtpSettingsModel.Host = smtpSettings.Host;
                        smtpSettingsModel.Port = smtpSettings.Port;
                        smtpSettingsModel.Username = smtpSettings.Username;
                        smtpSettingsModel.Password = smtpSettings.Password;
                        smtpSettingsModel.AdminEmail = smtpSettings.AdminEmail;
                    }

                    string message = "", subject = "", footerMessage = "";
                    var emailMessage = unitWork.EmailMessagesRepository.GetOne(m => m.MessageType == EmailMessageType.ProjectDeletionRequest);
                    if (emailMessage != null)
                    {
                        subject = emailMessage.Subject;
                        message = emailMessage.Message;
                        footerMessage = emailMessage.FooterMessage;
                    }
                    IEmailHelper emailHelper = new EmailHelper(smtpSettingsModel.AdminEmail, smtpSettingsModel);
                    emailHelper.SendEmailToUsers(usersEmailList, subject, subject, message, footerMessage);
                }
                return response;
            }
        }
    }
}
