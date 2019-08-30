using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailKit_Core
{
    public class MailRepository
    {
        private readonly string hostImap, hostSmtp;
        public string login, password;
        private readonly int portImap, portSmtp;
        private readonly bool sslImap, sslSmtp;

        private ImapClient imapClient;
        private SmtpClient smtpClient;

        public class MailRepositoryResult<T>
        {
            public MailRepositoryResult()
            {
                this.IsErr = false;
                this.Obj = new List<T>();
                this.TotalCount = 0;
                this.RecentCount = 0;
            }

            public bool IsErr { get; set; }
            public List<T> Obj { get; set; }

            public int StartIndex { get; set; }
            public int LastIndex { get; set; }

            public int DataCount { get; set; }

            public int TotalCount { get; set; }
            public int RecentCount { get; set; }
        }
        public class RepositoryMessage
        {
            public bool Read { get; set; }
            public bool Flagged { get; set; }
            public MimeMessage MimeMessage { get; set; }
        }

        public class EnvelopeEmail
        {
            public UniqueId Uid { get; set; }
            public string FromDisplayName { get; set; }
            public string FromEmail { get; set; }
            public string To { get; set; }
            public string Subject { get; set; }
            public string PreviewText { get; set; }
            public DateTime TimeReceived { get; set; }
            public MessageFlags? MessageFlags { get; set; }
            public bool HasAttachment { get; set; }
        }

        public MailRepository(string hostImap, int portImap, bool sslImap, string hostSmtp, int portSmtp, bool sslSmtp, string login, string password)
        {
            try
            {
                this.login = login;
                this.password = password;

                this.hostImap = hostImap;
                this.portImap = portImap;
                this.sslImap = sslImap;

                this.hostSmtp = hostSmtp;
                this.portSmtp = portSmtp;
                this.sslSmtp = sslSmtp;

                this.imapClient = new ImapClient();
                this.smtpClient = new SmtpClient();

                this.imapClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                this.imapClient.Connect(this.hostImap, this.portImap, this.sslImap);
                this.imapClient.Authenticate(this.login, this.password);

                this.smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                this.smtpClient.Connect(this.hostSmtp, this.portSmtp, this.sslSmtp);
                this.smtpClient.Authenticate(this.login, this.password);

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public MailRepository()
        {
            this.imapClient = new ImapClient();
            this.smtpClient = new SmtpClient();
        }

        public MailRepositoryResult<EnvelopeEmail> SearchAdvanced(int skip = 0, int take = 50, SpecialFolder specialFolder = SpecialFolder.All)
        {
            MailRepositoryResult<EnvelopeEmail> MailRepositoryResult = new MailRepositoryResult<EnvelopeEmail>();

            try
            {
                if (specialFolder != SpecialFolder.All)
                {
                    var inbox = imapClient.GetFolder(specialFolder);
                    inbox.Open(FolderAccess.ReadOnly);

                    if (inbox.Count > 0)
                    {
                        MailRepositoryResult.TotalCount = inbox.Count;
                        MailRepositoryResult.RecentCount = inbox.Recent;
                        MailRepositoryResult.StartIndex = skip;

                        var uids = inbox.Search(SearchQuery.All).Reverse().Skip(skip).Take(take).ToList();
                        var messages = inbox.Fetch(uids, MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.PreviewText | MessageSummaryItems.UniqueId);

                        messages = messages.OrderByDescending(message => message.Envelope.Date.Value.DateTime).ToList();
                        MailRepositoryResult.DataCount = messages.Count;
                        MailRepositoryResult.LastIndex = skip + messages.Count;

                        foreach (var message in messages)
                        {
                            MailRepositoryResult.Obj.Add(new EnvelopeEmail()
                            {
                                Uid = message.UniqueId,
                                FromDisplayName = message.Envelope.From.First().Name,
                                FromEmail = message.Envelope.From.First().ToString(),
                                To = message.Envelope.To.ToString(),
                                Subject = message.NormalizedSubject,
                                PreviewText = message.PreviewText,
                                TimeReceived = message.Envelope.Date.Value.DateTime,
                                MessageFlags = message.Flags,
                                HasAttachment = message.Attachments.Count() > 0 ? true : false
                            });
                        }
                    }
                }
                else
                {
                    var inbox = imapClient.Inbox;
                    inbox.Open(FolderAccess.ReadOnly);

                    if (inbox.Count > 0)
                    {
                        MailRepositoryResult.TotalCount = inbox.Count;
                        MailRepositoryResult.RecentCount = inbox.Recent;
                        MailRepositoryResult.StartIndex = skip;

                        var uids = inbox.Search(SearchQuery.All).Reverse().Skip(skip).Take(take).ToList();
                        var messages = inbox.Fetch(uids, MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.PreviewText | MessageSummaryItems.UniqueId);

                        messages = messages.OrderByDescending(message => message.Envelope.Date.Value.DateTime).ToList();
                        MailRepositoryResult.DataCount = messages.Count;
                        MailRepositoryResult.LastIndex = skip + messages.Count;

                        foreach (var message in messages)
                        {
                            MailRepositoryResult.Obj.Add(new EnvelopeEmail()
                            {
                                Uid = message.UniqueId,
                                FromDisplayName = message.Envelope.From.First().Name,
                                FromEmail = message.Envelope.From.First().ToString(),
                                To = message.Envelope.To.ToString(),
                                Subject = message.NormalizedSubject,
                                PreviewText = message.PreviewText,
                                TimeReceived = message.Envelope.Date.Value.DateTime,
                                MessageFlags = message.Flags,
                                HasAttachment = message.Attachments.Count() > 0 ? true : false
                            });
                        }
                    }
                }

                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                MailRepositoryResult.Obj = new List<EnvelopeEmail>() { };

                return MailRepositoryResult;
            }
        }

        public bool AddFlagsAdvanced(string uniqueId, MessageFlags messageFlags)
        {
            try
            {
                UniqueId uniqueIdTrue = UniqueId.Parse(uniqueId);

                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                if (inbox.Count > 0)
                {
                    inbox.AddFlags(uniqueIdTrue, messageFlags, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool RemoveFlagsAdvanced(string uniqueId, MessageFlags messageFlags)
        {
            try
            {
                UniqueId uniqueIdTrue = UniqueId.Parse(uniqueId);

                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                if (inbox.Count > 0)
                {
                    inbox.RemoveFlags(uniqueIdTrue, messageFlags, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool MultiAddFlagsAdvanced(List<string> uniqueIds, MessageFlags messageFlags)
        {
            try
            {
                List<UniqueId> uniqueIdTrue = new List<UniqueId>();
                uniqueIds.ForEach(x =>
                {
                    uniqueIdTrue.Add(UniqueId.Parse(x));
                });

                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                if (inbox.Count > 0)
                {
                    inbox.AddFlags(uniqueIdTrue, messageFlags, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool MultiRemoveFlagsAdvanced(List<string> uniqueIds, MessageFlags messageFlags)
        {
            try
            {
                List<UniqueId> uniqueIdTrue = new List<UniqueId>();
                uniqueIds.ForEach(x =>
                {
                    uniqueIdTrue.Add(UniqueId.Parse(x));
                });

                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                if (inbox.Count > 0)
                {
                    inbox.RemoveFlags(uniqueIdTrue, messageFlags, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool MoveAdvanced(string uniqueId, SpecialFolder oldFolder, SpecialFolder specialFolder)
        {
            try
            {
                UniqueId uniqueIdTrue = UniqueId.Parse(uniqueId);

                if (oldFolder != SpecialFolder.All)
                {
                    var inbox = imapClient.GetFolder(oldFolder);

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        inbox.MoveTo(uniqueIdTrue, moveFolder);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var inbox = imapClient.Inbox;

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        inbox.MoveTo(uniqueIdTrue, moveFolder);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }


            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool MultiMoveAdvanced(List<string> uniqueIds, SpecialFolder oldFolder, SpecialFolder specialFolder)
        {
            try
            {
                List<UniqueId> uniqueIdTrue = new List<UniqueId>();
                uniqueIds.ForEach(x =>
                {
                    uniqueIdTrue.Add(UniqueId.Parse(x));
                });

                if (oldFolder != SpecialFolder.All)
                {
                    var inbox = imapClient.GetFolder(oldFolder);

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        inbox.MoveTo(uniqueIdTrue, moveFolder);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var inbox = imapClient.Inbox;

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        inbox.MoveTo(uniqueIdTrue, moveFolder);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public MimeMessage GetMessage(string uniqueId, SpecialFolder specialFolder)
        {
            try
            {
                UniqueId uniqueIdTrue = UniqueId.Parse(uniqueId);

                if (specialFolder != SpecialFolder.All)
                {
                    var inbox = imapClient.GetFolder(specialFolder);

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        return inbox.GetMessage(uniqueIdTrue);
                    }
                    else
                    {
                        return new MimeMessage() { };
                    }
                }
                else
                {
                    var inbox = imapClient.Inbox;

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        return inbox.GetMessage(uniqueIdTrue);
                    }
                    else
                    {
                        return new MimeMessage() { };
                    }
                }
            }
            catch (Exception ex)
            {
                return new MimeMessage() { };
            }
        }
        public IMessageSummary GetMessageInfo(string uniqueId, SpecialFolder specialFolder)
        {
            try
            {
                UniqueId uniqueIdTrue = UniqueId.Parse(uniqueId);

                if (specialFolder != SpecialFolder.All)
                {
                    var inbox = imapClient.GetFolder(specialFolder);

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        var messages = inbox.Fetch(new List<UniqueId>() { uniqueIdTrue }, MessageSummaryItems.Flags);
                        messages = messages.ToList();

                        if (messages != null && messages.Count > 0)
                        {
                            return messages.FirstOrDefault();
                        }
                        else
                        {
                            return new MessageSummary(0) { };
                        }
                    }
                    else
                    {
                        return new MessageSummary(0) { };
                    }
                }
                else
                {
                    var inbox = imapClient.Inbox;

                    inbox.Open(FolderAccess.ReadWrite);
                    var moveFolder = imapClient.GetFolder(specialFolder);

                    if (inbox.Count > 0 && moveFolder != null)
                    {
                        var messages = inbox.Fetch(new List<UniqueId>() { uniqueIdTrue }, MessageSummaryItems.Flags);
                        messages = messages.ToList();

                        if (messages != null && messages.Count > 0)
                        {
                            return messages.FirstOrDefault();
                        }
                        else
                        {
                            return new MessageSummary(0) { };
                        }
                    }
                    else
                    {
                        return new MessageSummary(0) { };
                    }
                }
            }
            catch (Exception ex)
            {
                return new MessageSummary(0) { };
            }
        }

        public void SendAdvanced(List<MailboxAddress> toList, List<MailboxAddress> fromList, string subject, string body, string[] cc, string[] bcc)
        {
            var message = new MimeMessage();

            try
            {
                toList.ForEach(x =>
                {
                    message.To.Add(x);
                });

                fromList.ForEach(x =>
                {
                    message.From.Add(x);
                });

                if (cc.Length > 0)
                {
                    foreach (string item in cc)
                    {
                        message.Cc.Add(new MailboxAddress(item));
                    }
                }

                if (bcc.Length > 0)
                {
                    foreach (string item in bcc)
                    {
                        message.Bcc.Add(new MailboxAddress(item));
                    }
                }

                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = body };

                this.smtpClient.Send(message);
            }
            catch (Exception ex)
            {

            }
        }

        public void SendForwardAdvanced(List<MailboxAddress> toList, List<MailboxAddress> fromList, string subject, string body, string[] cc, string[] bcc)
        {
            var message = new MimeMessage();

            try
            {
                toList.ForEach(x =>
                {
                    message.To.Add(x);
                });

                fromList.ForEach(x =>
                {
                    message.From.Add(x);
                });

                fromList.ForEach(x =>
                {
                    message.ReplyTo.Add(x);
                });

                if (cc.Length > 0)
                {
                    foreach (string item in cc)
                    {
                        message.Cc.Add(new MailboxAddress(item));
                    }
                }

                if (bcc.Length > 0)
                {
                    foreach (string item in bcc)
                    {
                        message.Bcc.Add(new MailboxAddress(item));
                    }
                }

                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = body };

                this.smtpClient.Send(message);
            }
            catch (Exception ex)
            {

            }
        }





















        public MailRepositoryResult<RepositoryMessage> Search(SearchQuery searchQuery, int take, int skip)
        {
            MailRepositoryResult<RepositoryMessage> MailRepositoryResult = new MailRepositoryResult<RepositoryMessage>();

            try
            {
                // The Inbox folder is always available on all IMAP servers...
                var inbox = imapClient.Inbox;
                inbox.Open(FolderAccess.ReadOnly);

                MailRepositoryResult.TotalCount = inbox.Count;
                MailRepositoryResult.RecentCount = inbox.Recent;

                IList<UniqueId> UniqueIds = inbox.Search(searchQuery).Reverse().Skip(skip).Take(take).ToList();

                foreach (UniqueId item in UniqueIds)
                {
                    var messageInfo = inbox.Fetch(new[] { item }, MessageSummaryItems.Flags | MessageSummaryItems.GMailLabels);
                    MailRepositoryResult.Obj.Add(new RepositoryMessage() { MimeMessage = inbox.GetMessage(item), Read = messageInfo[0].Flags.Value.HasFlag(MessageFlags.Seen) });
                }

                MailRepositoryResult.IsErr = false;
                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                return MailRepositoryResult;
            }
        }
        public MailRepositoryResult<RepositoryMessage> Search(SearchQuery searchQuery, int take, int skip, SpecialFolder specialFolder)
        {
            MailRepositoryResult<RepositoryMessage> MailRepositoryResult = new MailRepositoryResult<RepositoryMessage>();

            try
            {
                IMailFolder inbox;

                if (specialFolder == SpecialFolder.All)
                {
                    inbox = imapClient.Inbox;
                }
                else
                {
                    inbox = imapClient.GetFolder(specialFolder);
                }

                inbox.Open(FolderAccess.ReadOnly);

                MailRepositoryResult.TotalCount = inbox.Count;
                MailRepositoryResult.RecentCount = inbox.Recent;

                IList<UniqueId> UniqueIds = inbox.Search(searchQuery).Reverse().Skip(skip).Take(take).ToList();
                var orderBy = new[] { OrderBy.Date };
                //IList<UniqueId> UniqueIds = inbox.Sort(searchQuery, orderBy);

                foreach (UniqueId item in UniqueIds)
                {
                    var messageInfo = inbox.Fetch(new[] { item }, MessageSummaryItems.Flags | MessageSummaryItems.GMailLabels);
                    MailRepositoryResult.Obj.Add(new RepositoryMessage()
                    {
                        MimeMessage = inbox.GetMessage(item),
                        Read = messageInfo[0].Flags.Value.HasFlag(MessageFlags.Seen),
                        Flagged = messageInfo[0].Flags.Value.HasFlag(MessageFlags.Flagged)
                    });
                }

                //foreach (var summary in inbox.Fetch(0, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId))
                //{
                //    Console.WriteLine("[summary] {0:D2}: {1}", summary.Index, summary.Envelope.Subject);
                //}

                /*
                for (int i = 0; i < 10; i++)
                {
                    MailRepositoryResult.Obj.Add(new RepositoryMessage() { mimeMessage = new MimeMessage(), Read = true, Flagged = true });
                }*/

                MailRepositoryResult.IsErr = false;
                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                return MailRepositoryResult;
            }
        }

        public MailRepositoryResult<MimeMessage> Move(string Id, SpecialFolder oldFolder, SpecialFolder specialFolder)
        {
            MailRepositoryResult<MimeMessage> MailRepositoryResult = new MailRepositoryResult<MimeMessage>();

            try
            {
                // The Inbox folder is always available on all IMAP servers...
                var inbox = imapClient.Inbox;

                if (oldFolder != SpecialFolder.All)
                {
                    inbox = imapClient.GetFolder(oldFolder);
                }

                if (inbox != null)
                {
                    inbox.Open(FolderAccess.ReadWrite);

                    var uid = inbox.Search(SearchQuery.HeaderContains("Message-Id", Id));

                    MailRepositoryResult.TotalCount = inbox.Count;
                    MailRepositoryResult.RecentCount = inbox.Recent;

                    var moveFolder = imapClient.GetFolder(specialFolder);
                    if (moveFolder != null)
                    {
                        inbox.MoveTo(uid, moveFolder);
                    }

                    MailRepositoryResult.IsErr = false;
                }
                else
                {
                    MailRepositoryResult.IsErr = true;
                }

                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                return MailRepositoryResult;
            }
        }

        public MailRepositoryResult<MimeMessage> SetFlagged(string Id, MessageFlags messageFlags, SpecialFolder specialFolder)
        {
            MailRepositoryResult<MimeMessage> MailRepositoryResult = new MailRepositoryResult<MimeMessage>();

            try
            {
                // The Inbox folder is always available on all IMAP servers...
                var inbox = imapClient.Inbox;

                if (specialFolder != SpecialFolder.All)
                {
                    inbox = imapClient.GetFolder(specialFolder);
                }

                if (inbox != null)
                {
                    inbox.Open(FolderAccess.ReadWrite);

                    var uid = inbox.Search(SearchQuery.HeaderContains("Message-Id", Id));

                    MailRepositoryResult.TotalCount = inbox.Count;
                    MailRepositoryResult.RecentCount = inbox.Recent;

                    var moveFolder = imapClient.GetFolder(specialFolder);
                    if (moveFolder != null)
                    {
                        inbox.AddFlags(uid, messageFlags, silent: false);
                    }

                    MailRepositoryResult.IsErr = false;
                }
                else
                {
                    MailRepositoryResult.IsErr = true;
                }

                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                return MailRepositoryResult;
            }
        }
        public MailRepositoryResult<MimeMessage> UnSetFlagged(string Id, MessageFlags messageFlags, SpecialFolder specialFolder)
        {
            MailRepositoryResult<MimeMessage> MailRepositoryResult = new MailRepositoryResult<MimeMessage>();

            try
            {
                // The Inbox folder is always available on all IMAP servers...
                var inbox = imapClient.Inbox;

                if (specialFolder != SpecialFolder.All)
                {
                    inbox = imapClient.GetFolder(specialFolder);
                }

                if (inbox != null)
                {
                    inbox.Open(FolderAccess.ReadWrite);

                    var uid = inbox.Search(SearchQuery.HeaderContains("Message-Id", Id));

                    MailRepositoryResult.TotalCount = inbox.Count;
                    MailRepositoryResult.RecentCount = inbox.Recent;

                    var moveFolder = imapClient.GetFolder(specialFolder);
                    if (moveFolder != null)
                    {
                        inbox.RemoveFlags(uid, messageFlags, silent: false);
                    }

                    MailRepositoryResult.IsErr = false;
                }
                else
                {
                    MailRepositoryResult.IsErr = true;
                }

                return MailRepositoryResult;
            }
            catch (Exception ex)
            {
                MailRepositoryResult.IsErr = true;
                return MailRepositoryResult;
            }
        }

        public void Send(List<MailboxAddress> toList, List<MailboxAddress> fromList, string subject, string body, string[] cc, string[] bcc)
        {
            var message = new MimeMessage();

            try
            {
                toList.ForEach(x =>
                {
                    message.To.Add(x);
                });

                fromList.ForEach(x =>
                {
                    message.From.Add(x);
                });

                if (cc.Length > 0)
                {
                    foreach (string item in cc)
                    {
                        message.Cc.Add(new MailboxAddress(item));
                    }
                }

                if (bcc.Length > 0)
                {
                    foreach (string item in bcc)
                    {
                        message.Bcc.Add(new MailboxAddress(item));
                    }
                }

                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = body };

                this.smtpClient.Send(message);
            }
            catch (Exception ex)
            {

            }
        }
        public bool InformationCheck(string hostImapC, int portImapC, bool sslImapC, string hostSmtpC, int portSmtpC, bool sslSmtpC, string loginC, string passwordC)
        {
            try
            {
                imapClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                imapClient.Connect(hostImapC, portImapC, sslImapC);
                imapClient.Authenticate(loginC, passwordC);

                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                smtpClient.Connect(hostSmtpC, portSmtpC, sslSmtpC);
                smtpClient.Authenticate(loginC, passwordC);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
