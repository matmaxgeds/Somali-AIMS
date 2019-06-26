﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIMS.Models;
using AIMS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        IContactService service;
        IEmailService emailService;

        public ContactController(IContactService contactService, IEmailService eService)
        {
            service = contactService;
            emailService = eService;
        }

        [HttpPost]
        public IActionResult Post([FromBody] ContactEmailRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Data is not in valid format");
            }

            List<EmailAddress> usersEmails = null;
            if (model.EmailType == ContactEmailType.Help)
            {
                usersEmails = service.GetManagerUsersEmails().ToList();
            }
            else if(model.EmailType == ContactEmailType.Information)
            {
                if (model.ProjectId <= 0)
                {
                    return BadRequest("Invalid project id provided");
                }
                int projectId = (int)model.ProjectId;
                usersEmails = service.GetProjectUsersEmails(projectId).ToList();
            }

            ActionResponse response = null;
            if (usersEmails.Count > 0)
            {
                response = emailService.SendContactEmail(model);
                if (!response.Success)
                {
                    return BadRequest(response.Message);
                }
            }
            return Ok(true);
        }
    }
}