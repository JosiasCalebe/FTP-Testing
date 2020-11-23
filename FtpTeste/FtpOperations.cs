using FluentFTP;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using Dapper;
using System.Data.SqlClient;
using System.Linq;

namespace FtpTeste
{
    class FtpOperations
    {
        private static readonly IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json");
        private static readonly IConfigurationRoot config = builder.Build();

        private readonly string diretorioLocal = config.GetSection("Parametros:DiretorioLocal").Value;
        private readonly string extensao = config.GetSection("Parametros:Extensao").Value;

        public async void Consultar(string query)
        {
            string orderId = "";

            using (var db = new SqlConnection(config.GetSection("Parametros:ConnectionString").Value))
            {
                await db.OpenAsync();
                var result = await db.QueryAsync<dynamic>(query);
                var results = result.ToList();

                var file = new StringBuilder();
                foreach (var i in results)
                {
                    string newLine = "";
                    bool first = true;
                    foreach (var a in i)
                    {
                        newLine += (first) ? $"{a.Value}" : $"{config.GetSection("Parametros:Delimitador").Value}{a.Value}";
                        if (first && a.Key == "orderId")
                        {
                            orderId = a.Value;
                            first = false;
                        }
                    }
                    Console.WriteLine(newLine);
                    file.AppendLine(newLine);
                }
                File.WriteAllText($"{diretorioLocal}\\{orderId}{extensao}", file.ToString());
                Enviar(orderId);
            }
        }
        public async void Enviar(string orderId)
        {
            string fileName = $"{orderId}{extensao}";
            string route = $"{diretorioLocal}\\{fileName}";
            try
            {
                FtpClient client = new FtpClient(config.GetSection("Parametros:FtpUrl").Value);
                client.Credentials = new NetworkCredential(config.GetSection("Parametros:FtpUser").Value, config.GetSection("Parametros:FtpPwd").Value);
                await client.ConnectAsync();
                await client.UploadFileAsync(route, $"{config.GetSection("Parametros:FtpDiretorio").Value}{fileName}");
                if (bool.Parse(config.GetSection("Parametros:Email:EnviarEmail").Value)) SendEmail(orderId);
                string routeProcessed = route.Replace("Local", "Processed");
                if (File.Exists(routeProcessed))
                {
                    File.WriteAllText(routeProcessed, routeProcessed);
                    File.Delete(route);
                }
                else File.Move(route, routeProcessed);

            }
            catch (Exception)
            {
                string routeLog = route.Replace("Local", "Log");
                if (File.Exists(routeLog))
                {
                    File.WriteAllText(routeLog, routeLog);
                    File.Delete(route);
                }
                else File.Move(route, routeLog);
            }
        }

        private void SendEmail(string orderId)
        {
            string arquivo = $"{diretorioLocal}\\{orderId}{extensao}";
            string emailTo = config.GetSection("Parametros:Email:EmailTo").Value;
            string emailCredentials = config.GetSection("Parametros:Email:EmailFrom").Value;
            string senhaCredentials = config.GetSection("Parametros:Email:Senha").Value;
            string smtpClient = config.GetSection("Parametros:Email:SmtpClient").Value;
            int smtpPort = int.Parse(config.GetSection("Parametros:Email:SmtpPort").Value);

            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient(smtpClient);
            mail.From = new MailAddress(emailCredentials);
            mail.To.Add(emailTo);
            mail.Subject = $"pedido: {orderId}";
            mail.Body = $"Arquivo {extensao} com o pedido: {orderId}";
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(arquivo);
            mail.Attachments.Add(attachment);
            SmtpServer.Port = smtpPort;
            SmtpServer.Credentials = new System.Net.NetworkCredential(emailCredentials, senhaCredentials);
            SmtpServer.EnableSsl = true;
            SmtpServer.Send(mail);
            Console.WriteLine($"Email enviado para {emailTo}");
        }
    }
}
