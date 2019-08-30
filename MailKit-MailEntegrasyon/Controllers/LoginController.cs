using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MailKit;
using MailKit.Search;
using MailKit_Core;
using MailKit_MailEntegrasyon.Models;
using static MailKit_Core.MailRepository;

namespace MailKit_MailEntegrasyon.Controllers
{
    public class LoginController : Controller
    { 
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult MailLogin(string Email, string EmailPassword)
        {
            try
            {
                if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(EmailPassword))
                {
                    MailAccount mailAccount = new MailAccount();
                    mailAccount.Email = Email;
                    mailAccount.Password = EmailPassword;
                    var MailType = Email.Split('@')[1];
                    if (MailType.Contains("gmail"))
                    {
                        mailAccount.ImapSettings.Host = "imap.gmail.com";
                        mailAccount.ImapSettings.Port = 993;
                        mailAccount.SmtpSettings.Host = "smtp.gmail.com";
                        mailAccount.SmtpSettings.Port = 465;
                    }
                    else if (MailType.Contains("outlook"))
                    {
                        mailAccount.ImapSettings.Host = "imap-mail.outlook.com";
                        mailAccount.ImapSettings.Port = 993;
                        mailAccount.SmtpSettings.Host = "smtp-mail.outlook.com";
                        mailAccount.SmtpSettings.Port = 597;
                    }
                    else if (MailType.Contains("yahoo"))
                    {
                        mailAccount.ImapSettings.Host = "imap.mail.yahoo.com";
                        mailAccount.ImapSettings.Port = 993;
                        mailAccount.SmtpSettings.Host = "smtp.mail.yahoo.com";
                        mailAccount.SmtpSettings.Port = 465;
                    }
                    else
                    {
                        mailAccount.ImapSettings.Host = "imap.yandex.com";
                        mailAccount.ImapSettings.Port = 993;
                        mailAccount.SmtpSettings.Host = "smtp.yandex.com";
                        mailAccount.SmtpSettings.Port = 465;
                    }
                    
                    MailRepository mailRepository = new MailRepository(mailAccount.ImapSettings.Host, mailAccount.ImapSettings.Port, mailAccount.ImapSettings.Ssl, mailAccount.SmtpSettings.Host, mailAccount.SmtpSettings.Port, mailAccount.SmtpSettings.Ssl, mailAccount.Email, mailAccount.Password);
                    Session["MailRepository"] = mailRepository;
                    return Json(true, JsonRequestBehavior.AllowGet);                    
                }
                else
                {
                    return Json(false, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult MailList(int skip = 0, int take = 50, SpecialFolder specialFolder = SpecialFolder.All)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                MailRepositoryResult<EnvelopeEmail> response = mailRepository.SearchAdvanced(skip, take, specialFolder);
                return View(response.Obj);
            }
            catch (Exception)
            {
                return View("null");
            }       
        }

        public ActionResult MailDetails(string mailId)
        {
            try
            {
                if (!string.IsNullOrEmpty(mailId))
                {
                    MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                    return View(mailRepository.GetMessage(mailId, SpecialFolder.All));
                }
                else
                {
                    return View("null");
                }
            }
            catch (Exception)
            {
                return View("null");
            }
        }

        [ValidateInput(false)]
        public ActionResult MailSend(List<string> Receiver, string Subject, string MailContent, List<string> cc, List<string> bcc)
        {
            try
            {
                List<MimeKit.MailboxAddress> receiverList = new List<MimeKit.MailboxAddress>();
                Receiver.ForEach(x =>
                {
                    receiverList.Add(new MimeKit.MailboxAddress(x));
                });

                if (cc == null)
                {
                    cc = new List<string>();
                }

                if (bcc == null)
                {
                    bcc = new List<string>();
                }

                MailRepository mailRepository = Session["MailRepository"] as MailRepository;

                mailRepository.SendAdvanced(
                    receiverList,
                    new List<MimeKit.MailboxAddress>() { new MimeKit.MailboxAddress(mailRepository.login) },
                    Subject,
                    MailContent,
                    cc.ToArray(),
                    bcc.ToArray());

                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult MailMove(string uniqueId, SpecialFolder rightNowFolder, SpecialFolder specialFolder)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                return Json(mailRepository.MoveAdvanced(uniqueId, rightNowFolder, specialFolder), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult MailMultiRemoveFlags(List<string> uniqueId, MessageFlags messageFlags)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                return Json(mailRepository.MultiRemoveFlagsAdvanced(uniqueId, messageFlags), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult MailModifyAdvanced(string uniqueId, SpecialFolder specialFolder)
        {
            MailRepository mailRepository = Session["MailRepository"] as MailRepository;
            IMessageSummary getMessageInfo = mailRepository.GetMessageInfo(uniqueId, specialFolder);
            ViewBag.getMessageInfo = getMessageInfo;

            if (!getMessageInfo.Flags.Value.HasFlag(MessageFlags.Seen))
            {
                MailAddFlags(uniqueId, MessageFlags.Seen);
            }

            return View("~/Views/ControlPanel/MailAdvanced/MailModifyAdvanced.cshtml", mailRepository.GetMessage(uniqueId, specialFolder));
        }

        [HttpPost]
        public JsonResult MailAddFlags(string uniqueId, MessageFlags messageFlags)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                return Json(mailRepository.AddFlagsAdvanced(uniqueId, messageFlags), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult MailRemoveFlags(string uniqueId, MessageFlags messageFlags)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                return Json(mailRepository.RemoveFlagsAdvanced(uniqueId, messageFlags), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult MailMultiAddFlags(List<string> uniqueId, MessageFlags messageFlags)
        {
            try
            {
                MailRepository mailRepository = Session["MailRepository"] as MailRepository;
                return Json(mailRepository.MultiAddFlagsAdvanced(uniqueId, messageFlags), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
    }
}