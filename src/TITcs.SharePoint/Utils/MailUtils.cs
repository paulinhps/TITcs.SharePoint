﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using TITcs.SharePoint.Log;

namespace TITcs.SharePoint.Utils
{
    public static class MailUtils
    {
        #region Send
        public static bool Send(string to, string subject, string body)
        {
            return Send(null, to, subject, body, new List<Attachment>(), null);
        }

        public static bool Send(string to, string subject, string body, IList<Attachment> attachments)
        {
            return Send(null, to, subject, body, attachments, null);
        }

        public static bool Send(string from, string to, string subject, string body, IList<Attachment> attachments, NetworkCredential networkCredential)
        {
            string stmpServer = SPAdministrationWebApplication.Local.OutboundMailServiceInstance.Server.Address;
            string stmpFrom = SPAdministrationWebApplication.Local.OutboundMailSenderAddress;

            if (string.IsNullOrEmpty(from))
                from = stmpFrom;

            try
            {
                MailMessage mailMessage = new MailMessage(from, to)
                {
                    IsBodyHtml = true,
                    Subject = subject,
                    Body = body
                };

                Logger.Information("MailUtils.Send.Attachments", "Count: {0}", attachments.Count);

                foreach (var attachment in attachments)
                {
                    Logger.Information("MailUtils.Send.Attachments", "Name: {0}", attachment.Name);

                    mailMessage.Attachments.Add(attachment);
                }

                SmtpClient smtpClient = new SmtpClient(stmpServer)
                {
                    UseDefaultCredentials = true
                };

                if (networkCredential != null)
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = networkCredential;
                }

                smtpClient.SendCompleted += (sender, e) =>
                {
                    Logger.Information("MailUtils.Send.SendCompleted");

                    if (e.Error != null)
                    {
                        Logger.Unexpected("MailUtils.Send.SendCompleted.Error", e.Error.Message);
                    }
                };

                smtpClient.Send(mailMessage);

                return true;
            }
            catch (Exception e)
            {
                Logger.Information("MailUtils.Send.Error", e.Message);
                return false;
            }
        }

        #endregion Send

        #region GetAttachment

        public static Attachment GetAttachment(SPWeb web, string relativePath)
        {
            if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            var file = web.Url + relativePath;

            Logger.Information("MailUtils.GetAttachment", "File: {0}", file);

            var spFile = web.GetFile(file);

            var attach = new Attachment(spFile.OpenBinaryStream(), spFile.Name)
            {
                ContentId = Guid.NewGuid().ToString()
            };

            attach.ContentType.MediaType = "image/" + Path.GetExtension(spFile.Url).Replace(".", "");
            attach.ContentDisposition.Inline = true;
            attach.ContentDisposition.DispositionType = DispositionTypeNames.Inline;

            return attach;
        }

        /// <summary>
        /// </summary>
        /// <param name="fullPath">string</param>
        /// <sample>GetAttachment("http://mysite.com/images/image.jpg")</sample>
        /// <returns>Attachment</returns>
        public static Attachment GetAttachment(string fullPath)
        {
            var site = SPContext.Current.Site;

            using (var web = site.OpenWeb())
            {
                Logger.Information("MailUtils.GetAttachment", "File: {0}", fullPath);

                var spFile = web.GetFile(fullPath);

                var attach = new Attachment(spFile.OpenBinaryStream(), spFile.Name)
                {
                    ContentId = Guid.NewGuid().ToString()
                };

                attach.ContentType.MediaType = "image/" + Path.GetExtension(spFile.Url).Replace(".", "");
                attach.ContentDisposition.Inline = true;
                attach.ContentDisposition.DispositionType = DispositionTypeNames.Inline;

                return attach;
            }
        }

        #endregion GetAttachment
    }
}
