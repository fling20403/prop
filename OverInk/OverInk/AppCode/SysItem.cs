using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OverInk.AppCode
{
    public class SysItem
    {
        private static string _logPath = "C:\\OverInkMapManualLauncherLog\\";
        private static string _SMTPServer = "172.21.140.1";
        private static string _MailFrom= "report_admin@silanic.cn";
        private static string _MailPWD = "";
        private static SmtpClient smtpClient;
        private static Mutex mailerMutex = new Mutex();
        //读写锁, 当资源处于写入模式时, 其他线程写入需要等待本次写入结束之后才能继续写入
        static ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();
        public static void writeLog(string log)
        {
            try
            {
                DateTime dt = DateTime.Now;
                if (!Directory.Exists(_logPath))
                {
                    Directory.CreateDirectory(_logPath);
                }
                string filepath = _logPath + "OverInkMap_" + dt.ToString("yyyyMMdd") + ".log";
                //FileInfo fi;
                log = dt.ToString("yyyyMMdd HH:mm:ss") + "  " + log;
                Console.WriteLine(log);
                LogWriteLock.EnterWriteLock();

                if (File.Exists(filepath))
                {
                    StreamReader sr = File.OpenText(filepath);
                    string input = sr.ReadToEnd();
                    sr.Close();
                    StreamWriter sw = new StreamWriter(filepath);
                    sw.Write(input);
                    //      sw.WriteLine();
                    sw.WriteLine(log);
                    sw.Close();
                }
                else
                {
                    FileStream fs = new FileStream(filepath, FileMode.CreateNew);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(log);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            finally
            {
                LogWriteLock.ExitWriteLock();
            }


        }
        public static string XmlSerialize<T>(T obj)
        {
            using (StringWriter sw = new StringWriter())
            {
                Type t = obj.GetType();
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                serializer.Serialize(sw, obj);
                sw.Close();
                return sw.ToString();
            }
        }

        public static void sendmail(object body2)
        {
            //创建一个互斥锁来保证线程同步
            InkModel.InkInfo ink = (InkModel.InkInfo)body2;
            string subject = "[Notice!!!!] " +ink.OverInkVersion + " Over INK MAP Mail Alarm";

            mailerMutex.WaitOne();
            MailMessage mailMessage = new MailMessage();
            try
            {
                mailMessage.From = new MailAddress(_MailFrom);
                if (!string.IsNullOrEmpty(ink.MAIL_SENDTO))
                {
                    string[] to_list = ink.MAIL_SENDTO.Split(';');
                    foreach (var to in to_list)
                    {
                        if (!string.IsNullOrWhiteSpace(to))
                        {
                            mailMessage.To.Add(new MailAddress(to));
                        }
                    }

                }

                if (!string.IsNullOrEmpty(ink.MAIL_CC))
                {
                    string[] cc_list = ink.MAIL_CC.Split(';');
                    foreach (var cc in cc_list)
                    {
                        if (!string.IsNullOrWhiteSpace(cc))
                        {
                            mailMessage.CC.Add(new MailAddress(cc));
                        }
                    }
                }          

                mailMessage.Subject = subject;
                mailMessage.Body = ink.MAIL_MSG;
                mailMessage.IsBodyHtml = false;
                mailMessage.Priority = MailPriority.Normal;
                using (smtpClient = new SmtpClient(_SMTPServer))
                {
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    if (string.IsNullOrEmpty(_MailPWD))
                    {
                        smtpClient.UseDefaultCredentials = false;
                    }
                    else
                    {
                        smtpClient.UseDefaultCredentials = true;
                        smtpClient.Credentials = new NetworkCredential(_MailFrom, _MailPWD);
                    }
                    smtpClient.Send(mailMessage);
                }
                //   Console.WriteLine("sendmail success");
                writeLog("send alarm notice mail success. " + ink.MAIL_MSG);
            }
            catch (ThreadStartException tsEx)
            {
                Console.WriteLine("Mailer.SendMessage.ThreadStartError: Send Failed to " + ink.MAIL_SENDTO + ", via " + (smtpClient == null ? "" : smtpClient.Host) + ". Stack Trace Message : " + tsEx.Message + "\r\n" + tsEx.StackTrace);
            }
            catch (ThreadAbortException taEx)
            {
                Console.WriteLine("Mailer.SendMessage.ThreadAbortError: Send Failed to " + ink.MAIL_SENDTO + ", via " + (smtpClient == null ? "" : smtpClient.Host) + ". Stack Trace Message : " + taEx.Message + "\r\n" + taEx.StackTrace);
            }
            catch (ThreadInterruptedException tiEx)
            {
                Console.WriteLine("Mailer.SendMessage.ThreadInterruptedError: Send Failed to " + ink.MAIL_SENDTO + ", via " + (smtpClient == null ? "" : smtpClient.Host) + ". Stack Trace Message : " + tiEx.Message + "\r\n" + tiEx.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mailer.SendMessage : Send Failed to " + ink.MAIL_SENDTO + ", via " + (smtpClient == null ? "" : smtpClient.Host) + ". Stack Trace Message : " + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
                mailMessage.Dispose();
                //  Release the Mutex.            
                mailerMutex.ReleaseMutex();
                Thread t = Thread.CurrentThread;
                t.Abort();
            }
        }
    }
        
}
